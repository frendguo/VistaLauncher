using System.Text.RegularExpressions;
using VistaLauncher.Models;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// 通用 HTML 版本提供者（用于 Custom 更新源）
/// </summary>
public sealed partial class HtmlVersionProvider : IVersionProvider
{
    private readonly HttpClient _httpClient;

    public HtmlVersionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool Supports(ToolItem tool)
    {
        return tool.UpdateSource == UpdateSource.Custom &&
               !string.IsNullOrEmpty(tool.HomepageUrl);
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
                ReleaseDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTML version check failed for {tool.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从 HTML 页面提取版本号
    /// </summary>
    private static string? ExtractVersion(string html)
    {
        // 按优先级尝试多种模式
        var match = VersionLabelRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = VPrefixVersionRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = ReleaseLabelRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = DataVersionAttrRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        match = DownloadLinkVersionRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }

    [GeneratedRegex(@"[Vv]ersion[:\s]+v?([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionLabelRegex();

    [GeneratedRegex(@"\bv\s?([\d.]+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex VPrefixVersionRegex();

    [GeneratedRegex(@"[Rr]elease[:\s]+v?([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ReleaseLabelRegex();

    [GeneratedRegex(@"(?:data-)?version=[""']([\d.]+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex DataVersionAttrRegex();

    [GeneratedRegex(@"[\w-]+-(\d+\.\d+(?:\.\d+)?(?:\.\d+)?)\.(zip|exe|msi)", RegexOptions.IgnoreCase)]
    private static partial Regex DownloadLinkVersionRegex();
}
