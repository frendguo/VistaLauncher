using CommunityToolkit.Mvvm.ComponentModel;

namespace VistaLauncher.Models;

/// <summary>
/// 工具分组
/// </summary>
public partial class ToolGroup : ObservableObject
{
    /// <summary>
    /// 分组唯一标识
    /// </summary>
    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// 分组显示名称
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 分组图标 (emoji 或图标路径)
    /// </summary>
    [ObservableProperty]
    private string _icon = string.Empty;
}
