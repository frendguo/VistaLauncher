using System.Text.RegularExpressions;
using VistaLauncher.Models;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// NirSoft 工具版本提供者
/// </summary>
public sealed partial class NirSoftVersionProvider : IVersionProvider
{
    private readonly HttpClient _httpClient;

    public NirSoftVersionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool Supports(ToolItem tool)
    {
        return tool.UpdateSource == UpdateSource.NirSoft ||
               (!string.IsNullOrEmpty(tool.HomepageUrl) &&
                tool.HomepageUrl.Contains("nirsoft.net", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return null;

        try
        {
            var html = await _httpClient.GetStringAsync(tool.HomepageUrl, cancellationToken);
            var version = ExtractVersion(html);

            if (string.IsNullOrEmpty(version))
                return null;

            return new UpdateInfo
            {
                Version = version,
                InfoUrl = tool.HomepageUrl,
                DownloadUrl = InferDownloadUrl(tool.HomepageUrl),
                ReleaseDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NirSoft version check failed for {tool.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从 HTML 页面提取版本号
    /// </summary>
    private static string? ExtractVersion(string html)
    {
        var match = VersionInTdRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = VersionTextRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = VersionInTagRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }

    /// <summary>
    /// 从主页 URL 推断下载 URL
    /// </summary>
    private static string? InferDownloadUrl(string homepageUrl)
    {
        var match = NirSoftUrlRegex().Match(homepageUrl);
        if (match.Success)
        {
            var toolName = match.Groups[1].Value;
            return $"https://www.nirsoft.net/utils/{toolName}.zip";
        }

        return null;
    }

    [GeneratedRegex(@"<td[^>]*>v([\d.]+)</td>", RegexOptions.IgnoreCase)]
    private static partial Regex VersionInTdRegex();

    [GeneratedRegex(@"[Vv]ersion[:\s]+v?([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionTextRegex();

    [GeneratedRegex(@">v([\d.]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex VersionInTagRegex();

    [GeneratedRegex(@"nirsoft\.net/utils/([^/]+)\.html", RegexOptions.IgnoreCase)]
    private static partial Regex NirSoftUrlRegex();
}
