# VistaLauncher 工具升级功能实现方案

> 工具版本检查、自动更新、管理增强和数据迁移的完整实现方案
>
> 文档版本: v1.0
> 创建日期: 2025-01-22
> 作者: VistaLauncher Team

---

## 1. 概述

### 1.1 功能目标

本实现方案旨在为 VistaLauncher 添加完整的工具升级管理功能：

| 功能模块 | 描述 | 优先级 |
|---------|------|-------|
| **版本检查** | 检查工具是否有新版本可用，显示更新提示 | P0 |
| **自动更新** | 自动下载并安装工具的新版本 | P1 |
| **工具管理增强** | 为工具添加更多元数据（下载链接、发布页等） | P0 |
| **数据迁移** | tools.json 数据格式版本迁移和兼容性处理 | P0 |

### 1.2 现状分析

#### 已有基础
- `ToolItem.Version` 字段已存在
- `ToolItem.HomepageUrl` 字段已存在
- 完整的 MVVM 架构和服务层
- JSON 序列化使用 Source Generator 优化

#### 需要新增
- 版本比较服务
- HTTP 网络请求能力
- 文件下载和解压功能
- 更新信息存储结构
- 数据迁移机制

---

## 2. 数据模型扩展

### 2.1 UpdateInfo - 更新信息模型

```csharp
// 文件: Models/UpdateInfo.cs

namespace VistaLauncher.Models;

/// <summary>
/// 工具更新信息
/// </summary>
public partial class UpdateInfo : ObservableObject
{
    /// <summary>
    /// 最新版本号
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("version")]
    private string _version = string.Empty;

    /// <summary>
    /// 直接下载链接
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("downloadUrl")]
    private string? _downloadUrl;

    /// <summary>
    /// 发布信息页面（用于检查更新）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("infoUrl")]
    private string? _infoUrl;

    /// <summary>
    /// SHA256 校验和（用于验证下载完整性）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("sha256Checksum")]
    private string? _sha256Checksum;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("fileSize")]
    private long? _fileSize;

    /// <summary>
    /// 是否需要管理员权限安装
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("requiresAdmin")]
    private bool _requiresAdmin;

    /// <summary>
    /// 更新日志 / 发布说明
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("releaseNotes")]
    private string? _releaseNotes;

    /// <summary>
    /// 发布日期
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("releaseDate")]
    private DateTime? _releaseDate;

    /// <summary>
    /// 是否为强制更新
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("isMandatory")]
    private bool _isMandatory;

    /// <summary>
    /// 最低兼容版本（用于版本兼容性检查）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("minCompatibleVersion")]
    private string? _minCompatibleVersion;
}
```

### 2.2 ToolItem 扩展

```csharp
// 文件: Models/ToolItem.cs
// 新增字段

/// <summary>
/// 更新信息（缓存的最新版本数据）
/// </summary>
[ObservableProperty]
[property: JsonPropertyName("updateInfo")]
private UpdateInfo? _updateInfo;

/// <summary>
/// 最后检查更新的时间
/// </summary>
[ObservableProperty]
[property: JsonPropertyName("lastUpdateCheck")]
private DateTime? _lastUpdateCheck;

/// <summary>
/// 更新源类型（用于确定版本检查方式）
/// </summary>
[ObservableProperty]
[property: JsonPropertyName("updateSource")]
private UpdateSource _updateSource = UpdateSource.None;

/// <summary>
/// 是否跳过此工具的更新检查
/// </summary>
[ObservableProperty]
[property: JsonPropertyName("skipUpdateCheck")]
private bool _skipUpdateCheck;

/// <summary>
/// 是否有可用更新
/// </summary>
[JsonIgnore]
public bool HasUpdate => UpdateInfo != null &&
                         !string.IsNullOrEmpty(UpdateInfo.Version) &&
                         UpdateInfo.Version != Version;
```

### 2.3 UpdateSource 枚举

```csharp
// 文件: Models/Enums.cs
// 新增枚举

/// <summary>
/// 更新源类型
/// </summary>
public enum UpdateSource
{
    /// <summary>
    /// 无更新源
    /// </summary>
    None,

    /// <summary>
    /// NirSoft 工具（从 nirsoft.net 页面解析）
    /// </summary>
    NirSoft,

    /// <summary>
    /// Sysinternals 工具（从微软官网获取）
    /// </summary>
    Sysinternals,

    /// <summary>
    /// GitHub Release（从 GitHub API 获取）
    /// </summary>
    GitHub,

    /// <summary>
    /// 自定义 URL（通用 HTML 解析）
    /// </summary>
    Custom,

    /// <summary>
    /// 工具包级别更新（如 NirLauncher 整包）
    /// </summary>
    Package
}
```

### 2.4 ToolsData 扩展

```csharp
// 文件: Models/ToolsData.cs
// 新增字段

/// <summary>
/// 数据格式版本（用于迁移）
/// </summary>
public string DataFormatVersion { get; set; } = "2.0";

/// <summary>
/// 最后全局更新检查时间
/// </summary>
public DateTime? LastGlobalUpdateCheck { get; set; }

/// <summary>
/// 自动更新配置
/// </summary>
public UpdateConfig UpdateConfig { get; set; } = new();
```

