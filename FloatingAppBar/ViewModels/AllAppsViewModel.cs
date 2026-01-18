using System.Collections.ObjectModel;

namespace FloatingAppBar.ViewModels;

public sealed class AllAppsViewModel
{
    private readonly MainWindowViewModel _main;

    public AllAppsViewModel(MainWindowViewModel main, ObservableCollection<AppItemViewModel> items)
    {
        _main = main;
        Items = items;
    }

    public ObservableCollection<AppItemViewModel> Items { get; }

    public void DeleteItem(AppItemViewModel item) => _main.DeleteItem(item);
}
