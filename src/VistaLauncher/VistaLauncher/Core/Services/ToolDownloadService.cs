using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using VistaLauncher.Models;
using VistaLauncher.Services.Updates;

namespace VistaLauncher.Services;

/// <summary>
/// 工具下载服务实现
/// 负责从远程源下载并安装工具
/// </summary>
public sealed class ToolDownloadService : IToolDownloadService
{
    private const int BufferSize = 81920; // 80KB buffer
    private static readonly string[] s_sizeSuffixes = ["B", "KB", "MB", "GB"];

    private readonly HttpClient _httpClient;
    private readonly IPathResolverService _pathResolver;
    private readonly IToolDataService _dataService;
    private readonly IVersionCheckService _versionCheckService;
    private readonly string _tempDir;

    /// <summary>
    /// 创建工具下载服务
    /// </summary>
    /// <param name="pathResolver">路径解析服务</param>
    /// <param name="dataService">工具数据服务</param>
    /// <param name="versionCheckService">版本检查服务</param>
    /// <param name="httpClient">HTTP 客户端（可选）</param>
    public ToolDownloadService(
        IPathResolverService pathResolver,
        IToolDataService dataService,
        IVersionCheckService versionCheckService,
        HttpClient? httpClient = null)
    {
        _pathResolver = pathResolver;
        _dataService = dataService;
        _versionCheckService = versionCheckService;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VistaLauncher",
            "Downloads");

