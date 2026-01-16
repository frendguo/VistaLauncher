using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using VistaLauncher.Services;

namespace VistaLauncher.Controls;

public sealed partial class ImportDialog : ContentDialog
{
    private readonly IImportService _importService;

    /// <summary>
    /// 用户选择的 NirLauncher 目录路径
    /// </summary>
    public string SelectedPath => PathTextBox.Text;

    public ImportDialog(IImportService importService)
    {
        _importService = importService;
        InitializeComponent();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;

            // 打开文件选择器前禁用主窗口的自动隐藏
            (App.MainWindow as MainWindow)?.SuppressAutoHide(true);

            try
            {
                var picker = new FolderPicker(this.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    ViewMode = PickerViewMode.List
                };

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    PathTextBox.Text = folder.Path;
                }
            }
            finally
            {
                // 文件选择器关闭后恢复自动隐藏
                (App.MainWindow as MainWindow)?.SuppressAutoHide(false);
                button.IsEnabled = true;
            }
        }
    }
}
