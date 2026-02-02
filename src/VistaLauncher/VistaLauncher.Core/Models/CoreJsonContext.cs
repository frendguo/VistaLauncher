using System.Text.Json;
using System.Text.Json.Serialization;

namespace VistaLauncher.Models;

/// <summary>
/// Core 项目的 JSON 序列化上下文，用于 AOT 编译优化
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ToolsData))]
[JsonSerializable(typeof(ToolItem))]
[JsonSerializable(typeof(ToolGroup))]
[JsonSerializable(typeof(List<ToolItem>))]
[JsonSerializable(typeof(List<ToolGroup>))]
[JsonSerializable(typeof(UpdateInfo))]
[JsonSerializable(typeof(UpdateConfig))]
[JsonSerializable(typeof(List<UpdateInfo>))]
[JsonSerializable(typeof(StartupConfig))]
internal partial class CoreJsonContext : JsonSerializerContext
{
}
