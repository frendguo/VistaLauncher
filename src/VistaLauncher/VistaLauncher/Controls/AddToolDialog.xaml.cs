using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VistaLauncher.Models;
using VistaLauncher.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VistaLauncher.Controls;

public sealed partial class AddToolDialog : ContentDialog
{
    private readonly IToolManagementService _toolService;
    private List<ToolGroup> _groups = [];

    /// <summary>
    /// 编辑模式时传入的工具
    /// </summary>
    public ToolItem? EditingTool { get; set; }

    /// <summary>
    /// 是否为编辑模式
    /// </summary>
    public bool IsEditMode => EditingTool != null;

    /// <summary>
    /// 保存后的工具（用于返回结果）
    /// </summary>
    public ToolItem? SavedTool { get; private set; }

    public AddToolDialog(IToolManagementService toolService)
    {
        _toolService = toolService;
        InitializeComponent();

        Loaded += OnLoaded;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载分组列表
        var groups = await _toolService.GetGroupsAsync();
        _groups = groups.ToList();
        GroupComboBox.ItemsSource = _groups;

        // 编辑模式：填充现有数据
        if (EditingTool != null)
        {
            Title = "编辑工具";
            ToolNameTextBox.Text = EditingTool.Name;
            ExecutablePathTextBox.Text = EditingTool.ExecutablePath;
            ShortDescriptionTextBox.Text = EditingTool.ShortDescription;
            LongDescriptionTextBox.Text = EditingTool.LongDescription;
            HomepageUrlTextBox.Text = EditingTool.HomepageUrl;
            TagsTextBox.Text = string.Join(", ", EditingTool.Tags);
            IsConsoleCheckBox.IsChecked = EditingTool.Type == ToolType.Console;
            // RequiresAdmin 需要在 ToolItem 中有对应字段，暂时跳过

            // 选中对应的分组
            var selectedGroup = _groups.FirstOrDefault(g => g.Id == EditingTool.GroupId);
            if (selectedGroup != null)
            {
                GroupComboBox.SelectedItem = selectedGroup;
            }
        }
    }

    private async void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");

        // ContentDialog 的 XamlRoot 不可靠，直接使用 App.MainWindow
        if (App.MainWindow != null)
        {
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ExecutablePathTextBox.Text = file.Path;

            // 自动填充名称（如果为空）
            if (string.IsNullOrWhiteSpace(ToolNameTextBox.Text))
            {
                ToolNameTextBox.Text = Path.GetFileNameWithoutExtension(file.Name);
            }
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 验证必填字段
        if (string.IsNullOrWhiteSpace(ToolNameTextBox.Text))
        {
            args.Cancel = true;
            ToolNameTextBox.Focus(FocusState.Programmatic);
            return;
        }

        if (string.IsNullOrWhiteSpace(ExecutablePathTextBox.Text))
        {
            args.Cancel = true;
            ExecutablePathTextBox.Focus(FocusState.Programmatic);
            return;
        }

        if (GroupComboBox.SelectedItem == null)
        {
            args.Cancel = true;
            GroupComboBox.Focus(FocusState.Programmatic);
            return;
        }

        var deferral = args.GetDeferral();

        try
        {
            var tool = EditingTool ?? new ToolItem();
            var selectedGroup = GroupComboBox.SelectedItem as ToolGroup;

            tool.Name = ToolNameTextBox.Text.Trim();
            tool.ExecutablePath = ExecutablePathTextBox.Text.Trim();
            tool.ShortDescription = ShortDescriptionTextBox.Text?.Trim() ?? string.Empty;
            tool.LongDescription = LongDescriptionTextBox.Text?.Trim() ?? string.Empty;
            tool.HomepageUrl = HomepageUrlTextBox.Text?.Trim() ?? string.Empty;
            tool.Tags = (TagsTextBox.Text ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
            tool.Type = IsConsoleCheckBox.IsChecked == true ? ToolType.Console : ToolType.GUI;
            tool.GroupId = selectedGroup?.Id ?? "default";

            if (IsEditMode)
            {
                await _toolService.UpdateToolAsync(tool);
            }
            else
            {
                tool = await _toolService.AddToolAsync(tool);
            }

            SavedTool = tool;
        }
        finally
        {
            deferral.Complete();
        }
    }
}
