# VistaLauncher NirLauncher 集成实现方案

> 基于 nirlauncher-integration-prd.md 的技术实现方案
>
> 文档版本: v1.1
> 创建日期: 2025-01-15
> 更新日期: 2025-01-15
> 作者: VistaLauncher Team

---

## 1. 现状分析

### 1.1 现有代码结构

```
src/VistaLauncher/VistaLauncher/
├── Models/
│   ├── ToolItem.cs          ✅ 需要扩展
│   ├── ToolGroup.cs         ✅ 已存在
│   ├── ToolsData.cs         ✅ 需要扩展
│   ├── JsonContext.cs       ✅ 需要更新
│   ├── Enums.cs             ✅ 已存在
│   └── AppConfig.cs         ✅ 已存在
├── Services/
│   ├── IToolDataService.cs      ✅ 需要扩展
│   ├── ToolDataService.cs       ✅ 需要扩展
│   ├── ISearchProvider.cs       ✅ 已存在
│   ├── TextMatchSearchProvider.cs  ✅ 需要增强
│   ├── ProcessLauncher.cs       ✅ 已存在
│   ├── HotkeyService.cs         ✅ 已存在
│   ├── IconExtractor.cs         ✅ 已存在
│   └── ConfigService.cs         ✅ 已存在
├── ViewModels/
│   ├── LauncherViewModel.cs     ✅ 需要扩展
│   └── ToolItemViewModel.cs     ✅ 需要扩展
└── Controls/
    ├── SearchBar.xaml(.cs)      ✅ 已存在
    └── CommandBar.xaml(.cs)     ✅ 需要扩展
```

### 1.2 差距分析

| 功能 | 现状 | 需要 |
|------|------|------|
| ToolItem 模型 | 基础字段 | 添加 HelpContent、BasePath、Exe64Path 等 |
| NirLauncher 解析 | 无 | 新增 INirLauncherParser 服务 |
| 搜索功能 | 名称+描述+标签 | 增加 HelpContent 搜索 |
| 版本检查 | 无 | 新增 IVersionCheckService 服务 |
| 帮助内容导入 | 无 | 新增 IHelpContentImporter 服务 |
| 工具管理 UI | 无 | 添加/编辑/删除对话框 |
| 导入 UI | 无 | 导入按钮和进度显示 |

---

## 2. 实施阶段

> **阶段依赖说明**：
> - Phase 1-2: 基础层（模型 + 数据服务接口），无外部依赖
> - Phase 3: 解析层，依赖 Phase 1
> - Phase 4-6: 业务层，依赖 Phase 2
> - Phase 7: UI 层，依赖 Phase 4-6
> - Phase 8-9: P1 功能，可并行开发

### Phase 1: 数据模型扩展 (P0)

**目标**: 扩展现有模型以支持 NirLauncher 数据

#### 2.1.1 ToolItem.cs 扩展

```csharp
// 文件: Models/ToolItem.cs
// 新增字段

/// <summary>
/// 工具包基础路径（绝对路径）
/// 来源：NirLauncher 包目录 或 用户指定
/// </summary>
[ObservableProperty]
private string _basePath = string.Empty;

/// <summary>
/// 可执行文件名（相对于 BasePath）
/// 来源：NirLauncher .nlp 文件的 exe 字段
/// </summary>
[ObservableProperty]
private string _exeFileName = string.Empty;

/// <summary>
/// 64位版本相对路径（如 "x64\Tool.exe"）
/// 来源：NirLauncher .nlp 文件的 exe64 字段
/// </summary>
[ObservableProperty]
private string? _exe64Path;

/// <summary>
/// ARM64 版本相对路径
/// </summary>
[ObservableProperty]
private string? _exeArm64Path;

/// <summary>
/// 帮助文件名（.chm 文件名）
/// 来源：NirLauncher .nlp 文件的 help 字段
/// </summary>
[ObservableProperty]
private string _helpFileName = string.Empty;

/// <summary>
/// 帮助文档内容（由外部工具解析后导入，用于搜索）
/// 来源：IHelpContentImporter 导入
/// </summary>
[ObservableProperty]
private string _helpContent = string.Empty;

/// <summary>
/// 官方主页 URL（用于访问官网和版本检查）
/// 来源：NirLauncher .nlp 文件的 url 字段
/// </summary>
[ObservableProperty]
private string _homepageUrl = string.Empty;

/// <summary>
/// 当前版本号
/// 来源：用户手动填写 或 从 exe 文件属性读取
/// </summary>
[ObservableProperty]
private string _version = string.Empty;

/// <summary>
/// 启动次数
/// 来源：每次启动时自动累加
/// </summary>
[ObservableProperty]
private int _launchCount;

/// <summary>
/// 最后启动时间
/// 来源：每次启动时自动更新
/// </summary>
[ObservableProperty]
private DateTime? _lastLaunchDate;

/// <summary>
/// 是否为命令行工具
/// </summary>
[ObservableProperty]
private bool _isConsoleApp;

/// <summary>
/// 是否需要管理员权限
/// </summary>
[ObservableProperty]
private bool _requiresAdmin;

/// <summary>
/// 是否启用
/// </summary>
[ObservableProperty]
private bool _isEnabled = true;

/// <summary>
/// 导入来源标识（如 "NirSoft", "Sysinternals", "Manual"）
/// </summary>
[ObservableProperty]
private string _source = "Manual";

/// <summary>
/// 获取实际可执行文件路径（根据系统架构选择）
/// </summary>
public string GetExecutablePath()
{
    // 优先使用 ExecutablePath（兼容现有数据）
    if (!string.IsNullOrEmpty(ExecutablePath))
        return ExecutablePath;

    // 根据系统架构选择合适的版本
    var arch = RuntimeInformation.ProcessArchitecture;
    string? relativePath = arch switch
    {
        System.Runtime.InteropServices.Architecture.X64
            when !string.IsNullOrEmpty(Exe64Path) => Exe64Path,
        System.Runtime.InteropServices.Architecture.Arm64
            when !string.IsNullOrEmpty(ExeArm64Path) => ExeArm64Path,
        _ => ExeFileName
    };

    return Path.Combine(BasePath, relativePath ?? ExeFileName);
}
```

#### 2.1.2 ToolsData.cs 扩展

```csharp
// 文件: Models/ToolsData.cs
// 新增字段

/// <summary>
/// 最后检查更新时间
/// </summary>
public DateTime? LastUpdateCheck { get; set; }

/// <summary>
/// 导入来源路径（如 NirLauncher 目录）
/// </summary>
public string? ImportSource { get; set; }

/// <summary>
/// 工具包列表（用于管理导入的包）
/// </summary>
public List<ToolPackage> Packages { get; set; } = [];
```

#### 2.1.3 新增 ToolPackage.cs

```csharp
// 文件: Models/ToolPackage.cs

namespace VistaLauncher.Models;

/// <summary>
/// 工具包数据模型（对应 NirLauncher 的 .nlp 包）
/// </summary>
public class ToolPackage
{
    /// <summary>
    /// 包唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 包名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 包描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 包版本
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 包文件路径（.nlp 文件）
    /// </summary>
    public string PackageFile { get; set; } = string.Empty;

    /// <summary>
    /// 包基础目录
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// 包内工具 ID 列表
    /// </summary>
    public List<string> ToolIds { get; set; } = [];

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.Now;
}
```

#### 2.1.4 新增 NirLauncher 解析模型

```csharp
// 文件: Models/NirLauncher/NirLauncherConfig.cs

namespace VistaLauncher.Models.NirLauncher;

/// <summary>
/// NirLauncher.cfg 配置模型
/// </summary>
public class NirLauncherConfig
{
    /// <summary>
    /// 包列表
    /// </summary>
    public List<NirLauncherPackageRef> Packages { get; set; } = [];
}

/// <summary>
/// 包引用（cfg 文件中的 [PackageX] 部分）
/// </summary>
public class NirLauncherPackageRef
{
    public string NlpFilename { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
```

