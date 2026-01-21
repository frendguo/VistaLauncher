using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using VistaLauncher.Models;
using VistaLauncher.Services;

namespace VistaLauncher.ViewModels;

/// <summary>
/// 工具列表项 ViewModel，用于 UI 绑定
/// </summary>
public partial class ToolItemViewModel : ObservableObject
{
    private readonly ToolItem _toolItem;

    public ToolItemViewModel(ToolItem toolItem, int index = 0)
    {
        _toolItem = toolItem;
        _index = index;

        // 异步加载图标
        _ = LoadIconAsync();
    }

    /// <summary>
    /// 原始工具项
    /// </summary>
    public ToolItem ToolItem => _toolItem;

    /// <summary>
    /// 列表索引 (0-based)
    /// </summary>
    private int _index;
    public int Index => _index;

    /// <summary>
    /// 更新索引并通知 UI
    /// </summary>
    public void UpdateIndex(int newIndex)
    {
        if (_index != newIndex)
        {
            _index = newIndex;
            OnPropertyChanged(nameof(Index));
            OnPropertyChanged(nameof(ShortcutKeyDisplay));
            OnPropertyChanged(nameof(ShowShortcutKey));
        }
    }

    /// <summary>
    /// 重置选中状态
    /// </summary>
    public void ResetSelection()
    {
        IsSelected = false;
    }

    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name => _toolItem.Name;

    /// <summary>
    /// 简短描述
    /// </summary>
    public string Description => _toolItem.ShortDescription;

    /// <summary>
    /// 副标题 (用于列表项显示)
    /// </summary>
    public string Subtitle => _toolItem.ShortDescription;

    /// <summary>
    /// 是否显示副标题
    /// </summary>
    public bool ShowSubtitle => !string.IsNullOrWhiteSpace(_toolItem.ShortDescription);

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> Tags => _toolItem.Tags ?? [];

    /// <summary>
    /// 是否有标签
    /// </summary>
    public bool HasTags => Tags.Count > 0;

    /// <summary>
    /// 快捷键显示 (Ctrl+1 ~ Ctrl+9)
    /// </summary>
    public string ShortcutKeyDisplay => Index < 9 ? $"Ctrl+{Index + 1}" : string.Empty;

    /// <summary>
    /// 是否显示快捷键
    /// </summary>
    public bool ShowShortcutKey => Index < 9;

    /// <summary>
    /// 图标图像
    /// </summary>
    [ObservableProperty]
    private BitmapImage? _iconImage;

    /// <summary>
    /// 是否正在加载图标
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingIcon = true;

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 搜索匹配分数（用于排序，分数越高排序越靠前）
    /// </summary>
    private int _searchScore;
    public int SearchScore => _searchScore;

    /// <summary>
    /// 更新搜索分数
    /// </summary>
    public void UpdateSearchScore(int score)
    {
        if (_searchScore != score)
        {
            _searchScore = score;
            OnPropertyChanged(nameof(SearchScore));
        }
    }

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private async Task LoadIconAsync()
    {
        IsLoadingIcon = true;
        try
        {
            // 优先使用可执行文件路径提取图标
            var exePath = _toolItem.ExecutablePath;
            if (!string.IsNullOrEmpty(exePath))
            {
                IconImage = await IconExtractor.ExtractIconAsync(exePath);
            }
        }
        finally
        {
            IsLoadingIcon = false;
        }
    }

    /// <summary>
    /// 刷新图标
    /// </summary>
    public async Task RefreshIconAsync()
    {
        await LoadIconAsync();
    }
}
