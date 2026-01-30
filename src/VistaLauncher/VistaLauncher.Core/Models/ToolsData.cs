namespace VistaLauncher.Models;

/// <summary>
/// 工具数据根对象，用于 JSON 序列化
/// </summary>
public class ToolsData
{
    /// <summary>
    /// 当前数据格式版本（用于迁移判断）
    /// </summary>
    public const string CurrentDataFormatVersion = "2.0";

    /// <summary>
    /// 数据格式版本（用于迁移）
    /// </summary>
    public string DataFormatVersion { get; set; } = CurrentDataFormatVersion;

    /// <summary>
    /// 数据版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后全局更新检查时间
    /// </summary>
    public DateTime? LastGlobalUpdateCheck { get; set; }

    /// <summary>
    /// 自动更新配置
    /// </summary>
    public UpdateConfig UpdateConfig { get; set; } = new();

    /// <summary>
    /// 工具分组列表
    /// </summary>
    public List<ToolGroup> Groups { get; set; } = [];

    /// <summary>
    /// 工具列表
    /// </summary>
    public List<ToolItem> Tools { get; set; } = [];
}
