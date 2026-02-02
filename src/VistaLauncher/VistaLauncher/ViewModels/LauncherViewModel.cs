using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using VistaLauncher.Models;
using VistaLauncher.Services;

namespace VistaLauncher.ViewModels;

/// <summary>
/// 主启动器 ViewModel
/// </summary>
public partial class LauncherViewModel : ObservableObject
{
    private readonly IToolDataService _toolDataService;
    private readonly ISearchProvider _searchProvider;
    private readonly IProcessLauncher _processLauncher;
    private readonly IToolAvailabilityService? _availabilityService;
    private readonly IToolDownloadService? _downloadService;

    private List<ToolItem> _allTools = [];
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _downloadCts;

    /// <summary>
    /// 所有工具的 ViewModel 集合（底层数据源）
    /// </summary>
    private readonly ObservableCollection<ToolItemViewModel> _allToolViewModels = [];

    /// <summary>
    /// ViewModel 缓存，避免每次搜索都重建 ViewModel
    /// Key: ToolItem.Id
    /// </summary>
    private readonly Dictionary<string, ToolItemViewModel> _vmCache = [];

    /// <summary>
    /// 当前搜索查询的小写形式（缓存）
    /// </summary>
    private string _currentLowerQuery = string.Empty;

    /// <summary>
    /// 当前搜索查询的分词结果（缓存）
    /// </summary>
    private string[] _currentQueryTokens = [];

    public LauncherViewModel(
        IToolDataService toolDataService,
        ISearchProvider searchProvider,
        IProcessLauncher processLauncher,
        IToolAvailabilityService? availabilityService = null,
        IToolDownloadService? downloadService = null)
    {
        _toolDataService = toolDataService;
        _searchProvider = searchProvider;
        _processLauncher = processLauncher;
        _availabilityService = availabilityService;
        _downloadService = downloadService;
    }

    /// <summary>
    /// 搜索查询文本
    /// </summary>
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    /// 工具列表是否展开
    /// </summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// 过滤后的工具列表视图（使用 AdvancedCollectionView 实现过滤和排序）
    /// </summary>
    private AdvancedCollectionView? _filteredTools;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AdvancedCollectionView sorting is used with known types only")]
    public AdvancedCollectionView FilteredTools => _filteredTools ??= new AdvancedCollectionView(_allToolViewModels);

    /// <summary>
    /// 过滤后的工具数量
    /// </summary>
    public int FilteredToolsCount => FilteredTools.Count;

    /// <summary>
    /// 选中的工具
    /// </summary>
    [ObservableProperty]
    public partial ToolItemViewModel? SelectedTool { get; set; }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表可见性 (用于 XAML 绑定)
    /// </summary>
    public Microsoft.UI.Xaml.Visibility ListVisibility =>
        IsExpanded ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>
    /// 命令栏可见性
    /// </summary>
    public Microsoft.UI.Xaml.Visibility CommandBarVisibility =>
        IsExpanded ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>
    /// 主命令名称
    /// </summary>
    public string PrimaryCommandName => "Open";

    /// <summary>
    /// 次命令名称
    /// </summary>
    public string SecondaryCommandName => "Run as Admin";

    /// <summary>
    /// 是否有选中的工具
    /// </summary>
    public bool HasSelectedTool => SelectedTool != null;

    /// <summary>
    /// 选中工具的标题
    /// </summary>
    public string SelectedToolTitle => SelectedTool?.Name ?? string.Empty;

    /// <summary>
    /// 状态文本
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// 是否正在下载
    /// </summary>
    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    /// <summary>
    /// 下载进度百分比
    /// </summary>
    [ObservableProperty]
    public partial double DownloadProgress { get; set; }

    /// <summary>
    /// 下载状态文本
    /// </summary>
    [ObservableProperty]
    public partial string DownloadStatus { get; set; }