        Directory.CreateDirectory(_tempDir);
    }

    public bool CanDownload(ToolItem tool)
    {
        return tool.UpdateSource != UpdateSource.None;
    }

    public async Task<UpdateInfo?> GetDownloadInfoAsync(
        ToolItem tool,
        CancellationToken cancellationToken = default)
    {
        if (!CanDownload(tool))
            return null;

        var result = await _versionCheckService.CheckVersionAsync(tool, cancellationToken);
        if (result == null || result.CheckFailed)
            return null;

        return tool.UpdateInfo;
    }

    public async Task<ToolDownloadResult> DownloadAndInstallAsync(
        ToolItem tool,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        try
        {
            // 1. 获取下载信息
            progress?.Report(new DownloadProgress { Status = "获取下载信息..." });
            var downloadInfo = await GetDownloadInfoAsync(tool, cancellationToken);
            if (downloadInfo == null || string.IsNullOrEmpty(downloadInfo.DownloadUrl))
            {
                return new ToolDownloadResult
                {
                    Success = false,
                    ToolId = tool.Id,
                    ToolName = tool.Name,
                    ErrorMessage = "无法获取下载信息，请检查工具的更新源配置"
                };
            }

            // 2. 下载文件
            var downloadedFile = await DownloadFileAsync(tool, downloadInfo.DownloadUrl, progress, cancellationToken);

            // 3. 验证文件（如果提供了 SHA256）
            if (!string.IsNullOrEmpty(downloadInfo.Sha256Checksum))
            {
                progress?.Report(new DownloadProgress { Status = "验证文件完整性..." });
                var hash = await ComputeSHA256Async(downloadedFile, cancellationToken);
                if (!hash.Equals(downloadInfo.Sha256Checksum, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(downloadedFile);
                    return new ToolDownloadResult
                    {
                        Success = false,
                        ToolId = tool.Id,
                        ToolName = tool.Name,
                        ErrorMessage = "文件校验失败，SHA256 不匹配"
                    };
                }
            }

            // 4. 解压并安装
            progress?.Report(new DownloadProgress { Status = "安装工具..." });
            var installedPath = await InstallToolAsync(tool, downloadedFile, cancellationToken);

            // 5. 更新工具信息
            tool.Version = downloadInfo.Version;
            tool.ExecutablePath = installedPath;
            tool.UpdatedAt = DateTime.Now;
            tool.UpdateInfo = null; // 清除更新信息
            await _dataService.UpdateToolAsync(tool);

            messages.Add($"成功安装 {tool.Name} v{downloadInfo.Version}");
            messages.Add($"安装路径: {installedPath}");

            return new ToolDownloadResult
            {
                Success = true,
                ToolId = tool.Id,
                ToolName = tool.Name,
                Version = downloadInfo.Version,
                InstalledPath = installedPath,
                Messages = messages
            };
        }
        catch (OperationCanceledException)
        {
            return new ToolDownloadResult
            {
                Success = false,
                ToolId = tool.Id,
                ToolName = tool.Name,
                ErrorMessage = "下载已取消"
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Download failed for {tool.Name}: {ex.Message}");
            return new ToolDownloadResult
            {
                Success = false,
                ToolId = tool.Id,
                ToolName = tool.Name,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 下载文件到临时目录
    /// </summary>
    private async Task<string> DownloadFileAsync(
        ToolItem tool,
        string url,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(new Uri(url).LocalPath);
        if (string.IsNullOrEmpty(fileName))
            fileName = $"{tool.Name}.zip";

        var destPath = Path.Combine(_tempDir, $"{tool.Id}_{fileName}");

        progress?.Report(new DownloadProgress
        {
            Status = "准备下载...",
            CurrentFile = fileName
        });

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

        var totalRead = 0L;
        var buffer = new byte[BufferSize];
        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
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

        return destPath;
    }

    /// <summary>
    /// 安装工具到目标目录
    /// </summary>
    private async Task<string> InstallToolAsync(
        ToolItem tool,
        string downloadedFilePath,
        CancellationToken cancellationToken)
    {
        // 获取工具安装目录
        var toolDir = _pathResolver.GetToolInstallDirectory(tool.Id, tool.Name);
        Directory.CreateDirectory(toolDir);

        if (downloadedFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            // 解压 ZIP 文件
            var extractDir = Path.Combine(_tempDir, $"extract_{tool.Id}");
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, true);
            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(downloadedFilePath, extractDir, overwriteFiles: true);

            // 查找可执行文件
            var exeFiles = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
            string? targetExe = null;

            if (exeFiles.Length == 0)
            {
                throw new InvalidOperationException("压缩包中未找到可执行文件");
            }

            // 查找最佳匹配的 exe 文件
            targetExe = FindBestMatchingExe(exeFiles, tool.Name);

            // 复制到目标目录
            var destPath = Path.Combine(toolDir, Path.GetFileName(targetExe) ?? "tool.exe");
            File.Copy(targetExe!, destPath, true);

            // 清理临时目录
            try { Directory.Delete(extractDir, true); }
            catch { /* ignore */ }

            return destPath;
        }
        else if (downloadedFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            // 直接复制 exe 文件
            var destPath = Path.Combine(toolDir, Path.GetFileName(downloadedFilePath));
            File.Copy(downloadedFilePath, destPath, true);
            return destPath;
        }
        else
        {
            throw new InvalidOperationException($"不支持的文件类型: {Path.GetExtension(downloadedFilePath)}");
        }
    }

    /// <summary>
    /// 从多个 exe 文件中找到最佳匹配
    /// </summary>
    private static string? FindBestMatchingExe(string[] exeFiles, string toolName)
    {
        if (exeFiles.Length == 0)
            return null;

        if (exeFiles.Length == 1)
            return exeFiles[0];

        var lowerToolName = toolName.ToLowerInvariant();

        // 优先选择与工具名同名的
        var sameNameExe = exeFiles.FirstOrDefault(e =>
            Path.GetFileNameWithoutExtension(e).Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (sameNameExe != null)
            return sameNameExe;

        // 其次选择包含工具名的
        var containsNameExe = exeFiles.FirstOrDefault(e =>
            Path.GetFileNameWithoutExtension(e).Contains(toolName, StringComparison.OrdinalIgnoreCase));
        if (containsNameExe != null)
            return containsNameExe;

        // 排除安装程序、卸载程序等
        var filteredExe = exeFiles.Where(e =>
        {
            var name = Path.GetFileNameWithoutExtension(e).ToLowerInvariant();
            return !name.Contains("install") &&
                   !name.Contains("uninstall") &&
                   !name.Contains("setup") &&
                   !name.Contains("configuration");
        }).ToArray();

        if (filteredExe.Length > 0)
            return filteredExe[0];

        return exeFiles[0];
    }

    /// <summary>
    /// 异步计算文件的 SHA256 哈希值
    /// </summary>
    private static async Task<string> ComputeSHA256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 格式化字节数为可读字符串
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        var counter = 0;
        var number = (double)bytes;
        while (Math.Round(number / 1024) >= 1 && counter < s_sizeSuffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {s_sizeSuffixes[counter]}";
    }
}
