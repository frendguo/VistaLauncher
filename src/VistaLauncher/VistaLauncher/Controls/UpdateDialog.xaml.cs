using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using VistaLauncher.Models;
using VistaLauncher.Services;
using VistaLauncher.ViewModels;

namespace VistaLauncher.Controls;

/// <summary>
/// 更新对话框
/// </summary>
public sealed partial class UpdateDialog : ContentDialog
{
    private readonly IVersionCheckService _versionCheckService;
    private readonly IUpdateService _updateService;
    private readonly IToolDataService _dataService;
    private readonly ObservableCollection<UpdateItemViewModel> _updates = [];
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 可用更新列表
    /// </summary>
    public ObservableCollection<UpdateItemViewModel> Updates => _updates;

    /// <summary>
    /// 更新数量
    /// </summary>
    public int UpdateCount => _updates.Count;

    public UpdateDialog(
        IVersionCheckService versionCheckService,
        IUpdateService updateService,
        IToolDataService dataService)
    {
        _versionCheckService = versionCheckService;
        _updateService = updateService;
        _dataService = dataService;

        InitializeComponent();
        UpdateListView.ItemsSource = _updates;
    }

    /// <summary>
    /// 开始检查更新
    /// </summary>
    public async Task CheckUpdatesAsync(IEnumerable<ToolItem> tools)
    {
        _cts = new CancellationTokenSource();
        _updates.Clear();

        StatusText.Text = "正在检查更新...";
        CheckingRing.IsActive = true;
        IsPrimaryButtonEnabled = false;
        IsSecondaryButtonEnabled = false;

        var toolList = tools.ToList();
        var progress = new Progress<CheckProgress>(p =>
        {
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressText.Text = $"检查中: {p.CurrentTool} ({p.Completed}/{p.Total})";
            ProgressBar.Value = p.Percentage;
        });

        try
        {
            var results = await _versionCheckService.CheckVersionsAsync(toolList, progress, _cts.Token);

            foreach (var result in results.Where(r => r.HasUpdate))
            {
                var tool = toolList.FirstOrDefault(t => t.Id == result.ToolId);
                var vm = UpdateItemViewModel.FromCheckResult(result, tool);
                _updates.Add(vm);
            }

            if (_updates.Count > 0)
            {
                StatusText.Text = $"发现 {_updates.Count} 个可用更新";
                IsPrimaryButtonEnabled = true;
                IsSecondaryButtonEnabled = true;
                UpdateListView.SelectAll();
            }
            else
            {
                StatusText.Text = "所有工具都是最新版本";
            }

            LastCheckText.Text = $"上次检查: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "检查已取消";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"检查失败: {ex.Message}";
        }
        finally
        {
            CheckingRing.IsActive = false;
            ProgressPanel.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// 更新选中的工具
    /// </summary>
    private async void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        var selectedItems = UpdateListView.SelectedItems.Cast<UpdateItemViewModel>().ToList();
        if (selectedItems.Count == 0)
            return;

        await UpdateToolsAsync(selectedItems);
    }

    /// <summary>
    /// 更新所有工具
    /// </summary>
    private async void SecondaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        await UpdateToolsAsync(_updates.ToList());
    }

    /// <summary>
    /// 执行更新
    /// </summary>
    private async Task UpdateToolsAsync(List<UpdateItemViewModel> items)
    {
        IsPrimaryButtonEnabled = false;
        IsSecondaryButtonEnabled = false;

        foreach (var item in items)
        {
            if (item.ToolItem == null || item.UpdateSuccess == true)
                continue;

            item.IsUpdating = true;
            item.UpdateStatus = "准备中...";

            var progress = new Progress<DownloadProgress>(p =>
            {
                item.UpdateProgress = p.Percentage;
                item.UpdateStatus = p.Status;
            });

            try
            {
                var result = await _updateService.UpdateToolAsync(item.ToolItem, progress);
                item.UpdateSuccess = result.Success;
                item.UpdateStatus = result.Success ? "更新成功" : string.Join("; ", result.Messages);

                if (result.Success)
                {
                    item.CurrentVersion = result.NewVersion;
                }
            }
            catch (Exception ex)
            {
                item.UpdateSuccess = false;
                item.UpdateStatus = ex.Message;
            }
            finally
            {
                item.IsUpdating = false;
            }
        }

        var hasRemaining = _updates.Any(u => u.UpdateSuccess != true);
        IsPrimaryButtonEnabled = hasRemaining;
        IsSecondaryButtonEnabled = hasRemaining;

        if (!hasRemaining)
        {
            StatusText.Text = "所有更新已完成";
        }
    }

    private void UpdateListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = UpdateListView.SelectedItems.Count > 0;
    }

    private void OpenPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: UpdateItemViewModel item } && item.ToolItem != null)
        {
            _versionCheckService.OpenDownloadPage(item.ToolItem);
        }
    }
}
