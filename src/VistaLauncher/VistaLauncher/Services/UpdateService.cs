using System.IO.Compression;
using System.Security.Cryptography;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 更新服务实现
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private const int BufferSize = 81920; // 80KB buffer
    private static readonly string[] s_sizeSuffixes = ["B", "KB", "MB", "GB"];

    private readonly HttpClient _httpClient;
    private readonly string _tempDir;
    private readonly IToolDataService _dataService;

    /// <summary>
    /// 创建更新服务
    /// </summary>
    /// <param name="dataService">工具数据服务</param>
    /// <param name="httpClient">HTTP 客户端（可选）</param>
    public UpdateService(IToolDataService dataService, HttpClient? httpClient = null)
    {
        _dataService = dataService;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
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
            throw new InvalidOperationException($"没有可用的下载链接: {tool.Name}");

        var url = tool.UpdateInfo.DownloadUrl;
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

        // 验证 SHA256（如果提供）
        if (!string.IsNullOrEmpty(tool.UpdateInfo.Sha256Checksum))
        {
            progress?.Report(new DownloadProgress
            {
                BytesDownloaded = totalRead,
                TotalBytes = totalBytes,
                CurrentFile = fileName,
                Status = "验证文件完整性..."
            });

            var hash = await ComputeSHA256Async(destPath, cancellationToken);
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
        string? backupPath = null;

        try
        {
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
                    Messages = [$"原文件不存在: {currentPath}"]
                };
            }

            // 创建备份
            var toolDir = Path.GetDirectoryName(currentPath) ?? Path.GetTempPath();
            backupPath = Path.Combine(toolDir, $"{Path.GetFileNameWithoutExtension(currentPath)}_backup_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(currentPath)}");
            File.Copy(currentPath, backupPath, true);
            messages.Add($"已备份原文件到: {backupPath}");

            if (downloadedFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractDir = Path.Combine(_tempDir, tool.Id);
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);

                messages.Add("解压文件...");
                ZipFile.ExtractToDirectory(downloadedFilePath, extractDir, overwriteFiles: true);

                var exeFiles = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
                var targetExe = FindBestMatchingExe(exeFiles, tool.Name, currentPath);

                if (targetExe != null)
                {
                    File.Copy(targetExe, currentPath, true);
                    messages.Add($"已更新: {currentPath}");
                }
                else
                {
                    throw new InvalidOperationException("压缩包中未找到匹配的可执行文件");
                }

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
                throw new InvalidOperationException($"不支持的文件类型: {Path.GetExtension(downloadedFilePath)}");
            }

            var oldVersion = tool.Version;
            tool.Version = tool.UpdateInfo?.Version ?? oldVersion;
            tool.UpdatedAt = DateTime.Now;
            tool.UpdateInfo = null;
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
            messages.Add($"错误: {ex.Message}");
            return new UpdateResult
            {
                Success = false,
                ToolId = tool.Id,
                ToolName = tool.Name,
                OldVersion = tool.Version,
                NewVersion = tool.UpdateInfo?.Version ?? string.Empty,
                Messages = messages,
                BackupPath = backupPath
            };
        }
        finally
        {
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
            var tool = await _dataService.GetToolByIdAsync(updateResult.ToolId);
            if (tool == null)
                return false;

            File.Copy(updateResult.BackupPath, tool.ExecutablePath, true);
            tool.Version = updateResult.OldVersion;
            await _dataService.UpdateToolAsync(tool);
            File.Delete(updateResult.BackupPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从多个 exe 文件中找到最佳匹配
    /// </summary>
    private static string? FindBestMatchingExe(string[] exeFiles, string toolName, string currentPath)
    {
        if (exeFiles.Length == 0)
            return null;

        if (exeFiles.Length == 1)
            return exeFiles[0];

        var currentExeName = Path.GetFileName(currentPath);

        // 优先选择与当前文件同名的
        var sameNameExe = exeFiles.FirstOrDefault(e =>
            Path.GetFileName(e).Equals(currentExeName, StringComparison.OrdinalIgnoreCase));
        if (sameNameExe != null)
            return sameNameExe;

        // 其次选择包含工具名的
        var containsNameExe = exeFiles.FirstOrDefault(e =>
            Path.GetFileName(e).Contains(toolName, StringComparison.OrdinalIgnoreCase));
        if (containsNameExe != null)
            return containsNameExe;

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
