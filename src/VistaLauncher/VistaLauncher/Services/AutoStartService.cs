using Windows.ApplicationModel;

namespace VistaLauncher.Services;

/// <summary>
/// 开机自启动服务接口
/// </summary>
public interface IAutoStartService : IDisposable
{
    /// <summary>
    /// 获取当前是否启用开机自启动
    /// </summary>
    Task<bool> IsEnabledAsync();

    /// <summary>
    /// 启用开机自启动
    /// </summary>
    Task<bool> EnableAsync();

    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    Task<bool> DisableAsync();

    /// <summary>
    /// 切换开机自启动状态
    /// </summary>
    Task<bool> ToggleAsync();

    /// <summary>
    /// 检查是否支持开机自启动功能
    /// </summary>
    bool IsSupported();
}

/// <summary>
/// 开机自启动服务实现
/// 使用 Windows.ApplicationModel.StartupTask API (适用于 MSIX 打包应用)
/// </summary>
public class AutoStartService : IAutoStartService
{
    private const string StartupTaskId = "VistaLauncherStartup";

    /// <summary>
    /// 获取当前是否启用开机自启动
    /// </summary>
    public async Task<bool> IsEnabledAsync()
    {
        if (!IsSupported())
        {
            return false;
        }

        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            return startupTask.State == StartupTaskState.Enabled ||
                   startupTask.State == StartupTaskState.EnabledByPolicy;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public async Task<bool> EnableAsync()
    {
        if (!IsSupported())
        {
            return false;
        }

        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);

            // 如果已经启用，直接返回成功
            if (startupTask.State == StartupTaskState.Enabled ||
                startupTask.State == StartupTaskState.EnabledByPolicy)
            {
                return true;
            }

            // 请求启用开机自启动
            var result = await startupTask.RequestEnableAsync();
            return result == StartupTaskState.Enabled ||
                   result == StartupTaskState.EnabledByPolicy;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public async Task<bool> DisableAsync()
    {
        if (!IsSupported())
        {
            return false;
        }

        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);

            if (startupTask.State == StartupTaskState.Disabled ||
                startupTask.State == StartupTaskState.DisabledByUser ||
                startupTask.State == StartupTaskState.DisabledByPolicy)
            {
                return true;
            }

            startupTask.Disable();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 切换开机自启动状态
    /// </summary>
    public async Task<bool> ToggleAsync()
    {
        var isEnabled = await IsEnabledAsync();
        return isEnabled ? await DisableAsync() : await EnableAsync();
    }

    /// <summary>
    /// 检查是否支持开机自启动功能
    /// </summary>
    public bool IsSupported()
    {
        try
        {
            // 只有在 MSIX 打包的应用中才能使用 StartupTask API
            return !string.IsNullOrEmpty(Package.Current.Id.Name);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        // 清理资源（如果有）
        GC.SuppressFinalize(this);
    }
}
