# VistaLauncher 数据模型设计文档

> 基于 NirLauncher 配置结构分析，结合软件更新功能需求的设计方案
>
> 文档版本: v1.0
> 创建日期: 2025-01-15

---

## 1. 设计目标

1. 参考 NirLauncher 的成熟配置结构，保持良好的兼容性
2. 支持多架构 (x86/x64/ARM64) 可执行文件
3. 内置软件更新检查和下载功能
4. 支持工具包管理模式（类似 NirLauncher 的 .nlp 包）
5. 保持数据结构的可扩展性

---

## 2. 数据结构概览

```
ToolsData (根对象)
├── MetaInfo          // 元信息
├── Groups            // 分组列表
├── Tools             // 工具列表
└── Packages          // 工具包列表（可选）
```

---

## 3. 详细数据结构

### 3.1 MetaInfo - 元信息

```csharp
public class MetaInfo
{
    public string Name { get; set; }           // 工具包名称
    public string Version { get; set; }        // 配置版本号
    public int GroupCount { get; set; }        // 分组总数
    public int ToolCount { get; set; }         // 工具总数
    public string? HomePage { get; set; }      // 主页地址
    public DateTime? LastUpdateCheck { get; set; }  // 最后检查更新时间
}
```

**对应 NirSoft 字段**: `[General]` 部分

---

### 3.2 ToolGroup - 分组

```csharp
public class ToolGroup
{
    public string Id { get; set; }             // 分组唯一标识
    public string Name { get; set; }           // 分组显示名称
    public string? Icon { get; set; }          // 分组图标（可选）
    public int SortOrder { get; set; }         // 排序顺序
    public bool IsShowAll { get; set; }        // 是否为"全部"虚拟分组
    public string? Description { get; set; }   // 分组描述
}
```

**对应 NirSoft 字段**: `[GroupX]` 部分

---

### 3.3 UpdateInfo - 更新信息

```csharp
public class UpdateInfo
{
    /// <summary>
    /// 最新版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 直接下载链接
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// 发布信息页面（用于检查更新）
    /// </summary>
    public string? InfoUrl { get; set; }

    /// <summary>
    /// SHA256 校验和（用于验证下载完整性）
    /// </summary>
    public string? Sha256Checksum { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// 是否需要管理员权限安装
    /// </summary>
    public bool? RequiresAdmin { get; set; }

    /// <summary>
    /// 更新日志 / 发布说明
    /// </summary>
    public string? ReleaseNotes { get; set; }

    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// 是否为强制更新
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// 最低兼容版本（用于版本兼容性检查）
    /// </summary>
    public string? MinCompatibleVersion { get; set; }
}
```

---

### 3.4 ToolItem - 工具项

```csharp
public class ToolItem
{
    // ==================== 基础信息 ====================

    /// <summary>
    /// 唯一标识符（GUID 或自定义 ID）
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 简短描述（一行，用于列表显示）
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// 详细描述（多行，用于详情页）
    /// </summary>
    public string? LongDescription { get; set; }


    // ==================== 文件路径 ====================

    /// <summary>
    /// 可执行文件名（相对于 BasePath）
    /// </summary>
    public string ExeFileName { get; set; }

    /// <summary>
    /// 64位版本相对路径（空表示无64位版本）
    /// </summary>
    public string? Exe64Path { get; set; }

    /// <summary>
    /// 32位版本相对路径（空表示无32位版本）
    /// </summary>
    public string? Exe86Path { get; set; }

    /// <summary>
    /// ARM64 版本相对路径
    /// </summary>
    public string? ExeArm64Path { get; set; }

    /// <summary>
    /// 帮助文档文件名（.chm, .pdf, .html 等）
    /// </summary>
    public string? HelpFile { get; set; }

    /// <summary>
    /// 工具包基础路径（绝对路径或相对于配置文件的路径）
    /// </summary>
    public string BasePath { get; set; }


    // ==================== 在线信息 ====================

    /// <summary>
    /// 官方网址
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// 在线图标地址（用于动态加载图标）
    /// </summary>
    public string? IconUrl { get; set; }


    // ==================== 版本与更新 ====================

    /// <summary>
    /// 当前安装版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 软件主页（用于检查更新）
    /// </summary>
    public string? HomePage { get; set; }

    /// <summary>
    /// 更新信息
    /// </summary>
    public UpdateInfo? UpdateInfo { get; set; }

    /// <summary>
    /// 最后检查更新的时间
    /// </summary>
    public DateTime? LastUpdateCheck { get; set; }


    // ==================== 分类与属性 ====================

    /// <summary>
    /// 所属分组 ID
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// 标签（逗号分隔，如 "network,monitoring,port"）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 是否为命令行工具（无 GUI）
    /// </summary>
    public bool IsConsoleApp { get; set; }

    /// <summary>
    /// 是否需要管理员权限运行
    /// </summary>
    public bool RequiresAdmin { get; set; }

    /// <summary>
    /// 是否为便携版（无需安装）
    /// </summary>
    public bool IsPortable { get; set; }

    /// <summary>
    /// 语言（如 "en-US", "zh-CN"）
    /// </summary>
    public string? Language { get; set; }


    // ==================== 使用统计 ====================

    /// <summary>
    /// 启动次数
    /// </summary>
    public int LaunchCount { get; set; }

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastLaunchDate { get; set; }


    // ==================== 其他 ====================

    /// <summary>
    /// 用户自定义排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用（false 则隐藏但不删除）
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 扩展数据（JSON 格式，用于存储自定义字段）
    /// </summary>
    public string? ExtendedData { get; set; }
}
```

