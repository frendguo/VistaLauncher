using VistaLauncher.Models;
using Xunit;

namespace VistaLauncher.Tests;

/// <summary>
/// 开机自启动相关测试
/// </summary>
public class AutoStartServiceTests
{
    [Fact]
    public void StartupConfig_Initialization_HasDefaultValues()
    {
        // 验证 StartupConfig 有正确的默认值
        var config = new StartupConfig();

        // 默认情况下不启用开机自启动
        Assert.False(config.RunOnWindowsStartup);

        // 默认情况下启用最小化到托盘
        Assert.True(config.MinimizeToTray);
    }

    [Fact]
    public void StartupConfig_Properties_CanBeModified()
    {
        // 验证 StartupConfig 属性可以被修改
        var config = new StartupConfig();

        config.RunOnWindowsStartup = true;
        Assert.True(config.RunOnWindowsStartup);

        config.MinimizeToTray = false;
        Assert.False(config.MinimizeToTray);
    }

    [Fact]
    public void StartupConfig_Properties_NotificationWorks()
    {
        // 验证属性更改通知
        var config = new StartupConfig();
        bool propertyChanged = false;

        config.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(StartupConfig.RunOnWindowsStartup))
            {
                propertyChanged = true;
            }
        };

        config.RunOnWindowsStartup = true;
        Assert.True(propertyChanged);
    }
}
