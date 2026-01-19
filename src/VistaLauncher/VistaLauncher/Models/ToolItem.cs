using System.Text.Json.Serialization;
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
    [property: JsonPropertyName("id")]
    private string _id = Guid.NewGuid().ToString();

    /// <summary>
    /// 工具显示名称
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("name")]
    private string _name = string.Empty;

    /// <summary>
    /// 简短描述，用于列表预览
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("shortDescription")]
    private string _shortDescription = string.Empty;

    /// <summary>
    /// 详细描述，用于详情页
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("longDescription")]
    private string _longDescription = string.Empty;

    /// <summary>
    /// 可执行文件路径
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("executablePath")]
    private string _executablePath = string.Empty;

    /// <summary>
    /// 启动参数
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("arguments")]
    private string _arguments = string.Empty;

    /// <summary>
    /// 工作目录
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("workingDirectory")]
    private string _workingDirectory = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("version")]
    private string _version = string.Empty;

    /// <summary>
    /// 工具类型 (GUI/Console)
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("type")]
    private ToolType _type = ToolType.GUI;

    /// <summary>
    /// 应用架构 (x64/x86)，默认 x64
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("architecture")]
    private Architecture _architecture = Architecture.x64;

    /// <summary>
    /// 图标路径
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("icon")]
    private string _icon = string.Empty;

    /// <summary>
    /// 所属分组 ID
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("groupId")]
    private string _groupId = string.Empty;

    /// <summary>
    /// 标签列表，用于搜索
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("tags")]
    private List<string> _tags = [];

    /// <summary>
    /// 应用主页 URL
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("homepageUrl")]
    private string _homepageUrl = string.Empty;

    /// <summary>
    /// 帮助页面 URL
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("helpUrl")]
    private string _helpUrl = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("createdAt")]
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("updatedAt")]
    private DateTime _updatedAt = DateTime.Now;

    /// <summary>
    /// 获取用于显示的类型字符串
    /// </summary>
    [JsonIgnore]
    public string TypeDisplay => Type == ToolType.GUI ? "GUI" : "Console";

    /// <summary>
    /// 获取用于显示的更新时间字符串
    /// </summary>
    [JsonIgnore]
    public string UpdatedAtDisplay => UpdatedAt.ToString("yyyy-MM-dd");
}