### 2.5 UpdateConfig - 更新配置

```csharp
// 文件: Models/UpdateConfig.cs

namespace VistaLauncher.Models;

/// <summary>
/// 自动更新配置
/// </summary>
public class UpdateConfig
{
    /// <summary>
    /// 是否启用自动检查更新
    /// </summary>
    public bool AutoCheckEnabled { get; set; } = true;

    /// <summary>
    /// 自动检查间隔（小时）
    /// </summary>
    public int CheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// 是否自动下载更新
    /// </summary>
    public bool AutoDownloadEnabled { get; set; } = false;

    /// <summary>
    /// 是否包含预发布版本
    /// </summary>
    public bool IncludePrerelease { get; set; } = false;

    /// <summary>
    /// 每次最多检查的工具数量（避免过度请求）
    /// </summary>
    public int MaxConcurrentChecks { get; set; } = 5;
}
```

### 2.6 JsonContext 更新

```csharp
// 文件: Models/JsonContext.cs
// 添加新类型

[JsonSerializable(typeof(UpdateInfo))]
[JsonSerializable(typeof(UpdateConfig))]
[JsonSerializable(typeof(List<UpdateInfo>))]
internal partial class JsonContext : JsonSerializerContext
{
    // 现有类型...
}
```

---

## 3. 服务层设计

### 3.1 IVersionCheckService - 版本检查服务

```csharp
// 文件: Services/IVersionCheckService.cs

namespace VistaLauncher.Services;

/// <summary>
/// 版本检查结果
/// </summary>
public class VersionCheckResult
{
    public required string ToolId { get; init; }
    public required string ToolName { get; init; }
    public string CurrentVersion { get; init; } = string.Empty;
    public string? LatestVersion { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public long? FileSize { get; init; }
    public bool HasUpdate => !string.IsNullOrEmpty(LatestVersion) &&
                            LatestVersion != CurrentVersion;
    public bool CheckFailed { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 批量检查进度
/// </summary>
public class CheckProgress
{
    public int Completed { get; init; }
    public int Total { get; init; }
    public int UpdateFound { get; init; }
    public string? CurrentTool { get; init; }
}

/// <summary>
/// 版本检查服务接口
/// </summary>
public interface IVersionCheckService
{
    /// <summary>
    /// 检查单个工具版本
    /// </summary>
    Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量检查版本
    /// </summary>
    Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<CheckProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开工具下载页面
    /// </summary>
    void OpenDownloadPage(ToolItem tool);

    /// <summary>
    /// 比较两个版本号
    /// </summary>
    /// <returns>负数表示 v1 < v2，0 表示相等，正数表示 v1 > v2</returns>
    int CompareVersions(string v1, string v2);
}
```

### 3.2 VersionCheckService 实现

```csharp
// 文件: Services/VersionCheckService.cs

namespace VistaLauncher.Services;

public class VersionCheckService : IVersionCheckService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<UpdateSource, IVersionProvider> _providers;

    public VersionCheckService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _providers = new Dictionary<UpdateSource, IVersionProvider>
        {
            [UpdateSource.NirSoft] = new NirSoftVersionProvider(_httpClient),
            [UpdateSource.Sysinternals] = new SysinternalsVersionProvider(_httpClient),
            [UpdateSource.GitHub] = new GitHubVersionProvider(_httpClient),
            [UpdateSource.Custom] = new HtmlVersionProvider(_httpClient)
        };
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        return client;
    }

    public async Task<VersionCheckResult?> CheckVersionAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (tool.SkipUpdateCheck || tool.UpdateSource == UpdateSource.None)
            return null;

        if (!_providers.TryGetValue(tool.UpdateSource, out var provider))
            return null;

        try
        {
            var info = await provider.GetUpdateInfoAsync(tool, cancellationToken);
            if (info == null) return null;

            // 更新工具的 UpdateInfo
            tool.UpdateInfo = info;
            tool.LastUpdateCheck = DateTime.Now;

            return new VersionCheckResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                CurrentVersion = tool.Version,
                LatestVersion = info.Version,
                DownloadUrl = info.DownloadUrl,
                ReleaseNotes = info.ReleaseNotes,
                ReleaseDate = info.ReleaseDate,
                FileSize = info.FileSize
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Version check failed for {tool.Name}: {ex.Message}");
            return new VersionCheckResult
            {
                ToolId = tool.Id,
                ToolName = tool.Name,
                CurrentVersion = tool.Version,
                CheckFailed = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<VersionCheckResult>> CheckVersionsAsync(
        IEnumerable<ToolItem> tools,
        IProgress<CheckProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VersionCheckResult>();
        var toolList = tools.Where(t => t.UpdateSource != UpdateSource.None && !t.SkipUpdateCheck).ToList();
        var total = toolList.Count;
        var completed = 0;
        var updateFound = 0;

        foreach (var tool in toolList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new CheckProgress
            {
                Completed = completed,
                Total = total,
                UpdateFound = updateFound,
                CurrentTool = tool.Name
            });

            var result = await CheckVersionAsync(tool, cancellationToken);
            if (result != null)
            {
                results.Add(result);
                if (result.HasUpdate) updateFound++;
            }

            completed++;

            // 避免请求过快
            await Task.Delay(500, cancellationToken);
        }

        return results;
    }

    public void OpenDownloadPage(ToolItem tool)
    {
        string? url = tool.UpdateInfo?.DownloadUrl ?? tool.HomepageUrl;
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    public int CompareVersions(string v1, string v2)
    {
        return VersionComparer.Compare(v1, v2);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

### 3.3 IVersionProvider - 版本提供者接口

```csharp
// 文件: Services/Updates/IVersionProvider.cs

