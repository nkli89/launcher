using System.Windows.Input;

namespace FloatingAppBar.ViewModels;

public sealed class ActionMenuItemViewModel : ViewModelBase
{
    private string _title;

    public ActionMenuItemViewModel(string title, ICommand command)
    {
        _title = title;
        Command = command;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ICommand Command { get; }
}
