using Avalonia.Controls;

namespace FloatingAppBar;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message)
    {
        InitializeComponent();
        DataContext = new ConfirmDialogViewModel(title, message);
    }

    private void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}

public sealed class ConfirmDialogViewModel
{
    public ConfirmDialogViewModel(string title, string message)
    {
        TitleText = title;
        MessageText = message;
    }

    public string TitleText { get; }
    public string MessageText { get; }
}
