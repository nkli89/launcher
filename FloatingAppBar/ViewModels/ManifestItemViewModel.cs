using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using FloatingAppBar.Models;

namespace FloatingAppBar.ViewModels;

public sealed class AppItemViewModel : ViewModelBase
{
    private bool _isPinned;
    private bool _isRunning;
    private readonly ActionMenuItemViewModel _pinMenuItem;
    private readonly Action<AppItemViewModel>? _deleteAction;

    public AppItemViewModel(
        string title,
        IImage? icon,
        ICommand activateCommand,
        ObservableCollection<ActionMenuItemViewModel> menuActions,
        bool isPinned,
        string? manifestPath,
        Action<AppItemViewModel>? deleteAction,
        RunAction? runAction,
        OpenUrlAction? openUrlAction)
    {
        Title = title;
        Icon = icon;
        ActivateCommand = activateCommand;
        MenuActions = menuActions;
        _isPinned = isPinned;
        ManifestPath = manifestPath;
        RunAction = runAction;
        OpenUrlAction = openUrlAction;
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
    public RunAction? RunAction { get; }
    public OpenUrlAction? OpenUrlAction { get; }

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

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                NotifyPropertyChanged(nameof(IsNotRunning));
                NotifyPropertyChanged(nameof(ItemOpacity));
            }
        }
    }

    public bool IsNotRunning => !IsRunning;

    public double ItemOpacity => IsRunning ? 0.4 : 1.0;

    private void TogglePinned()
    {
        IsPinned = !IsPinned;
    }

    private void Delete()
    {
        _deleteAction?.Invoke(this);
    }
}
