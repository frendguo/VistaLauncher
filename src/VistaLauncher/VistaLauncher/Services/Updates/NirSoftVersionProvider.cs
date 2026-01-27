using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VistaLauncher.Models;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// NirSoft 工具版本提供者
/// 支持通过 PAD (Portable Application Description) 文件获取版本信息
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
               !string.IsNullOrEmpty(tool.PadUrl) ||
               (!string.IsNullOrEmpty(tool.HomepageUrl) &&
                tool.HomepageUrl.Contains("nirsoft.net", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        // 优先使用 PAD 文件
        if (!string.IsNullOrEmpty(tool.PadUrl))
        {
            var padResult = await GetUpdateInfoFromPadAsync(tool, cancellationToken);
            if (padResult != null)
                return padResult;
        }

        // 退回到 HTML 解析方式
        return await GetUpdateInfoFromHtmlAsync(tool, cancellationToken);
    }

    /// <summary>
    /// 从 PAD XML 文件获取更新信息
    /// </summary>
    private async Task<UpdateInfo?> GetUpdateInfoFromPadAsync(
        ToolItem tool,
        CancellationToken cancellationToken)
    {
        try
        {
            var xmlContent = await _httpClient.GetStringAsync(tool.PadUrl, cancellationToken);
            var doc = XDocument.Parse(xmlContent);

            var root = doc.Element("XML_DIZ_INFO");
            if (root == null)
                return null;

            var programInfo = root.Element("Program_Info");
            var webInfo = root.Element("Web_Info");

            if (programInfo == null || webInfo == null)
                return null;

            // 提取版本号
            var version = programInfo.Element("Program_Version")?.Value;
            if (string.IsNullOrEmpty(version))
                return null;

            // 提取下载链接
            var downloadUrls = webInfo.Element("Download_URLs");
            var primaryDownloadUrl = downloadUrls?.Element("Primary_Download_URL")?.Value;

            // 根据架构推导下载链接
            var downloadUrl = GetDownloadUrlForArchitecture(primaryDownloadUrl, tool.Architecture);

            // 提取工具信息页面
            var applicationUrls = webInfo.Element("Application_URLs");
            var infoUrl = applicationUrls?.Element("Application_Info_URL")?.Value;

            // 提取发布日期
            DateTime? releaseDate = null;
            var year = programInfo.Element("Program_Release_Year")?.Value;
            var month = programInfo.Element("Program_Release_Month")?.Value;
            var day = programInfo.Element("Program_Release_Day")?.Value;

            if (int.TryParse(year, out var y) &&
                int.TryParse(month, out var m) &&
                int.TryParse(day, out var d))
            {
                try
                {
                    releaseDate = new DateTime(y, m, d);
                }
                catch
                {
                    // 日期解析失败，忽略
                }
            }

            // 提取文件大小
            long? fileSize = null;
            var fileSizeBytes = programInfo.Element("File_Info")?.Element("File_Size_Bytes")?.Value;
            if (long.TryParse(fileSizeBytes, out var size))
            {
                fileSize = size;
            }

            // 提取描述作为发布说明
            var descriptions = root.Element("Program_Descriptions")?.Element("English");
            var releaseNotes = descriptions?.Element("Char_Desc_250")?.Value;

            return new UpdateInfo
            {
                Version = version,
                DownloadUrl = downloadUrl,
                InfoUrl = infoUrl,
                ReleaseDate = releaseDate,
                FileSize = fileSize,
                ReleaseNotes = releaseNotes
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PAD parse failed for {tool.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据架构获取对应的下载链接
    /// </summary>
    private static string? GetDownloadUrlForArchitecture(string? primaryUrl, Architecture architecture)
    {
        if (string.IsNullOrEmpty(primaryUrl))
            return null;

        if (architecture == Architecture.x64)
        {
            // 将 .zip 替换为 -x64.zip
            return ZipExtensionRegex().Replace(primaryUrl, "-x64.zip");
        }

        return primaryUrl;
    }

    /// <summary>
    /// 从 HTML 页面获取更新信息（后备方案）
    /// </summary>
    private async Task<UpdateInfo?> GetUpdateInfoFromHtmlAsync(
        ToolItem tool,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return null;

        try
        {
            var html = await _httpClient.GetStringAsync(tool.HomepageUrl, cancellationToken);
            var version = ExtractVersionFromHtml(html);

            if (string.IsNullOrEmpty(version))
                return null;

            return new UpdateInfo
            {
                Version = version,
                InfoUrl = tool.HomepageUrl,
                DownloadUrl = InferDownloadUrlFromHomepage(tool.HomepageUrl, tool.Architecture),
                ReleaseDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HTML version check failed for {tool.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从 HTML 页面提取版本号
    /// </summary>
    private static string? ExtractVersionFromHtml(string html)
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
    private static string? InferDownloadUrlFromHomepage(string homepageUrl, Architecture architecture)
    {
        var match = NirSoftUrlRegex().Match(homepageUrl);
        if (match.Success)
        {
            var toolName = match.Groups[1].Value;
            var suffix = architecture == Architecture.x64 ? "-x64" : "";
            return $"https://www.nirsoft.net/utils/{toolName}{suffix}.zip";
        }

        return null;
    }

    [GeneratedRegex(@"\.zip$", RegexOptions.IgnoreCase)]
    private static partial Regex ZipExtensionRegex();

    [GeneratedRegex(@"<td[^>]*>v([\d.]+)</td>", RegexOptions.IgnoreCase)]
    private static partial Regex VersionInTdRegex();

    [GeneratedRegex(@"[Vv]ersion[:\s]+v?([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionTextRegex();

    [GeneratedRegex(@">v([\d.]+)<", RegexOptions.IgnoreCase)]
    private static partial Regex VersionInTagRegex();

    [GeneratedRegex(@"nirsoft\.net/utils/([^/]+)\.html", RegexOptions.IgnoreCase)]
    private static partial Regex NirSoftUrlRegex();
}
