using System.Text.Json.Serialization;

namespace VistaLauncher.Models;

/// <summary>
/// 自动更新配置
/// </summary>
public class UpdateConfig
{
    /// <summary>
    /// 是否启用自动检查更新
    /// </summary>
    [JsonPropertyName("autoCheckEnabled")]
    public bool AutoCheckEnabled { get; set; } = true;

    /// <summary>
    /// 自动检查间隔（小时）
    /// </summary>
    [JsonPropertyName("checkIntervalHours")]
    public int CheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// 是否自动下载更新
    /// </summary>
    [JsonPropertyName("autoDownloadEnabled")]
    public bool AutoDownloadEnabled { get; set; } = false;

    /// <summary>
    /// 是否包含预发布版本
    /// </summary>
    [JsonPropertyName("includePrerelease")]
    public bool IncludePrerelease { get; set; } = false;

    /// <summary>
    /// 每次最多检查的工具数量（避免过度请求）
    /// </summary>
    [JsonPropertyName("maxConcurrentChecks")]
    public int MaxConcurrentChecks { get; set; } = 5;
}
