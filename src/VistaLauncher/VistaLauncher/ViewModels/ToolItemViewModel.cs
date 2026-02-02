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
    private Task? _iconLoadTask;
    private ToolAvailability _availability = ToolAvailability.Available;

    public ToolItemViewModel(ToolItem toolItem, int index = 0, bool deferIconLoading = true)
    {
        _toolItem = toolItem;
        _index = index;

        // 延迟加载图标：只在需要时加载
        if (!deferIconLoading)
        {
            // 立即加载（用于测试或特殊场景）
            _ = LoadIconAsync();
        }
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
    public partial BitmapImage? IconImage { get; set; }

    /// <summary>
    /// 是否正在加载图标
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoadingIcon { get; set; }

    /// <summary>
    /// 图标是否已加载（包括已尝试加载但失败的情况）
    /// </summary>
    private bool _isIconLoaded = false;

    /// <summary>
    /// 图标是否已加载
    /// </summary>
    public bool IsIconLoaded => _isIconLoaded;

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

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
    /// 请求加载图标（延迟加载入口）
    /// 当项目进入可见区域时调用
    /// </summary>
    public void RequestLoadIcon()
    {
        if (_isIconLoaded || _iconLoadTask != null)
        {
            return;
        }

        _iconLoadTask = LoadIconAsync();
    }

    /// <summary>
    /// 异步加载图标
    /// </summary>
    private async Task LoadIconAsync()
    {
        if (_isIconLoaded) return;

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
            _isIconLoaded = true;
        }
    }

    /// <summary>
    /// 刷新图标
    /// </summary>
    public async Task RefreshIconAsync()
    {
        _isIconLoaded = false;
        _iconLoadTask = null;
        await LoadIconAsync();
    }

    /// <summary>
    /// 工具可用性状态
    /// </summary>
    public ToolAvailability Availability
    {
        get => _availability;
        private set
        {
            if (_availability != value)
            {
                _availability = value;
                OnPropertyChanged(nameof(Availability));
                OnPropertyChanged(nameof(IsNotInstalled));
                OnPropertyChanged(nameof(HasUpdate));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusTooltip));
            }
        }
    }

    /// <summary>
    /// 更新工具可用性状态
    /// </summary>
    public void UpdateAvailability(ToolAvailability availability)
    {
        Availability = availability;
    }

    /// <summary>
    /// 是否未安装
    /// </summary>
    public bool IsNotInstalled => Availability == ToolAvailability.NotInstalled;

    /// <summary>
    /// 是否有可用更新
    /// </summary>
    public bool HasUpdate => Availability == ToolAvailability.UpdateAvailable;

    /// <summary>
    /// 状态图标字符
    /// </summary>
    public string StatusIcon => Availability switch
    {
        ToolAvailability.NotInstalled => "\uE896", // Download icon
        ToolAvailability.UpdateAvailable => "\uE8AB", // Sync/Update icon
        _ => string.Empty
    };

    /// <summary>
    /// 状态提示文本
    /// </summary>
    public string StatusTooltip => Availability switch
    {
        ToolAvailability.NotInstalled => "未安装，按 Enter 下载",
        ToolAvailability.UpdateAvailable => "有可用更新",
        _ => string.Empty
    };
}
