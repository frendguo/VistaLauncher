using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具下载结果
/// </summary>
public class ToolDownloadResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 工具 ID
    /// </summary>
    public string ToolId { get; init; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// 安装的版本
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// 安装路径
    /// </summary>
    public string? InstalledPath { get; init; }

    /// <summary>
    /// 操作消息列表
    /// </summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 工具下载服务接口
/// </summary>
public interface IToolDownloadService
{
    /// <summary>
    /// 检查是否支持下载指定工具
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <returns>是否支持下载</returns>
    bool CanDownload(ToolItem tool);

    /// <summary>
    /// 获取工具的下载信息（版本和 URL）
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新信息，如果无法获取则返回 null</returns>
    Task<UpdateInfo?> GetDownloadInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载并安装工具
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="progress">下载进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载结果</returns>
    Task<ToolDownloadResult> DownloadAndInstallAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
