using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VistaLauncher.Services;
using VistaLauncher.ViewModels;

namespace VistaLauncher.Controls;

/// <summary>
/// 设置对话框
/// </summary>
public sealed partial class SettingsDialog : ContentDialog
{
    private readonly SettingsViewModel _viewModel;

    public SettingsDialog(IAutoStartService autoStartService, ConfigService configService)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(autoStartService, configService);

        // 加载设置
        Loaded += SettingsDialog_Loaded;
    }

    private async void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();

        // 根据支持状态显示/隐藏提示和开关
        NotSupportedInfoBar.IsOpen = !_viewModel.IsAutoStartSupported;
        AutoStartToggleSwitch.IsEnabled = _viewModel.IsAutoStartSupported;
        AutoStartToggleSwitch.IsOn = _viewModel.AutoStartEnabled;
    }

    private async void AutoStartToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            await _viewModel.ToggleAutoStartAsync();

            // 更新状态消息
            if (!string.IsNullOrEmpty(_viewModel.StatusMessage))
            {
                StatusTextBlock.Text = _viewModel.StatusMessage;
                StatusTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                StatusTextBlock.Visibility = Visibility.Collapsed;
            }
        }
    }
}