namespace VistaLauncher.Services.Updates;

/// <summary>
/// 版本提供者接口（用于不同更新源的版本检查）
/// </summary>
public interface IVersionProvider
{
    /// <summary>
    /// 获取更新信息
    /// </summary>
    Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否支持该工具
    /// </summary>
    bool Supports(ToolItem tool);
}
```

### 3.4 NirSoftVersionProvider 实现

```csharp
// 文件: Services/Updates/NirSoftVersionProvider.cs

namespace VistaLauncher.Services.Updates;

/// <summary>
/// NirSoft 工具版本提供者
/// </summary>
public class NirSoftVersionProvider : IVersionProvider
{
    private readonly HttpClient _httpClient;
    private static readonly Regex VersionRegex = new(
        @"<td[^>]*>v([\d.]+)</td>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NirSoftVersionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool Supports(ToolItem tool)
    {
        return tool.UpdateSource == UpdateSource.NirSoft ||
               tool.HomepageUrl.Contains("nirsoft.net", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return null;

        var html = await _httpClient.GetStringAsync(tool.HomepageUrl, cancellationToken);
        var match = VersionRegex.Match(html);

        if (!match.Success)
            return null;

        var version = match.Groups[1].Value;
        return new UpdateInfo
        {
            Version = version,
            InfoUrl = tool.HomepageUrl,
            DownloadUrl = InferDownloadUrl(tool.HomepageUrl),
            ReleaseDate = DateTime.Now // NirSoft 页面不提供发布日期
        };
    }

    private static string? InferDownloadUrl(string homepageUrl)
    {
        // nirsoft.net/utils/toolname.html -> nirsoft.net/downloads/toolname.zip
        var match = Regex.Match(homepageUrl, @"nirsoft\.net/utils/([^/]+)\.html");
        if (match.Success)
        {
            return $"https://www.nirsoft.net/downloads/{match.Groups[1].Value}.zip";
        }
        return null;
    }
}
```

### 3.5 GitHubVersionProvider 实现

```csharp
// 文件: Services/Updates/GitHubVersionProvider.cs

namespace VistaLauncher.Services.Updates;

/// <summary>
/// GitHub Release 版本提供者
/// </summary>
public class GitHubVersionProvider : IVersionProvider
{
    private readonly HttpClient _httpClient;

    public GitHubVersionProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool Supports(ToolItem tool)
    {
        return tool.UpdateSource == UpdateSource.GitHub ||
               tool.HomepageUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<UpdateInfo?> GetUpdateInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        // 从 HomepageUrl 解析 owner/repo
        var match = Regex.Match(tool.HomepageUrl, @"github\.com/([^/]+)/([^/]+)");
        if (!match.Success)
            return null;

        var owner = match.Groups[1].Value;
        var repo = match.Groups[2].Value;

        // 调用 GitHub API
        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VistaLauncher");

        var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var version = data.GetProperty("tag_name").GetString()?.TrimStart('v');
        var releaseNotes = data.TryGetProperty("body", out var notes)
            ? notes.GetString()
            : null;

        // 获取下载链接
        string? downloadUrl = null;
        long? fileSize = null;
        if (data.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                // 优先选择 .zip 文件
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    fileSize = asset.GetProperty("size").GetInt64();
                    break;
                }
            }
        }

        // 解析发布日期
        DateTime? releaseDate = null;
        if (data.TryGetProperty("published_at", out var publishedAt))
        {
            if (DateTime.TryParse(publishedAt.GetString(), out var date))
                releaseDate = date;
        }

        return new UpdateInfo
        {
            Version = version ?? string.Empty,
            DownloadUrl = downloadUrl,
            InfoUrl = tool.HomepageUrl,
            ReleaseNotes = releaseNotes,
            ReleaseDate = releaseDate,
            FileSize = fileSize
        };
    }
}
```

### 3.6 IUpdateService - 更新服务

```csharp
// 文件: Services/IUpdateService.cs

namespace VistaLauncher.Services;

