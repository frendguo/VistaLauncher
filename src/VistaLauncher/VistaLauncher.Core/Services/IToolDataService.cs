using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具数据服务接口
/// </summary>
public interface IToolDataService
{
    /// <summary>
    /// 获取所有工具
    /// </summary>
    Task<IEnumerable<ToolItem>> GetToolsAsync();

    /// <summary>
    /// 获取所有分组
    /// </summary>
    Task<IEnumerable<ToolGroup>> GetGroupsAsync();

    /// <summary>
    /// 根据 ID 获取工具
    /// </summary>
    Task<ToolItem?> GetToolByIdAsync(string id);

    /// <summary>
    /// 添加工具
    /// </summary>
    Task<bool> AddToolAsync(ToolItem tool);

    /// <summary>
    /// 更新工具
    /// </summary>
    Task<bool> UpdateToolAsync(ToolItem tool);

    /// <summary>
    /// 删除工具
    /// </summary>
    Task<bool> DeleteToolAsync(string id);

    /// <summary>
    /// 添加分组
    /// </summary>
    Task<bool> AddGroupAsync(ToolGroup group);

    /// <summary>
    /// 删除分组
    /// </summary>
    Task<bool> DeleteGroupAsync(string id);

    /// <summary>
    /// 保存所有数据
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// 重新加载数据
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// 获取用户配置文件路径
    /// </summary>
    string GetToolsFilePath();

    /// <summary>
    /// 获取默认配置文件路径（软件目录）
    /// </summary>
    string GetDefaultConfigPath();

    /// <summary>
    /// 检查指定工具是否为默认工具
    /// </summary>
    bool IsDefaultTool(string toolId);

    /// <summary>
    /// 重置工具到默认配置（移除用户的覆盖）
    /// </summary>
    Task<bool> ResetToolToDefaultAsync(string toolId);
}