```csharp
// 文件: Models/NirLauncher/NirLauncherPackage.cs

namespace VistaLauncher.Models.NirLauncher;

/// <summary>
/// NirLauncher .nlp 包模型
/// </summary>
public class NirLauncherPackage
{
    /// <summary>
    /// 包名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 分组数量
    /// </summary>
    public int GroupCount { get; set; }

    /// <summary>
    /// 工具数量
    /// </summary>
    public int SoftwareCount { get; set; }

    /// <summary>
    /// 主页
    /// </summary>
    public string? HomePage { get; set; }

    /// <summary>
    /// 分组列表
    /// </summary>
    public List<NirLauncherGroup> Groups { get; set; } = [];

    /// <summary>
    /// 工具列表
    /// </summary>
    public List<NirLauncherSoftware> Software { get; set; } = [];
}

/// <summary>
/// NirLauncher 分组
/// </summary>
public class NirLauncherGroup
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// NirLauncher 软件项
/// </summary>
public class NirLauncherSoftware
{
    public string AppName { get; set; } = string.Empty;
    public string ShortDesc { get; set; } = string.Empty;
    public string LongDesc { get; set; } = string.Empty;
    public string Exe { get; set; } = string.Empty;
    public string? Exe64 { get; set; }
    public string? Help { get; set; }
    public string? Url { get; set; }
    public int Group { get; set; }
    public bool Console { get; set; }
    public bool Admin { get; set; }
}
```

#### 2.1.5 更新 JsonContext.cs

```csharp
// 文件: Models/JsonContext.cs
// 添加新类型到现有 JsonContext 类

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ToolsData))]
[JsonSerializable(typeof(ToolItem))]
[JsonSerializable(typeof(ToolGroup))]
[JsonSerializable(typeof(List<ToolItem>))]
[JsonSerializable(typeof(List<ToolGroup>))]
// 新增
[JsonSerializable(typeof(ToolPackage))]
[JsonSerializable(typeof(List<ToolPackage>))]
[JsonSerializable(typeof(HelpContentData))]           // Phase 9
[JsonSerializable(typeof(List<HelpContentEntry>))]    // Phase 9
internal partial class JsonContext : JsonSerializerContext
{
}
```

---

### Phase 2: 工具数据服务扩展 (P0)

**目标**: 扩展数据服务支持导入和统计（需在解析服务之前完成，因为解析服务依赖这些接口）

#### 2.2.1 扩展 IToolDataService

```csharp
// 文件: Services/IToolDataService.cs
// 新增方法

/// <summary>
/// 批量导入工具
/// </summary>
/// <param name="tools">工具列表</param>
/// <param name="groups">分组列表</param>
/// <returns>导入成功的数量</returns>
Task<int> ImportToolsAsync(IEnumerable<ToolItem> tools, IEnumerable<ToolGroup> groups);

/// <summary>
/// 更新工具启动统计
/// </summary>
/// <param name="toolId">工具 ID</param>
Task UpdateLaunchStatsAsync(string toolId);

/// <summary>
/// 获取导入来源路径
/// </summary>
string? GetImportSource();

/// <summary>
/// 设置导入来源路径
/// </summary>
Task SetImportSourceAsync(string sourcePath);

/// <summary>
/// 检查工具是否已存在（基于 ExeFileName + GroupId）
/// </summary>
bool ToolExists(string exeFileName, string groupId);

/// <summary>
/// 获取所有分组
/// </summary>
Task<IEnumerable<ToolGroup>> GetGroupsAsync();

/// <summary>
/// 添加分组
/// </summary>
Task AddGroupAsync(ToolGroup group);
```

#### 2.2.2 扩展 ToolDataService 实现

```csharp
// 文件: Services/ToolDataService.cs
// 新增方法实现

public async Task<int> ImportToolsAsync(IEnumerable<ToolItem> tools, IEnumerable<ToolGroup> groups)
{
    var importedCount = 0;

    // 导入分组（避免重复）
    foreach (var group in groups)
    {
        if (!_data.Groups.Any(g => g.Id == group.Id))
        {
            _data.Groups.Add(group);
        }
    }

    // 导入工具（避免重复）
    foreach (var tool in tools)
    {
        if (!ToolExists(tool.ExeFileName, tool.GroupId))
        {
            _data.Tools.Add(tool);
            importedCount++;
        }
    }

    _data.LastModified = DateTime.Now;
    await SaveAsync();

    return importedCount;
}

public async Task UpdateLaunchStatsAsync(string toolId)
{
    var tool = _data.Tools.FirstOrDefault(t => t.Id == toolId);
    if (tool != null)
    {
        tool.LaunchCount++;
        tool.LastLaunchDate = DateTime.Now;
        await SaveAsync();
    }
}

public string? GetImportSource() => _data.ImportSource;

public async Task SetImportSourceAsync(string sourcePath)
{
    _data.ImportSource = sourcePath;
    await SaveAsync();
}

public bool ToolExists(string exeFileName, string groupId)
{
    return _data.Tools.Any(t =>
        t.ExeFileName == exeFileName && t.GroupId == groupId);
}

public Task<IEnumerable<ToolGroup>> GetGroupsAsync()
{
    return Task.FromResult(_data.Groups.AsEnumerable());
}

public async Task AddGroupAsync(ToolGroup group)
{
    if (!_data.Groups.Any(g => g.Id == group.Id))
    {
        _data.Groups.Add(group);
        await SaveAsync();
    }
}

/// <summary>
/// 保存数据到文件（含自动备份）
/// </summary>
private async Task SaveAsync()
{
    var filePath = GetDataFilePath();
    var backupPath = filePath + ".bak";

    // 自动备份：保存前将现有文件复制为 .bak
    if (File.Exists(filePath))
    {
        try
        {
            File.Copy(filePath, backupPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
        }
    }

    var json = JsonSerializer.Serialize(_data, JsonContext.Default.ToolsData);
    await File.WriteAllTextAsync(filePath, json);
}

/// <summary>
/// 加载数据（含损坏恢复）
/// </summary>
public async Task<ToolsData> LoadAsync()
{
    var filePath = GetDataFilePath();
    var backupPath = filePath + ".bak";

    // 尝试加载主文件
    if (File.Exists(filePath))
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize(json, JsonContext.Default.ToolsData);
            if (ValidateData(data))
            {
                _data = data!;
                return _data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load failed, trying backup: {ex.Message}");
        }
    }

    // 主文件损坏，尝试从备份恢复
    if (File.Exists(backupPath))
    {
        try
        {
            var json = await File.ReadAllTextAsync(backupPath);
            var data = JsonSerializer.Deserialize(json, JsonContext.Default.ToolsData);
            if (ValidateData(data))
            {
                _data = data!;
                // 恢复成功，覆盖损坏的主文件
                File.Copy(backupPath, filePath, overwrite: true);
                return _data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup recovery failed: {ex.Message}");
        }
    }

    // 均失败，返回默认数据
    _data = CreateDefaultData();
    return _data;
}

/// <summary>
/// 校验数据完整性
/// </summary>
private static bool ValidateData(ToolsData? data)
{
    if (data == null) return false;
    if (string.IsNullOrEmpty(data.Version)) return false;
    if (data.Tools == null) return false;
    if (data.Groups == null) return false;
    return true;
}
```

---

### Phase 3: NirLauncher 解析服务 (P0)

**目标**: 实现 NirLauncher 配置文件解析

#### 2.3.1 INirLauncherParser 接口