    /// <summary>
    /// 当 IsExpanded 变化时通知 ListVisibility 也变化
    /// </summary>
    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ListVisibility));
        OnPropertyChanged(nameof(CommandBarVisibility));
    }

    /// <summary>
    /// 当选中工具变化时通知相关属性
    /// </summary>
    partial void OnSelectedToolChanged(ToolItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedTool));
        OnPropertyChanged(nameof(SelectedToolTitle));
    }

    /// <summary>
    /// 初始化加载数据
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            _allTools = (await _toolDataService.GetToolsAsync()).ToList();

            // 清空并重建所有 ViewModel
            _allToolViewModels.Clear();
            _vmCache.Clear();

            foreach (var tool in _allTools)
            {
                var vm = new ToolItemViewModel(tool);
                _vmCache[tool.Id] = vm;
                _allToolViewModels.Add(vm);
            }

            // 设置排序（按 SearchScore 降序）
            FilteredTools.SortDescriptions.Clear();
            FilteredTools.SortDescriptions.Add(new SortDescription(nameof(ToolItemViewModel.SearchScore), SortDirection.Descending));

            // 初始化时显示所有工具，设置默认分数
            _currentLowerQuery = string.Empty;
            _currentQueryTokens = [];
            UpdateScoresAndRefresh();

            // 检查工具可用性
            await CheckToolAvailabilitiesAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 检查所有工具的可用性状态
    /// </summary>
    private async Task CheckToolAvailabilitiesAsync()
    {
        if (_availabilityService == null)
            return;

        try
        {
            var availabilities = await _availabilityService.CheckAvailabilitiesAsync(_allTools);
            foreach (var (toolId, availability) in availabilities)
            {
                if (_vmCache.TryGetValue(toolId, out var vm))
                {
                    vm.UpdateAvailability(availability);
                }
            }
        }
        catch
        {
            // 可用性检查失败不影响主流程
        }
    }

    /// <summary>
    /// 更新所有 ViewModel 的搜索分数并刷新视图
    /// </summary>
    private void UpdateScoresAndRefresh()
    {
        var isEmptyQuery = string.IsNullOrWhiteSpace(_currentLowerQuery);

        // 更新每个 ViewModel 的分数
        foreach (var vm in _allToolViewModels)
        {
            if (isEmptyQuery)
            {
                // 空查询时所有工具分数相同，按原始顺序
                vm.UpdateSearchScore(1);
            }
            else
            {
                var score = CalculateScore(vm.ToolItem, _currentLowerQuery, _currentQueryTokens);
                vm.UpdateSearchScore(score);
            }
        }

        // 设置过滤器（分数 > 0 的才显示）
        if (isEmptyQuery)
        {
            FilteredTools.Filter = null!; // 空查询显示所有
        }
        else
        {
            FilteredTools.Filter = item => item is ToolItemViewModel vm && vm.SearchScore > 0;
        }

        // 刷新排序和过滤
        FilteredTools.RefreshFilter();
        FilteredTools.RefreshSorting();

        // 更新索引（基于过滤和排序后的顺序）
        UpdateIndices();

        // 通知 UI 更新
        OnPropertyChanged(nameof(FilteredToolsCount));

        // 默认选中第一个
        if (FilteredTools.Count > 0)
        {
            SelectedTool = FilteredTools[0] as ToolItemViewModel;
        }
        else
        {
            SelectedTool = null;
        }
    }

    /// <summary>
    /// 更新过滤后视图中每个项目的索引
    /// </summary>
    private void UpdateIndices()
    {
        int index = 0;
        foreach (var item in FilteredTools)
        {
            if (item is ToolItemViewModel vm)
            {
                vm.UpdateIndex(index);
                vm.ResetSelection();
                index++;
            }
        }
    }

    /// <summary>
    /// 计算工具的匹配分数
    /// </summary>
    private static int CalculateScore(ToolItem tool, string lowerQuery, string[] queryTokens)
    {
        // 检查是否所有 token 都匹配
        var allTokensMatch = queryTokens.All(token =>
        {
            var nameMatch = tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase);
            var shortDescMatch = tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
            var longDescMatch = tool.LongDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
            var tagMatch = tool.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase));
            return nameMatch || shortDescMatch || longDescMatch || tagMatch;
        });

        if (!allTokensMatch)
        {
            return 0; // 不匹配
        }

        var score = 1; // 基础分数
        var lowerName = tool.Name.ToLower();

        // 名称完全匹配得分最高
        if (lowerName == lowerQuery)
        {
            score += 100;
        }
        // 名称以查询开头
        else if (lowerName.StartsWith(lowerQuery))
        {
            score += 50;
        }
        // 名称包含查询
        else if (lowerName.Contains(lowerQuery))
        {
            score += 25;
        }

        // 每个匹配的 token 加分
        foreach (var token in queryTokens)
        {
            if (tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 10;
            if (tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 5;
            if (tool.Tags.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase)))
                score += 3;
        }

        return score;
    }

    /// <summary>
    /// 搜索查询变化时调用
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        _ = SearchAsync(value);
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    private async Task SearchAsync(string query)
    {
        // 取消之前的搜索
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // 延迟搜索，避免频繁触发
            await Task.Delay(100, token);

            if (!token.IsCancellationRequested)
            {
                // 更新缓存的查询字符串
                _currentLowerQuery = query.ToLower();
                _currentQueryTokens = _currentLowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // 更新分数并刷新视图
                UpdateScoresAndRefresh();

                // 如果有搜索内容，自动展开列表
                if (!string.IsNullOrWhiteSpace(query))
                {
                    IsExpanded = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 搜索被取消，忽略
        }
    }

    /// <summary>
    /// 展开/收起工具列表
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// 展开工具列表
    /// </summary>
    [RelayCommand]
    private void Expand()
    {
        IsExpanded = true;
    }

    /// <summary>
    /// 收起工具列表
    /// </summary>
    [RelayCommand]
    private void Collapse()
    {
        IsExpanded = false;
        SearchQuery = string.Empty;
    }

    /// <summary>
    /// 启动选中的工具
    /// </summary>
    [RelayCommand]
    public async Task LaunchSelectedAsync()
    {
        if (SelectedTool != null)
        {
            await LaunchToolAsync(SelectedTool.ToolItem, runAsAdmin: false);
        }
        else if (FilteredTools.Count > 0 && FilteredTools[0] is ToolItemViewModel firstTool)
        {
            await LaunchToolAsync(firstTool.ToolItem, runAsAdmin: false);
        }
    }

    /// <summary>
    /// 启动选中的工具 (同步版本，用于命令绑定)
    /// </summary>
    public void LaunchSelected()
    {
        _ = LaunchSelectedAsync();
    }

    /// <summary>
    /// 以管理员身份启动选中的工具
    /// </summary>
    [RelayCommand]
    public async Task LaunchSelectedAsAdminAsync()
    {
        if (SelectedTool != null)
        {
            await LaunchToolAsync(SelectedTool.ToolItem, runAsAdmin: true);
        }
        else if (FilteredTools.Count > 0 && FilteredTools[0] is ToolItemViewModel firstTool)
        {
            await LaunchToolAsync(firstTool.ToolItem, runAsAdmin: true);
        }
    }

    /// <summary>
    /// 启动指定工具
    /// </summary>
    public async Task LaunchToolAsync(ToolItem tool, bool runAsAdmin = false)
    {
        // 如果工具未安装，先下载
        if (_availabilityService != null &&
            _downloadService != null &&
            !_availabilityService.IsToolInstalled(tool))
        {
            await DownloadAndLaunchAsync(tool);
            return;
        }

        await _processLauncher.LaunchAsync(tool);

        // 启动后重置状态
        SearchQuery = string.Empty;
        IsExpanded = false;
    }

    /// <summary>
    /// 下载并启动工具
    /// </summary>
    public async Task DownloadAndLaunchAsync(ToolItem tool)
    {
        if (_downloadService == null || _availabilityService == null)
            return;

        // 检查是否支持下载
        if (!_downloadService.CanDownload(tool))
        {
            StatusText = $"工具 {tool.Name} 不支持自动下载";
            return;
        }

        // 取消之前的下载
        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadStatus = "准备下载...";

        try
        {
            // 创建进度报告
            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadProgress = p.Percentage;
                DownloadStatus = p.Status;
            });

            // 下载并安装
            var result = await _downloadService.DownloadAndInstallAsync(
                tool,
                progress,
                _downloadCts.Token);

            if (result.Success)
            {
                StatusText = $"已安装 {result.ToolName} v{result.Version}";

                // 更新工具的可用性状态
                if (_vmCache.TryGetValue(tool.Id, out var vm))
                {
                    vm.UpdateAvailability(ToolAvailability.Available);
                    await vm.RefreshIconAsync();
                }

                // 启动工具
                await _processLauncher.LaunchAsync(tool);

                // 重置状态
                SearchQuery = string.Empty;
                IsExpanded = false;
            }
            else
            {
                StatusText = $"下载失败: {result.ErrorMessage}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "下载已取消";
        }
        catch (Exception ex)
        {
            StatusText = $"下载失败: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 0;
        }
    }

    /// <summary>
    /// 取消下载
    /// </summary>
    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCts?.Cancel();
        StatusText = "下载已取消";
    }

    /// <summary>
    /// 通过索引启动工具 (Ctrl+1 ~ Ctrl+9)
    /// </summary>
    [RelayCommand]
    public async Task LaunchByIndexAsync(int index)
    {
        if (index >= 0 && index < FilteredTools.Count && FilteredTools[index] is ToolItemViewModel vm)
        {
            await LaunchToolAsync(vm.ToolItem);
        }
    }

    /// <summary>
    /// 选择下一个工具
    /// </summary>
    [RelayCommand]
    private void SelectNext()
    {
        if (FilteredTools.Count == 0) return;

        var currentIndex = SelectedTool != null ? FilteredTools.IndexOf(SelectedTool) : -1;
        var nextIndex = (currentIndex + 1) % FilteredTools.Count;
        SelectedTool = FilteredTools[nextIndex] as ToolItemViewModel;
    }

    /// <summary>
    /// 选择上一个工具
    /// </summary>
    [RelayCommand]
    private void SelectPrevious()
    {
        if (FilteredTools.Count == 0) return;

        var currentIndex = SelectedTool != null ? FilteredTools.IndexOf(SelectedTool) : 0;
        var prevIndex = currentIndex <= 0 ? FilteredTools.Count - 1 : currentIndex - 1;
        SelectedTool = FilteredTools[prevIndex] as ToolItemViewModel;
    }

    /// <summary>
    /// 刷新工具列表
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        // 清空 ViewModel 缓存和图标缓存
        _vmCache.Clear();
        IconExtractor.ClearCache();

        await _toolDataService.ReloadAsync();
        await InitializeAsync();
    }
}
