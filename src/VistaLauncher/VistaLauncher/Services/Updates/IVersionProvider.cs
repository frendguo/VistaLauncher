using VistaLauncher.Models;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// 版本提供者接口（用于不同更新源的版本检查）
/// </summary>
public interface IVersionProvider
{
    /// <summary>
    /// 获取更新信息
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新信息，如果无法获取则返回 null</returns>
    Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否支持该工具
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <returns>是否支持</returns>
    bool Supports(ToolItem tool);
}
