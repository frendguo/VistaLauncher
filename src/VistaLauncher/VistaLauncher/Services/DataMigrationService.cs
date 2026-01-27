using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 数据迁移接口
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// 检查是否需要迁移
    /// </summary>
    /// <param name="currentVersion">当前数据格式版本</param>
    /// <returns>是否需要迁移</returns>
    bool NeedsMigration(string? currentVersion);

    /// <summary>
    /// 执行迁移
    /// </summary>
    /// <param name="data">要迁移的数据</param>
    /// <param name="targetVersion">目标版本</param>
    /// <returns>迁移后的数据</returns>
    Task<ToolsData> MigrateAsync(ToolsData data, string targetVersion);

    /// <summary>
    /// 备份当前数据
    /// </summary>
    /// <param name="dataFilePath">数据文件路径</param>
    /// <returns>备份文件路径</returns>
    Task<string> BackupAsync(string dataFilePath);
}

/// <summary>
/// 数据迁移步骤接口
/// </summary>
public interface IDataMigrationStep
{
    /// <summary>
    /// 源版本
    /// </summary>
    string FromVersion { get; }

    /// <summary>
    /// 目标版本
    /// </summary>
    string ToVersion { get; }

    /// <summary>
    /// 执行迁移
    /// </summary>
    Task<ToolsData> MigrateAsync(ToolsData data);
}

/// <summary>
/// 数据迁移服务实现
/// </summary>
public sealed class DataMigrationService : IDataMigrationService
{
    private const int MaxBackupCount = 5;
    private readonly List<IDataMigrationStep> _migrationSteps;

    public DataMigrationService()
    {
        _migrationSteps = [new MigrationV1ToV2()];
    }

    public bool NeedsMigration(string? currentVersion)
    {
        if (string.IsNullOrEmpty(currentVersion))
            return true;

        return currentVersion != ToolsData.CurrentDataFormatVersion;
    }

    public async Task<ToolsData> MigrateAsync(ToolsData data, string targetVersion)
    {
        var currentVersion = data.DataFormatVersion ?? "1.0";
        var migrationPath = BuildMigrationPath(currentVersion, targetVersion);

        foreach (var step in migrationPath)
        {
            System.Diagnostics.Debug.WriteLine($"执行迁移: {step.FromVersion} -> {step.ToVersion}");
            data = await step.MigrateAsync(data);
        }

        data.DataFormatVersion = targetVersion;
        return data;
    }

    public async Task<string> BackupAsync(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
            throw new FileNotFoundException("数据文件不存在", dataFilePath);

        var backupDir = Path.GetDirectoryName(dataFilePath) ?? Path.GetTempPath();
        var fileName = Path.GetFileNameWithoutExtension(dataFilePath);
        var extension = Path.GetExtension(dataFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"{fileName}_backup_{timestamp}{extension}");

        await Task.Run(() => File.Copy(dataFilePath, backupPath, true));

        CleanupOldBackups(backupDir, fileName, extension);

        return backupPath;
    }

    /// <summary>
    /// 构建迁移路径
    /// </summary>
    private List<IDataMigrationStep> BuildMigrationPath(string fromVersion, string toVersion)
    {
        var path = new List<IDataMigrationStep>();
        var current = fromVersion;

        while (current != toVersion)
        {
            var step = _migrationSteps.FirstOrDefault(s => s.FromVersion == current);
            if (step == null)
            {
                System.Diagnostics.Debug.WriteLine($"未找到从 {current} 到 {toVersion} 的迁移步骤");
                break;
            }

            path.Add(step);
            current = step.ToVersion;
        }

        return path;
    }

    /// <summary>
    /// 清理旧备份文件
    /// </summary>
    private static void CleanupOldBackups(string directory, string fileName, string extension)
    {
        try
        {
            var backupFiles = Directory.GetFiles(directory, $"{fileName}_backup_*{extension}")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(MaxBackupCount)
                .ToList();

            foreach (var file in backupFiles)
            {
                try { File.Delete(file); }
                catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }
}

/// <summary>
/// 1.0 -> 2.0 迁移步骤
/// </summary>
public sealed class MigrationV1ToV2 : IDataMigrationStep
{
    public string FromVersion => "1.0";
    public string ToVersion => "2.0";

    public Task<ToolsData> MigrateAsync(ToolsData data)
    {
        foreach (var tool in data.Tools)
        {
            if (tool.UpdateSource == UpdateSource.None)
            {
                tool.UpdateSource = InferUpdateSource(tool);
            }
        }

        data.UpdateConfig ??= new UpdateConfig();
        data.DataFormatVersion = ToVersion;

        return Task.FromResult(data);
    }

    /// <summary>
    /// 根据工具的 HomepageUrl 推断 UpdateSource
    /// </summary>
    private static UpdateSource InferUpdateSource(ToolItem tool)
    {
        if (string.IsNullOrEmpty(tool.HomepageUrl))
            return UpdateSource.None;

        var url = tool.HomepageUrl.ToLowerInvariant();

        if (url.Contains("nirsoft.net"))
            return UpdateSource.NirSoft;

        if (url.Contains("github.com"))
            return UpdateSource.GitHub;

        if (url.Contains("learn.microsoft.com") ||
            url.Contains("sysinternals") ||
            url.Contains("microsoft.com"))
            return UpdateSource.Sysinternals;

        return UpdateSource.Custom;
    }
}
