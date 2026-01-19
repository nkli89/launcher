using System;
using System.Linq;
using System.Management;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FloatingAppBar.Services;
using FloatingAppBar.ViewModels;

namespace FloatingAppBar;

public partial class MainWindow : Window
{
    private const int SnapThreshold = 20;
    private const int BrightnessMin = 0;
    private const int BrightnessMax = 100;

    private AppItemViewModel? _dragItem;
    private bool _dragInProgress;
    private bool _dragReady;
    private DispatcherTimer? _dragDelayTimer;
    private PointerEventArgs? _lastPointerEvent;
    private Point _dragStartPoint;
    private AllAppsWindow? _allAppsWindow;
    private Slider? _brightnessSlider;
    private TextBlock? _brightnessValueText;
    private TextBlock? _brightnessStatusText;
    private DispatcherTimer? _brightnessTimer;
    private byte? _pendingBrightness;
    private bool _brightnessSupported;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Topmost = true;
        InitializeBrightnessControls();
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
            SetPressedState(control, true);
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
                if (sender is Control control)
                {
                    SetPressedState(control, false);
                    OpenAppItem(_dragItem, control);
                }
            }
            _dragItem = null;
            _dragReady = false;
            _lastPointerEvent = null;
        }
        else if (sender is Control draggedControl)
        {
            SetPressedState(draggedControl, false);
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
        if (_allAppsWindow is not null)
        {
            _allAppsWindow.Close();
            _allAppsWindow = null;
        }

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

    private void OnWindowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        SnapToEdges();
    }

    private static void SetPressedState(Control control, bool isPressed)
    {
        if (isPressed)
        {
            control.Classes.Add("pressed");
        }
        else
        {
            control.Classes.Remove("pressed");
        }
    }

    private void OnAllAppsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var anchor = sender as Control;
        if (_allAppsWindow is null)
        {
            _allAppsWindow = new AllAppsWindow(new AllAppsViewModel(viewModel, viewModel.AllItems));
            _allAppsWindow.Closed += (_, _) => _allAppsWindow = null;
        }

        _allAppsWindow.AttachAnchor(anchor, this);
        _allAppsWindow.Show();
        _allAppsWindow.Activate();
    }

    private void InitializeBrightnessControls()
    {
        _brightnessSlider = this.FindControl<Slider>("BrightnessSlider");
        _brightnessValueText = this.FindControl<TextBlock>("BrightnessValueText");
        _brightnessStatusText = this.FindControl<TextBlock>("BrightnessStatusText");

        if (_brightnessSlider is null)
        {
            return;
        }

        var current = TryGetBrightness();
        if (current.HasValue)
        {
            _brightnessSupported = true;
            _brightnessSlider.Value = current.Value;
            UpdateBrightnessText(current.Value);
            UpdateBrightnessStatus(string.Empty);
        }
        else
        {
            _brightnessSupported = false;
            _brightnessSlider.IsEnabled = false;
            UpdateBrightnessText(null);
            UpdateBrightnessStatus("בהירות לא נתמכת במסך זה");
        }
    }

    private void OnBrightnessChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_brightnessSupported)
        {
            return;
        }

        var value = (byte)Math.Clamp((int)Math.Round(e.NewValue), BrightnessMin, BrightnessMax);
        UpdateBrightnessText(value);
        QueueBrightnessSet(value);
    }

    private void QueueBrightnessSet(byte value)
    {
        _pendingBrightness = value;
        if (_brightnessTimer is null)
        {
            _brightnessTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _brightnessTimer.Tick += OnBrightnessTimerTick;
        }

        _brightnessTimer.Stop();
        _brightnessTimer.Start();
    }

    private void OnBrightnessTimerTick(object? sender, EventArgs e)
    {
        _brightnessTimer?.Stop();
        if (_pendingBrightness is null)
        {
            return;
        }

        var success = TrySetBrightness(_pendingBrightness.Value);
        UpdateBrightnessStatus(success ? string.Empty : "שינוי בהירות נכשל");
    }

    private void UpdateBrightnessText(int? brightness)
    {
        if (_brightnessValueText is null)
        {
            return;
        }

        _brightnessValueText.Text = brightness.HasValue ? $"{brightness.Value}%" : "--%";
    }

    private void UpdateBrightnessStatus(string message)
    {
        if (_brightnessStatusText is null)
        {
            return;
        }

        _brightnessStatusText.Text = message;
        _brightnessStatusText.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private static byte? TryGetBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "root\\wmi",
                "SELECT CurrentBrightness FROM WmiMonitorBrightness");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentBrightness"] is byte current)
                {
                    return current;
                }
            }
        }
        catch
        {
            // Ignore unsupported hardware or permission issues.
        }

        return null;
    }

    private static bool TrySetBrightness(byte brightness)
    {
        try
        {
            using var methods = new ManagementClass("root\\wmi", "WmiMonitorBrightnessMethods", null);
            foreach (ManagementObject instance in methods.GetInstances())
            {
                instance.InvokeMethod("WmiSetBrightness", new object[] { 0, brightness });
                return true;
            }
        }
        catch
        {
            // Ignore unsupported hardware or permission issues.
        }

        return false;
    }

    private void SnapToEdges()
    {
        var current = Position;
        var screen = Screens.ScreenFromPoint(current) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var working = screen.WorkingArea;
        var width = (int)Math.Max(1, Bounds.Width);
        var height = (int)Math.Max(1, Bounds.Height);

        var snapX = current.X;
        var snapY = current.Y;

        if (Math.Abs(current.X - working.X) <= SnapThreshold)
        {
            snapX = working.X;
        }
        else if (Math.Abs((current.X + width) - working.Right) <= SnapThreshold)
        {
            snapX = working.Right - width;
        }

        if (Math.Abs(current.Y - working.Y) <= SnapThreshold)
        {
            snapY = working.Y;
        }
        else if (Math.Abs((current.Y + height) - working.Bottom) <= SnapThreshold)
        {
            snapY = working.Bottom - height;
        }

        if (snapX == current.X && snapY == current.Y)
        {
            return;
        }

        Position = new PixelPoint(snapX, snapY);
    }
}