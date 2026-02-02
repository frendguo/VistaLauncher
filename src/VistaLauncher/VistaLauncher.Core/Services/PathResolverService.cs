using System.Text;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 路径解析服务实现，用于解析工具配置中的路径变量
/// 支持的变量：
/// - ${ToolsPath}: 工具下载根目录，默认为 %AppData%\VistaLauncher\Tools
/// - ${TempPath}: 临时目录
/// - ${LocalAppData}: 本地应用数据目录
/// - ${AppData}: 漫游应用数据目录
/// </summary>
public sealed class PathResolverService : IPathResolverService
{
    private const string ToolsDirName = "VistaLauncher";
    private const string ToolsSubDirName = "Tools";

    private readonly string _toolsPath;

    /// <summary>
    /// 创建路径解析服务
    /// </summary>
    /// <param name="toolsPath">自定义工具目录路径（可选）</param>
    public PathResolverService(string? toolsPath = null)
    {
        _toolsPath = toolsPath ?? GetDefaultToolsPath();
        EnsureDirectoryExists(_toolsPath);
    }

    /// <summary>
    /// 获取默认工具目录路径
    /// </summary>
    private static string GetDefaultToolsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, ToolsDirName, ToolsSubDirName);
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var result = new StringBuilder(path);
        var environment = new Dictionary<string, string>
        {
            ["ToolsPath"] = _toolsPath,
            ["TempPath"] = Path.GetTempPath(),
            ["LocalAppData"] = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ["AppData"] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ["ProgramFiles"] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            ["ProgramFilesX86"] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        // 替换 ${VariableName} 格式的变量
        foreach (var (key, value) in environment)
        {
            result.Replace($"${{{key}}}", value);
        }

        // 替换 %VariableName% 格式的环境变量
        result.Replace("%LocalAppData%", environment["LocalAppData"]);
        result.Replace("%AppData%", environment["AppData"]);
        result.Replace("%Temp%", environment["TempPath"]);

        return result.ToString();
    }

    public string GetToolsDirectory()
    {
        return _toolsPath;
    }

    public string GetToolInstallDirectory(string toolId, string toolName)
    {
        // 使用工具 ID 作为子目录名称，确保唯一性
        var sanitizedName = SanitizeDirectoryName(toolName);
        var toolDir = Path.Combine(_toolsPath, sanitizedName);
        EnsureDirectoryExists(toolDir);
        return toolDir;
    }

    /// <summary>
    /// 清理目录名称，移除非法字符
    /// </summary>
    private static string SanitizeDirectoryName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitizedName = new StringBuilder(name);
        foreach (var c in invalidChars)
        {
            sanitizedName.Replace(c, '_');
        }
        // 移除或替换其他可能有问题的字符
        sanitizedName.Replace(' ', '_');
        sanitizedName.Replace('/', '_');
        sanitizedName.Replace('\\', '_');
        return sanitizedName.ToString();
    }
}
