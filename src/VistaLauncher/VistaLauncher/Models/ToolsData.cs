namespace VistaLauncher.Models;

/// <summary>
/// 工具数据根对象，用于 JSON 序列化
/// </summary>
public class ToolsData
{
    /// <summary>
    /// 数据版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// 工具分组列表
    /// </summary>
    public List<ToolGroup> Groups { get; set; } = [];

    /// <summary>
    /// 工具列表
    /// </summary>
    public List<ToolItem> Tools { get; set; } = [];
}
