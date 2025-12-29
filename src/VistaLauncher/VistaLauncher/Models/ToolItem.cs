using CommunityToolkit.Mvvm.ComponentModel;

namespace VistaLauncher.Models;

/// <summary>
/// 工具项数据模型
/// </summary>
public partial class ToolItem : ObservableObject
{
    /// <summary>
    /// 工具唯一标识
    /// </summary>
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    /// <summary>
    /// 工具显示名称
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// 简短描述，用于列表预览
    /// </summary>
    [ObservableProperty]
    private string _shortDescription = string.Empty;

    /// <summary>
    /// 详细描述，用于详情页
    /// </summary>
    [ObservableProperty]
    private string _longDescription = string.Empty;

    /// <summary>
    /// 可执行文件路径
    /// </summary>
    [ObservableProperty]
    private string _executablePath = string.Empty;

    /// <summary>
    /// 启动参数
    /// </summary>
    [ObservableProperty]
    private string _arguments = string.Empty;

    /// <summary>
    /// 工作目录
    /// </summary>
    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    [ObservableProperty]
    private string _version = string.Empty;

    /// <summary>
    /// 工具类型 (GUI/Console)
    /// </summary>
    [ObservableProperty]
    private ToolType _type = ToolType.GUI;

    /// <summary>
    /// 应用架构 (x64/x86)，默认 x64
    /// </summary>
    [ObservableProperty]
    private Architecture _architecture = Architecture.x64;

    /// <summary>
    /// 图标路径
    /// </summary>
    [ObservableProperty]
    private string _icon = string.Empty;

    /// <summary>
    /// 所属分组 ID
    /// </summary>
    [ObservableProperty]
    private string _groupId = string.Empty;

    /// <summary>
    /// 标签列表，用于搜索
    /// </summary>
    [ObservableProperty]
    private List<string> _tags = [];

    /// <summary>
    /// 应用主页 URL
    /// </summary>
    [ObservableProperty]
    private string _homepageUrl = string.Empty;

    /// <summary>
    /// 帮助页面 URL
    /// </summary>
    [ObservableProperty]
    private string _helpUrl = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime _updatedAt = DateTime.Now;

    /// <summary>
    /// 获取用于显示的类型字符串
    /// </summary>
    public string TypeDisplay => Type == ToolType.GUI ? "GUI" : "Console";

    /// <summary>
    /// 获取用于显示的更新时间字符串
    /// </summary>
    public string UpdatedAtDisplay => UpdatedAt.ToString("yyyy-MM-dd");
}
