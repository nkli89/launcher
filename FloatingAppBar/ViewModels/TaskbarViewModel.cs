using System.Windows.Input;

namespace FloatingAppBar.ViewModels;

public sealed class TaskbarViewModel : ViewModelBase
{
    private bool _isSquareView;

    public TaskbarViewModel()
    {
        InfoPanel = new InfoPanelViewModel();
        ShowSquareCommand = new RelayCommand(() => IsSquareView = true);
        ShowDefaultCommand = new RelayCommand(() => IsSquareView = false);
    }

    public InfoPanelViewModel InfoPanel { get; }

    public bool IsSquareView
    {
        get => _isSquareView;
        set
        {
            if (SetProperty(ref _isSquareView, value))
            {
                NotifyPropertyChanged(nameof(IsDefaultView));
            }
        }
    }

    public bool IsDefaultView => !IsSquareView;

    public ICommand ShowSquareCommand { get; }
    public ICommand ShowDefaultCommand { get; }
}
