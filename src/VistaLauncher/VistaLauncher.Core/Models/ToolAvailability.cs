namespace VistaLauncher.Models;

/// <summary>
/// 工具可用性状态
/// </summary>
public enum ToolAvailability
{
    /// <summary>
    /// 工具已安装，可以正常使用
    /// </summary>
    Available,

    /// <summary>
    /// 工具未安装，需要先下载
    /// </summary>
    NotInstalled,

    /// <summary>
    /// 工具有可用更新
    /// </summary>
    UpdateAvailable
}