```csharp
// 文件: Services/NirLauncher/INirLauncherParser.cs

namespace VistaLauncher.Services.NirLauncher;

/// <summary>
/// NirLauncher 配置解析服务接口
/// </summary>
public interface INirLauncherParser
{
    /// <summary>
    /// 解析 NirLauncher.cfg 主配置文件
    /// </summary>
    /// <param name="cfgPath">cfg 文件路径</param>
    /// <returns>配置对象</returns>
    Task<NirLauncherConfig> ParseConfigAsync(string cfgPath);

    /// <summary>
    /// 解析 .nlp 包文件
    /// </summary>
    /// <param name="nlpPath">nlp 文件路径</param>
    /// <returns>包对象</returns>
    Task<NirLauncherPackage> ParsePackageAsync(string nlpPath);

    /// <summary>
    /// 将 NirLauncher 数据转换为 VistaLauncher 格式
    /// </summary>
    /// <param name="package">NirLauncher 包</param>
    /// <param name="basePath">包基础路径</param>
    /// <param name="existingTools">现有工具列表（用于增量导入）</param>
    /// <returns>转换后的工具和分组</returns>
    (List<ToolItem> Tools, List<ToolGroup> Groups) ConvertToVistaFormat(
        NirLauncherPackage package,
        string basePath,
        IEnumerable<ToolItem>? existingTools = null);
}
```

#### 2.3.2 NirLauncherParser 实现

```csharp
// 文件: Services/NirLauncher/NirLauncherParser.cs

namespace VistaLauncher.Services.NirLauncher;

/// <summary>
/// NirLauncher INI 格式配置解析器
/// </summary>
public class NirLauncherParser : INirLauncherParser
{
    /// <summary>
    /// 解析 NirLauncher.cfg
    /// </summary>
    public async Task<NirLauncherConfig> ParseConfigAsync(string cfgPath)
    {
        var config = new NirLauncherConfig();
        var lines = await File.ReadAllLinesAsync(cfgPath);

        NirLauncherPackageRef? currentPackage = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 跳过空行和注释
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';'))
                continue;

            // 节头 [PackageX]
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var section = trimmed[1..^1];
                if (section.StartsWith("Package", StringComparison.OrdinalIgnoreCase))
                {
                    currentPackage = new NirLauncherPackageRef();
                    config.Packages.Add(currentPackage);
                }
                continue;
            }

            // 键值对
            if (currentPackage != null && trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                var key = parts[0].Trim().ToLower();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "nlpfilename":
                        currentPackage.NlpFilename = value;
                        break;
                    case "folder":
                        currentPackage.Folder = value;
                        break;
                    case "enabled":
                        currentPackage.Enabled = value == "1";
                        break;
                }
            }
        }

        return config;
    }

    /// <summary>
    /// 解析 .nlp 包文件
    /// </summary>
    public async Task<NirLauncherPackage> ParsePackageAsync(string nlpPath)
    {
        var package = new NirLauncherPackage();
        var lines = await File.ReadAllLinesAsync(nlpPath, Encoding.UTF8);

        string? currentSection = null;
        NirLauncherSoftware? currentSoftware = null;
        NirLauncherGroup? currentGroup = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';'))
                continue;

            // 节头
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1];

                if (currentSection.StartsWith("Software", StringComparison.OrdinalIgnoreCase))
                {
                    currentSoftware = new NirLauncherSoftware();
                    package.Software.Add(currentSoftware);
                    currentGroup = null;
                }
                else if (currentSection.StartsWith("Group", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(currentSection[5..], out var idx))
                    {
                        currentGroup = new NirLauncherGroup { Index = idx };
                        package.Groups.Add(currentGroup);
                    }
                    currentSoftware = null;
                }
                else
                {
                    currentSoftware = null;
                    currentGroup = null;
                }
                continue;
            }

            // 键值对
            if (!trimmed.Contains('=')) continue;

            var parts = trimmed.Split('=', 2);
            var key = parts[0].Trim().ToLower();
            var value = parts[1].Trim();

            // [General] 部分
            if (currentSection == "General")
            {
                switch (key)
                {
                    case "name":
                        package.Name = value;
                        break;
                    case "version":
                        package.Version = value;
                        break;
                    case "groupcount":
                        int.TryParse(value, out var gc);
                        package.GroupCount = gc;
                        break;
                    case "softwarecount":
                        int.TryParse(value, out var sc);
                        package.SoftwareCount = sc;
                        break;
                    case "homepage":
                        package.HomePage = value;
                        break;
                }
            }
            // [GroupX] 部分
            else if (currentGroup != null)
            {
                if (key == "name")
                    currentGroup.Name = value;
            }
            // [SoftwareX] 部分
            else if (currentSoftware != null)
            {
                switch (key)
                {
                    case "appname":
                        currentSoftware.AppName = value;
                        break;
                    case "shortdesc":
                        currentSoftware.ShortDesc = value;
                        break;
                    case "longdesc":
                        currentSoftware.LongDesc = value;
                        break;
                    case "exe":
                        currentSoftware.Exe = value;
                        break;
                    case "exe64":
                        currentSoftware.Exe64 = value;
                        break;
                    case "help":
                        currentSoftware.Help = value;
                        break;
                    case "url":
                        currentSoftware.Url = value;
                        break;
                    case "group":
                        int.TryParse(value, out var g);
                        currentSoftware.Group = g;
                        break;
                    case "console":
                        currentSoftware.Console = value == "1";
                        break;
                    case "admin":
                        currentSoftware.Admin = value == "1";
                        break;
                }
            }
        }

        return package;
    }

    /// <summary>
    /// 转换为 VistaLauncher 格式
    /// </summary>
    public (List<ToolItem> Tools, List<ToolGroup> Groups) ConvertToVistaFormat(
        NirLauncherPackage package,
        string basePath,
        IEnumerable<ToolItem>? existingTools = null)
    {
        var existingSet = existingTools?
            .Select(t => $"{t.ExeFileName}|{t.GroupId}")
            .ToHashSet() ?? [];

        // 转换分组
        var groups = package.Groups
            .Select(g => new ToolGroup
            {
                Id = $"{package.Name.ToLower()}-group-{g.Index}",
                Name = g.Name,
                SortOrder = g.Index
            })
            .ToList();

        // 转换工具
        var tools = new List<ToolItem>();
        foreach (var sw in package.Software)
        {
            var groupId = groups.FirstOrDefault(g =>
                g.Id.EndsWith($"-{sw.Group}"))?.Id ?? "default";

            // 增量导入：跳过已存在的工具
            var key = $"{sw.Exe}|{groupId}";
            if (existingSet.Contains(key))
                continue;

            var tool = new ToolItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = sw.AppName,
                ShortDescription = sw.ShortDesc,
                LongDescription = sw.LongDesc,
                BasePath = basePath,
                ExeFileName = sw.Exe,
                Exe64Path = sw.Exe64,
                HelpFileName = sw.Help ?? string.Empty,
                HomepageUrl = sw.Url ?? string.Empty,
                GroupId = groupId,
                IsConsoleApp = sw.Console,
                RequiresAdmin = sw.Admin,
                Source = package.Name,
                IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            tools.Add(tool);
        }

        return (tools, groups);
    }
}
```

---

### Phase 4: 搜索增强 (P0)

**目标**: 扩展搜索支持 HelpContent 和优化排序

#### 2.4.1 更新 TextMatchSearchProvider

