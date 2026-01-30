using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具可用性检查服务接口
/// </summary>
public interface IToolAvailabilityService
{
    /// <summary>
    /// 检查工具的可用性状态
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <returns>工具可用性状态</returns>
    Task<ToolAvailability> CheckAvailabilityAsync(ToolItem tool);

    /// <summary>
    /// 检查工具文件是否存在
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <returns>文件是否存在</returns>
    bool IsToolInstalled(ToolItem tool);

    /// <summary>
    /// 获取工具的实际可执行文件路径（解析变量后）
    /// </summary>
    /// <param name="tool">工具项</param>
    /// <returns>解析后的可执行文件路径</returns>
    string GetResolvedExecutablePath(ToolItem tool);

    /// <summary>
    /// 批量检查工具可用性
    /// </summary>
    /// <param name="tools">工具列表</param>
    /// <returns>工具 ID 到可用性状态的映射</returns>
    Task<Dictionary<string, ToolAvailability>> CheckAvailabilitiesAsync(IEnumerable<ToolItem> tools);
}
