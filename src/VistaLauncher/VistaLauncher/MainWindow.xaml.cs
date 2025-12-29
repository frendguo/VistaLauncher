using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using VistaLauncher.Services;
using VistaLauncher.ViewModels;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace VistaLauncher;

/// <summary>
/// 主启动器窗口
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow? _appWindow;
    private HotkeyService? _hotkeyService;
    private IntPtr _hWnd;
    
    // 固定尺寸
    private const int WindowWidth = 680;
    private const int CollapsedHeight = 90;
    private const int ItemHeight = 52;
    private const int MaxVisibleItems = 9;
    
    // Win32 API
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
    
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;
    
    public LauncherViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        // 创建服务和 ViewModel
        var toolDataService = new ToolDataService();
        var searchProvider = new TextMatchSearchProvider();
        var processLauncher = new Services.ProcessLauncher();

        ViewModel = new LauncherViewModel(toolDataService, searchProvider, processLauncher);

        // 设置窗口属性
        SetupWindow();
        
        // 设置热键
        SetupHotkey();
        
        // 初始化数据
        _ = InitializeAsync();
        
        // 窗口关闭时清理
        Closed += MainWindow_Closed;
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
            
            // 设置窗口圆角 (Windows 11)
            SetWindowRoundCorners();
            
            // 设置窗口初始大小
            _appWindow.Resize(new SizeInt32(WindowWidth, CollapsedHeight));
            
            // 居中显示
            CenterWindow();
        }
    }

    private void RemoveWindowBorder()
    {
        // 获取当前窗口样式
        var style = GetWindowLong(_hWnd, GWL_STYLE);
        
        // 移除标题栏、边框、系统菜单
        style &= ~(int)(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        
        SetWindowLong(_hWnd, GWL_STYLE, style);
        
        // 获取扩展样式
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);
        
        // 移除对话框边框样式
        exStyle &= ~(int)(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
        
        SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);
        
        // 应用更改
        SetWindowPos(_hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
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
            if (_appWindow != null)
            {
                _appWindow.Show(true);
                Activate();
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.SelectAll();
            }
        });
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
            var y = (workArea.Height - windowSize.Height) / 3 + workArea.Y;
            
            _appWindow.Move(new PointInt32(x, y));
        }
    }

    private void UpdateWindowSize()
    {
        if (_appWindow == null) return;

        int newHeight;
        if (ViewModel.IsExpanded && ViewModel.FilteredTools.Count > 0)
        {
            var itemCount = Math.Min(ViewModel.FilteredTools.Count, MaxVisibleItems);
            var listHeight = itemCount * ItemHeight;
            newHeight = CollapsedHeight + 1 + listHeight + 8;
        }
        else
        {
            newHeight = CollapsedHeight;
        }
        
        _appWindow.Resize(new SizeInt32(WindowWidth, newHeight));
        CenterWindow();
    }

    private async Task InitializeAsync()
    {
        await ViewModel.InitializeAsync();
        
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsExpanded) || 
                e.PropertyName == nameof(ViewModel.FilteredTools))
            {
                UpdateWindowSize();
            }
        };
        
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // 处理 Ctrl+数字快捷键
        var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrlPressed && e.Key >= VirtualKey.Number1 && e.Key <= VirtualKey.Number9)
        {
            var index = (int)e.Key - (int)VirtualKey.Number1;
            ViewModel.LaunchByIndexCommand.Execute(index);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Down:
                ViewModel.IsExpanded = true;
                ViewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Up:
                ViewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Enter:
                ViewModel.LaunchSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case VirtualKey.Escape:
                if (ViewModel.IsExpanded)
                {
                    ViewModel.CollapseCommand.Execute(null);
                    SearchBox.Focus(FocusState.Programmatic);
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
                    ViewModel.IsExpanded = true;
                }
                e.Handled = true;
                break;
        }
    }

    private void HideWindow()
    {
        ViewModel.SearchQuery = string.Empty;
        ViewModel.IsExpanded = false;
        _appWindow?.Hide();
    }

    private void ToolsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolItemViewModel tool)
        {
            ViewModel.SelectedTool = tool;
        }
    }

    private void ToolsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.LaunchSelectedCommand.Execute(null);
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _hotkeyService?.Dispose();
    }
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
}