**对应 NirSoft 字段**: `[SoftwareX]` 部分

---

### 3.5 ToolPackage - 工具包（可选，用于管理第三方工具集）

```csharp
public class ToolPackage
{
    public string Id { get; set; }             // 包唯一标识
    public string Name { get; set; }           // 包名称
    public string? Description { get; set; }   // 包描述
    public string Version { get; set; }        // 包版本
    public string Author { get; set; }         // 作者
    public string HomePage { get; set; }       // 主页
    public string PackageFile { get; set; }    // .nlp 包文件路径
    public string BasePath { get; set; }       // 工具基础目录
    public List<string> ToolIds { get; set; }  // 包含的工具 ID 列表
    public UpdateInfo? UpdateInfo { get; set; }  // 包更新信息
    public DateTime? LastUpdateCheck { get; set; }
    public bool IsEnabled { get; set; }        // 是否启用
}
```

---

## 4. 配置文件格式

### 4.1 主配置文件 (tools.json)

```json
{
  "MetaInfo": {
    "Name": "VistaLauncher Tools",
    "Version": "1.0",
    "GroupCount": 5,
    "ToolCount": 20,
    "HomePage": "https://github.com/yourusername/VistaLauncher",
    "LastUpdateCheck": "2025-01-15T10:30:00+08:00"
  },
  "Groups": [
    {
      "Id": "password-recovery",
      "Name": "Password Recovery Utilities",
      "Icon": null,
      "SortOrder": 0,
      "IsShowAll": false,
      "Description": "密码恢复工具"
    },
    {
      "Id": "network-tools",
      "Name": "Network Monitoring Tools",
      "SortOrder": 1
    }
  ],
  "Tools": [
    {
      "Id": "advrun-001",
      "Name": "AdvancedRun",
      "ShortDescription": "Run a program with different settings that you choose.",
      "LongDescription": "AdvancedRun is a simple tool for Windows...",
      "ExeFileName": "AdvancedRun.exe",
      "Exe64Path": "x64\\AdvancedRun.exe",
      "Exe86Path": null,
      "ExeArm64Path": null,
      "HelpFile": "AdvancedRun.chm",
      "BasePath": "C:\\Tools\\NirSoft",
      "WebsiteUrl": "https://www.nirsoft.net/utils/advanced_run.html",
      "IconUrl": null,
      "Version": "1.50",
      "HomePage": "https://www.nirsoft.net/utils/advanced_run.html",
      "UpdateInfo": {
        "Version": "1.51",
        "DownloadUrl": "https://www.nirsoft.net/utils/advancedrun.zip",
        "InfoUrl": "https://www.nirsoft.net/utils/advanced_run.html",
        "Sha256Checksum": "abc123...",
        "FileSize": 102400,
        "ReleaseNotes": "- Added new feature...",
        "ReleaseDate": "2025-01-10T00:00:00Z"
      },
      "LastUpdateCheck": "2025-01-15T10:30:00+08:00",
      "GroupId": "system-tools",
      "Tags": "system,launcher,advanced",
      "IsConsoleApp": false,
      "RequiresAdmin": false,
      "IsPortable": true,
      "Language": "en-US",
      "LaunchCount": 15,
      "LastLaunchDate": "2025-01-14T15:20:00+08:00",
      "SortOrder": 0,
      "IsEnabled": true,
      "IsFavorite": false
    }
  ],
  "Packages": []
}
```

