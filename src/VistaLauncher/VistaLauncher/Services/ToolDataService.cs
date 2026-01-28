using System.Text.Json;
using System.Text.Json.Serialization;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具数据服务实现，支持双配置源（默认配置 + 用户配置）
/// </summary>
public class ToolDataService : IToolDataService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _userDataDirectory;
    private readonly string _userConfigPath;
    private readonly string _defaultConfigPath;

    private ToolsData? _defaultData;
    private ToolsData? _userData;
    private ToolsData _mergedData = new();

    private bool _isLoaded = false;
    private readonly object _lock = new();

    public ToolDataService()
    {
        // 默认配置路径（软件目录）
        _defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "tools.json");

        // 用户配置路径（AppData/Roaming/VistaLauncher）
        _userDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VistaLauncher");
        _userConfigPath = Path.Combine(_userDataDirectory, "tools.json");

        // 确保用户目录存在
        Directory.CreateDirectory(_userDataDirectory);
    }

    public string GetToolsFilePath() => _userConfigPath;

    public string GetDefaultConfigPath() => _defaultConfigPath;

    public bool IsDefaultTool(string toolId)
    {
        return _defaultData?.Tools.Any(t => t.Id == toolId) ?? false;
    }

    public async Task<bool> ResetToolToDefaultAsync(string toolId)
    {
        await EnsureLoadedAsync();

        // 只能重置默认工具
        if (!IsDefaultTool(toolId))
        {
            return false;
        }

        // 从用户数据中移除该工具的覆盖
        var userTool = _userData?.Tools.FirstOrDefault(t => t.Id == toolId);
        if (userTool != null)
        {
            _userData!.Tools.Remove(userTool);
        }

        // 重新合并配置
        MergeConfigs();
        await SaveAsync();

        return true;
    }

    public async Task<IEnumerable<ToolItem>> GetToolsAsync()
    {
        await EnsureLoadedAsync();
        return _mergedData.Tools;
    }

    public async Task<IEnumerable<ToolGroup>> GetGroupsAsync()
    {
        await EnsureLoadedAsync();
        return _mergedData.Groups;
    }

    public async Task<ToolItem?> GetToolByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        return _mergedData.Tools.FirstOrDefault(t => t.Id == id);
    }

    public async Task<bool> AddToolAsync(ToolItem tool)
    {
        await EnsureLoadedAsync();

        // 检查是否已存在
        if (_mergedData.Tools.Any(t => t.Id == tool.Id))
        {
            return false;
        }

        tool.CreatedAt = DateTime.Now;
        tool.UpdatedAt = DateTime.Now;
        tool.Source = ToolSource.User;

        // 添加到用户数据
        _userData ??= new ToolsData();
        _userData.Tools.Add(tool);

        // 重新合并并保存
        MergeConfigs();
        await SaveAsync();
        return true;
    }

    public async Task<bool> UpdateToolAsync(ToolItem tool)
    {
        await EnsureLoadedAsync();

        var existingTool = _mergedData.Tools.FirstOrDefault(t => t.Id == tool.Id);
        if (existingTool == null)
        {
            return false;
        }

        tool.UpdatedAt = DateTime.Now;

        // 初始化用户数据
        _userData ??= new ToolsData();

        // 检查是否是默认工具
        if (IsDefaultTool(tool.Id))
        {
            // 修改默认工具，标记为 UserOverride
            tool.Source = ToolSource.UserOverride;

            // 更新或添加到用户数据
            var userToolIndex = _userData.Tools.FindIndex(t => t.Id == tool.Id);
            if (userToolIndex >= 0)
            {
                _userData.Tools[userToolIndex] = tool;
            }
            else
            {
                _userData.Tools.Add(tool);
            }
        }
        else
        {
            // 用户工具，保持 User 来源
            tool.Source = ToolSource.User;
            var userToolIndex = _userData.Tools.FindIndex(t => t.Id == tool.Id);
            if (userToolIndex >= 0)
            {
                _userData.Tools[userToolIndex] = tool;
            }
        }

        // 重新合并并保存
        MergeConfigs();
        await SaveAsync();
        return true;
    }

    public async Task<bool> DeleteToolAsync(string id)
    {
        await EnsureLoadedAsync();

        var tool = _mergedData.Tools.FirstOrDefault(t => t.Id == id);
        if (tool == null)
        {
            return false;
        }

        // 不允许删除默认工具
        if (IsDefaultTool(id))
        {
            return false;
        }

        // 从用户数据中删除
        var userTool = _userData?.Tools.FirstOrDefault(t => t.Id == id);
        if (userTool != null)
        {
            _userData!.Tools.Remove(userTool);
        }

        // 重新合并并保存
        MergeConfigs();
        await SaveAsync();
        return true;
    }

    public async Task<bool> AddGroupAsync(ToolGroup group)
    {
        await EnsureLoadedAsync();

        if (_mergedData.Groups.Any(g => g.Id == group.Id))
        {
            return false;
        }

        // 添加到用户数据
        _userData ??= new ToolsData();
        _userData.Groups.Add(group);

        // 重新合并并保存
        MergeConfigs();
        await SaveAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(string id)
    {
        await EnsureLoadedAsync();

        var group = _mergedData.Groups.FirstOrDefault(g => g.Id == id);
        if (group == null)
        {
            return false;
        }

        // 不允许删除默认分组
        if (_defaultData?.Groups.Any(g => g.Id == id) ?? false)
        {
            return false;
        }

        // 从用户数据中删除
        var userGroup = _userData?.Groups.FirstOrDefault(g => g.Id == id);
        if (userGroup != null)
        {
            _userData!.Groups.Remove(userGroup);
        }

        // 重新合并并保存
        MergeConfigs();
        await SaveAsync();
        return true;
    }

    public async Task SaveAsync()
    {
        // 只保存用户数据
        if (_userData == null || (_userData.Tools.Count == 0 && _userData.Groups.Count == 0))
        {
            // 如果用户数据为空且文件存在，删除文件
            if (File.Exists(_userConfigPath))
            {
                File.Delete(_userConfigPath);
            }
            return;
        }

        _userData.LastModified = DateTime.Now;

        var json = JsonSerializer.Serialize(_userData, _jsonOptions);
        await File.WriteAllTextAsync(_userConfigPath, json);
    }

    public async Task ReloadAsync()
    {
        _isLoaded = false;
        _defaultData = null;
        _userData = null;
        await EnsureLoadedAsync();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        lock (_lock)
        {
            if (_isLoaded) return;
        }

        // 加载默认配置（软件目录）
        _defaultData = await LoadConfigAsync(_defaultConfigPath);

        // 加载用户配置（用户目录）
        _userData = await LoadConfigAsync(_userConfigPath);

        // 如果两个配置都不存在，创建默认数据
        if (_defaultData == null && _userData == null)
        {
            _userData = CreateDefaultData();
            await SaveAsync();
        }

        // 合并配置
        MergeConfigs();

        _isLoaded = true;
    }

    private static async Task<ToolsData?> LoadConfigAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ToolsData>(json, _jsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void MergeConfigs()
    {
        _mergedData = new ToolsData
        {
            Version = _userData?.Version ?? _defaultData?.Version ?? "1.0.0",
            LastModified = DateTime.Now
        };

        // 合并分组
        var groupDict = new Dictionary<string, ToolGroup>();

        // 先添加默认分组
        if (_defaultData?.Groups != null)
        {
            foreach (var group in _defaultData.Groups)
            {
                groupDict[group.Id] = group;
            }
        }

        // 用户分组覆盖或追加
        if (_userData?.Groups != null)
        {
            foreach (var group in _userData.Groups)
            {
                groupDict[group.Id] = group;
            }
        }

        _mergedData.Groups = [.. groupDict.Values];

        // 合并工具
        var toolDict = new Dictionary<string, ToolItem>();

        // 先添加默认工具，标记来源为 Default
        if (_defaultData?.Tools != null)
        {
            foreach (var tool in _defaultData.Tools)
            {
                tool.Source = ToolSource.Default;
                toolDict[tool.Id] = tool;
            }
        }

        // 用户工具覆盖或追加
        if (_userData?.Tools != null)
        {
            foreach (var tool in _userData.Tools)
            {
                if (toolDict.ContainsKey(tool.Id))
                {
                    // 覆盖默认工具
                    tool.Source = ToolSource.UserOverride;
                }
                else
                {
                    // 用户新增工具
                    tool.Source = ToolSource.User;
                }
                toolDict[tool.Id] = tool;
            }
        }

        _mergedData.Tools = [.. toolDict.Values];
    }

    private static ToolsData CreateDefaultData()
    {
        return new ToolsData
        {
            Version = "1.0.0",
            LastModified = DateTime.Now,
            Groups =
            [
                new ToolGroup { Id = "development", Name = "开发工具", Icon = "🛠️" },
                new ToolGroup { Id = "productivity", Name = "效率工具", Icon = "⚡" },
                new ToolGroup { Id = "system", Name = "系统工具", Icon = "⚙️" }
            ],
            Tools =
            [
                new ToolItem
                {
                    Id = "notepad",
                    Name = "记事本",
                    ShortDescription = "Windows 内置文本编辑器",
                    LongDescription = "Windows 记事本是一款简单的文本编辑器，适合编辑纯文本文件。",
                    ExecutablePath = @"C:\Windows\System32\notepad.exe",
                    Version = "11.0",
                    Type = ToolType.GUI,
                    Architecture = Architecture.x64,
                    GroupId = "productivity",
                    Tags = ["editor", "text", "notepad"],
                    HomepageUrl = "https://www.microsoft.com",
                    HelpUrl = "https://support.microsoft.com",
                    Source = ToolSource.User
                },
                new ToolItem
                {
                    Id = "cmd",
                    Name = "命令提示符",
                    ShortDescription = "Windows 命令行工具",
                    LongDescription = "Windows 命令提示符（CMD）是 Windows 系统的命令行解释器。",
                    ExecutablePath = @"C:\Windows\System32\cmd.exe",
                    Version = "10.0",
                    Type = ToolType.Console,
                    Architecture = Architecture.x64,
                    GroupId = "system",
                    Tags = ["terminal", "command", "shell"],
                    HomepageUrl = "https://www.microsoft.com",
                    HelpUrl = "https://docs.microsoft.com/windows-server/administration/windows-commands/cmd",
                    Source = ToolSource.User
                }
            ]
        };
    }
}
