# NirSoft 工具更新方案

## 概述

NirSoft 是一个提供 200+ 免费 Windows 实用工具的网站。本文档描述如何在 VistaLauncher 中实现 NirSoft 工具的自动更新检查和下载。

## 核心发现

### PAD (Portable Application Description) 文件

NirSoft 为每个工具提供标准化的 PAD XML 文件，包含版本信息和下载链接。

- **PAD 索引文件**: https://www.nirsoft.net/pad/pad-links.txt
- **单个 PAD 文件**: `https://www.nirsoft.net/pad/{toolname}.xml`

### PAD XML 关键字段

```xml
<Program_Info>
    <Program_Name>HashMyFiles</Program_Name>
    <Program_Version>2.50</Program_Version>
    <Program_Release_Month>12</Program_Release_Month>
    <Program_Release_Day>08</Program_Release_Day>
    <Program_Release_Year>2024</Program_Release_Year>
</Program_Info>

<Web_Info>
    <Application_URLs>
        <Application_Info_URL>https://www.nirsoft.net/utils/hash_my_files.html</Application_Info_URL>
        <Application_XML_File_URL>https://www.nirsoft.net/pad/hashmyfiles.xml</Application_XML_File_URL>
    </Application_URLs>
    <Download_URLs>
        <Primary_Download_URL>https://www.nirsoft.net/utils/hashmyfiles.zip</Primary_Download_URL>
    </Download_URLs>
</Web_Info>
```

| 字段 | 路径 | 用途 |
|------|------|------|
| 版本号 | `Program_Info/Program_Version` | 检查是否有更新 |
| 发布日期 | `Program_Info/Program_Release_*` | 显示更新日期 |
| 工具页面 | `Web_Info/Application_URLs/Application_Info_URL` | 跳转到详情页 |
| 下载链接 | `Web_Info/Download_URLs/Primary_Download_URL` | 32位下载地址 |
| PAD URL | `Web_Info/Application_URLs/Application_XML_File_URL` | 自引用 |

## 下载链接规则

### URL 格式

NirSoft 工具下载链接有两种路径：

| 类型 | URL 格式 |
|------|----------|
| 常规工具 | `https://www.nirsoft.net/utils/{name}.zip` |
| 密码工具 | `https://www.nirsoft.net/toolsdownload/{name}.zip` |

### 64 位版本推导规则（已验证）

```
32位 URL: https://www.nirsoft.net/utils/{name}.zip
64位 URL: https://www.nirsoft.net/utils/{name}-x64.zip
```

**规则**: 将 `.zip` 替换为 `-x64.zip`

**验证结果** (2026-01-27):

| 工具 | 版本 | 64位 URL |
|------|------|----------|
| hashmyfiles | 2.50 | ✅ OK |
| bluescreenview | 1.55 | ✅ OK |
| advancedrun | 1.51 | ✅ OK |
| regscanner | 2.75 | ✅ OK |
| searchmyfiles | 3.35 | ✅ OK |
| cports | 2.77 | ✅ OK |
| webbrowserpassview | 2.18 | ✅ OK (toolsdownload 路径) |
| chromepass | 1.63 | ✅ OK (toolsdownload 路径) |

## 实现方案

### 数据模型扩展

在 `ToolItem` 中添加 `padUrl` 字段：

```csharp
/// <summary>
/// PAD (Portable Application Description) 文件 URL
/// 用于 NirSoft 等支持 PAD 标准的工具更新检查
/// </summary>
[ObservableProperty]
[property: JsonPropertyName("padUrl")]
private string? _padUrl;
```

### tools.json 示例

```json
{
  "id": "hashmyfiles",
  "name": "HashMyFiles",
  "executablePath": "D:\\softs\\NirSoft\\x64\\HashMyFiles.exe",
  "version": "2.44",
  "architecture": "x64",
  "updateSource": "NirSoft",
  "padUrl": "https://www.nirsoft.net/pad/hashmyfiles.xml"
}
```

### 更新检查流程

```
┌─────────────────────────────────────────────────────────────┐
│                    检查更新流程                              │
├─────────────────────────────────────────────────────────────┤
│  1. 读取 tool.padUrl                                        │
│  2. 下载 PAD XML 文件                                       │
│  3. 解析 <Program_Version> 获取最新版本                     │
│  4. 与本地 tool.version 对比                                │
│  5. 如有更新，填充 tool.updateInfo:                         │
│     - version: 最新版本号                                   │
│     - downloadUrl: 根据架构选择 32/64 位链接                │
│     - releaseDate: 从 PAD 解析发布日期                      │
│     - infoUrl: 工具详情页                                   │
└─────────────────────────────────────────────────────────────┘
```

