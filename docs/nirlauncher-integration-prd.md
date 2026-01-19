# VistaLauncher NirLauncher 工具集管理 PRD

> 产品需求文档：NirLauncher 工具集集成与管理
>
> 文档版本: v1.3
> 创建日期: 2025-01-15
> 更新日期: 2025-01-15
> 作者: VistaLauncher Team

---

## 1. 项目背景

### 1.1 现状分析

用户拥有一个位于 `C:\Users\frend\OneDrive\Softs\开发工具\NirLauncher` 的工具集目录，包含：

- **NirSoft**: 约 257 个工具，涵盖密码恢复、网络监控、系统工具等 13 个分组
- **SysinternalsSuit**: 微软 Sysinternals 工具套件

这些工具具有以下特点：
- 每个工具都是独立的可执行文件 (.exe)
- 拥有简短描述 (ShortDesc) 和详细描述 (LongDesc)
- 部分工具提供多架构版本 (x86/x64/ARM64)
- 自带帮助文档 (.chm 文件)
- 使用 NirLauncher 的 .nlp 配置格式管理

### 1.2 痛点

1. **工具发现困难**: 300+ 工具，难以快速找到需要的��具
2. **启动效率低**: 需要打开 NirLauncher 浏览分类或记忆工具名
3. **更新管理不便**: 工具版本分散，无法统一检查更新
4. **搜索体验差**: NirLauncher 仅支持文件名搜索，无法搜索描述内容
5. **AI 搜索缺失**: 无法通过自然语言描述找到相关工具

### 1.3 解决方案

通过 VistaLauncher 提供统一的工具集管理界面，支持：
- 全局热键快速唤起
- 多维度智能搜索 (名称 + 描述)
- AI 语义搜索 (基于解析的 CHM 内容)
- 自动更新检查
- 本地 JSON 数据持久化

---

## 2. 产品目标

### 2.1 核心目标

1. **工具集导入**: 自动解析 NirLauncher 配置，导入 NirSoft 和 SysinternalsSuit 工具
2. **本地数据管理**: 导入后维护独立的 JSON 数据文件，不依赖 NirLauncher 配置
3. **智能搜索**: 支持通过工具名称、简短描述、详细描述、CHM 内容进行匹配
4. **AI 搜索扩展**: 基于解析的 CHM 内容，支持未来智能语义搜索
5. **工具管理**: 支持添加、编辑、删除工具
6. **自动更新**: 支持自动检查和更新单一 exe 工具

### 2.2 非目标

- 不修改 NirLauncher 原始配置文件
- 不管理需要安装的非便携式工具
- 不提供工具本身的分发托管

---

## 3. 功能需求

### 3.1 工具集导入 (P0)

| ID | 需�� | 描述 |
|----|------|------|
| F01 | 解析 .nlp 文件 | 自动读取 NirLauncher 的 .nlp 包配置文件 |
| F02 | 解析 .cfg 文件 | 读取 NirLauncher.cfg 中的包配置和分组信息 |
| F03 | 提取工具信息 | 提取 AppName、ShortDesc、LongDesc、url、group 等字段 |
| F04 | 多架构支持 | 识别 exe64/exearm64 字段，支持 x86/x64/ARM64 多版本，运行时自动选择 |
| F05 | 帮助内容导入 | 支持导入外部解析好的帮助内容（由独立 CHM 解析工具生成） |
| F06 | 增量导入 | 支持增量添加新工具，基于 `ExeFileName + GroupId` 判断重复 |
| F07 | 数据持久化 | 导入后保存到本地 JSON 文件 |

**数据映射关系**:

| NirLauncher 字段 | VistaLauncher 字段 |
|-----------------|-------------------|
| `[SoftwareX].AppName` | `ToolItem.Name` |
| `[SoftwareX].ShortDesc` | `ToolItem.ShortDescription` |
| `[SoftwareX].LongDesc` | `ToolItem.LongDescription` |
| `[SoftwareX].exe` | `ToolItem.ExeFileName` |
| `[SoftwareX].exe64` | `ToolItem.Exe64Path` |
| `[SoftwareX].url` | `ToolItem.HomepageUrl` |
| `[SoftwareX].group` | `ToolItem.GroupId` |
| `[GroupX].Name` | `ToolGroup.Name` |
| `[SoftwareX].help` | `ToolItem.HelpFileName`（帮助内容由外部工具解析后导入） |

