using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 搜索提供者接口
/// </summary>
public interface ISearchProvider
{
    /// <summary>
    /// 搜索工具
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <param name="tools">工具列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的工具列表</returns>
    Task<IEnumerable<ToolItem>> SearchAsync(
        string query,
        IEnumerable<ToolItem> tools,
        CancellationToken cancellationToken = default);
}
