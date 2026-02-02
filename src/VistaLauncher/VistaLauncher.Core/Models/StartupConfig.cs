using CommunityToolkit.Mvvm.ComponentModel;

namespace VistaLauncher.Models;

/// <summary>
/// 启动配置
/// </summary>
public partial class StartupConfig : ObservableObject
{
    /// <summary>
    /// 开机自启动
    /// </summary>
    [ObservableProperty]
    private bool _runOnWindowsStartup = false;

    /// <summary>
    /// 最小化到托盘
    /// </summary>
    [ObservableProperty]
    private bool _minimizeToTray = true;
}
