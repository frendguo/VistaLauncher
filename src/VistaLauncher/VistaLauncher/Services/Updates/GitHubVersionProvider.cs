using System.Text.Json;
using System.Text.RegularExpressions;
using VistaLauncher.Models;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// GitHub Release 版本提供者
/// </summary>
public sealed partial class GitHubVersionProvider : IVersionProvider
{
    private readonly HttpClient _httpClient;

    public GitHubVersionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool Supports(ToolItem tool)
    {
        return tool.UpdateSource == UpdateSource.GitHub ||
               (!string.IsNullOrEmpty(tool.HomepageUrl) &&
                tool.HomepageUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return null;

        try
        {
            var match = GitHubUrlRegex().Match(tool.HomepageUrl);
            if (!match.Success)
                return null;

            var owner = match.Groups[1].Value;
            var repo = match.Groups[2].Value;
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Accept.ParseAdd("application/vnd.github.v3+json");
            request.Headers.UserAgent.ParseAdd("VistaLauncher/1.0");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"GitHub API returned {response.StatusCode} for {owner}/{repo}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var version = tagName?.TrimStart('v', 'V');

            if (string.IsNullOrEmpty(version))
                return null;

            var releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;

            DateTime? releaseDate = null;
            if (root.TryGetProperty("published_at", out var publishedProp))
            {
                if (DateTime.TryParse(publishedProp.GetString(), out var date))
                    releaseDate = date;
            }

            string? downloadUrl = null;
            long? fileSize = null;

            if (root.TryGetProperty("assets", out var assetsProp))
            {
                (downloadUrl, fileSize) = FindBestAsset(assetsProp, tool.Architecture);
            }

            return new UpdateInfo
            {
                Version = version,
                DownloadUrl = downloadUrl,
                InfoUrl = $"https://github.com/{owner}/{repo}/releases/latest",
                ReleaseNotes = releaseNotes,
                ReleaseDate = releaseDate,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GitHub version check failed for {tool.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从资产列表中查找最佳下载资源
    /// </summary>
    private static (string? url, long? size) FindBestAsset(JsonElement assets, Architecture architecture)
    {
        string? downloadUrl = null;
        long? fileSize = null;

        // 首选匹配架构的资源
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (string.IsNullOrEmpty(name))
                continue;

            if (IsPreferredAsset(name, architecture))
            {
                downloadUrl = asset.TryGetProperty("browser_download_url", out var urlProp)
                    ? urlProp.GetString()
                    : null;
                fileSize = asset.TryGetProperty("size", out var sizeProp)
                    ? sizeProp.GetInt64()
                    : null;
                break;
            }
        }

        // 回退到第一个 zip 或 exe
        if (downloadUrl == null)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.TryGetProperty("browser_download_url", out var urlProp)
                        ? urlProp.GetString()
                        : null;
                    fileSize = asset.TryGetProperty("size", out var sizeProp)
                        ? sizeProp.GetInt64()
                        : null;
                    break;
                }
            }
        }

        return (downloadUrl, fileSize);
    }

    /// <summary>
    /// 判断是否为首选的下载资源
    /// </summary>
    private static bool IsPreferredAsset(string name, Architecture architecture)
    {
        var lowerName = name.ToLowerInvariant();

        // 排除 Linux/Mac 版本
        if (lowerName.Contains("linux") || lowerName.Contains("darwin") ||
            lowerName.Contains("macos") || lowerName.Contains("mac"))
            return false;

        // 检查是否为 Windows 版本
        var isWindows = lowerName.Contains("win") || lowerName.Contains("windows") ||
                       lowerName.EndsWith(".exe") || lowerName.EndsWith(".msi");

        if (!isWindows && !lowerName.EndsWith(".zip"))
            return false;

        // 检查架构匹配
        if (architecture == Architecture.x64)
        {
            return lowerName.Contains("x64") || lowerName.Contains("amd64") ||
                   lowerName.Contains("64bit") || lowerName.Contains("win64");
        }
        else
        {
            return lowerName.Contains("x86") || lowerName.Contains("32bit") ||
                   lowerName.Contains("win32") || lowerName.Contains("i386");
        }
    }

    [GeneratedRegex(@"github\.com/([^/]+)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubUrlRegex();
}