### 下载更新流程

```
┌─────────────────────────────────────────────────────────────┐
│                    下载更新流程                              │
├─────────────────────────────────────────────────────────────┤
│  1. 获取 downloadUrl (根据 architecture 选择 32/64 位)      │
│  2. 下载 ZIP 文件到临时目录                                 │
│  3. 解压 ZIP 文件                                           │
│  4. 备份原有 exe 文件                                       │
│  5. 复制新 exe 到目标目录                                   │
│  6. 更新 tool.version                                       │
│  7. 清理临时文件                                            │
└─────────────────────────────────────────────────────────────┘
```

### 版本检查服务接口

```csharp
public interface IVersionCheckService
{
    /// <summary>
    /// 检查单个工具的更新
    /// </summary>
    Task<UpdateInfo?> CheckUpdateAsync(ToolItem tool, CancellationToken ct = default);

    /// <summary>
    /// 批量检查更新
    /// </summary>
    Task<IReadOnlyList<(ToolItem Tool, UpdateInfo Update)>> CheckUpdatesAsync(
        IEnumerable<ToolItem> tools,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
```

### NirSoft PAD 解析实现要点

```csharp
public class NirSoftVersionChecker
{
    public async Task<UpdateInfo?> CheckFromPadAsync(string padUrl, Architecture arch)
    {
        // 1. 下载 PAD XML
        var xml = await DownloadXmlAsync(padUrl);

        // 2. 解析版本信息
        var version = xml.SelectSingleNode("//Program_Version")?.InnerText;
        var primaryUrl = xml.SelectSingleNode("//Primary_Download_URL")?.InnerText;

        // 3. 根据架构推导下载链接
        var downloadUrl = arch == Architecture.x64
            ? primaryUrl?.Replace(".zip", "-x64.zip")
            : primaryUrl;

        // 4. 解析发布日期
        var year = xml.SelectSingleNode("//Program_Release_Year")?.InnerText;
        var month = xml.SelectSingleNode("//Program_Release_Month")?.InnerText;
        var day = xml.SelectSingleNode("//Program_Release_Day")?.InnerText;

        return new UpdateInfo
        {
            Version = version,
            DownloadUrl = downloadUrl,
            ReleaseDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day)),
            InfoUrl = xml.SelectSingleNode("//Application_Info_URL")?.InnerText
        };
    }
}
```

## 批量填充 padUrl

### exe 名称到 PAD URL 的映射

大多数 NirSoft 工具的 PAD 文件名与 exe 文件名一致（小写）：

```
HashMyFiles.exe  →  https://www.nirsoft.net/pad/hashmyfiles.xml
BlueScreenView.exe  →  https://www.nirsoft.net/pad/bluescreenview.xml
```

### 映射脚本思路

```powershell
# 1. 下载 pad-links.txt 获取所有 PAD URL
$padLinks = (Invoke-WebRequest 'https://www.nirsoft.net/pad/pad-links.txt').Content -split "`n"

# 2. 构建 PAD 名称 → URL 的字典
$padMap = @{}
foreach ($link in $padLinks) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($link.Trim())
    $padMap[$name.ToLower()] = $link.Trim()
}

# 3. 遍历 tools.json，为 NirSoft 工具匹配 padUrl
foreach ($tool in $tools) {
    $exeName = [System.IO.Path]::GetFileNameWithoutExtension($tool.executablePath)
    $padUrl = $padMap[$exeName.ToLower()]
    if ($padUrl) {
        $tool.padUrl = $padUrl
        $tool.updateSource = "NirSoft"
    }
}
```

## 注意事项

1. **网络请求限流**: 批量检查更新时应添加延迟，避免被服务器封禁
2. **缓存策略**: PAD 文件可缓存 24 小时，减少重复请求
3. **版本比较**: 使用 `Version.Parse()` 或自定义比较逻辑处理版本号
4. **错误处理**: PAD 文件可能不存在或格式变化，需要优雅降级
5. **64 位检测**: 部分工具可能没有 64 位版本，需要 fallback 到 32 位

## 相关文件

- 模型: `src/VistaLauncher/VistaLauncher/Models/ToolItem.cs`
- 枚举: `src/VistaLauncher/VistaLauncher/Models/Enums.cs` (UpdateSource.NirSoft)
- 更新信息: `src/VistaLauncher/VistaLauncher/Models/UpdateInfo.cs`
- 配置文件: `%LocalAppData%\Packages\...\LocalCache\Roaming\VistaLauncher\tools.json`

## 参考链接

- NirSoft 官网: https://www.nirsoft.net
- PAD 索引: https://www.nirsoft.net/pad/pad-links.txt
- PAD 标准: http://www.asp-shareware.org/pad
