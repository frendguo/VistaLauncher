using System.Diagnostics;

namespace VistaLauncher.Core.Services;

/// <summary>
/// 文件版本信息辅助类
/// </summary>
public static class FileVersionHelper
{
    /// <summary>
    /// 从可执行文件中提取版本号
    /// </summary>
    /// <param name="exePath">可执行文件路径</param>
    /// <returns>版本号字符串，提取失败返回空字符串</returns>
    public static string ExtractVersionFromExe(string exePath)
    {
        try
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return string.Empty;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            var version = versionInfo.FileVersion?.Trim();

            return string.IsNullOrEmpty(version) ? string.Empty : version;
        }
        catch
        {
            // 如果提取失败，返回空字符串
            return string.Empty;
        }
    }
}