/// <summary>
/// 下载进度
/// </summary>
public class DownloadProgress
{
    public long BytesDownloaded { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
    public string? CurrentFile { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// 更新结果
/// </summary>
public class UpdateResult
{
    public bool Success { get; init; }
    public string ToolId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string OldVersion { get; init; } = string.Empty;
    public string NewVersion { get; init; } = string.Empty;
    public List<string> Messages { get; init; } = [];
    public string? BackupPath { get; init; }
}

/// <summary>
/// 更新服务接口
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// 下载更新
    /// </summary>
    Task<string> DownloadUpdateAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 安装更新
    /// </summary>
    Task<UpdateResult> InstallUpdateAsync(
        ToolItem tool,
        string downloadedFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载并安装更新（完整流程）
    /// </summary>
    Task<UpdateResult> UpdateToolAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚更新
    /// </summary>
    Task<bool> RollbackUpdateAsync(UpdateResult updateResult);
}
```

### 3.7 UpdateService 实现

```csharp
// 文件: Services/UpdateService.cs

namespace VistaLauncher.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _tempDir;
    private readonly IToolDataService _dataService;

    public UpdateService(IToolDataService dataService, HttpClient? httpClient = null)
    {
        _dataService = dataService;
        _httpClient = httpClient ?? new HttpClient();
        _tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VistaLauncher",
            "Updates");

        Directory.CreateDirectory(_tempDir);
    }

    public async Task<string> DownloadUpdateAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (tool.UpdateInfo?.DownloadUrl == null)
            throw new InvalidOperationException($"No download URL for {tool.Name}");

        var url = tool.UpdateInfo.DownloadUrl;
        var fileName = Path.GetFileName(new Uri(url).LocalPath) ?? $"{tool.Name}.zip";
        var destPath = Path.Combine(_tempDir, $"{tool.Id}_{fileName}");

        progress?.Report(new DownloadProgress
        {
            Status = "准备下载...",
            CurrentFile = fileName
        });

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[81920];

        using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            var totalRead = 0L;
            var read = 0;

            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                progress?.Report(new DownloadProgress
                {
                    BytesDownloaded = totalRead,
                    TotalBytes = totalBytes,
                    CurrentFile = fileName,
                    Status = $"下载中... {FormatBytes(totalRead)} / {FormatBytes(totalBytes)}"
                });
            }
        }

        // 验证 SHA256
        if (!string.IsNullOrEmpty(tool.UpdateInfo.Sha256Checksum))
        {
            var hash = ComputeSHA256(destPath);
            if (!hash.Equals(tool.UpdateInfo.Sha256Checksum, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(destPath);
                throw new InvalidOperationException("文件校验失败，SHA256 不匹配");
            }
        }

        return destPath;
    }

    public async Task<UpdateResult> InstallUpdateAsync(
        ToolItem tool,
        string downloadedFilePath,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var backupPath = string.Empty;

        try
        {
            // 获取当前工具路径
            var currentPath = tool.ExecutablePath;
            if (!File.Exists(currentPath))
            {
                return new UpdateResult
                {
                    Success = false,
                    ToolId = tool.Id,
                    ToolName = tool.Name,
                    OldVersion = tool.Version,
                    NewVersion = tool.UpdateInfo?.Version ?? string.Empty,
                    Messages = { $"原文件不存在: {currentPath}" }
                };
            }

            // 创建备份
            var toolDir = Path.GetDirectoryName(currentPath) ?? Path.GetTempPath();
            backupPath = Path.Combine(toolDir, $"{Path.GetFileNameWithoutExtension(currentPath)}_backup_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(currentPath)}");
            File.Copy(currentPath, backupPath, true);
            messages.Add($"已备份原文件到: {backupPath}");

            // 解压并替换
            if (downloadedFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractDir = Path.Combine(_tempDir, tool.Id);
                Directory.CreateDirectory(extractDir);

                messages.Add("解压文件...");
                ZipFile.ExtractToDirectory(downloadedFilePath, extractDir, overwriteFiles: true);

                // 查找可执行文件
                var exeFiles = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
                if (exeFiles.Length > 0)
                {
                    var newExe = exeFiles[0]; // 简单处理，取第一个 exe
                    File.Copy(newExe, currentPath, true);
                    messages.Add($"已更新: {currentPath}");
                }
                else
                {
                    throw new InvalidOperationException("压缩包中未找到可执行文件");
                }

                // 清理临时文件
                try { Directory.Delete(extractDir, true); }
                catch { /* ignore */ }
            }
            else if (downloadedFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(downloadedFilePath, currentPath, true);
                messages.Add($"已更新: {currentPath}");
            }
            else
            {
                throw new InvalidOperationException("不支持的文件类型");
            }

            // 更新版本信息
            var oldVersion = tool.Version;
            tool.Version = tool.UpdateInfo?.Version ?? oldVersion;
            tool.UpdatedAt = DateTime.Now;
            tool.UpdateInfo = null; // 清除更新信息
            await _dataService.UpdateToolAsync(tool);

            return new UpdateResult
            {
                Success = true,
                ToolId = tool.Id,
                ToolName = tool.Name,
                OldVersion = oldVersion,
                NewVersion = tool.Version,
                Messages = messages,
                BackupPath = backupPath
            };
        }
        catch (Exception ex)
        {
            return new UpdateResult
            {
                Success = false,
                ToolId = tool.Id,
                ToolName = tool.Name,
                OldVersion = tool.Version,
                NewVersion = tool.UpdateInfo?.Version ?? string.Empty,
                Messages = messages.Concat(new[] { ex.Message }).ToList(),
                BackupPath = backupPath
            };
        }
        finally
        {
            // 删除下载文件
            try { File.Delete(downloadedFilePath); }
            catch { /* ignore */ }
        }
    }

    public async Task<UpdateResult> UpdateToolAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress { Status = "开始更新..." });

        var downloadedFile = await DownloadUpdateAsync(tool, progress, cancellationToken);
        progress?.Report(new DownloadProgress { Status = "安装更新..." });

        return await InstallUpdateAsync(tool, downloadedFile, cancellationToken);
    }

    public async Task<bool> RollbackUpdateAsync(UpdateResult updateResult)
    {
        if (string.IsNullOrEmpty(updateResult.BackupPath) || !File.Exists(updateResult.BackupPath))
            return false;

        try
        {
            // 恢复备份文件
            var tool = await _dataService.GetToolByIdAsync(updateResult.ToolId);
            if (tool == null) return false;

            File.Copy(updateResult.BackupPath, tool.ExecutablePath, true);
            tool.Version = updateResult.OldVersion;
            await _dataService.UpdateToolAsync(tool);

            // 删除备份
            File.Delete(updateResult.BackupPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        double number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1}{suffixes[counter]}";
    }
}
```

### 3.8 IDataMigrationService - 数据迁移服务

```csharp
// 文件: Services/IDataMigrationService.cs

