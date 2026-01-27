using System.Diagnostics;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具管理服务实现
/// </summary>
public class ToolManagementService : IToolManagementService
{
    private readonly IToolDataService _dataService;

    public ToolManagementService(IToolDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<ToolItem> AddToolAsync(ToolItem tool)
    {
        tool.Id = Guid.NewGuid().ToString();
        tool.CreatedAt = DateTime.Now;
        tool.UpdatedAt = DateTime.Now;

        // 尝试从文件读取版本
        if (File.Exists(tool.ExecutablePath))
        {
            tool.Version = GetVersionFromFile(tool.ExecutablePath) ?? string.Empty;
        }

        await _dataService.AddToolAsync(tool);
        return tool;
    }

    public async Task<bool> UpdateToolAsync(ToolItem tool)
    {
        tool.UpdatedAt = DateTime.Now;
        return await _dataService.UpdateToolAsync(tool);
    }

    public async Task<bool> DeleteToolAsync(string toolId)
    {
        return await _dataService.DeleteToolAsync(toolId);
    }

    public Task<bool> ValidateToolAsync(ToolItem tool)
    {
        return Task.FromResult(File.Exists(tool.ExecutablePath));
    }

    public string? GetVersionFromFile(string exePath)
    {
        try
        {
            if (!File.Exists(exePath)) return null;

            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            return versionInfo.FileVersion ?? versionInfo.ProductVersion;
        }
        catch
        {
            return null;
        }
    }

    public Task<IEnumerable<ToolGroup>> GetGroupsAsync()
    {
        return _dataService.GetGroupsAsync();
    }

    public async Task<ToolGroup> AddGroupAsync(string name)
    {
        var group = new ToolGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Icon = "Folder"
        };

        await _dataService.AddGroupAsync(group);
        return group;
    }
}
