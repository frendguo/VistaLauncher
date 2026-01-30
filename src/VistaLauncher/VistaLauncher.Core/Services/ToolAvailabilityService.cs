using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具可用性检查服务实现
/// </summary>
public sealed class ToolAvailabilityService : IToolAvailabilityService
{
    private readonly IPathResolverService _pathResolver;

    /// <summary>
    /// 创建工具可用性检查服务
    /// </summary>
    /// <param name="pathResolver">路径解析服务</param>
    public ToolAvailabilityService(IPathResolverService pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public Task<ToolAvailability> CheckAvailabilityAsync(ToolItem tool)
    {
        var resolvedPath = GetResolvedExecutablePath(tool);

        if (!File.Exists(resolvedPath))
        {
            return Task.FromResult(ToolAvailability.NotInstalled);
        }

        // 检查是否有更新
        if (tool.HasUpdate)
        {
            return Task.FromResult(ToolAvailability.UpdateAvailable);
        }

        return Task.FromResult(ToolAvailability.Available);
    }

    public bool IsToolInstalled(ToolItem tool)
    {
        var resolvedPath = GetResolvedExecutablePath(tool);
        return File.Exists(resolvedPath);
    }

    public string GetResolvedExecutablePath(ToolItem tool)
    {
        var path = tool.ExecutablePath;
        if (string.IsNullOrEmpty(path))
        {
            // 如果没有配置路径，返回默认路径
            var toolDir = _pathResolver.GetToolInstallDirectory(tool.Id, tool.Name);
            return Path.Combine(toolDir, $"{tool.Name}.exe");
        }

        return _pathResolver.ResolvePath(path);
    }

    public async Task<Dictionary<string, ToolAvailability>> CheckAvailabilitiesAsync(IEnumerable<ToolItem> tools)
    {
        var result = new Dictionary<string, ToolAvailability>();
        foreach (var tool in tools)
        {
            result[tool.Id] = await CheckAvailabilityAsync(tool);
        }
        return result;
    }
}
