using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FloatingAppBar.Services;
using FloatingAppBar.ViewModels;

namespace FloatingAppBar;

public partial class MainWindow : Window
{
    private AppItemViewModel? _dragItem;
    private bool _dragInProgress;
    private bool _dragReady;
    private DispatcherTimer? _dragDelayTimer;
    private PointerEventArgs? _lastPointerEvent;
    private Point _dragStartPoint;
    private AllAppsWindow? _allAppsWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Topmost = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginMoveDrag(e);
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (sender is Control control && control.DataContext is AppItemViewModel item)
        {
            _dragItem = item;
            _dragInProgress = false;
            _dragReady = false;
            _lastPointerEvent = null;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(control);
            StartDragDelayTimer();
            e.Handled = true;
        }
    }

    private async void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragItem is null || _dragInProgress)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastPointerEvent = e;
        if (!_dragReady)
        {
            return;
        }

        if (!IsDragThresholdExceeded(e))
        {
            return;
        }

        _dragInProgress = true;
        var data = new DataObject();
        data.Set("app-item", _dragItem);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _dragItem = null;
        _dragInProgress = false;
        _dragReady = false;
        _lastPointerEvent = null;
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        StopDragDelayTimer();
        e.Pointer.Capture(null);
        if (!_dragInProgress)
        {
            if (_dragItem is not null)
            {
                OpenAppItem(_dragItem, sender as Control);
            }
            _dragItem = null;
            _dragReady = false;
            _lastPointerEvent = null;
        }
    }

    private void OnItemsAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (sender is Control control && control.DataContext is AppItemViewModel item)
        {
            _dragItem = item;
            _dragInProgress = false;
            _dragReady = false;
            _lastPointerEvent = null;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(control);
            StartDragDelayTimer();
        }
    }

    private async void OnItemsAreaPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragItem is null || _dragInProgress)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _lastPointerEvent = e;
        if (!_dragReady)
        {
            return;
        }

        if (!IsDragThresholdExceeded(e))
        {
            return;
        }

        _dragInProgress = true;
        var data = new DataObject();
        data.Set("app-item", _dragItem);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _dragItem = null;
        _dragInProgress = false;
        _dragReady = false;
        _lastPointerEvent = null;
    }

    private void StartDragDelayTimer()
    {
        StopDragDelayTimer();
        _dragDelayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _dragDelayTimer.Tick += OnDragDelayTick;
        _dragDelayTimer.Start();
    }

    private void StopDragDelayTimer()
    {
        if (_dragDelayTimer is null)
        {
            return;
        }

        _dragDelayTimer.Tick -= OnDragDelayTick;
        _dragDelayTimer.Stop();
        _dragDelayTimer = null;
    }

    private void OnDragDelayTick(object? sender, EventArgs e)
    {
        StopDragDelayTimer();
        _dragReady = true;
        TryStartDragFromTimer();
    }

    private void TryStartDragFromTimer()
    {
        if (_dragItem is null || _dragInProgress || _lastPointerEvent is null)
        {
            return;
        }

        if (!_lastPointerEvent.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!IsDragThresholdExceeded(_lastPointerEvent))
        {
            return;
        }

        _dragInProgress = true;
        var data = new DataObject();
        data.Set("app-item", _dragItem);

        Dispatcher.UIThread.Post(async () =>
        {
            await DragDrop.DoDragDrop(_lastPointerEvent, data, DragDropEffects.Move);
            _dragItem = null;
            _dragInProgress = false;
            _dragReady = false;
            _lastPointerEvent = null;
        });
    }

    private bool IsDragThresholdExceeded(PointerEventArgs e)
    {
        var current = e.GetPosition(this);
        var dx = current.X - _dragStartPoint.X;
        var dy = current.Y - _dragStartPoint.Y;
        return Math.Abs(dx) >= 4 || Math.Abs(dy) >= 4;
    }

    private async void OpenAppItem(AppItemViewModel item, Control? anchor)
    {
        if (!item.IsNotRunning)
        {
            return;
        }

        if (item.OpenUrlAction is { } openUrlAction && !string.IsNullOrWhiteSpace(openUrlAction.Url))
        {
            var availability = DashboardWorkspaceLauncher.GetAvailability();
            var dialog = new LayoutChoiceDialog(availability, anchor, this);
            dialog.ChoiceSelected += async choice =>
            {
                if (choice is null)
                {
                    return;
                }

                if (!DashboardWorkspaceLauncher.TryOpen(item.Title, openUrlAction.Url, choice.Value))
                {
                    var message = new ConfirmDialog("אין מקום פנוי", "אין מקום פנוי להצגת הממשק בפריסה שבחרת.");
                    await message.ShowDialog<bool>(this);
                }
            };
            dialog.Show();
            return;
        }

        if (item.ActivateCommand.CanExecute(null))
        {
            item.ActivateCommand.Execute(null);
        }
    }

    private void OnItemsAreaDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!e.Data.Contains("app-item"))
        {
            return;
        }

        var source = e.Data.Get("app-item") as AppItemViewModel;
        var target = FindAppItem(e.Source as Control);
        if (source is null || target is null)
        {
            return;
        }

        viewModel.MovePinnedItem(source, target);
        e.Handled = true;
    }

    private static AppItemViewModel? FindAppItem(Control? control)
    {
        if (control is null)
        {
            return null;
        }

        if (control.DataContext is AppItemViewModel direct)
        {
            return direct;
        }

        foreach (var ancestor in control.GetVisualAncestors())
        {
            if (ancestor is Control ancestorControl && ancestorControl.DataContext is AppItemViewModel item)
            {
                return item;
            }
        }

        return null;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnCompactPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            BeginMoveDrag(e);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            viewModel.RestoreCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnAllAppsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (_allAppsWindow is null)
        {
            _allAppsWindow = new AllAppsWindow(new AllAppsViewModel(viewModel, viewModel.AllItems));
            _allAppsWindow.Closed += (_, _) => _allAppsWindow = null;
        }

        _allAppsWindow.Show();
        _allAppsWindow.Activate();
    }
}