```csharp
// 文件: Services/TextMatchSearchProvider.cs
// 修改 SearchAsync 方法

public Task<IEnumerable<ToolItem>> SearchAsync(
    string query,
    IEnumerable<ToolItem> tools,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Task.FromResult(tools);
    }

    var queryTokens = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    var results = tools.Where(tool =>
    {
        return queryTokens.All(token =>
        {
            var nameMatch = tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase);
            var shortDescMatch = tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
            var longDescMatch = tool.LongDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
            var tagMatch = tool.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase));
            // 新增：帮助内容搜索
            var helpMatch = !string.IsNullOrEmpty(tool.HelpContent) &&
                           tool.HelpContent.Contains(token, StringComparison.OrdinalIgnoreCase);

            return nameMatch || shortDescMatch || longDescMatch || tagMatch || helpMatch;
        });
    });

    // 按匹配度和使用频率排序
    var sortedResults = results.OrderByDescending(tool =>
    {
        var score = 0;
        var lowerName = tool.Name.ToLower();
        var lowerQuery = query.ToLower();

        // 1. 名称精确匹配 (100分)
        if (lowerName == lowerQuery)
            score += 100;
        // 2. 名称前缀匹配 (50分)
        else if (lowerName.StartsWith(lowerQuery))
            score += 50;
        // 3. 名称包含匹配 (25分)
        else if (lowerName.Contains(lowerQuery))
            score += 25;

        // 4. 标签匹配 (15分)
        if (tool.Tags.Any(t => t.Equals(query, StringComparison.OrdinalIgnoreCase)))
            score += 15;

        // 5. Token 匹配计分
        foreach (var token in queryTokens)
        {
            if (tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 10;
            if (tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 5;
            if (tool.Tags.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase)))
                score += 3;
            // 6. 详述匹配 (2分)
            if (tool.LongDescription.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 2;
            // 7. 帮助内容匹配 (1分)
            if (!string.IsNullOrEmpty(tool.HelpContent) &&
                tool.HelpContent.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 1;
        }

        // 8. 使用频率加分（每次启动 +0.1 分，最多 +10 分）
        score += (int)Math.Min(tool.LaunchCount * 0.1, 10);

        return score;
    });

    return Task.FromResult(sortedResults.AsEnumerable());
}
```

---

### Phase 5: 工具管理服务 (P0)

**目标**: 实现工具的增删改查管理

#### 2.5.1 IToolManagementService 接口（依赖 Phase 2 的 IToolDataService 扩展）

```csharp
// 文件: Services/IToolManagementService.cs

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
```

#### 2.5.2 ToolManagementService 实现

```csharp
// 文件: Services/ToolManagementService.cs

namespace VistaLauncher.Services;

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
        var exePath = tool.GetExecutablePath();
        if (File.Exists(exePath))
        {
            tool.Version = GetVersionFromFile(exePath) ?? string.Empty;
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
        var exePath = tool.GetExecutablePath();
        return Task.FromResult(File.Exists(exePath));
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
            SortOrder = (await _dataService.GetGroupsAsync()).Count()
        };

        await _dataService.AddGroupAsync(group);
        return group;
    }
}
```

---

### Phase 6: 导入服务 (P0)

**目标**: 实现 NirLauncher 工具集导入功能

#### 2.6.1 IImportService 接口（依赖 Phase 2 和 Phase 3）

```csharp
// 文件: Services/IImportService.cs

namespace VistaLauncher.Services;

/// <summary>
/// 导入进度事件参数
/// </summary>
public class ImportProgressEventArgs : EventArgs
{
    public int Current { get; init; }
    public int Total { get; init; }
    public string CurrentItem { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// 导入结果
/// </summary>
public class ImportResult
{
    public bool Success { get; init; }
    public int ImportedTools { get; init; }
    public int ImportedGroups { get; init; }
    public int SkippedTools { get; init; }
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// 导入服务接口
/// </summary>
public interface IImportService
{
    /// <summary>
    /// 导入进度事件
    /// </summary>
    event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// 从 NirLauncher 目录导入
    /// </summary>
    /// <param name="nirLauncherPath">NirLauncher 目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ImportResult> ImportFromNirLauncherAsync(
        string nirLauncherPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从单个 .nlp 文件导入
    /// </summary>
    Task<ImportResult> ImportFromNlpFileAsync(
        string nlpPath,
        string basePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证 NirLauncher 目录
    /// </summary>
    bool ValidateNirLauncherDirectory(string path);
}
```

#### 2.6.2 ImportService 实现

```csharp
// 文件: Services/ImportService.cs

namespace VistaLauncher.Services;

public class ImportService : IImportService
{
    private readonly INirLauncherParser _parser;
    private readonly IToolDataService _dataService;

    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    public ImportService(INirLauncherParser parser, IToolDataService dataService)
    {
        _parser = parser;
        _dataService = dataService;
    }

    public async Task<ImportResult> ImportFromNirLauncherAsync(
        string nirLauncherPath,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult { Success = false };
        var errors = new List<string>();
        var totalTools = 0;
        var totalGroups = 0;
        var skipped = 0;

        try
        {
            // 解析主配置文件
            var cfgPath = Path.Combine(nirLauncherPath, "NirLauncher.cfg");
            if (!File.Exists(cfgPath))
            {
                errors.Add($"未找到配置文件: {cfgPath}");
                return result with { Errors = errors };
            }

            var config = await _parser.ParseConfigAsync(cfgPath);
            var enabledPackages = config.Packages.Where(p => p.Enabled).ToList();

            ReportProgress(0, enabledPackages.Count, "开始导入...", "初始化");

            // 遍历所有启用的包
            for (int i = 0; i < enabledPackages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pkgRef = enabledPackages[i];
                var nlpPath = Path.Combine(nirLauncherPath, pkgRef.Folder, pkgRef.NlpFilename);

                if (!File.Exists(nlpPath))
                {
                    errors.Add($"包文件不存在: {nlpPath}");
                    continue;
                }

                ReportProgress(i, enabledPackages.Count, pkgRef.NlpFilename, "解析包文件");

                try
                {
                    var package = await _parser.ParsePackageAsync(nlpPath);
                    var basePath = Path.Combine(nirLauncherPath, pkgRef.Folder);
                    var existingTools = await _dataService.GetToolsAsync();

                    var (tools, groups) = _parser.ConvertToVistaFormat(package, basePath, existingTools);

                    skipped += package.Software.Count - tools.Count;

                    var imported = await _dataService.ImportToolsAsync(tools, groups);
                    totalTools += imported;
                    totalGroups += groups.Count;
                }
                catch (Exception ex)
                {
                    errors.Add($"解析 {pkgRef.NlpFilename} 失败: {ex.Message}");
                }
            }

            // 保存导入来源
            await _dataService.SetImportSourceAsync(nirLauncherPath);

            ReportProgress(enabledPackages.Count, enabledPackages.Count, "完成", "导入完成");

            return new ImportResult
            {
                Success = true,
                ImportedTools = totalTools,
                ImportedGroups = totalGroups,
                SkippedTools = skipped,
                Errors = errors
            };
        }
        catch (OperationCanceledException)
        {
            errors.Add("导入已取消");
            return result with { Errors = errors };
        }
        catch (Exception ex)
        {
            errors.Add($"导入失败: {ex.Message}");
            return result with { Errors = errors };
        }
    }

    public async Task<ImportResult> ImportFromNlpFileAsync(
        string nlpPath,
        string basePath,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        try
        {
            var package = await _parser.ParsePackageAsync(nlpPath);
            var existingTools = await _dataService.GetToolsAsync();

            var (tools, groups) = _parser.ConvertToVistaFormat(package, basePath, existingTools);
            var imported = await _dataService.ImportToolsAsync(tools, groups);

            return new ImportResult
            {
                Success = true,
                ImportedTools = imported,
                ImportedGroups = groups.Count,
                SkippedTools = package.Software.Count - tools.Count,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return new ImportResult { Success = false, Errors = errors };
        }
    }

    public bool ValidateNirLauncherDirectory(string path)
    {
        if (!Directory.Exists(path)) return false;

        var cfgPath = Path.Combine(path, "NirLauncher.cfg");
        return File.Exists(cfgPath);
    }

    private void ReportProgress(int current, int total, string item, string status)
    {
        ProgressChanged?.Invoke(this, new ImportProgressEventArgs
        {
            Current = current,
            Total = total,
            CurrentItem = item,
            Status = status
        });
    }
}
```