namespace VistaLauncher.Services;

/// <summary>
/// 数据迁移接口
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// 检查是否需要迁移
    /// </summary>
    bool NeedsMigration(string currentVersion);

    /// <summary>
    /// 执行迁移
    /// </summary>
    Task<ToolsData> MigrateAsync(ToolsData data, string targetVersion);

    /// <summary>
    /// 备份当前数据
    /// </summary>
    Task<string> BackupAsync(string dataFilePath);
}

/// <summary>
/// 数据迁移实现
/// </summary>
public class DataMigrationService : IDataMigrationService
{
    private readonly Dictionary<string, IDataMigrationStep> _migrationSteps;

    public DataMigrationService()
    {
        _migrationSteps = new Dictionary<string, IDataMigrationStep>
        {
            ["1.0->2.0"] = new MigrationV1ToV2(),
            // 未来添加更多迁移步骤
        };
    }

    public bool NeedsMigration(string currentVersion)
    {
        return currentVersion != ToolsData.CurrentDataFormatVersion;
    }

    public async Task<ToolsData> MigrateAsync(ToolsData data, string targetVersion)
    {
        var currentVersion = data.DataFormatVersion ?? "1.0";
        var path = BuildMigrationPath(currentVersion, targetVersion);

        foreach (var stepKey in path)
        {
            if (_migrationSteps.TryGetValue(stepKey, out var step))
            {
                data = await step.MigrateAsync(data);
            }
        }

        data.DataFormatVersion = targetVersion;
        return data;
    }

    public async Task<string> BackupAsync(string dataFilePath)
    {
        var backupDir = Path.GetDirectoryName(dataFilePath) ?? Path.GetTempPath();
        var fileName = Path.GetFileNameWithoutExtension(dataFilePath);
        var extension = Path.GetExtension(dataFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"{fileName}_backup_{timestamp}{extension}");

        await Task.Run(() => File.Copy(dataFilePath, backupPath, true));
        return backupPath;
    }

    private static List<string> BuildMigrationPath(string from, string to)
    {
        // 简化实现，实际应解析版本号并构建完整路径
        var path = new List<string>();
        if (from == "1.0" && to == "2.0")
        {
            path.Add("1.0->2.0");
        }
        return path;
    }
}

/// <summary>
/// 数据迁移步骤接口
/// </summary>
public interface IDataMigrationStep
{
    Task<ToolsData> MigrateAsync(ToolsData data);
}

/// <summary>
/// 1.0 -> 2.0 迁移步骤
/// </summary>
public class MigrationV1ToV2 : IDataMigrationStep
{
    public async Task<ToolsData> MigrateAsync(ToolsData data)
    {
        // 添加新字段的默认值
        foreach (var tool in data.Tools)
        {
            if (string.IsNullOrEmpty(tool.DataFormatVersion))
            {
                tool.DataFormatVersion = "1.0";
            }
            tool.UpdateSource = InferUpdateSource(tool);
        }

        // 添加更新配置
        data.UpdateConfig ??= new UpdateConfig();
        data.DataFormatVersion = "2.0";

        return await Task.FromResult(data);
    }

    private static UpdateSource InferUpdateSource(ToolItem tool)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return UpdateSource.None;

        if (tool.HomepageUrl.Contains("nirsoft.net"))
            return UpdateSource.NirSoft;
        if (tool.HomepageUrl.Contains("github.com"))
            return UpdateSource.GitHub;
        if (tool.HomepageUrl.Contains("learn.microsoft.com") ||
            tool.HomepageUrl.Contains("sysinternals"))
            return UpdateSource.Sysinternals;

