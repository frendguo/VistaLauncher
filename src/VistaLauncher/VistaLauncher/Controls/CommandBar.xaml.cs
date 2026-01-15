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

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        // Flyout 自动处理
    }
}
