using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    private List<ToolItem> _allTools = [];
    private CancellationTokenSource? _searchCts;

    public LauncherViewModel(
        IToolDataService toolDataService,
        ISearchProvider searchProvider,
        IProcessLauncher processLauncher)
    {
        _toolDataService = toolDataService;
        _searchProvider = searchProvider;
        _processLauncher = processLauncher;
    }

    /// <summary>
    /// 搜索查询文本
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// 工具列表是否展开
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = false;

    /// <summary>
    /// 过滤后的工具列表 (使用 ViewModel 包装)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ToolItemViewModel> _filteredTools = [];

    /// <summary>
    /// 选中的工具
    /// </summary>
    [ObservableProperty]
    private ToolItemViewModel? _selectedTool;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = false;

    /// <summary>
    /// 列表可见性 (用于 XAML 绑定)
    /// </summary>
    public Microsoft.UI.Xaml.Visibility ListVisibility => 
        IsExpanded ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    /// <summary>
    /// 当 IsExpanded 变化时通知 ListVisibility 也变化
    /// </summary>
    partial void OnIsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ListVisibility));
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
            UpdateFilteredTools(_allTools);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新过滤后的工具列表
    /// </summary>
    private void UpdateFilteredTools(IEnumerable<ToolItem> tools)
    {
        var viewModels = tools
            .Select((tool, index) => new ToolItemViewModel(tool, index))
            .ToList();
        
        FilteredTools = new ObservableCollection<ToolItemViewModel>(viewModels);
        
        // 默认选中第一个
        if (FilteredTools.Count > 0)
        {
            SelectedTool = FilteredTools[0];
        }
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

            var results = await _searchProvider.SearchAsync(query, _allTools, token);
            
            if (!token.IsCancellationRequested)
            {
                UpdateFilteredTools(results);
                
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
    private async Task LaunchSelectedAsync()
    {
        if (SelectedTool != null)
        {
            await LaunchToolAsync(SelectedTool.ToolItem);
        }
        else if (FilteredTools.Count > 0)
        {
            await LaunchToolAsync(FilteredTools[0].ToolItem);
        }
    }

    /// <summary>
    /// 启动指定工具
    /// </summary>
    private async Task LaunchToolAsync(ToolItem tool)
    {
        await _processLauncher.LaunchAsync(tool);
        
        // 启动后重置状态
        SearchQuery = string.Empty;
        IsExpanded = false;
    }

    /// <summary>
    /// 通过索引启动工具 (Ctrl+1 ~ Ctrl+9)
    /// </summary>
    [RelayCommand]
    private async Task LaunchByIndexAsync(int index)
    {
        if (index >= 0 && index < FilteredTools.Count)
        {
            await LaunchToolAsync(FilteredTools[index].ToolItem);
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
        SelectedTool = FilteredTools[nextIndex];
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
        SelectedTool = FilteredTools[prevIndex];
    }

    /// <summary>
    /// 刷新工具列表
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await _toolDataService.ReloadAsync();
        await InitializeAsync();
    }
}
