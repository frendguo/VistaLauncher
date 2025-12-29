using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace VistaLauncher.Services;

/// <summary>
/// 热键服务接口
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// 注册全局热键
    /// </summary>
    /// <param name="modifiers">修饰键</param>
    /// <param name="key">主键</param>
    /// <returns>是否注册成功</returns>
    bool RegisterHotKey(HotkeyModifiers modifiers, uint key);

    /// <summary>
    /// 取消注册热键
    /// </summary>
    void UnregisterHotKey();

    /// <summary>
    /// 热键按下事件
    /// </summary>
    event EventHandler? HotkeyPressed;
}

/// <summary>
/// 热键修饰键
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000
}

/// <summary>
/// 虚拟键码常量
/// </summary>
public static class VirtualKeyCodes
{
    public const uint VK_F1 = 0x70;
    public const uint VK_F2 = 0x71;
    public const uint VK_F3 = 0x72;
    public const uint VK_F4 = 0x73;
    public const uint VK_F5 = 0x74;
    public const uint VK_F6 = 0x75;
    public const uint VK_F7 = 0x76;
    public const uint VK_F8 = 0x77;
    public const uint VK_F9 = 0x78;
    public const uint VK_F10 = 0x79;
    public const uint VK_F11 = 0x7A;
    public const uint VK_F12 = 0x7B;
    public const uint VK_SPACE = 0x20;
}

/// <summary>
/// 热键服务实现
/// </summary>
public partial class HotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    private IntPtr _windowHandle;
    private bool _isRegistered = false;
    private nint _originalWndProc;
    private WNDPROC? _newWndProc;

    public event EventHandler? HotkeyPressed;

    // Win32 API declarations
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static partial nint CallWindowProc(nint lpPrevWndFunc, IntPtr hWnd, uint Msg, nint wParam, nint lParam);

    private const int GWLP_WNDPROC = -4;

    private delegate nint WNDPROC(IntPtr hWnd, uint Msg, nint wParam, nint lParam);

    /// <summary>
    /// 初始化热键服务并关联窗口
    /// </summary>
    public void Initialize(Window window)
    {
        _windowHandle = WindowNative.GetWindowHandle(window);
        
        // 子类化窗口以接收热键消息
        _newWndProc = new WNDPROC(WndProc);
        _originalWndProc = GetWindowLongPtr(_windowHandle, GWLP_WNDPROC);
        SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    private nint WndProc(IntPtr hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_HOTKEY && wParam == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return IntPtr.Zero;
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    public bool RegisterHotKey(HotkeyModifiers modifiers, uint key)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("HotkeyService must be initialized with a window first.");
        }

        // 先取消之前的注册
        if (_isRegistered)
        {
            UnregisterHotKey();
        }

        _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)modifiers | (uint)HotkeyModifiers.NoRepeat, key);
        return _isRegistered;
    }

    public void UnregisterHotKey()
    {
        if (_isRegistered && _windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    public void Dispose()
    {
        UnregisterHotKey();
        
        // 恢复原始窗口过程
        if (_originalWndProc != IntPtr.Zero && _windowHandle != IntPtr.Zero)
        {
            SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _originalWndProc);
        }
        
        GC.SuppressFinalize(this);
    }
}