        return UpdateSource.Custom;
    }
}
```

---

## 4. UI 组件设计

### 4.1 UpdateDialog - 更新对话框

```xml
<!-- 文件: Controls/UpdateDialog.xaml -->
<ContentDialog
    x:Class="VistaLauncher.Controls.UpdateDialog"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="检查更新"
    PrimaryButtonText="全部更新"
    CloseButtonText="关闭"
    DefaultButton="Primary">

    <Grid MinWidth="500">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 顶部信息 -->
        <StackPanel Grid.Row="0" Spacing="8" Padding="0,0,0,16">
            <TextBlock Style="{StaticResource SubtitleTextBlockStyle}">
                <Run Text="发现 "/>
                <Run Text="{x:Bind UpdateCount, Mode=OneWay}" FontWeight="SemiBold"/>
                <Run Text=" 个可用更新"/>
            </TextBlock>
            <TextBlock
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                Text="{x:Bind LastCheckTime, Mode=OneWay}"/>
        </StackPanel>

        <!-- 更新列表 -->
        <ListView
            Grid.Row="1"
            ItemsSource="{x:Bind Updates}"
            SelectionMode="Multiple"
            MaxHeight="400"
            VerticalAlignment="Stretch">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding="8">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <TextBlock
                            Grid.Row="0"
                            Text="{Binding Name}"
                            FontWeight="SemiBold"/>

                        <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
                            <TextBlock
                                Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                <Run Text="当前版本: "/>
                                <Run Text="{Binding CurrentVersion}" Foreground="{ThemeResource CautionForegroundColor}"/>
                            </TextBlock>
                            <TextBlock
                                Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                <Run Text="→"/>
                            </TextBlock>
                            <TextBlock
                                Foreground="{ThemeResource SuccessForegroundColor}">
                                <Run Text="{Binding LatestVersion}"/>
                            </TextBlock>
                        </StackPanel>

                        <TextBlock
                            Grid.Row="2"
                            Foreground="{ThemeResource TextFillColorTertiaryBrush}"
                            TextWrapping="Wrap"
                            MaxWidth="400"
                            Text="{Binding ReleaseNotes}"
                            Visibility="{Binding HasReleaseNotes}"/>

                        <Button
                            Grid.Column="1"
                            Grid.RowSpan="3"
                            Content="下载"
                            Click="Download_Click"/>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <!-- 进度条 -->
        <StackPanel
            Grid.Row="2"
            Spacing="8"
            Visibility="{x:Bind IsDownloading, Mode=OneWay}">
            <TextBlock Text="{x:Bind DownloadStatus, Mode=OneWay}"/>
            <ProgressBar Value="{x:Bind DownloadProgress, Mode=OneWay}"/>
        </StackPanel>
    </Grid>
</ContentDialog>
```

### 4.2 UpdateItem ViewModel

```csharp
// 文件: ViewModels/UpdateItemViewModel.cs

namespace VistaLauncher.ViewModels;

