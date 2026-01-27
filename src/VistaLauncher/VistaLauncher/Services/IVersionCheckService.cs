using VistaLauncher.Models;
using VistaLauncher.Services.Updates;

namespace VistaLauncher.Services;

/// <summary>
/// 版本检查结果
/// </summary>
public class VersionCheckResult
{
    /// <summary>
    /// 工具 ID
    /// </summary>
    public required string ToolId { get; init; }

    /// <summary>
    /// 工具名称
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// 当前版本
    /// </summary>
    public string CurrentVersion { get; init; } = string.Empty;

    /// <summary>
    /// 最新版本
    /// </summary>
    public string? LatestVersion { get; init; }

    /// <summary>
    /// 下载链接
    /// </summary>
    public string? DownloadUrl { get; init; }

    /// <summary>
    /// 发布说明
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTime? ReleaseDate { get; init; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? FileSize { get; init; }

    /// <summary>
    /// 是否有可用更新
    /// </summary>
    public bool HasUpdate => !string.IsNullOrEmpty(LatestVersion) &&
                             !string.IsNullOrEmpty(CurrentVersion) &&
                             VersionComparer.IsNewer(LatestVersion, CurrentVersion);

    /// <summary>
    /// 检查是否失败
    /// </summary>
    public bool CheckFailed { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 批量检查进度
/// </summary>
public class CheckProgress
{
    /// <summary>
    /// 已完成数量
    /// </summary>
    public int Completed { get; init; }

    /// <summary>
    /// 总数量
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// 发现更新数量
    /// </summary>
    public int UpdateFound { get; init; }

    /// <summary>
    /// 当前正在检查的工具名称
    /// </summary>
    public string? CurrentTool { get; init; }

    /// <summary>
    /// 进度百分比
    /// </summary>
    public double Percentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}

/// <summary>
/// 版本检查服务接口
/// </summary>
public interface IVersionCheckService
{
    /// <summary>
    /// 检查单个工具版本
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本检查结果</returns>
    Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量检查版本
    /// </summary>
    /// <param name="tools">工具列表</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本检查结果列表</returns>
    Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<CheckProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开工具下载页面
    /// </summary>
    /// <param name="tool">工具项</param>
    void OpenDownloadPage(ToolItem tool);

    /// <summary>
    /// 比较两个版本号
    /// </summary>
    /// <param name="v1">版本号1</param>
    /// <param name="v2">版本号2</param>
    /// <returns>负数表示 v1 &lt; v2，0 表示相等，正数表示 v1 &gt; v2</returns>
    int CompareVersions(string v1, string v2);
}
