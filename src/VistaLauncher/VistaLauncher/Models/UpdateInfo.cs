using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VistaLauncher.Models;

/// <summary>
/// 工具更新信息
/// </summary>
public partial class UpdateInfo : ObservableObject
{
    /// <summary>
    /// 最新版本号
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("version")]
    private string _version = string.Empty;

    /// <summary>
    /// 直接下载链接
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("downloadUrl")]
    private string? _downloadUrl;

    /// <summary>
    /// 发布信息页面（用于检查更新）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("infoUrl")]
    private string? _infoUrl;

    /// <summary>
    /// SHA256 校验和（用于验证下载完整性）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("sha256Checksum")]
    private string? _sha256Checksum;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("fileSize")]
    private long? _fileSize;

    /// <summary>
    /// 是否需要管理员权限安装
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("requiresAdmin")]
    private bool _requiresAdmin;

    /// <summary>
    /// 更新日志 / 发布说明
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("releaseNotes")]
    private string? _releaseNotes;

    /// <summary>
    /// 发布日期
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("releaseDate")]
    private DateTime? _releaseDate;

    /// <summary>
    /// 是否为强制更新
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("isMandatory")]
    private bool _isMandatory;

    /// <summary>
    /// 最低兼容版本（用于版本兼容性检查）
    /// </summary>
    [ObservableProperty]
    [property: JsonPropertyName("minCompatibleVersion")]
    private string? _minCompatibleVersion;
}
