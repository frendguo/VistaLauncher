using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace VistaLauncher.Controls;

public sealed partial class SearchBar : UserControl
{
    public SearchBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 搜索框文本
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchBar),
            new PropertyMetadata(string.Empty, OnTextChanged));

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchBar searchBar && e.NewValue is string text)
        {
            searchBar.FilterBox.Text = text;
        }
    }

    /// <summary>
    /// 文本变化事件
    /// </summary>
    public event TypedEventHandler<SearchBar, string>? TextChanged;

    /// <summary>
    /// 键盘事件
    /// </summary>
    public event TypedEventHandler<SearchBar, KeyRoutedEventArgs>? TextBoxKeyDown;

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = FilterBox.Text;
        TextChanged?.Invoke(this, FilterBox.Text);
    }

    private void FilterBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        TextBoxKeyDown?.Invoke(this, e);
    }

    /// <summary>
    /// 聚焦搜索框
    /// </summary>
    public void Focus()
    {
        FilterBox.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// 全选文本
    /// </summary>
    public void SelectAll()
    {
        FilterBox.SelectAll();
    }
}
