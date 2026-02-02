using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using VistaLauncher.Models;

namespace VistaLauncher.ViewModels;

/// <summary>
/// 更新项 ViewModel
/// </summary>
public sealed partial class UpdateItemViewModel : ObservableObject
{
    private static readonly string[] s_sizeSuffixes = ["B", "KB", "MB", "GB"];

    /// <summary>
    /// 工具 ID
    /// </summary>
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本
    /// </summary>
    [ObservableProperty]
    public partial string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最新版本
    /// </summary>
    [ObservableProperty]
    public partial string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// 发布说明
    /// </summary>
    [ObservableProperty]
    public partial string? ReleaseNotes { get; set; }

    /// <summary>
    /// 发布日期
    /// </summary>
    [ObservableProperty]
    public partial DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [ObservableProperty]
    public partial long? FileSize { get; set; }

    /// <summary>
    /// 下载链接
    /// </summary>
    [ObservableProperty]
    public partial string? DownloadUrl { get; set; }

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;

    /// <summary>
    /// 是否正在更新
    /// </summary>
    [ObservableProperty]
    public partial bool IsUpdating { get; set; }

    /// <summary>
    /// 更新进度（0-100）
    /// </summary>
    [ObservableProperty]
    public partial double UpdateProgress { get; set; }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    [ObservableProperty]
    public partial string UpdateStatus { get; set; } = string.Empty;

    /// <summary>
    /// 是否更新成功
    /// </summary>
    [ObservableProperty]
    public partial bool? UpdateSuccess { get; set; }

    /// <summary>
    /// 关联的工具项
    /// </summary>
    [JsonIgnore]
    public ToolItem? ToolItem { get; set; }

    /// <summary>
    /// 是否有发布说明
    /// </summary>
    [JsonIgnore]
    public bool HasReleaseNotes => !string.IsNullOrEmpty(ReleaseNotes);

    /// <summary>
    /// 文件大小显示文本
    /// </summary>
    [JsonIgnore]
    public string FileSizeDisplay => FileSize.HasValue ? FormatFileSize(FileSize.Value) : string.Empty;

    /// <summary>
    /// 发布日期显示文本
    /// </summary>
    [JsonIgnore]
    public string ReleaseDateDisplay => ReleaseDate?.ToString("yyyy-MM-dd") ?? string.Empty;

    /// <summary>
    /// 从版本检查结果创建
    /// </summary>
    public static UpdateItemViewModel FromCheckResult(Services.VersionCheckResult result, ToolItem? tool = null)
    {
        return new UpdateItemViewModel
        {
            Id = result.ToolId,
            Name = result.ToolName,
            CurrentVersion = result.CurrentVersion,
            LatestVersion = result.LatestVersion ?? string.Empty,
            ReleaseNotes = result.ReleaseNotes,
            ReleaseDate = result.ReleaseDate,
            FileSize = result.FileSize,
            DownloadUrl = result.DownloadUrl,
            ToolItem = tool
        };
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        var counter = 0;
        var number = (double)bytes;
        while (Math.Round(number / 1024) >= 1 && counter < s_sizeSuffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {s_sizeSuffixes[counter]}";
    }
}