---

### Phase 7: UI 实现 (P0)

**目标**: 实现导入和工具管理的用户界面

#### 2.7.1 新增文件列表

```
Controls/
├── ImportDialog.xaml          # 导入对话框
├── ImportDialog.xaml.cs
├── AddToolDialog.xaml         # 添加/编辑工具对话框
├── AddToolDialog.xaml.cs
├── UpdateInfoDialog.xaml      # 更新提示对话框（P1）
└── UpdateInfoDialog.xaml.cs
```

#### 2.7.2 ImportDialog.xaml 设计

```xml
<!-- 文件: Controls/ImportDialog.xaml -->
<ContentDialog
    x:Class="VistaLauncher.Controls.ImportDialog"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="导入 NirLauncher 工具集"
    PrimaryButtonText="导入"
    CloseButtonText="取消"
    DefaultButton="Primary"
    IsPrimaryButtonEnabled="{x:Bind CanImport, Mode=OneWay}">

    <StackPanel Spacing="16" MinWidth="400">
        <!-- 路径选择 -->
        <StackPanel Spacing="4">
            <TextBlock Text="NirLauncher 目录" Style="{StaticResource BodyStrongTextBlockStyle}"/>
            <Grid ColumnSpacing="8">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBox
                    x:Name="PathTextBox"
                    PlaceholderText="选择 NirLauncher 目录..."
                    Text="{x:Bind SelectedPath, Mode=TwoWay}"/>
                <Button Grid.Column="1" Content="浏览..." Click="BrowseButton_Click"/>
            </Grid>
        </StackPanel>

        <!-- 验证状态 -->
        <InfoBar
            x:Name="ValidationInfoBar"
            IsOpen="{x:Bind ShowValidation, Mode=OneWay}"
            Severity="{x:Bind ValidationSeverity, Mode=OneWay}"
            Title="{x:Bind ValidationTitle, Mode=OneWay}"
            Message="{x:Bind ValidationMessage, Mode=OneWay}"/>

        <!-- 进度显示 -->
        <StackPanel
            x:Name="ProgressPanel"
            Spacing="8"
            Visibility="{x:Bind IsImporting, Mode=OneWay}">
            <TextBlock Text="{x:Bind ProgressStatus, Mode=OneWay}"/>
            <ProgressBar
                Value="{x:Bind ProgressValue, Mode=OneWay}"
                Maximum="{x:Bind ProgressMax, Mode=OneWay}"/>
            <TextBlock
                Text="{x:Bind CurrentItem, Mode=OneWay}"
                Style="{StaticResource CaptionTextBlockStyle}"
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
        </StackPanel>

        <!-- 结果显示 -->
        <StackPanel
            x:Name="ResultPanel"
            Spacing="4"
            Visibility="{x:Bind ShowResult, Mode=OneWay}">
            <TextBlock Style="{StaticResource BodyStrongTextBlockStyle}">
                <Run Text="导入完成："/>
                <Run Text="{x:Bind ImportedCount, Mode=OneWay}"/>
                <Run Text=" 个工具"/>
            </TextBlock>
            <TextBlock
                Text="{x:Bind SkippedMessage, Mode=OneWay}"
                Visibility="{x:Bind HasSkipped, Mode=OneWay}"
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
        </StackPanel>
    </StackPanel>
</ContentDialog>
```

#### 2.7.3 ImportDialog.xaml.cs 代码

```csharp
// 文件: Controls/ImportDialog.xaml.cs

namespace VistaLauncher.Controls;

public sealed partial class ImportDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IImportService _importService;
    private CancellationTokenSource? _cancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    // 绑定属性
    private string _selectedPath = string.Empty;
    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (_selectedPath != value)
            {
                _selectedPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanImport));
                ValidatePath();
            }
        }
    }

    public bool CanImport => !string.IsNullOrEmpty(SelectedPath) &&
                            _importService.ValidateNirLauncherDirectory(SelectedPath) &&
                            !IsImporting;

    // 验证状态
    private bool _showValidation;
    public bool ShowValidation
    {
        get => _showValidation;
        set { _showValidation = value; OnPropertyChanged(); }
    }

    private InfoBarSeverity _validationSeverity;
    public InfoBarSeverity ValidationSeverity
    {
        get => _validationSeverity;
        set { _validationSeverity = value; OnPropertyChanged(); }
    }

    private string _validationTitle = string.Empty;
    public string ValidationTitle
    {
        get => _validationTitle;
        set { _validationTitle = value; OnPropertyChanged(); }
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set { _validationMessage = value; OnPropertyChanged(); }
    }

    // 进度状态
    private bool _isImporting;
    public bool IsImporting
    {
        get => _isImporting;
        set
        {
            _isImporting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanImport));
        }
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    private double _progressMax = 100;
    public double ProgressMax
    {
        get => _progressMax;
        set { _progressMax = value; OnPropertyChanged(); }
    }

    private string _progressStatus = string.Empty;
    public string ProgressStatus
    {
        get => _progressStatus;
        set { _progressStatus = value; OnPropertyChanged(); }
    }

    private string _currentItem = string.Empty;
    public string CurrentItem
    {
        get => _currentItem;
        set { _currentItem = value; OnPropertyChanged(); }
    }

    // 结果状态
    private bool _showResult;
    public bool ShowResult
    {
        get => _showResult;
        set { _showResult = value; OnPropertyChanged(); }
    }

    private int _importedCount;
    public int ImportedCount
    {
        get => _importedCount;
        set { _importedCount = value; OnPropertyChanged(); }
    }

    private int _skippedCount;
    public int SkippedCount
    {
        get => _skippedCount;
        set
        {
            _skippedCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSkipped));
            OnPropertyChanged(nameof(SkippedMessage));
        }
    }

    public bool HasSkipped => SkippedCount > 0;
    public string SkippedMessage => $"跳过 {SkippedCount} 个已存在的工具";

    public ImportDialog(IImportService importService)
    {
        _importService = importService;
        InitializeComponent();

        _importService.ProgressChanged += OnProgressChanged;
        PrimaryButtonClick += OnPrimaryButtonClick;
        CloseButtonClick += OnCloseButtonClick;
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");

        // 获取窗口句柄
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            SelectedPath = folder.Path;
        }
    }

    private void ValidatePath()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            ShowValidation = false;
            return;
        }

        if (_importService.ValidateNirLauncherDirectory(SelectedPath))
        {
            ValidationSeverity = InfoBarSeverity.Success;
            ValidationTitle = "验证通过";
            ValidationMessage = "找到有效的 NirLauncher 配置";
        }
        else
        {
            ValidationSeverity = InfoBarSeverity.Error;
            ValidationTitle = "无效目录";
            ValidationMessage = "未找到 NirLauncher.cfg 文件";
        }
        ShowValidation = true;
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 阻止对话框关闭，等待导入完成
        var deferral = args.GetDeferral();

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            IsImporting = true;
            ShowResult = false;

            var result = await _importService.ImportFromNirLauncherAsync(
                SelectedPath,
                _cancellationTokenSource.Token);

            ImportedCount = result.ImportedTools;
            SkippedCount = result.SkippedTools;
            ShowResult = true;
            IsImporting = false;

            if (result.Errors.Count > 0)
            {
                ValidationSeverity = InfoBarSeverity.Warning;
                ValidationTitle = "部分导入";
                ValidationMessage = string.Join("; ", result.Errors.Take(3));
                ShowValidation = true;
            }
        }
        catch (OperationCanceledException)
        {
            ValidationSeverity = InfoBarSeverity.Informational;
            ValidationTitle = "已取消";
            ValidationMessage = "导入操作已取消";
            ShowValidation = true;
        }
        finally
        {
            IsImporting = false;
            deferral.Complete();
        }
    }

    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _cancellationTokenSource?.Cancel();
    }

    private void OnProgressChanged(object? sender, ImportProgressEventArgs e)
    {
        // 确保在 UI 线程更新
        DispatcherQueue.TryEnqueue(() =>
        {
            ProgressValue = e.Current;
            ProgressMax = e.Total;
            ProgressStatus = e.Status;
            CurrentItem = e.CurrentItem;
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### 2.7.4 AddToolDialog.xaml 设计

```xml
<!-- 文件: Controls/AddToolDialog.xaml -->
<ContentDialog
    x:Class="VistaLauncher.Controls.AddToolDialog"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="添加工具"
    PrimaryButtonText="保存"
    CloseButtonText="取消"
    DefaultButton="Primary"
    IsPrimaryButtonEnabled="{x:Bind CanSave, Mode=OneWay}">

    <ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="500">
        <StackPanel Spacing="16" MinWidth="450">
            <!-- 工具名称 -->
            <TextBox
                Header="名称 *"
                PlaceholderText="输入工具名称"
                Text="{x:Bind ToolName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>

            <!-- 可执行文件路径 -->
            <StackPanel Spacing="4">
                <TextBlock Text="可执行文件 *"/>
                <Grid ColumnSpacing="8">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox
                        PlaceholderText="选择或输入 exe 文件路径"
                        Text="{x:Bind ExecutablePath, Mode=TwoWay}"/>
                    <Button Grid.Column="1" Content="浏览..." Click="BrowseExe_Click"/>
                </Grid>
            </StackPanel>

            <!-- 简短描述 -->
            <TextBox
                Header="简短描述"
                PlaceholderText="一句话描述工具功能"
                Text="{x:Bind ShortDescription, Mode=TwoWay}"/>

            <!-- 详细描述 -->
            <TextBox
                Header="详细描述"
                PlaceholderText="详细说明工具用途"
                TextWrapping="Wrap"
                AcceptsReturn="True"
                MinHeight="80"
                Text="{x:Bind LongDescription, Mode=TwoWay}"/>

            <!-- 分组选择 -->
            <ComboBox
                Header="分组 *"
                PlaceholderText="选择分组"
                ItemsSource="{x:Bind Groups}"
                SelectedItem="{x:Bind SelectedGroup, Mode=TwoWay}"
                DisplayMemberPath="Name"
                HorizontalAlignment="Stretch"/>

            <!-- 主页 URL -->
            <TextBox
                Header="主页 URL"
                PlaceholderText="https://..."
                Text="{x:Bind HomepageUrl, Mode=TwoWay}"/>

            <!-- 标签 -->
            <TextBox
                Header="标签"
                PlaceholderText="多个标签用逗号分隔"
                Text="{x:Bind TagsText, Mode=TwoWay}"/>

            <!-- 选项 -->
            <StackPanel Spacing="8">
                <CheckBox Content="命令行工具 (无 GUI)" IsChecked="{x:Bind IsConsoleApp, Mode=TwoWay}"/>
                <CheckBox Content="需要管理员权限" IsChecked="{x:Bind RequiresAdmin, Mode=TwoWay}"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</ContentDialog>
