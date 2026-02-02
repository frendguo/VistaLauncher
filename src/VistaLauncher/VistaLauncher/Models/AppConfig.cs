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
    public partial List<string> Modifiers { get; set; } = ["Ctrl"];

    /// <summary>
    /// 主键
    /// </summary>
    [ObservableProperty]
    public partial string Key { get; set; } = "F2";
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
    public partial string Theme { get; set; } = "System";

    /// <summary>
    /// 窗口宽度
    /// </summary>
    [ObservableProperty]
    public partial int WindowWidth { get; set; } = 600;

    /// <summary>
    /// 窗口高度
    /// </summary>
    [ObservableProperty]
    public partial int WindowHeight { get; set; } = 400;

    /// <summary>
    /// 失去焦点时隐藏
    /// </summary>
    [ObservableProperty]
    public partial bool HideOnFocusLost { get; set; } = true;
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
    public partial string Provider { get; set; } = "CloudAI";

    /// <summary>
    /// AI 提供者 (OpenAI, Azure, etc.)
    /// </summary>
    [ObservableProperty]
    public partial string AiProvider { get; set; } = "OpenAI";

    /// <summary>
    /// AI API 端点
    /// </summary>
    [ObservableProperty]
    public partial string AiEndpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// AI API 密钥
    /// </summary>
    [ObservableProperty]
    public partial string AiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// AI 模型名称
    /// </summary>
    [ObservableProperty]
    public partial string AiModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 是否回退到文本匹配
    /// </summary>
    [ObservableProperty]
    public partial bool FallbackToTextMatch { get; set; } = true;
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
    public partial string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 热键配置
    /// </summary>
    [ObservableProperty]
    public partial HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>
    /// UI 配置
    /// </summary>
    [ObservableProperty]
    public partial UIConfig Ui { get; set; } = new();

    /// <summary>
    /// 搜索配置
    /// </summary>
    [ObservableProperty]
    public partial SearchConfig Search { get; set; } = new();

    /// <summary>
    /// 启动配置
    /// </summary>
    [ObservableProperty]
    public partial StartupConfig Startup { get; set; } = new();
}
