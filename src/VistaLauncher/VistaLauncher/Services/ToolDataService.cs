using System.Text.Json;
using System.Text.Json.Serialization;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 工具数据服务实现，使用 JSON 文件存储
/// </summary>
public class ToolDataService : IToolDataService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _dataDirectory;
    private readonly string _toolsFilePath;
    private ToolsData _data = new();
    private bool _isLoaded = false;
    private readonly object _lock = new();

    public ToolDataService()
    {
        // 使用 AppData/Roaming/VistaLauncher 作为数据目录
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VistaLauncher");
        _toolsFilePath = Path.Combine(_dataDirectory, "tools.json");

        // 确保目录存在
        Directory.CreateDirectory(_dataDirectory);
    }

    public string GetToolsFilePath() => _toolsFilePath;

    public async Task<IEnumerable<ToolItem>> GetToolsAsync()
    {
        await EnsureLoadedAsync();
        return _data.Tools;
    }

    public async Task<IEnumerable<ToolGroup>> GetGroupsAsync()
    {
        await EnsureLoadedAsync();
        return _data.Groups;
    }

    public async Task<ToolItem?> GetToolByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        return _data.Tools.FirstOrDefault(t => t.Id == id);
    }

    public async Task<bool> AddToolAsync(ToolItem tool)
    {
        await EnsureLoadedAsync();
        
        // 检查是否已存在
        if (_data.Tools.Any(t => t.Id == tool.Id))
        {
            return false;
        }

        tool.CreatedAt = DateTime.Now;
        tool.UpdatedAt = DateTime.Now;
        _data.Tools.Add(tool);
        await SaveAsync();
        return true;
    }

    public async Task<bool> UpdateToolAsync(ToolItem tool)
    {
        await EnsureLoadedAsync();
        
        var existingIndex = _data.Tools.FindIndex(t => t.Id == tool.Id);
        if (existingIndex < 0)
        {
            return false;
        }

        tool.UpdatedAt = DateTime.Now;
        _data.Tools[existingIndex] = tool;
        await SaveAsync();
        return true;
    }

    public async Task<bool> DeleteToolAsync(string id)
    {
        await EnsureLoadedAsync();
        
        var tool = _data.Tools.FirstOrDefault(t => t.Id == id);
        if (tool == null)
        {
            return false;
        }

        _data.Tools.Remove(tool);
        await SaveAsync();
        return true;
    }

    public async Task<bool> AddGroupAsync(ToolGroup group)
    {
        await EnsureLoadedAsync();
        
        if (_data.Groups.Any(g => g.Id == group.Id))
        {
            return false;
        }

        _data.Groups.Add(group);
        await SaveAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(string id)
    {
        await EnsureLoadedAsync();
        
        var group = _data.Groups.FirstOrDefault(g => g.Id == id);
        if (group == null)
        {
            return false;
        }

        _data.Groups.Remove(group);
        await SaveAsync();
        return true;
    }

    public async Task SaveAsync()
    {
        _data.LastModified = DateTime.Now;

        var json = JsonSerializer.Serialize(_data, _jsonOptions);
        await File.WriteAllTextAsync(_toolsFilePath, json);
    }

    public async Task ReloadAsync()
    {
        _isLoaded = false;
        await EnsureLoadedAsync();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        lock (_lock)
        {
            if (_isLoaded) return;
        }

        if (File.Exists(_toolsFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_toolsFilePath);
                var data = JsonSerializer.Deserialize<ToolsData>(json, _jsonOptions);
                if (data != null)
                {
                    _data = data;
                }
            }
            catch (Exception)
            {
                // 如果加载失败，使用默认数据
                _data = CreateDefaultData();
            }
        }
        else
        {
            // 创建默认数据
            _data = CreateDefaultData();
            await SaveAsync();
        }

        _isLoaded = true;
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
                    HelpUrl = "https://support.microsoft.com"
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
                    HelpUrl = "https://docs.microsoft.com/windows-server/administration/windows-commands/cmd"
                }
            ]
        };
    }
}