```

#### 2.7.5 AddToolDialog.xaml.cs 代码

```csharp
// 文件: Controls/AddToolDialog.xaml.cs

namespace VistaLauncher.Controls;

public sealed partial class AddToolDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly IToolManagementService _toolService;

    public event PropertyChangedEventHandler? PropertyChanged;

    // 编辑模式：传入现有 ToolItem
    public ToolItem? EditingTool { get; set; }
    public bool IsEditMode => EditingTool != null;

    // 绑定属性
    private string _toolName = string.Empty;
    public string ToolName
    {
        get => _toolName;
        set { _toolName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    private string _executablePath = string.Empty;
    public string ExecutablePath
    {
        get => _executablePath;
        set { _executablePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    private string _shortDescription = string.Empty;
    public string ShortDescription
    {
        get => _shortDescription;
        set { _shortDescription = value; OnPropertyChanged(); }
    }

    private string _longDescription = string.Empty;
    public string LongDescription
    {
        get => _longDescription;
        set { _longDescription = value; OnPropertyChanged(); }
    }

    private string _homepageUrl = string.Empty;
    public string HomepageUrl
    {
        get => _homepageUrl;
        set { _homepageUrl = value; OnPropertyChanged(); }
    }

    private string _tagsText = string.Empty;
    public string TagsText
    {
        get => _tagsText;
        set { _tagsText = value; OnPropertyChanged(); }
    }

    private bool _isConsoleApp;
    public bool IsConsoleApp
    {
        get => _isConsoleApp;
        set { _isConsoleApp = value; OnPropertyChanged(); }
    }

    private bool _requiresAdmin;
    public bool RequiresAdmin
    {
        get => _requiresAdmin;
        set { _requiresAdmin = value; OnPropertyChanged(); }
    }

    // 分组
    public ObservableCollection<ToolGroup> Groups { get; } = [];

    private ToolGroup? _selectedGroup;
    public ToolGroup? SelectedGroup
    {
        get => _selectedGroup;
        set { _selectedGroup = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    public bool CanSave => !string.IsNullOrWhiteSpace(ToolName) &&
                          !string.IsNullOrWhiteSpace(ExecutablePath) &&
                          SelectedGroup != null;

    public AddToolDialog(IToolManagementService toolService)
    {
        _toolService = toolService;
        InitializeComponent();

        Loaded += OnLoaded;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载分组列表
        var groups = await _toolService.GetGroupsAsync();
        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        // 编辑模式：填充现有数据
        if (EditingTool != null)
        {
            Title = "编辑工具";
            ToolName = EditingTool.Name;
            ExecutablePath = EditingTool.GetExecutablePath();
            ShortDescription = EditingTool.ShortDescription;
            LongDescription = EditingTool.LongDescription;
            HomepageUrl = EditingTool.HomepageUrl;
            TagsText = string.Join(", ", EditingTool.Tags);
            IsConsoleApp = EditingTool.IsConsoleApp;
            RequiresAdmin = EditingTool.RequiresAdmin;
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == EditingTool.GroupId);
        }
    }

    private async void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add(".exe");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ExecutablePath = file.Path;

            // 自动填充名称（如果为空）
            if (string.IsNullOrEmpty(ToolName))
            {
                ToolName = Path.GetFileNameWithoutExtension(file.Name);
            }
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            var tool = EditingTool ?? new ToolItem();

            tool.Name = ToolName;
            tool.ExecutablePath = ExecutablePath;
            tool.ShortDescription = ShortDescription;
            tool.LongDescription = LongDescription;
            tool.HomepageUrl = HomepageUrl;
            tool.Tags = TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim())
                               .ToList();
            tool.IsConsoleApp = IsConsoleApp;
            tool.RequiresAdmin = RequiresAdmin;
            tool.GroupId = SelectedGroup?.Id ?? "default";
            tool.Source = "Manual";

            if (IsEditMode)
            {
                await _toolService.UpdateToolAsync(tool);
            }
            else
            {
                await _toolService.AddToolAsync(tool);
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### 2.7.6 CommandBar 扩展

```xml
<!-- 文件: Controls/CommandBar.xaml -->
<!-- 在 More 菜单中添加 -->
<MenuFlyout Placement="TopEdgeAlignedRight">
    <MenuFlyoutItem Text="Open file location" Click="OpenFileLocation_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE838;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    <MenuFlyoutItem Text="Copy path" Click="CopyPath_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE8C8;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    <MenuFlyoutSeparator/>
    <MenuFlyoutItem Text="Edit" Click="EditTool_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE70F;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    <MenuFlyoutItem Text="Remove" Click="RemoveTool_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE74D;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    <MenuFlyoutSeparator/>
    <!-- 新增 -->
    <MenuFlyoutItem Text="Add Tool" Click="AddTool_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE710;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
    <MenuFlyoutItem Text="Import NirLauncher" Click="ImportNirLauncher_Click">
        <MenuFlyoutItem.Icon>
            <FontIcon Glyph="&#xE8B5;"/>
        </MenuFlyoutItem.Icon>
    </MenuFlyoutItem>
</MenuFlyout>
```

#### 2.7.7 MainWindow TopBar 扩展

```xml
<!-- 文件: MainWindow.xaml -->
<!-- TopBarGrid 右侧添加按钮 -->
<Grid x:Name="TopBarGrid">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>   <!-- Icon -->
        <ColumnDefinition Width="*"/>       <!-- SearchBox -->
        <ColumnDefinition Width="Auto"/>   <!-- 操作按钮 -->
    </Grid.ColumnDefinitions>

    <!-- 现有内容... -->

    <!-- 新增操作按钮区域 -->
    <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4">
        <Button
            x:Name="ImportButton"
            ToolTipService.ToolTip="导入 NirLauncher 工具集"
            Click="ImportButton_Click"
            Style="{StaticResource SubtleButtonStyle}">
            <FontIcon Glyph="&#xE8B5;" FontSize="14"/>
        </Button>
        <Button
            x:Name="AddToolButton"
            ToolTipService.ToolTip="添加工具"
            Click="AddToolButton_Click"
            Style="{StaticResource SubtleButtonStyle}">
            <FontIcon Glyph="&#xE710;" FontSize="14"/>
        </Button>
    </StackPanel>
</Grid>
```

---

### Phase 8: 版本检查服务 (P1)

**目标**: 实现工具版本检查和更新提示

#### 2.8.1 IVersionCheckService 接口

```csharp
// 文件: Services/IVersionCheckService.cs

namespace VistaLauncher.Services;

/// <summary>
/// 版本检查结果
/// </summary>
public class VersionCheckResult
{
    public string ToolId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string CurrentVersion { get; init; } = string.Empty;
    public string? LatestVersion { get; init; }
    public string? DownloadUrl { get; init; }
    public bool HasUpdate => !string.IsNullOrEmpty(LatestVersion) &&
                            LatestVersion != CurrentVersion;
}

/// <summary>
/// 版本检查服务接口
/// </summary>
public interface IVersionCheckService
{
    /// <summary>
    /// 检查单个工具版本（尽力而为）
    /// </summary>
    Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量检查版本
    /// </summary>
    Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开工具下载页面
    /// </summary>
    void OpenDownloadPage(ToolItem tool);
}
```

#### 2.8.2 VersionCheckService 实现（简化版）

```csharp
// 文件: Services/VersionCheckService.cs

namespace VistaLauncher.Services;

public class VersionCheckService : IVersionCheckService
{
    private readonly HttpClient _httpClient;
    private static readonly Dictionary<string, Regex> _versionPatterns = new()
    {
        ["nirsoft.net"] = new Regex(@"<td[^>]*>v([\d.]+)</td>", RegexOptions.Compiled),
    };

    /// <summary>
    /// 构造函数（建议在应用级别管理 HttpClient 生命周期或使用 IHttpClientFactory）
    /// </summary>
    /// <param name="httpClient">可选的 HttpClient 实例，为 null 时创建默认实例</param>
    public VersionCheckService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? CreateDefaultHttpClient();
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.Timeout = TimeSpan.FromSeconds(10);
        return client;
    }

    public async Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return null;

        try
        {
            var html = await _httpClient.GetStringAsync(tool.HomepageUrl, cancellationToken);

            // 尝试匹配版本号
            foreach (var (pattern, regex) in _versionPatterns)
            {
                if (!tool.HomepageUrl.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = regex.Match(html);
                if (match.Success)
                {
                    return new VersionCheckResult
                    {
                        ToolId = tool.Id,
                        ToolName = tool.Name,
                        CurrentVersion = tool.Version,
                        LatestVersion = match.Groups[1].Value,
                        DownloadUrl = tool.HomepageUrl
                    };
                }
            }
        }
        catch (Exception ex)
        {
            // 版本检查是尽力而为，失败记录日志后返回 null
            System.Diagnostics.Debug.WriteLine($"Version check failed for {tool.Name}: {ex.Message}");
        }

        return null;
    }

    public async Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VersionCheckResult>();
        var toolList = tools.ToList();
        var completed = 0;

        foreach (var tool in toolList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await CheckVersionAsync(tool, cancellationToken);
            if (result != null)
            {
                results.Add(result);
            }

            completed++;
            progress?.Report(completed);
        }

        return results;
    }

    public void OpenDownloadPage(ToolItem tool)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = tool.HomepageUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open download page: {ex.Message}");
        }
    }
}
```

---

### Phase 9: 帮助内容导入 (P1, 可选)

**目标**: 支持导入外部解析的 CHM 帮助内容

#### 2.9.1 IHelpContentImporter 接口

```csharp
// 文件: Services/IHelpContentImporter.cs

namespace VistaLauncher.Services;

/// <summary>
/// 帮助内容条目（外部工具生成的格式）
/// </summary>
public class HelpContentEntry
{
    public string ToolName { get; set; } = string.Empty;
    public string HelpFileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 帮助内容导入服务接口
/// </summary>
public interface IHelpContentImporter
{
    /// <summary>
    /// 从 JSON 文件导入帮助内容
    /// </summary>
    Task<int> ImportFromJsonAsync(string jsonPath);

    /// <summary>
    /// 从目录批量导入（文件名匹配）
    /// </summary>
    Task<int> ImportFromDirectoryAsync(string directory);
}
```

#### 2.9.2 HelpContentImporter 实现

```csharp
// 文件: Services/HelpContentImporter.cs

namespace VistaLauncher.Services;

public class HelpContentImporter : IHelpContentImporter
{
    private readonly IToolDataService _dataService;

    public HelpContentImporter(IToolDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<int> ImportFromJsonAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return 0;

        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<HelpContentData>(json);
        if (data?.Entries == null) return 0;

        var tools = (await _dataService.GetToolsAsync()).ToList();
        var imported = 0;

        foreach (var entry in data.Entries)
        {
            // 按 HelpFileName 匹配
            var tool = tools.FirstOrDefault(t =>
                !string.IsNullOrEmpty(t.HelpFileName) &&
                t.HelpFileName.Equals(entry.HelpFileName, StringComparison.OrdinalIgnoreCase));

            // 或按 Name 匹配
            tool ??= tools.FirstOrDefault(t =>
                t.Name.Equals(entry.ToolName, StringComparison.OrdinalIgnoreCase));

            if (tool != null)
            {
                tool.HelpContent = entry.Content;
                await _dataService.UpdateToolAsync(tool);
                imported++;
            }
        }

        return imported;
    }

    public async Task<int> ImportFromDirectoryAsync(string directory)
    {
        if (!Directory.Exists(directory)) return 0;

        var tools = (await _dataService.GetToolsAsync()).ToList();
        var imported = 0;

        foreach (var file in Directory.GetFiles(directory, "*.txt"))
        {
            var toolName = Path.GetFileNameWithoutExtension(file);
            var tool = tools.FirstOrDefault(t =>
                t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (tool != null)
            {
                tool.HelpContent = await File.ReadAllTextAsync(file);
                await _dataService.UpdateToolAsync(tool);
                imported++;
            }
        }

        return imported;
    }
}

internal class HelpContentData
{
    public DateTime GeneratedAt { get; set; }
    public List<HelpContentEntry> Entries { get; set; } = [];
}
```

---

## 3. 文件变更清单

### 3.1 新增文件

| 文件路径 | 说明 | 阶段 |
|----------|------|------|
| `Models/ToolPackage.cs` | 工具包模型 | Phase 1 |
| `Models/NirLauncher/NirLauncherConfig.cs` | NirLauncher 配置模型 | Phase 1 |
| `Models/NirLauncher/NirLauncherPackage.cs` | NirLauncher 包模型 | Phase 1 |
| `Services/NirLauncher/INirLauncherParser.cs` | 解析器接口 | Phase 3 |
| `Services/NirLauncher/NirLauncherParser.cs` | 解析器实现 | Phase 3 |
| `Services/IToolManagementService.cs` | 工具管理接口 | Phase 5 |
| `Services/ToolManagementService.cs` | 工具管理实现 | Phase 5 |
| `Services/IImportService.cs` | 导入服务接口 | Phase 6 |
| `Services/ImportService.cs` | 导入服务实现 | Phase 6 |
| `Controls/ImportDialog.xaml` | 导入对话框 | Phase 7 |
| `Controls/ImportDialog.xaml.cs` | 导入对话框代码 | Phase 7 |
| `Controls/AddToolDialog.xaml` | 添加工具对话框 | Phase 7 |
| `Controls/AddToolDialog.xaml.cs` | 添加工具对话框代码 | Phase 7 |
| `Services/IVersionCheckService.cs` | 版本检查接口 | Phase 8 |
| `Services/VersionCheckService.cs` | 版本检查实现 | Phase 8 |
| `Controls/UpdateInfoDialog.xaml` | 更新提示对话框 | Phase 8 |
| `Controls/UpdateInfoDialog.xaml.cs` | 更新提示对话框代码 | Phase 8 |
| `Services/IHelpContentImporter.cs` | 帮助导入接口 | Phase 9 |
| `Services/HelpContentImporter.cs` | 帮助导入实现 | Phase 9 |

### 3.2 修改文件

| 文件路径 | 修改内容 | 阶段 |
|----------|----------|------|
| `Models/ToolItem.cs` | 添加新字段 | Phase 1 |
| `Models/ToolsData.cs` | 添加 ImportSource、Packages 字段 | Phase 1 |
| `Models/JsonContext.cs` | 添加新类型序列化支持 | Phase 1 |
| `Services/IToolDataService.cs` | 添加导入和统计方法 | Phase 2 |
| `Services/ToolDataService.cs` | 实现新方法 | Phase 2 |
| `Services/TextMatchSearchProvider.cs` | 添加 HelpContent 搜索、优化排序 | Phase 4 |
| `Controls/CommandBar.xaml` | 添加菜单项 | Phase 7 |
| `Controls/CommandBar.xaml.cs` | 添加事件处理 | Phase 7 |
| `MainWindow.xaml` | TopBar 添加按钮 | Phase 7 |
| `MainWindow.xaml.cs` | 添加按钮事件和服务注入 | Phase 7 |
| `ViewModels/LauncherViewModel.cs` | 添加导入和管理命令 | Phase 7 |

---

## 4. 依赖注入配置

```csharp
// 文件: MainWindow.xaml.cs
// 服务初始化

private void InitializeServices()
{
    // 现有服务...
    _toolDataService = new ToolDataService();
    _searchProvider = new TextMatchSearchProvider();
    _processLauncher = new ProcessLauncher();
    _hotkeyService = new HotkeyService();

    // 新增服务
    _nirLauncherParser = new NirLauncherParser();
    _importService = new ImportService(_nirLauncherParser, _toolDataService);
    _toolManagementService = new ToolManagementService(_toolDataService);
    _versionCheckService = new VersionCheckService();
    _helpContentImporter = new HelpContentImporter(_toolDataService);

    // 注入到 ViewModel
    _viewModel = new LauncherViewModel(
        _toolDataService,
        _searchProvider,
        _processLauncher,
        _importService,
        _toolManagementService);
}
```

---

## 5. 测试计划

### 5.1 单元测试

| 测试类 | 测试内容 |
|--------|----------|
| `NirLauncherParserTests` | cfg/nlp 文件解析正确性 |
| `TextMatchSearchProviderTests` | 搜索排序和 HelpContent 匹配 |
| `ImportServiceTests` | 导入流程和增量导入 |
| `ToolManagementServiceTests` | CRUD 操作 |
| `ToolDataServiceTests` | 数据加载、保存、备份恢复 |

#### 边界条件测试

| 测试类 | 测试场景 | 预期行为 |
|--------|----------|----------|
| `NirLauncherParserTests` | 空 .nlp 文件 | 返回空 Package，不抛异常 |
| `NirLauncherParserTests` | 格式错误的 INI（无 = 号） | 跳过该行，继续解析 |
| `NirLauncherParserTests` | 特殊字符（中文、Unicode） | 正确解析 UTF-8 编码内容 |
| `NirLauncherParserTests` | 缺少必需字段（无 exe） | 跳过该工具，记录警告 |
| `ToolDataServiceTests` | JSON 文件不存在 | 创建默认数据 |
| `ToolDataServiceTests` | JSON 格式损坏 | 从 .bak 恢复 |
| `ToolDataServiceTests` | JSON 和 .bak 均损坏 | 返回默认数据 |
| `ImportServiceTests` | 导入过程中取消 | 抛出 OperationCanceledException |
| `ImportServiceTests` | .nlp 文件不存在 | 记录错误，继续其他包 |
| `TextMatchSearchProviderTests` | 空查询字符串 | 返回全部工具 |
| `TextMatchSearchProviderTests` | 特殊正则字符（.*+?） | 作为普通字符处理 |

### 5.2 集成测试

| 测试场景 | 验收标准 |
|----------|----------|
| 首次导入 | NirSoft 257 个工具全部导入成功 |
| 增量导入 | 重复工具自动跳过 |
| 搜索性能 | 300 工具搜索 < 100ms |
| 启动统计 | 启动后自动更新计数 |

### 5.3 UI 测试

| 测试场景 | 验收标准 |
|----------|----------|
| 导入对话框 | 进度显示正确，可取消 |
| 添加工具 | 必填校验，保存成功 |
| 编辑工具 | 数据正确回显 |
| 删除工具 | 确认后删除 |

---

## 6. 附录

### 6.1 NirLauncher INI 格式示例

**NirLauncher.cfg**:
```ini
[General]
Name=NirLauncher

[Package1]
NlpFilename=nirsoft.nlp
Folder=NirSoft
Enabled=1

[Package2]
NlpFilename=sysinternals.nlp
Folder=SysinternalsSuit
Enabled=1
```

**nirsoft.nlp**:
```ini
[General]
Name=NirSoft Utilities
Version=1.30.2
GroupCount=13
SoftwareCount=257
HomePage=https://www.nirsoft.net/

[Group0]
Name=Password Recovery Utilities

[Software0]
AppName=WebBrowserPassView
ShortDesc=View passwords stored by Web browsers
LongDesc=WebBrowserPassView is a password recovery tool...
exe=WebBrowserPassView.exe
exe64=x64\WebBrowserPassView.exe
help=WebBrowserPassView.chm
url=https://www.nirsoft.net/utils/web_browser_password.html
group=0
console=0
admin=0
```

### 6.2 数据存储位置

```
%AppData%\Roaming\VistaLauncher\
├── tools.json           # 主数据文件
├── tools.json.bak       # 自动备份
├── config.json          # 应用配置
└── cache\
    └── icons\           # 图标缓存
```

---

---

## 7. 变更记录

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2025-01-15 | 初始版本 |
| v1.1 | 2025-01-15 | 评审修订：补充 Version 字段；统一使用 HomepageUrl；同步 I/O 改异步；补充日志记录；调整阶段顺序（IToolDataService 扩展前移至 Phase 2）；补充 ImportDialog/AddToolDialog Code-Behind；补充备份恢复逻辑；补充边界条件测试；完善 JsonContext 类型定义 |

---

*文档结束*