### 3.2 本地数据管理 (P0)

| ID | 需求 | 描述 |
|----|------|------|
| F10 | JSON 存储 | 工具数据存储在 `%AppData%\Roaming\VistaLauncher\tools.json` |
| F11 | 数据版本 | 支持数据格式版本控制，便于迁移 |
| F12 | 增量保存 | 数据变更时增量更新 JSON 文件 |
| F13 | 数据校验 | 启动时校验 JSON 完整性（Version 字段、Tools 数组存在性），损坏时从 .bak 恢复 |

**数据结构**:

```json
{
  "Version": "1.0.0",
  "LastModified": "2025-01-15T10:30:00+08:00",
  "LastUpdateCheck": "2025-01-15T10:30:00+08:00",
  "ImportSource": "C:\\Users\\frend\\OneDrive\\Softs\\开发工具\\NirLauncher",
  "Groups": [...],
  "Tools": [...]
}
```

### 3.3 搜索功能 (P0)

| ID | 需求 | 描述 |
|----|------|------|
| F20 | 名称搜索 | 匹配工具名称 (AppName) |
| F21 | 简述搜索 | 匹配简短描述 (ShortDesc) |
| F22 | 详述搜索 | 匹配详细描述 (LongDesc) |
| F23 | 帮助内容搜索 | 搜索解析后的 CHM 内容 |
| F24 | 模糊匹配 | 支持拼音首字母、部分匹配 |
| F25 | 结果排序 | 按相关度、使用频率排序 |
| F26 | 搜索防抖 | 输入延迟 100ms 后触发搜索 |

**搜索优先级**（从高到低）:

```
1. 名称精确匹配
2. 名称前缀匹配
3. 名称包含匹配
4. 标签匹配
5. 简述匹配
6. 详述匹配
7. 帮助内容匹配（如已导入）

注：同一优先级内，按使用频率排序
```

### 3.4 AI 搜索扩展 (Future)

> **注意**：此功能为未来扩展方向，不在当前版本实施范围内。

| ID | 需求 | 描述 |
|----|------|------|
| F30 | 搜索接口抽象 | 现有 `ISearchProvider` 接口已支持扩展 |
| F31 | 语义搜索 | 未来可基于 HelpContent 实现自然语言搜索 |

**扩展说明**:

当前 `ISearchProvider` 接口设计已支持未来扩展：
- 可添加新的搜索实现类（如 `AISearchProvider`）
- HelpContent 字段可作为语义搜索的数据源
- 具体实现方案待未来需求明确后设计

### 3.5 工具管理 (P0)

| ID | 需求 | 描述 |
|----|------|------|
| F40 | 添加工具 | 支持手动添加新工具到本地数据库 |
| F41 | 编辑工具 | 支持修改工具名称、描述、路径等信息 |
| F42 | 删除工具 | 支持从本地数据库删除工具 |
| F43 | 添加界面 | 提供添加/编辑工具的对话框 |

**添加工具字段**:
- 工具名称 (必填)
- 可执行文件路径 (必填，支持浏览选择)
- 简短描述 (可选)
- 详细描述 (可选)
- 所属分组 (必填，可选现有分组或新建)
- 主页 URL (可选)
- 标签 (可选，逗号分隔)

### 3.6 版本检查与更新提示 (P1)

> **设计原则**：由于各工具网站格式不统一，自动下载存在风险，因此仅提供版本提示，由用户手动更新。

| ID | 需求 | 描述 |
|----|------|------|
| F50 | 手动检查 | 提供"检查更新"按钮，手动触发版本检查（不自动检查，避免网络请求影响启动速度） |
| F51 | 版本显示 | 显示当前版本和最新版本（如可获取） |
| F52 | 更新提示 | 有新版本时显示提示图标 |
| F53 | 打开下载页 | 点击后打开工具的官方下载页面 |
| F54 | 版本记录 | 用户可手动更新本地版本号 |

**更新流程**:

```
1. 用户点击"检查更新"按钮
2. 系统尝试从 HomePage 获取版本信息（尽力而为）
3. 对比本地版本，有更新时显示提示
4. 用户点击"打开下载页"跳转到官网
5. 用户手动下载、替换文件后，更新本地版本号

注：版本检测可能因网站格式变化而失败，不影响主功能使用
```

### 3.7 用户界面 (P0)

#### 3.7.1 主界面增强

| ID | 需求 | 描述 | 实现位置 |
|----|------|------|----------|
| F60 | 导入按钮 | TopBar 添加导入 NirLauncher 按钮 | TopBarGrid 右侧 |
| F61 | 更新状态指示 | 有更新时显示图标和数量 | TopBarGrid 右侧 |
| F62 | 添加工具按钮 | CommandBar "More" 菜单添加"添加工具" | More MenuFlyout |
| F63 | 更新按钮 | CommandBar 添加"检查更新"按钮 | Settings/新增区域 |

#### 3.7.2 UI 布局设计

**TopBarGrid 扩展**:
```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto" />   <!-- Icon -->
    <ColumnDefinition Width="*" />      <!-- SearchBox -->
    <ColumnDefinition Width="Auto" />   <!-- 新增: 导入/更新按钮 -->
</Grid.ColumnDefinitions>
```