public partial class UpdateItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private string _latestVersion = string.Empty;

    [ObservableProperty]
    private string? _releaseNotes;

    [ObservableProperty]
    private DateTime? _releaseDate;

    [ObservableProperty]
    private long? _fileSize;

    [ObservableProperty]
    private bool _isSelected = true;

    [JsonIgnore]
    public bool HasReleaseNotes => !string.IsNullOrEmpty(ReleaseNotes);

    [JsonIgnore]
    public string FileSizeDisplay => FileSize.HasValue ? FormatFileSize(FileSize.Value) : string.Empty;

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        double number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
```

---

## 5. 实施阶段

### Phase 1: 数据模型扩展 (P0)

**目标**: 扩展数据模型以支持更新功能

| 文件 | 修改内容 |
|------|----------|
| `Models/UpdateInfo.cs` | 新增更新信息模型 |
| `Models/UpdateConfig.cs` | 新增更新配置模型 |
| `Models/ToolItem.cs` | 添加更新相关字段 |
| `Models/ToolsData.cs` | 添加数据版本和更新配置 |
| `Models/Enums.cs` | 添加 UpdateSource 枚举 |
| `Models/JsonContext.cs` | 添加新类型序列化支持 |

### Phase 2: 版本检查服务 (P0)

**目标**: 实现版本检查功能

| 文件 | 修改内容 |
|------|----------|
| `Services/IVersionCheckService.cs` | 新增版本检查接口 |
| `Services/VersionCheckService.cs` | 新增版本检查实现 |
| `Services/Updates/IVersionProvider.cs` | 新增版本提供者接口 |
| `Services/Updates/NirSoftVersionProvider.cs` | 新增 NirSoft 版本提供者 |
| `Services/Updates/GitHubVersionProvider.cs` | 新增 GitHub 版本提供者 |
| `Services/Updates/SysinternalsVersionProvider.cs` | 新增 Sysinternals 版本提供者 |

### Phase 3: 更新服务 (P1)

**目标**: 实现下载和安装功能

| 文件 | 修改内容 |
|------|----------|
| `Services/IUpdateService.cs` | 新增更新服务接口 |
| `Services/UpdateService.cs` | 新增更新服务实现 |

### Phase 4: 数据迁移服务 (P0)

**目标**: 实现数据格式迁移

| 文件 | 修改内容 |
|------|----------|
| `Services/IDataMigrationService.cs` | 新增迁移服务接口 |
| `Services/DataMigrationService.cs` | 新增迁移服务实现 |

### Phase 5: UI 组件 (P0)

**目标**: 实现用户界面

| 文件 | 修改内容 |
|------|----------|
| `Controls/UpdateDialog.xaml` | 新增更新对话框 |
| `Controls/UpdateDialog.xaml.cs` | 新增对话框代码 |
| `ViewModels/UpdateItemViewModel.cs` | 新增更新项 ViewModel |
| `Controls/CommandBar.xaml` | 添加"检查更新"按钮 |

---

## 6. 文件变更清单

### 6.1 新增文件

| 文件路径 | 说明 | 阶段 |
|----------|------|------|
| `Models/UpdateInfo.cs` | 更新信息模型 | Phase 1 |
| `Models/UpdateConfig.cs` | 更新配置模型 | Phase 1 |
| `Services/IVersionCheckService.cs` | 版本检查接口 | Phase 2 |
| `Services/VersionCheckService.cs` | 版本检查实现 | Phase 2 |
| `Services/Updates/IVersionProvider.cs` | 版本提供者接口 | Phase 2 |
| `Services/Updates/NirSoftVersionProvider.cs` | NirSoft 版本提供者 | Phase 2 |
| `Services/Updates/GitHubVersionProvider.cs` | GitHub 版本提供者 | Phase 2 |
| `Services/Updates/SysinternalsVersionProvider.cs` | Sysinternals 版本提供者 | Phase 2 |
| `Services/Updates/HtmlVersionProvider.cs` | 通用 HTML 版本提供者 | Phase 2 |
| `Services/IUpdateService.cs` | 更新服务接口 | Phase 3 |
| `Services/UpdateService.cs` | 更新服务实现 | Phase 3 |
| `Services/IDataMigrationService.cs` | 数据迁移接口 | Phase 4 |
| `Services/DataMigrationService.cs` | 数据迁移实现 | Phase 4 |
| `Controls/UpdateDialog.xaml` | 更新对话框 | Phase 5 |
| `Controls/UpdateDialog.xaml.cs` | 更新对话框代码 | Phase 5 |
| `ViewModels/UpdateItemViewModel.cs` | 更新项 ViewModel | Phase 5 |

### 6.2 修改文件

| 文件路径 | 修改内容 | 阶段 |
|----------|----------|------|
| `Models/ToolItem.cs` | 添加更新相关字段 | Phase 1 |
| `Models/ToolsData.cs` | 添加数据版本和更新配置 | Phase 1 |
| `Models/Enums.cs` | 添加 UpdateSource 枚举 | Phase 1 |
| `Models/JsonContext.cs` | 添加新类型序列化支持 | Phase 1 |
| `Services/ToolDataService.cs` | 加载时检查数据版本 | Phase 4 |
| `Controls/CommandBar.xaml` | 添加"检查更新"菜单项 | Phase 5 |
| `Controls/CommandBar.xaml.cs` | 添加菜单项事件处理 | Phase 5 |
| `MainWindow.xaml.cs` | 注册更新服务 | Phase 5 |

---

## 7. 版本检查流程设计

```
┌─────────────────────────────────────────────────────────────────┐
│                        版本检查流程                             │
└─────────────────────────────────────────────────────────────────┘

1. 用户触发检查
   │
   ├── 手动点击"检查更新"按钮
   └── 定时自动检查（根据配置间隔）

2. 筛选需要检查的工具
   │
   ├── 排除 SkipUpdateCheck = true 的工具
   ├── 排除 UpdateSource = None 的工具
   └── 根据配置限制并发数量

3. 遍历工具进行检查
   │
   ├── 根据 UpdateSource 选择对应的 VersionProvider
   │
   ├── NirSoft
   │   └── 解析 nirsoft.net 页面获取版本
   │
   ├── GitHub
   │   └── 调用 GitHub API 获取 Release 信息
   │
   ├── Sysinternals
   │   └── 从微软官网获取版本信息
   │
   └── Custom
       └── 通用 HTML 解析

4. 版本比较
   │
   ├── 使用语义化版本比较
   └── 判断 UpdateInfo.Version > ToolItem.Version

5. 缓存更新信息
   │
   ├── 更新 ToolItem.UpdateInfo
   ├── 更新 ToolItem.LastUpdateCheck
   └── 保存到 tools.json

6. 显示结果
   │
   ├── 显示可用更新数量
   ├── 展示更新列表（版本号、更新日志）
   └── 用户可选择下载或跳过
```

---

## 8. 自动更新流程设计

```
┌─────────────────────────────────────────────────────────────────┐
│                        自动更新流程                             │
└─────────────────────────────────────────────────────────────────┘

1. 用户选择更新
   │
   ├── 单个工具更新
   └── 批量更新

2. 下载更新包
   │
   ├── 显示下载进度
   ├── 支持断点续传（可选）
   └── 验证 SHA256 校验和

