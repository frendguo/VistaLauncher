namespace VistaLauncher.Services;

/// <summary>
/// 路径解析服务接口，用于解析工具配置中的路径变量
/// </summary>
public interface IPathResolverService
{
    /// <summary>
    /// 解析路径字符串，替换其中的变量占位符
    /// </summary>
    /// <param name="path">包含变量占位符的路径</param>
    /// <returns>解析后的绝对路径</returns>
    string ResolvePath(string path);

    /// <summary>
    /// 获取工具下载目录的默认路径
    /// </summary>
    /// <returns>工具下载目录路径</returns>
    string GetToolsDirectory();

    /// <summary>
    /// 获取指定工具的安装目录
    /// </summary>
    /// <param name="toolId">工具 ID</param>
    /// <param name="toolName">工具名称（用于创建子目录）</param>
    /// <returns>工具安装目录路径</returns>
    string GetToolInstallDirectory(string toolId, string toolName);
}