---

## 5. 更新检查流程设计

```
┌────────────────────────���────────────────────────────────────┐
│                      更新检查流程                            │
└─────────────────────────────────────────────────────────────┘

1. 触发更新检查
   │
   ├── 用户手动点击"检查更新"
   └── 定时自动检查（可配置间隔）

2. 遍历所有工具
   │
   ├── 有 UpdateInfo.DownloadUrl
   │   └── 直接下载版本信息或解析下载页
   │
   ├── 有 HomePage 但无 DownloadUrl
   │   └── 根据 HomePage 解析最新版本
   │       - 使用预定义的解析规则
   │       - 或使用通用 HTML 解析器
   │
   └── 两者都无
       └── 跳过

3. 版本对比
   │
   └── 对比 Version 字段与获取到的最新版本

4. 生成更新列表
   │
   └── 汇总所有有新版本的工具

5. 用户确认下载
   │
   ├── 显示更新列表（版本号、文件大小、更新日志）
   ├── 用户选择要更新的工具
   └── 开始下载

6. 下载与安装
   │
   ├── 下载更新包（支持断点续传）
   ├── SHA256 校验
   ├── 备份原文件
   ├── 解压/替换文件
   └── 更新 Version 字段
```

---

## 6. 版本解析规则示例

为了从 HomePage 自动获取版本信息，可定义解析规则：

```json
{
  "VersionPatterns": [
    {
      "UrlPattern": "nirsoft.net",
      "VersionRegex": "<b>v([\\d.]+)</b>",
      "DownloadUrlRegex": "Download ([\\w-]+\\.zip)"
    },
    {
      "UrlPattern": "github.com",
      "VersionRegex": "/releases/tag/v([\\d.]+)",
      "DownloadUrlRegex": "href=\"(.*?[\\d.]+\\.zip)\""
    }
  ]
}
```

---

## 7. 与 NirLauncher 兼容性

支持导入 NirLauncher 的 `.nlp` 配置文件：

| NirSoft 字段 | VistaLauncher 字段 |
|-------------|-------------------|
| `[General].Name` | `MetaInfo.Name` |
| `[GroupX].Name` | `Groups[].Name` |
| `[SoftwareX].exe` | `Tools[].ExeFileName` |
| `[SoftwareX].exe64` | `Tools[].Exe64Path` |
| `[SoftwareX].AppName` | `Tools[].Name` |
| `[SoftwareX].ShortDesc` | `Tools[].ShortDescription` |
| `[SoftwareX].LongDesc` | `Tools[].LongDescription` |
| `[SoftwareX].url` | `Tools[].WebsiteUrl` |
| `[SoftwareX].group` | `Tools[].GroupId` |
| `[SoftwareX].help` | `Tools[].HelpFile` |

---

## 8. 未来扩展考虑

1. **多语言支持**: `LocalizedNames` 字典，支持多语言显示名称
2. **依赖关系**: `Dependencies` 列表，定义工具间依赖
3. **快捷方式**: 支持创建桌面快捷方式的配置
4. **命令行参数**: `DefaultArguments` 默认启动参数
5. **环境变量**: `EnvironmentVariables` 启动时设置的环境变量
6. **自动启动**: `AutoStart` 是否随系统启动
7. **热键绑定**: `Hotkey` 全局热键设置
8. **评分系统**: `Rating` 用户评分，`Downloads` 下载次数

---

