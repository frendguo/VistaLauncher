using System.Text.Json;
using System.Text.Json.Serialization;

namespace VistaLauncher.Models;

/// <summary>
/// JSON 序列化上下文，用于 AOT 编译优化
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ToolsData))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ToolItem))]
[JsonSerializable(typeof(ToolGroup))]
[JsonSerializable(typeof(List<ToolItem>))]
[JsonSerializable(typeof(List<ToolGroup>))]
[JsonSerializable(typeof(UpdateInfo))]
[JsonSerializable(typeof(UpdateConfig))]
[JsonSerializable(typeof(List<UpdateInfo>))]
internal partial class JsonContext : JsonSerializerContext
{
}
