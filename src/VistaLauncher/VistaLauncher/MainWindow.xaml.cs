using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using VistaLauncher.Controls;
using VistaLauncher.Services;
using VistaLauncher.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using WinUIEx;

namespace VistaLauncher;

/// <summary>
/// 主启动器窗口
/// </summary>
public sealed partial class MainWindow : WindowEx
{
    private AppWindow? _appWindow;
    private HotkeyService? _hotkeyService;
    private IntPtr _hWnd;
    private bool _isVisible;
    private bool _suppressHideOnDeactivate;

    // 服务
    private readonly IToolDataService _toolDataService;
    private readonly IToolManagementService _toolManagementService;
    private readonly IImportService _importService;
    private readonly IVersionCheckService _versionCheckService;
    private readonly IUpdateService _updateService;

    // Win32 API 常量
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_EX_DLGMODALFRAME = 0x00000001;
    private const uint WS_EX_WINDOWEDGE = 0x00000100;
    private const uint WS_EX_CLIENTEDGE = 0x00000200;
    private const uint WS_EX_STATICEDGE = 0x00020000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_APPWINDOW = 0x00040000;

    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_SHOWNA = 8;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_BOTTOM = new(1);

    // DWM 常量
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_CLOAK = 13;
    private const int DWMWCP_ROUND = 2;

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    #endregion

    public LauncherViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        // 创建服务
        _toolDataService = new ToolDataService();
        var searchProvider = new TextMatchSearchProvider();
        var processLauncher = new Services.ProcessLauncher();

        // 创建管理服务
        _toolManagementService = new ToolManagementService(_toolDataService);
        _importService = new ImportService(_toolDataService);

        // 创建更新服务
        _versionCheckService = new VersionCheckService();
        _updateService = new UpdateService(_toolDataService);

        // 创建 ViewModel
        ViewModel = new LauncherViewModel(_toolDataService, searchProvider, processLauncher);

        // 设置窗口属性
        SetupWindow();

        // 设置热键
        SetupHotkey();

        // 初始化数据
        _ = InitializeAsync();

        // 监听窗口激活状态变化 (失去焦点时隐藏)
        Activated += MainWindow_Activated;

        // 窗口关闭时清理
        Closed += MainWindow_Closed;

        // 连接 CommandBar 事件
        CommandBar.PrimaryCommand += async (s, e) =>
        {
            await ViewModel.LaunchSelectedAsync();
            HideWindow();
        };

        CommandBar.SecondaryCommand += async (s, e) =>
        {
            await ViewModel.LaunchSelectedAsAdminAsync();
            HideWindow();
        };

        CommandBar.SettingsClick += (s, e) =>
        {
            // TODO: 打开设置窗口
        };

        // 连接 CommandBar 更多菜单事件
        CommandBar.OpenFileLocationClick += CommandBar_OpenFileLocationClick;
        CommandBar.CopyPathClick += CommandBar_CopyPathClick;
        CommandBar.EditToolClick += CommandBar_EditToolClick;
        CommandBar.RemoveToolClick += CommandBar_RemoveToolClick;
        CommandBar.AddToolClick += CommandBar_AddToolClick;
        CommandBar.ImportNirLauncherClick += CommandBar_ImportNirLauncherClick;
        CommandBar.CheckUpdatesClick += CommandBar_CheckUpdatesClick;

