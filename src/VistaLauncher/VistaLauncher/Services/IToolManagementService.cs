using System.Diagnostics;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具管理服务接口
/// </summary>
public interface IToolManagementService
{
    /// <summary>
    /// 添加新工具
    /// </summary>
    Task<ToolItem> AddToolAsync(ToolItem tool);

    /// <summary>
    /// 更新工具信息
    /// </summary>
    Task<bool> UpdateToolAsync(ToolItem tool);

    /// <summary>
    /// 删除工具
    /// </summary>
    Task<bool> DeleteToolAsync(string toolId);

    /// <summary>
    /// 验证工具文件是否存在
    /// </summary>
    Task<bool> ValidateToolAsync(ToolItem tool);

    /// <summary>
    /// 从文件读取版本信息
    /// </summary>
    string? GetVersionFromFile(string exePath);

    /// <summary>
    /// 获取所有分组
    /// </summary>
    Task<IEnumerable<ToolGroup>> GetGroupsAsync();

    /// <summary>
    /// 添加分组
    /// </summary>
    Task<ToolGroup> AddGroupAsync(string name);
}
