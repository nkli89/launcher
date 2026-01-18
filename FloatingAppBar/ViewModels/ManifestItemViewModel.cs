using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;

namespace FloatingAppBar.ViewModels;

public sealed class AppItemViewModel : ViewModelBase
{
    private bool _isPinned;
    private readonly ActionMenuItemViewModel _pinMenuItem;
    private readonly Action<AppItemViewModel>? _deleteAction;

    public AppItemViewModel(
        string title,
        IImage? icon,
        ICommand activateCommand,
        ObservableCollection<ActionMenuItemViewModel> menuActions,
        bool isPinned,
        string? manifestPath,
        Action<AppItemViewModel>? deleteAction)
    {
        Title = title;
        Icon = icon;
        ActivateCommand = activateCommand;
        MenuActions = menuActions;
        _isPinned = isPinned;
        ManifestPath = manifestPath;
        _deleteAction = deleteAction;
        TogglePinnedCommand = new RelayCommand(TogglePinned);
        DeleteCommand = new RelayCommand(Delete);
        _pinMenuItem = new ActionMenuItemViewModel(PinMenuTitle, TogglePinnedCommand);
        ContextMenuEntries = new ObservableCollection<ActionMenuItemViewModel>(MenuActions)
        {
            _pinMenuItem
        };
    }

    public string Title { get; }
    public IImage? Icon { get; }
    public ICommand ActivateCommand { get; }
    public ObservableCollection<ActionMenuItemViewModel> MenuActions { get; }
    public ObservableCollection<ActionMenuItemViewModel> ContextMenuEntries { get; }
    public ICommand TogglePinnedCommand { get; }
    public ICommand DeleteCommand { get; }
    public string? ManifestPath { get; }

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (SetProperty(ref _isPinned, value))
            {
                NotifyPropertyChanged(nameof(PinMenuTitle));
                _pinMenuItem.Title = PinMenuTitle;
            }
        }
    }

    public string PinMenuTitle => IsPinned ? "הסר מהסרגל" : "הצג בסרגל";

    private void TogglePinned()
    {
        IsPinned = !IsPinned;
    }

    private void Delete()
    {
        _deleteAction?.Invoke(this);
    }
}