3. 安装更新
   │
   ├── 备份原文件
   ├── 解压更新包（如果是 .zip）
   ├── 替换可执行文件
   └── 清理临时文件

4. 更新数据
   │
   ├── 更新 ToolItem.Version
   ├── 清除 ToolItem.UpdateInfo
   └── 保存到 tools.json

5. 完成/回滚
   │
   ├── 成功：显示完成信息
   └── 失败：提供回滚选项
```

---

## 9. 数据迁移流程设计

```
┌─────────────────────────────────────────────────────────────────┐
│                        数据迁移流程                             │
└─────────────────────────────────────────────────────────────────┘

1. 应用启动时加载数据
   │
   ├── 读取 tools.json
   └── 检查 DataFormatVersion

2. 版本比较
   │
   ├── 当前版本 < 目标版本
   │   └── 需要迁移
   └── 当前版本 = 目标版本
       └── 无需迁移

3. 备份原始数据
   │
   ├── 创建带时间戳的备份文件
   └── 保留最近 N 个备份

4. 执行迁移步骤
   │
   ├── 1.0 -> 2.0
   │   ├── 添加 UpdateInfo 字段
   │   ├── 推断 UpdateSource
   │   └── 添加 UpdateConfig
   │
   └── 未来版本...

5. 验证迁移结果
   │
   ├── 检查必需字段
   └── 数据完整性校验

6. 保存迁移后的数据
   │
   ├── 更新 DataFormatVersion
   └── 写入 tools.json

7. 错误处理
   │
   ├── 迁移失败：从备份恢复
   └── 记录错误日志
```

---

## 10. 依赖注入配置

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
    _iconExtractor = new IconExtractor();
    _importService = new ImportService(...);
    _toolManagementService = new ToolManagementService(_toolDataService);

    // 新增服务
    _httpClient = new HttpClient();
    _versionCheckService = new VersionCheckService(_httpClient);
    _updateService = new UpdateService(_toolDataService, _httpClient);
    _dataMigrationService = new DataMigrationService();

    // 检查数据迁移
    CheckDataMigration();

    // 注入到 ViewModel
    _viewModel = new LauncherViewModel(
        _toolDataService,
        _searchProvider,
        _processLauncher,
        _importService,
        _toolManagementService,
        _versionCheckService);
}

private async void CheckDataMigration()
{
    var data = await _toolDataService.GetDataAsync();
    if (_dataMigrationService.NeedsMigration(data.DataFormatVersion))
    {
        // 备份
        var backupPath = await _dataMigrationService.BackupAsync(GetDataFilePath());

        // 迁移
        var migratedData = await _dataMigrationService.MigrateAsync(
            data,
            ToolsData.CurrentDataFormatVersion);

        await _toolDataService.SaveDataAsync(migratedData);
    }
}
```

---

## 11. 配置文件示例

### 11.1 更新后的 tools.json 结构

```json
{
  "dataFormatVersion": "2.0",
  "version": "1.0.0",
  "lastModified": "2025-01-22T10:30:00+08:00",
  "lastGlobalUpdateCheck": "2025-01-22T10:00:00+08:00",
  "updateConfig": {
    "autoCheckEnabled": true,
    "checkIntervalHours": 24,
    "autoDownloadEnabled": false,
    "includePrerelease": false,
    "maxConcurrentChecks": 5
  },
  "groups": [...],
  "tools": [
    {
      "id": "...",
      "name": "AdvancedRun",
      "version": "1.50",
      "homepageUrl": "https://www.nirsoft.net/utils/advanced_run.html",
      "updateSource": "NirSoft",
      "skipUpdateCheck": false,
      "lastUpdateCheck": "2025-01-22T10:00:00+08:00",
      "updateInfo": {
        "version": "1.52",
        "downloadUrl": "https://www.nirsoft.net/downloads/advancedrun.zip",
        "infoUrl": "https://www.nirsoft.net/utils/advanced_run.html",
        "releaseNotes": "Added new features...",
        "releaseDate": "2025-01-15T00:00:00Z",
        "fileSize": 102400
      },
      "executablePath": "C:\\Tools\\AdvancedRun.exe",
      ...
    }
  ]
}
```

---

## 12. 测试计划

### 12.1 单元测试

| 测试类 | 测试内容 |
|--------|----------|
| `VersionComparerTests` | 版本号比较逻辑 |
| `NirSoftVersionProviderTests` | NirSoft 页面解析 |
| `GitHubVersionProviderTests` | GitHub API 调用 |
| `UpdateServiceTests` | 下载和安装流程 |
| `DataMigrationServiceTests` | 数据迁移步骤 |

### 12.2 集成测试

| 测试场景 | 验收标准 |
|----------|----------|
| 版本检查 | 正确识别有更新的工具 |
| 下载更新 | 文件下载完整，SHA256 校验通过 |
| 安装更新 | 文件替换成功，版本号更新 |
| 回滚更新 | 能恢复到更新前的状态 |
| 数据迁移 | 旧数据能正确迁移到新格式 |

---

## 13. 变更记录

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| v1.0 | 2025-01-22 | 初始版本 |

---

*文档结束*