        // 连接 SearchBar 键盘事件
        SearchBox.TextBoxKeyDown += SearchBox_KeyDown;
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // 处理 Ctrl+数字快捷键
        var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrlPressed && e.Key >= VirtualKey.Number1 && e.Key <= VirtualKey.Number9)
        {
            var index = (int)e.Key - (int)VirtualKey.Number1;
            await ViewModel.LaunchByIndexAsync(index);
            HideWindow();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Down:
                ViewModel.ExpandCommand.Execute(null);
                ViewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Up:
                ViewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Enter:
                if (ctrlPressed)
                {
                    // Ctrl+Enter: 以管理员身份运行
                    await ViewModel.LaunchSelectedAsAdminAsync();
                }
                else
                {
                    // Enter: 普通运行
                    await ViewModel.LaunchSelectedAsync();
                }
                HideWindow();
                e.Handled = true;
                break;

            case VirtualKey.Escape:
                if (ViewModel.IsExpanded)
                {
                    ViewModel.CollapseCommand.Execute(null);
                    SearchBox.Focus();
                }
                else
                {
                    HideWindow();
                }
                e.Handled = true;
                break;

            case VirtualKey.Tab:
                if (!ViewModel.IsExpanded)
                {
                    ViewModel.ExpandCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }

    private void SetupWindow()
    {
        Title = "VistaLauncher";

        // 获取窗口句柄
        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // 使用 Win32 API 移除窗口边框
            RemoveWindowBorder();

            // 设置为工具窗口 (不显示在任务栏)
            SetToolWindowStyle();

            // 设置窗口圆角 (Windows 11)
            SetWindowRoundCorners();

            // 窗口大小由 XAML 中的 Width/Height 控制（逻辑像素，WindowEx 会自动处理 DPI 缩放）

            // 居中显示
            CenterWindow();

            // 初始隐藏窗口 (使用 Cloak 避免闪烁)
            HideWindowInternal();
        }
    }

    private void RemoveWindowBorder()
    {
        // 获取当前窗口样式
        var style = GetWindowLong(_hWnd, GWL_STYLE);

        // 移除标题栏、边框、系统菜单
        style &= ~(int)(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);

        SetWindowLong(_hWnd, GWL_STYLE, style);

        // 应用更改
        SetWindowPos(_hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
    }

    private void SetToolWindowStyle()
    {
        // 获取扩展样式
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);

        // 移除对话框边框样式，添加工具窗口样式 (不显示在任务栏)
        exStyle &= ~(int)(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE | WS_EX_APPWINDOW);
        exStyle |= (int)WS_EX_TOOLWINDOW;

        SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);
    }

    private void SetWindowRoundCorners()
    {
        // Windows 11 圆角窗口
        var preference = DWMWCP_ROUND;
        DwmSetWindowAttribute(_hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }

    private void SetupHotkey()
    {
        _hotkeyService = new HotkeyService();
        _hotkeyService.Initialize(this);

        // 注册 Ctrl+F2 热键
        var registered = _hotkeyService.RegisterHotKey(
            HotkeyModifiers.Control,
            VirtualKeyCodes.VK_F2);

        if (registered)
        {
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isVisible)
            {
                // 如果已可见，则隐藏
                HideWindow();
            }
            else
            {
                // 如果不可见，则显示
                ShowWindowInternal();
            }
        });
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    private void ShowWindowInternal()
    {
        if (_appWindow == null) return;

        // 居中定位
        CenterWindow();

        // 显示窗口
        ShowWindow(_hWnd, SW_SHOW);

        // 取消 Cloak
        Uncloak();

        // 设置前景窗口
        SetForegroundWindow(_hWnd);
        SetActiveWindow(_hWnd);

        // 置顶
        SetWindowPos(_hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

        _isVisible = true;

        // 聚焦搜索框
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    /// <summary>
    /// 隐藏窗口 (内部方法，不重置状态)
    /// </summary>
    private void HideWindowInternal()
    {
        // 使用 Cloak 隐藏窗口，避免动画
        Cloak();

        // 隐藏 HWND
        ShowWindow(_hWnd, SW_HIDE);

        // 再次显示窗口 (但保持 cloaked)，避免 WinUI3 首次显示时闪烁
        ShowWindow(_hWnd, SW_SHOWNA);

        // 将窗口放到底层
        SetWindowPos(_hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        _isVisible = false;
    }

    /// <summary>
    /// 隐藏窗口并重置状态
    /// </summary>
    private void HideWindow()
    {
        ViewModel.SearchQuery = string.Empty;
        ViewModel.IsExpanded = false;
        HideWindowInternal();
    }

    /// <summary>
    /// 设置是否抑制失去焦点时自动隐藏（用于打开对话框或文件选择器时）
    /// </summary>
    public void SuppressAutoHide(bool suppress)
    {
        _suppressHideOnDeactivate = suppress;
    }

    /// <summary>
    /// Cloak 窗口 (使窗口不可见但仍存在)
    /// </summary>
    private void Cloak()
    {
        var value = 1; // TRUE
        DwmSetWindowAttribute(_hWnd, DWMWA_CLOAK, ref value, sizeof(int));
    }

    /// <summary>
    /// Uncloak 窗口 (使窗口可见)
    /// </summary>
    private void Uncloak()
    {
        var value = 0; // FALSE
        DwmSetWindowAttribute(_hWnd, DWMWA_CLOAK, ref value, sizeof(int));
    }

    private void CenterWindow()
    {
        if (_appWindow == null) return;

        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var windowSize = _appWindow.Size;

            var x = (workArea.Width - windowSize.Width) / 2 + workArea.X;
            // 放在屏幕上方 1/3 处
            var y = (workArea.Height - windowSize.Height) / 3 + workArea.Y;

            _appWindow.Move(new PointInt32(x, y));
        }
    }

    private async Task InitializeAsync()
    {
        await ViewModel.InitializeAsync();

        // 不再需要监听属性变化来调整窗口大小
        // 窗口尺寸现在是固定的
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // 窗口失去焦点时隐藏（除非被抑制，如打开文件选择器时）
            if (_isVisible && !_suppressHideOnDeactivate)
            {
                HideWindow();
            }
        }
    }

    private void ToolsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolItemViewModel tool)
        {
            ViewModel.SelectedTool = tool;
        }
    }

    /// <summary>
    /// ListView 容器内容变化事件 - 用于延迟加载图标
    /// </summary>
    private void ToolsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
        {
            // 项目被回收，不做处理
            return;
        }

        if (args.Item is ToolItemViewModel vm)
        {
            // 阶段 0: 注册回调，在项目首次可见时触发图标加载
            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(ContainerContentChanging_DeferredIconLoad);
            }
            // 阶段 1: 触发延迟加载
            else if (args.Phase == 1)
            {
                // 只在项目可见时加载图标
                vm.RequestLoadIcon();
            }
        }
    }

    /// <summary>
    /// 延迟图标加载的回调
    /// </summary>
    private void ContainerContentChanging_DeferredIconLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is ToolItemViewModel vm)
        {
            // 触发图标加载
            vm.RequestLoadIcon();
        }

        // 标记处理完成
        args.Handled = true;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _hotkeyService?.Dispose();
    }

    #region CommandBar 事件处理

    private void CommandBar_OpenFileLocationClick(object sender, RoutedEventArgs e)
    {
        var selectedTool = ViewModel.SelectedTool;
        if (selectedTool?.ToolItem.ExecutablePath == null) return;

        var path = selectedTool.ToolItem.ExecutablePath;
        if (File.Exists(path))
        {
            // 打开文件所在目录并选中文件
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        else
        {
            var directory = Path.GetDirectoryName(path);
            if (directory != null && Directory.Exists(directory))
            {
                Process.Start("explorer.exe", directory);
            }
        }
    }

    private void CommandBar_CopyPathClick(object sender, RoutedEventArgs e)
    {
        var selectedTool = ViewModel.SelectedTool;
        if (selectedTool?.ToolItem.ExecutablePath == null) return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(selectedTool.ToolItem.ExecutablePath);
        Clipboard.SetContent(dataPackage);
    }

    private async void CommandBar_EditToolClick(object sender, RoutedEventArgs e)
    {
        var selectedTool = ViewModel.SelectedTool;
        if (selectedTool == null) return;

        var dialog = new AddToolDialog(_toolManagementService)
        {
            EditingTool = selectedTool.ToolItem,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.SavedTool != null)
        {
            // 刷新列表
            await ViewModel.RefreshAsync();
        }
    }

    private async void CommandBar_RemoveToolClick(object sender, RoutedEventArgs e)
    {
        var selectedTool = ViewModel.SelectedTool;
        if (selectedTool == null) return;

        // 确认删除
        var confirmDialog = new ContentDialog
        {
            Title = "确认删除",
            Content = $"确定要删除工具 \"{selectedTool.Name}\" 吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _toolManagementService.DeleteToolAsync(selectedTool.ToolItem.Id);
            await ViewModel.RefreshAsync();
        }
    }

    private async void CommandBar_AddToolClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddToolDialog(_toolManagementService)
        {
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.SavedTool != null)
        {
            // 刷新列表
            await ViewModel.RefreshAsync();
        }
    }

    private async void CommandBar_ImportNirLauncherClick(object sender, RoutedEventArgs e)
    {
        // 打开对话框前禁用自动隐藏
        SuppressAutoHide(true);

        try
        {
            var dialog = new ImportDialog(_importService)
            {
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var path = dialog.SelectedPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    await ShowMessageAsync("提示", "请选择 NirLauncher 目录");
                    return;
                }

                // 验证目录
                if (!_importService.ValidateNirLauncherDirectory(path))
                {
                    await ShowMessageAsync("错误", "无效的 NirLauncher 目录，请确保目录中包含 NirLauncher.cfg 文件");
                    return;
                }

                // 执行导入
                var importResult = await _importService.ImportFromNirLauncherAsync(path, CancellationToken.None);

                // 刷新列表
                await ViewModel.RefreshAsync();

                // 显示导入结果
                var message = $"导入完成\n成功: {importResult.ImportedTools} 个工具\n跳过: {importResult.SkippedTools} 个工具";
                if (importResult.Errors.Count > 0)
                {
                    message += $"\n错误: {importResult.Errors.Count} 个";
                }
                await ShowMessageAsync("导入结果", message);
            }
        }
        finally
        {
            // 恢复自动隐藏
            SuppressAutoHide(false);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void CommandBar_CheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        // 打开对话框前禁用自动隐藏
        SuppressAutoHide(true);

        try
        {
            var dialog = new UpdateDialog(_versionCheckService, _updateService, _toolDataService)
            {
                XamlRoot = Content.XamlRoot
            };

            // 获取所有工具
            var tools = await _toolDataService.GetToolsAsync();

            // 显示对话框并开始检查
            var showTask = dialog.ShowAsync();
            await dialog.CheckUpdatesAsync(tools);

            // 等待对话框关闭
            await showTask;

            // 刷新列表
            await ViewModel.RefreshAsync();
        }
        finally
        {
            // 恢复自动隐藏
            SuppressAutoHide(false);
        }
    }

    #endregion
}

/// <summary>
/// XAML 转换器
/// </summary>
public static class Converters
{
    public static Visibility IsNullToVisibility(object? value)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility IsNotNullToVisibility(object? value)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility BoolToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility InvertedBoolToVisibility(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }
}
