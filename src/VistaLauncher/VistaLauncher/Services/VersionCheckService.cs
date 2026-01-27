using System.Diagnostics;
using System.Net.Http.Headers;
using VistaLauncher.Models;
using VistaLauncher.Services.Updates;

namespace VistaLauncher.Services;

/// <summary>
/// 版本检查服务实现
/// </summary>
public sealed class VersionCheckService : IVersionCheckService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<UpdateSource, IVersionProvider> _providers;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// 创建版本检查服务
    /// </summary>
    /// <param name="httpClient">HTTP 客户端（可选，不提供则创建默认实例）</param>
    public VersionCheckService(HttpClient? httpClient = null)
    {
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? CreateDefaultHttpClient();

        _providers = new Dictionary<UpdateSource, IVersionProvider>
        {
            [UpdateSource.NirSoft] = new NirSoftVersionProvider(_httpClient),
            [UpdateSource.GitHub] = new GitHubVersionProvider(_httpClient),
            [UpdateSource.Custom] = new HtmlVersionProvider(_httpClient)
        };
    }

    /// <summary>
    /// 创建默认的 HTTP 客户端
    /// </summary>
    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        return client;
    }

    public async Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (tool.SkipUpdateCheck || tool.UpdateSource == UpdateSource.None)
            return null;

        if (!_providers.TryGetValue(tool.UpdateSource, out var provider))
        {
            Debug.WriteLine($"No provider for update source: {tool.UpdateSource}");
            return null;
        }

        try
        {
            var info = await provider.GetUpdateInfoAsync(tool, cancellationToken);
            if (info == null)
            {
                return new VersionCheckResult
                {
                    ToolId = tool.Id,
                    ToolName = tool.Name,
                    CurrentVersion = tool.Version,
                    CheckFailed = true,
                    ErrorMessage = "无法获取版本信息"
                };
            }

            tool.UpdateInfo = info;
            tool.LastUpdateCheck = DateTime.Now;

            return new VersionCheckResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                CurrentVersion = tool.Version,
                LatestVersion = info.Version,
                DownloadUrl = info.DownloadUrl,
                ReleaseNotes = info.ReleaseNotes,
                ReleaseDate = info.ReleaseDate,
                FileSize = info.FileSize
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Version check failed for {tool.Name}: {ex.Message}");
            return new VersionCheckResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                CurrentVersion = tool.Version,
                CheckFailed = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<CheckProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VersionCheckResult>();
        var toolList = tools
            .Where(t => t.UpdateSource != UpdateSource.None && !t.SkipUpdateCheck)
            .ToList();
        var total = toolList.Count;
        var completed = 0;
        var updateFound = 0;

        foreach (var tool in toolList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new CheckProgress
            {
                Completed = completed,
                Total = total,
                UpdateFound = updateFound,
                CurrentTool = tool.Name
            });

            var result = await CheckVersionAsync(tool, cancellationToken);
            if (result != null)
            {
                results.Add(result);
                if (result.HasUpdate)
                    updateFound++;
            }

            completed++;

            if (completed < total)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        progress?.Report(new CheckProgress
        {
            Completed = completed,
            Total = total,
            UpdateFound = updateFound,
            CurrentTool = null
        });

        return results;
    }

    public void OpenDownloadPage(ToolItem tool)
    {
        var url = tool.UpdateInfo?.DownloadUrl ?? tool.UpdateInfo?.InfoUrl ?? tool.HomepageUrl;
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    public int CompareVersions(string v1, string v2)
    {
        return VersionComparer.Compare(v1, v2);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
