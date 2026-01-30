using CommunityToolkit.Mvvm.ComponentModel;

namespace VistaLauncher.Models;

/// <summary>
/// 热键配置
/// </summary>
public partial class HotkeyConfig : ObservableObject
{
    /// <summary>
    /// 修饰键列表 (Ctrl, Alt, Shift, Win)
    /// </summary>
    [ObservableProperty]
    private List<string> _modifiers = ["Ctrl"];

    /// <summary>
    /// 主键
    /// </summary>
    [ObservableProperty]
    private string _key = "F2";
}

/// <summary>
/// UI 配置
/// </summary>
public partial class UIConfig : ObservableObject
{
    /// <summary>
    /// 主题 (System, Light, Dark)
    /// </summary>
    [ObservableProperty]
    private string _theme = "System";

    /// <summary>
    /// 窗口宽度
    /// </summary>
    [ObservableProperty]
    private int _windowWidth = 600;

    /// <summary>
    /// 窗口高度
    /// </summary>
    [ObservableProperty]
    private int _windowHeight = 400;

    /// <summary>
    /// 失去焦点时隐藏
    /// </summary>
    [ObservableProperty]
    private bool _hideOnFocusLost = true;
}

/// <summary>
/// 搜索配置
/// </summary>
public partial class SearchConfig : ObservableObject
{
    /// <summary>
    /// 搜索提供者 (TextMatch, CloudAI)
    /// </summary>
    [ObservableProperty]
    private string _provider = "CloudAI";

    /// <summary>
    /// AI 提供者 (OpenAI, Azure, etc.)
    /// </summary>
    [ObservableProperty]
    private string _aiProvider = "OpenAI";

    /// <summary>
    /// AI API 端点
    /// </summary>
    [ObservableProperty]
    private string _aiEndpoint = "https://api.openai.com/v1";

    /// <summary>
    /// AI API 密钥
    /// </summary>
    [ObservableProperty]
    private string _aiApiKey = string.Empty;

    /// <summary>
    /// AI 模型名称
    /// </summary>
    [ObservableProperty]
    private string _aiModel = "gpt-4o-mini";

    /// <summary>
    /// 是否回退到文本匹配
    /// </summary>
    [ObservableProperty]
    private bool _fallbackToTextMatch = true;
}

/// <summary>
/// 应用配置
/// </summary>
public partial class AppConfig : ObservableObject
{
    /// <summary>
    /// 配置版本
    /// </summary>
    [ObservableProperty]
    private string _version = "1.0.0";

    /// <summary>
    /// 热键配置
    /// </summary>
    [ObservableProperty]
    private HotkeyConfig _hotkey = new();

    /// <summary>
    /// UI 配置
    /// </summary>
    [ObservableProperty]
    private UIConfig _ui = new();

    /// <summary>
    /// 搜索配置
    /// </summary>
    [ObservableProperty]
    private SearchConfig _search = new();

    /// <summary>
    /// 启动配置
    /// </summary>
    [ObservableProperty]
    private StartupConfig _startup = new();
}
