using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VistaLauncher.Controls;

public sealed partial class CommandBar : UserControl
{
    public CommandBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 是否有选中项
    /// </summary>
    public bool HasSelection
    {
        get => (bool)GetValue(HasSelectionProperty);
        set => SetValue(HasSelectionProperty, value);
    }

    public static readonly DependencyProperty HasSelectionProperty =
        DependencyProperty.Register(nameof(HasSelection), typeof(bool), typeof(CommandBar),
            new PropertyMetadata(false));

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(CommandBar),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// 主要命令点击事件
    /// </summary>
    public event RoutedEventHandler? PrimaryCommand;

    /// <summary>
    /// 次要命令点击事件
    /// </summary>
    public event RoutedEventHandler? SecondaryCommand;

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    public event RoutedEventHandler? SettingsClick;

    /// <summary>
    /// 打开文件位置事件
    /// </summary>
    public event RoutedEventHandler? OpenFileLocationClick;

    /// <summary>
    /// 复制路径事件
    /// </summary>
    public event RoutedEventHandler? CopyPathClick;

    /// <summary>
    /// 编辑工具事件
    /// </summary>
    public event RoutedEventHandler? EditToolClick;

    /// <summary>
    /// 删除工具事件
    /// </summary>
    public event RoutedEventHandler? RemoveToolClick;

    /// <summary>
    /// 添加工具事件
    /// </summary>
    public event RoutedEventHandler? AddToolClick;

    /// <summary>
    /// 导入 NirLauncher 事件
    /// </summary>
    public event RoutedEventHandler? ImportNirLauncherClick;

    /// <summary>
    /// 检查更新事件
    /// </summary>
    public event RoutedEventHandler? CheckUpdatesClick;

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        PrimaryCommand?.Invoke(this, e);
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        SecondaryCommand?.Invoke(this, e);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsClick?.Invoke(this, e);
    }

    private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
    {
        OpenFileLocationClick?.Invoke(this, e);
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        CopyPathClick?.Invoke(this, e);
    }

    private void EditTool_Click(object sender, RoutedEventArgs e)
    {
        EditToolClick?.Invoke(this, e);
    }

    private void RemoveTool_Click(object sender, RoutedEventArgs e)
    {
        RemoveToolClick?.Invoke(this, e);
    }

    private void AddTool_Click(object sender, RoutedEventArgs e)
    {
        AddToolClick?.Invoke(this, e);
    }

    private void ImportNirLauncher_Click(object sender, RoutedEventArgs e)
    {
        ImportNirLauncherClick?.Invoke(this, e);
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdatesClick?.Invoke(this, e);
    }
}
