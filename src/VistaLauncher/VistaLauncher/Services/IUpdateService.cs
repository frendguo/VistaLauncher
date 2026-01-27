using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 下载进度
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// 已下载字节数
    /// </summary>
    public long BytesDownloaded { get; init; }

    /// <summary>
    /// 总字节数
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// 下载百分比
    /// </summary>
    public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;

    /// <summary>
    /// 当前文件名
    /// </summary>
    public string? CurrentFile { get; init; }

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// 更新结果
/// </summary>
public class UpdateResult
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
    /// 旧版本号
    /// </summary>
    public string OldVersion { get; init; } = string.Empty;

    /// <summary>
    /// 新版本号
    /// </summary>
    public string NewVersion { get; init; } = string.Empty;

    /// <summary>
    /// 操作消息列表
    /// </summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>
    /// 备份文件路径
    /// </summary>
    public string? BackupPath { get; init; }
}

/// <summary>
/// 更新服务接口
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// 下载更新
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="progress">下载进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载文件的本地路径</returns>
    Task<string> DownloadUpdateAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 安装更新
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="downloadedFilePath">已下载的文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<UpdateResult> InstallUpdateAsync(
        ToolItem tool,
        string downloadedFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载并安装更新（完整流程）
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="progress">下载进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<UpdateResult> UpdateToolAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚更新
    /// </summary>
    /// <param name="updateResult">更新结果（包含备份路径）</param>
    /// <returns>是否回滚成功</returns>
    Task<bool> RollbackUpdateAsync(UpdateResult updateResult);
}