**新增按钮**:
- 导入工具集 (Icon: &#xF8E6; Import)
- 检查更新 (Icon: &#xE895; Sync / &#xE72C; Update, 带角标显示数量)
- 添加工具 (Icon: &#xE710; Add)

**CommandBar More 菜单扩展**:
```xml
<MenuFlyout Placement="TopEdgeAlignedRight">
    <MenuFlyoutItem Icon="OpenLocal" Text="Open file location" />
    <MenuFlyoutItem Icon="Copy" Text="Copy path" />
    <MenuFlyoutSeparator />
    <MenuFlyoutItem Icon="Edit" Text="Edit" />
    <MenuFlyoutItem Icon="Delete" Text="Remove" />
    <MenuFlyoutSeparator />
    <!-- 新增 -->
    <MenuFlyoutItem Icon="Add" Text="Add Tool" />
    <MenuFlyoutItem Icon="Sync" Text="Check for Updates" />
</MenuFlyout>
```

#### 3.7.3 对话框设计

**添加/编辑工具对话框**:
```
┌─────────────────────────────────────────┐
│ Add/Edit Tool                    ×      │
├─────────────────────────────────────────┤
│                                         │
│ Name:                    [____________] │
│                                         │
│ Executable Path:    [...Browse]         │
│                    [C:\Tools\...]       │
│                                         │
│ Short Description:   [______________]   │
│                                         │
│ Long Description:                        │
│ ┌─────────────────────────────────────┐ │
│ │                                     │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ Group:          [▼ System Tools _]      │
│                                         │
│ Home Page:        [https://...]         │
│                                         │
│ Tags:             [network,monitoring]  │
│                                         │
│            [Cancel]        [Save]       │
└─────────────────────────────────────────┘
```

**更新提示对话框**:
```
┌─────────────────────────────────────────┐
│ Updates Available                ×      │
├─────────────────────────────────────────┤
│ 3 tools have updates available          │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ AdvancedRun  v1.50 → v1.51    [↗]  │ │
│ │    Network monitoring utility       │ │
│ ├─────────────────────────────────────┤ │
│ │ Sysmon      v15.0 → v15.1     [↗]  │ │
│ │    System monitoring tool           │ │
│ ├─────────────────────────────────────┤ │
│ │ ProcExp     v17.0 → v17.1     [↗]  │ │
│ │    Process explorer                 │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ [↗] = Open download page                │
│                                         │
│                          [Close]        │
└─────────────────────────────────────────┘
```

---

## 4. 非功能需求

### 4.1 性能

| 指标 | 要求 |
|------|------|
| 启动时间 | < 500ms (加载 300+ 工具) |
| 搜索响应 | < 100ms (防抖后) |
| 内存占用 | < 150MB (含 HelpContent) |
| 导入时间 | < 5s (首次导入 300+ 工具，含 CHM 解析) |

### 4.2 可靠性

| 指标 | 要求 |
|------|------|
| 配置容错 | 某个工具解析失败不影响整体导入 |
| 网络容错 | 更新检测失败不阻塞主流程 |
| 文件校验 | 下载后校验 SHA256，损坏自动回滚 |
| 数据备份 | JSON 修改前自动备份到 .bak 文件，仅保留最近一份 |

### 4.3 兼容性

| 项目 | 要求 |
|------|------|
| Windows 版本 | Windows 10 1809+ |
| 架构支持 | x86、x64、ARM64 |
| NirLauncher | 兼容现有 .nlp 格式 |

---

## 5. 用户故事

### 5.1 工具发现

> 作为开发者，我想通过描述关键词搜索工具，比如输入"网络监控"能找到 NetworkLatencyView。

**验收标准**:
- 搜索"网络"返回所有简述、详述或帮助内容包含"网络"的工具
- 搜索结果按相关度排序
- 支持 100ms 防抖避免频繁搜索

### 5.2 快速启动

> 作为开发者，我想通过全局热键快速唤起 VistaLauncher，输入工具名首字母即可启动。

**验收标准**:
- Ctrl+F2 (可配置) 唤起界面
- 输入"aru"能匹配到 AdvancedRun
- 按回车启动，按 ESC 收起

### 5.3 自动更新

> 作为开发者，我想系统自动检查工具更新，有更新时提醒我，并支持一键更新。

**验收标准**:
- 每周自动检查更新 (可配置)
- 有更新时在状态栏显示图标和数量
- 点击可查看更新列表并选择性更新

### 5.4 添加工具

> 作为开发者，我想手动添加自己的工具到 VistaLauncher，方便统一管理。

**验收标准**:
- 通过"添加工具"按钮打开对话框
- 浏览选择 exe 文件自动填充名称
- 保存后立即出现在搜索结果中

### 5.5 AI 搜索 (Future)

> 作为开发者，我想用自然语言描述需求，比如"查看端口占用情况的工具"，系统能推荐相应工具。

**说明**：此功能为未来扩展方向，当前版本不实现。HelpContent 字段已预留作为语义搜索数据源。

---

## 6. 技术设计

### 6.1 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                        VistaLauncher                        │
├─────────────────────────────────────────────────────────────┤
│  View Layer                                                 │
│  ├── MainWindow.xaml          (主界面)                     │
│  ├── AddToolDialog.xaml       (添加工具对话框)              │
│  ├── UpdateInfoDialog.xaml    (更新提示对话框)              │
│  └── LauncherTheme.xaml       (主题样式)                    │
├─────────────────────────────────────────────────────────────┤
│  ViewModel Layer                                             │
│  └── LauncherViewModel         (MVVM 模式)                  │
├─────────────────────────────────────────────────────────────┤
│  Service Layer                                               │
│  ├── IToolDataService         (工具数据 CRUD)               │
│  ├── ISearchProvider          (搜索抽象)                    │
│  │   └── TextMatchSearchProvider      (文本匹配搜索)        │
│  ├── IVersionCheckService     (版本检查，尽力而为)          │
│  ├── INirLauncherParser       (NirLauncher 解析)           │
│  ├── IHelpContentImporter     (帮助内容导入)                │
│  └── IProcessLauncher         (进程启动)                    │
├─────────────────────────────────────────────────────────────┤
│  Model Layer                                                │
│  ├── ToolItem                 (工具项)                      │
│  ├── ToolGroup                (工具分组)                    │
│  └── ToolsData                (数据根对象)                  │
├─────────────────────────────────────────────────────────────┤
│  Data Layer                                                 │
│  ├── tools.json               (本地数据存储)                │
│  ├── tools.json.bak           (自动备份)                    │
│  └── NirLauncher.cfg/.nlp     (外部配置导入)                │
├─────────────────────────────────────────────────────────────┤
│  External Tool (独立项目)                                    │
│  └── ChmExtractor             (CHM 解析 CLI 工具)           │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 新增服务

#### INirLauncherParser

```csharp
public interface INirLauncherParser
{
    /// <summary>
    /// 解析 NirLauncher .cfg 文件
    /// </summary>
    Task<NirLauncherConfig> ParseConfigAsync(string cfgPath);

    /// <summary>
    /// 解析 NirLauncher .nlp 包文件
    /// </summary>
    Task<NirLauncherPackage> ParsePackageAsync(string nlpPath);

    /// <summary>
    /// 将 NirLauncher 配置转换为 VistaLauncher 数据格式
    /// </summary>
    Task<ToolsData> ConvertToToolsDataAsync(
        NirLauncherConfig config,
        string basePath,
        IProgress<ImportProgress> progress);
}
```

#### IHelpContentImporter

```csharp
public interface IHelpContentImporter
{
    /// <summary>
    /// 从 JSON 文件导入帮助内容（由外部 ChmExtractor 工具生成）
    /// </summary>
    Task ImportFromJsonAsync(string jsonPath, IEnumerable<ToolItem> tools);

    /// <summary>
    /// 从目录批量导入帮助内容（文件名匹配工具名）
    /// </summary>
    Task ImportFromDirectoryAsync(string directory, IEnumerable<ToolItem> tools);
}

// 外部工具生成的帮助内容格式
public class HelpContentEntry
{
    public string ToolName { get; set; } = string.Empty;
    public string HelpFileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

> **注意**：CHM 解析由独立的 `ChmExtractor` CLI 工具完成，主项目只负责导入解析后的结果。

#### IVersionCheckService

```csharp
public interface IVersionCheckService
{
    /// <summary>
    /// 检查单个工具的版本（尽力而为，失败返回 null）
    /// </summary>
    Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量检查版本（失败的工具会被跳过）
    /// </summary>
    Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开工具的下载页面
    /// </summary>
    Task OpenDownloadPageAsync(ToolItem tool);
}

public class VersionCheckResult
{
    public string ToolId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; }  // null 表示无法获取
    public bool HasUpdate => LatestVersion != null && LatestVersion != CurrentVersion;
    public string? DownloadUrl { get; set; }
}
```

> **注意**：版本检查是"尽力而为"的功能，由于各网站格式不统一，可能无法获取所有工具的最新版本。

#### IToolManagementService

```csharp
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
    /// 获取工具
    /// </summary>
    Task<ToolItem?> GetToolAsync(string toolId);

    /// <summary>
    /// 验证工具文件是否存在
    /// </summary>
    Task<bool> ValidateToolAsync(ToolItem tool);
}
```

### 6.3 数据模型扩展

#### ToolItem 扩展字段

```csharp
public partial class ToolItem : ObservableObject
{
    // 现有字段...

    /// <summary>
    /// 帮助文档内容（由外部工具解析后导入，用于搜索）
    /// 来源：IHelpContentImporter 导入
    /// </summary>
    [ObservableProperty]
    private string _helpContent = string.Empty;

    /// <summary>
    /// 帮助文件名（.chm 文件名，用于关联帮助内容）
    /// 来源：NirLauncher .nlp 文件的 help 字段
    /// </summary>
    [ObservableProperty]
    private string _helpFileName = string.Empty;

    /// <summary>
    /// 当前版本号（用户手动维护或从文件属性读取）
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
}
```

**字段来源说明**:

| 字段 | 来源 | 说明 |
|------|------|------|
| HelpContent | 外部导入 | 由 ChmExtractor 工具解析后，通过 IHelpContentImporter 导入 |
| HelpFileName | .nlp 文件 | 从 NirLauncher 配置自动读取 |
| HomepageUrl | .nlp 文件 | 从 NirLauncher 配置的 url 字段读取 |
| Version | 用户/文件属性 | 用户手动填写，或从 exe 文件属性自动读取 |
| LaunchCount | 自动统计 | 每次启动工具时自动 +1 |
| LastLaunchDate | 自动记录 | 每次启动工具时自动更新 |

### 6.4 数据流程

```
NirLauncher.cfg
       │
       ├── 读取包列表 (PackageX)
       │
       ▼
┌─────────────────────────────────────────┐
│     NirSoft.nlp / Sysinternals.nlp      │
│     ├─ [General] 元信息                 │
│     ├─ [GroupX] 分组定义                │
│     └─ [SoftwareX] 工具定义             │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│          INirLauncherParser             │
│          解析并转换数据格式              │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│            tools.json                   │
│     VistaLauncher 独立数据存储           │
│     (不依赖 NirLauncher 配置)            │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│          ISearchProvider                │
│     TextMatchSearchProvider 搜索        │
└─────────────────────────────────────────┘


┌─────────────────────────────────────────┐
│      可选流程：帮助内容导入              │
└─────────────────────────────────────────┘

*.chm 文件
       │
       ▼ (外部独立工具)
┌─────────────────────────────────────────┐
│          ChmExtractor (CLI)             │
│     解析 CHM → help-content.json        │
└─────────────────────────────────────────┘
       │
       ▼ (主项目导入)
┌─────────────────────────────────────────┐
│       IHelpContentImporter              │
│     导入到 ToolItem.HelpContent         │
└─────────────────────────────────────────┘
```

### 6.5 ChmExtractor 独立工具

> **注意**：CHM 解析作为独立的 CLI 工具项目，不包含在 VistaLauncher 主项目中。

**工具职责**:
- 解析 .chm 文件，提取文本内容
- 输出 JSON 格式供主项目导入
- 支持批量处理

**输出格式**:
```json
{
  "generatedAt": "2025-01-15T10:00:00+08:00",
  "entries": [
    {
      "toolName": "AdvancedRun",
      "helpFileName": "AdvancedRun.chm",
      "content": "AdvancedRun is a simple tool..."
    }
  ]
}
```

**技术方案**（供独立项目参考）:
- 使用 `ChmReader` 或 `Microsoft.HtmlHelp` 库
- 提取 CHM 内部 HTML 文件并转换为纯文本

---

## 7. 实施计划

> **说明**：以下阶段按优先级和依赖关系排序，不包含时间估算。

### Phase 1: 基础导入与数据管理 (P0)

- [ ] 实现 INirLauncherParser 接口
- [ ] 实现 .cfg 文件解析
- [ ] 实现 .nlp 文件解析
- [ ] 实现 NirLauncher 到 VistaLauncher 数据转换
- [ ] 扩展 ToolItem 模型（添加 HelpFileName、Version 等字段）
- [ ] 实现本地 JSON 数据存储和加载
- [ ] 添加导入 UI 界面和进度显示

### Phase 2: 搜索增强 (P0)

- [ ] 实现多字段搜索（名称 + 简述 + 详述）
- [ ] 实现搜索结果排序算法（按优先级和使用频率）
- [ ] 优化搜索性能

### Phase 3: 工具管理 (P0)

- [ ] 实现 IToolManagementService 接口
- [ ] 设计并实现添加工具对话框（AddToolDialog）
- [ ] 设计并实现编辑工具对话框
- [ ] 实现工具删除功能
- [ ] 扩展 CommandBar 菜单

### Phase 4: UI 完善 (P0)

- [ ] 扩展 TopBarGrid 添加导入按钮
- [ ] 完善命令栏菜单
- [ ] 添加工具提示（ToolTip）
- [ ] 优化键盘快捷键

### Phase 5: 版本检查 (P1)

- [ ] 实现 IVersionCheckService 接口
- [ ] 实现 NirSoft 版本解析规则（尽力而为）
- [ ] 添加版本提示 UI
- [ ] 实现"打开下载页"功能

### Phase 6: 帮助内容导入 (P1，可选)

> 依赖外部 ChmExtractor 工具先完成

- [ ] 实现 IHelpContentImporter 接口
- [ ] 扩展搜索支持 HelpContent 字段
- [ ] 添加帮助内容导入 UI

### Phase 7: AI 搜索 (Future)

> 未来扩展方向，不在当前版本范围内

- [ ] 设计 AI 搜索方案
- [ ] 实现语义搜索功能

---

## 8. 验收标准

### 8.1 功能验收

| ID | 验收项 | 标准 |
|----|--------|------|
| A01 | 导入成功率 | NirSoft 工具 100% 导入（解析失败不阻塞整体） |
| A02 | 搜索准确性 | 输入"network"返回所有网络相关工具 |
| A03 | 搜索性能 | 300 工具搜索响应 < 100ms |
| A04 | 数据持久化 | 重启后数据完整保留 |
| A05 | 添加工具 | 成功添加并搜索到新工具 |
| A06 | 编辑/删除 | 编辑和删除操作正常工作 |
| A07 | 版本检查 | 点击检查版本能获取部分工具的版本信息 |
| A08 | 打开下载页 | 点击后正确跳转到工具官网 |
| A09 | 帮助内容导入 | （可选）成功导入外部解析的帮助内容 |

### 8.2 性能验收

| 指标 | 目标 | 测量方法 |
|------|------|----------|
| 冷启动时间 | < 500ms | 从进程启动到界面可交互 |
| 搜索延迟 | < 100ms | 从输入停止到结果显示 |
| 内存占用 | < 150MB | 稳定状态下的内存使用 |
| 导入时间 | < 3s | 首次导入 300 工具（不含帮助内容） |

### 8.3 UI/UX 验收

| 项目 | 标准 |
|------|------|
| 按钮位置 | 符合现有 UI 风格，直观易用 |
| 图标一致 | 使用 Fluent UI 图标系统 |
| 键盘导航 | 所有功能支持键盘操作 |
| 进度反馈 | 长时间操作显示进度 |

---

## 9. 风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| NirLauncher 配置格式变更 | 高 | 低 | 使用版本检测，格式变化时提示用户 |
| 版本检测解析失败 | 低 | 高 | 标记为"尽力而为"功能，失败不影响使用 |
| OneDrive 同步冲突 | 低 | 高 | 文件锁 + 重试机制 |
| JSON 数据损坏 | 中 | 低 | 自动备份 + 损坏恢复 |
| 帮助内容导入失败 | 低 | 中 | 可选功能，不影响核心功能 |

---

## 10. 附录

### 10.1 NirLauncher 目录结构

```
C:\Users\frend\OneDrive\Softs\开发工具\NirLauncher\
├── NirLauncher.cfg              # 主配置文件
├── NirSoft\
│   ├── nirsoft.nlp              # NirSoft 工具包配置
│   ├── *.exe                    # NirSoft 工具 (x86)
│   ├── x64\*.exe                # NirSoft 工具 (x64)
│   └── *.chm                    # 帮助文档（由 ChmExtractor 独立解析）
└── SysinternalsSuit\
    ├── sysinternals*.nlp        # Sysinternals 工具包配置
    ├── *.exe                    # Sysinternals 工具
    └── *.chm                    # 帮助文档（由 ChmExtractor 独立解析）
```

### 10.2 本地数据结构

```
%AppData%\Roaming\VistaLauncher\
├── tools.json                   # 主数据文件
├── tools.json.bak               # 自动备份
├── config.json                  # 应用配置 (可选)
└── cache\                       # 缓存目录
    └── icons\                   # 图标缓存
```

### 10.3 UI 图标参考

| 功能 | Fluent Icon | Glyph |
|------|-------------|-------|
| 导入 | Import | &#xF8E6; |
| 添加 | Add | &#xE710; |
| 编辑 | Edit | &#xE70F; |
| 删除 | Delete | &#xE74D; |
| 更新 | Sync / Update | &#xE895; / &#xE72C; |
| 设置 | Settings | &#xE713; |
| 更多 | More | &#xE10C; |
| 保存 | Save | &#xE74E; |
| 取消 | Cancel | &#xE711; |

### 10.4 参考

- NirLauncher 官网: https://www.nirsoft.net/utils/nir_launcher.html
- Sysinternals: https://learn.microsoft.com/en-us/sysinternals/
- 现有数据模型设计: `data-model-design.md`
- Fluent UI 图标: https://fluentui.microsoft.com/

---

## 11. 变更记录

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2025-01-15 | 初始版本 |
| v1.1 | 2025-01-15 | 新增本地 JSON 数据管理、工具添加/编辑、自动更新、CHM 解析功能 |
| v1.2 | 2025-01-15 | 重构：CHM 解析移至独立项目；简化 AI 搜索为未来扩展；自动更新改为版本提示；移除时间估算；明确数据字段来源 |
| v1.3 | 2025-01-15 | 补充 ARM64 支持；统一 HomepageUrl 字段；完善数据校验策略和备份策略说明 |

---

*文档结束*
