using Avalonia.Controls;
using Avalonia.Interactivity;
using FloatingAppBar.ViewModels;

namespace FloatingAppBar;

public partial class AllAppsWindow : Window
{
    public AllAppsWindow(AllAppsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AllAppsViewModel viewModel)
        {
            return;
        }

        if (sender is not Button button || button.DataContext is not AppItemViewModel item)
        {
            return;
        }

        var confirm = new ConfirmDialog("מחיקת מניפסט", "האם אתה בטוח שברצונך למחוק את המניפסט?");
        var result = await confirm.ShowDialog<bool>(this);
        if (result)
        {
            viewModel.DeleteItem(item);
        }
    }
}