## 9. 附录：JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "VistaLauncher Tools Data",
  "type": "object",
  "properties": {
    "MetaInfo": { "$ref": "#/definitions/MetaInfo" },
    "Groups": {
      "type": "array",
      "items": { "$ref": "#/definitions/ToolGroup" }
    },
    "Tools": {
      "type": "array",
      "items": { "$ref": "#/definitions/ToolItem" }
    },
    "Packages": {
      "type": "array",
      "items": { "$ref": "#/definitions/ToolPackage" }
    }
  },
  "required": ["MetaInfo", "Groups", "Tools"],
  "definitions": {
    "MetaInfo": {
      "type": "object",
      "properties": {
        "Name": { "type": "string" },
        "Version": { "type": "string" },
        "GroupCount": { "type": "integer" },
        "ToolCount": { "type": "integer" },
        "HomePage": { "type": "string", "nullable": true },
        "LastUpdateCheck": { "type": "string", "format": "date-time", "nullable": true }
      },
      "required": ["Name", "Version", "GroupCount", "ToolCount"]
    },
    "ToolGroup": {
      "type": "object",
      "properties": {
        "Id": { "type": "string" },
        "Name": { "type": "string" },
        "Icon": { "type": "string", "nullable": true },
        "SortOrder": { "type": "integer" },
        "IsShowAll": { "type": "boolean" },
        "Description": { "type": "string", "nullable": true }
      },
      "required": ["Id", "Name"]
    },
    "UpdateInfo": {
      "type": "object",
      "properties": {
        "Version": { "type": "string", "nullable": true },
        "DownloadUrl": { "type": "string", "nullable": true },
        "InfoUrl": { "type": "string", "nullable": true },
        "Sha256Checksum": { "type": "string", "nullable": true },
        "FileSize": { "type": "integer", "nullable": true },
        "RequiresAdmin": { "type": "boolean", "nullable": true },
        "ReleaseNotes": { "type": "string", "nullable": true },
        "ReleaseDate": { "type": "string", "format": "date-time", "nullable": true },
        "IsMandatory": { "type": "boolean" },
        "MinCompatibleVersion": { "type": "string", "nullable": true }
      }
    },
    "ToolItem": {
      "type": "object",
      "properties": {
        "Id": { "type": "string" },
        "Name": { "type": "string" },
        "ShortDescription": { "type": "string", "nullable": true },
        "LongDescription": { "type": "string", "nullable": true },
        "ExeFileName": { "type": "string" },
        "Exe64Path": { "type": "string", "nullable": true },
        "Exe86Path": { "type": "string", "nullable": true },
        "ExeArm64Path": { "type": "string", "nullable": true },
        "HelpFile": { "type": "string", "nullable": true },
        "BasePath": { "type": "string" },
        "WebsiteUrl": { "type": "string", "nullable": true },
        "IconUrl": { "type": "string", "nullable": true },
        "Version": { "type": "string", "nullable": true },
        "HomePage": { "type": "string", "nullable": true },
        "UpdateInfo": { "$ref": "#/definitions/UpdateInfo" },
        "LastUpdateCheck": { "type": "string", "format": "date-time", "nullable": true },
        "GroupId": { "type": "string" },
        "Tags": { "type": "string", "nullable": true },
        "IsConsoleApp": { "type": "boolean" },
        "RequiresAdmin": { "type": "boolean" },
        "IsPortable": { "type": "boolean" },
        "Language": { "type": "string", "nullable": true },
        "LaunchCount": { "type": "integer" },
        "LastLaunchDate": { "type": "string", "format": "date-time", "nullable": true },
        "SortOrder": { "type": "integer" },
        "IsEnabled": { "type": "boolean" },
        "IsFavorite": { "type": "boolean" },
        "ExtendedData": { "type": "string", "nullable": true }
      },
      "required": ["Id", "Name", "ExeFileName", "BasePath", "GroupId"]
    },
    "ToolPackage": {
      "type": "object",
      "properties": {
        "Id": { "type": "string" },
        "Name": { "type": "string" },
        "Description": { "type": "string", "nullable": true },
        "Version": { "type": "string" },
        "Author": { "type": "string" },
        "HomePage": { "type": "string" },
        "PackageFile": { "type": "string" },
        "BasePath": { "type": "string" },
        "ToolIds": { "type": "array", "items": { "type": "string" } },
        "UpdateInfo": { "$ref": "#/definitions/UpdateInfo" },
        "LastUpdateCheck": { "type": "string", "format": "date-time", "nullable": true },
        "IsEnabled": { "type": "boolean" }
      },
      "required": ["Id", "Name", "Version", "Author", "HomePage", "PackageFile", "BasePath"]
    }
  }
}
```

---

*文档结束*
