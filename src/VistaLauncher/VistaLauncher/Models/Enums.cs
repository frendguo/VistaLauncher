namespace VistaLauncher.Models;

/// <summary>
/// 工具类型
/// </summary>
public enum ToolType
{
    /// <summary>
    /// 图形界面应用
    /// </summary>
    GUI,

    /// <summary>
    /// 控制台应用
    /// </summary>
    Console
}

/// <summary>
/// 应用架构
/// </summary>
public enum Architecture
{
    /// <summary>
    /// 64 位应用
    /// </summary>
    x64,

    /// <summary>
    /// 32 位应用
    /// </summary>
    x86
}

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
