using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VistaLauncher.Services;
using VistaLauncher.Models;

namespace VistaLauncher.ViewModels;

/// <summary>
/// 设置窗口的 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IAutoStartService _autoStartService;
    private readonly ConfigService _configService;

    [ObservableProperty]
    private bool _autoStartEnabled;

    [ObservableProperty]
    private bool _isAutoStartSupported;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(IAutoStartService autoStartService, ConfigService configService)
    {
        _autoStartService = autoStartService;
        _configService = configService;
    }

    /// <summary>
    /// 初始化设置
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;

        try
        {
            IsAutoStartSupported = _autoStartService.IsSupported();

            if (IsAutoStartSupported)
            {
                AutoStartEnabled = await _autoStartService.IsEnabledAsync();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 切换开机自启动
    /// </summary>
    public async Task ToggleAutoStartAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var newState = await _autoStartService.ToggleAsync();
            AutoStartEnabled = newState;

            // 保存到配置
            var config = await _configService.GetConfigAsync();
            config.Startup.RunOnWindowsStartup = newState;
            await _configService.SaveConfigAsync(config);

            StatusMessage = newState ? "已启用开机自启动" : "已禁用开机自启动";
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var config = await _configService.GetConfigAsync();
            config.Startup.RunOnWindowsStartup = AutoStartEnabled;
            await _configService.SaveConfigAsync(config);

            StatusMessage = "设置已保存";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
