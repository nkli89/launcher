using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FloatingAppBar.Services;

namespace FloatingAppBar;

public partial class LayoutChoiceDialog : Window
{
    private readonly Control? _anchor;
    private readonly Window? _owner;
    public event Action<WorkspaceLayoutChoice?>? ChoiceSelected;

    public LayoutChoiceDialog(WorkspaceAvailability availability, Control? anchor, Window? owner)
    {
        InitializeComponent();
        DataContext = new LayoutChoiceDialogViewModel(availability);
        _anchor = anchor;
        _owner = owner;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionUnderAnchor();
        AttachOutsideCloseHandlers();
    }

    private void OnQuarterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChoiceSelected?.Invoke(WorkspaceLayoutChoice.Quarter);
        Close(WorkspaceLayoutChoice.Quarter);
    }

    private void OnHalfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChoiceSelected?.Invoke(WorkspaceLayoutChoice.Half);
        Close(WorkspaceLayoutChoice.Half);
    }

    private void OnFullClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChoiceSelected?.Invoke(WorkspaceLayoutChoice.Full);
        Close(WorkspaceLayoutChoice.Full);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ChoiceSelected?.Invoke(null);
        Close(null);
    }

    private void PositionUnderAnchor()
    {
        if (_anchor is null)
        {
            return;
        }

        var screenPoint = _anchor.PointToScreen(new Point(0, _anchor.Bounds.Height));
        var desiredX = screenPoint.X;
        var desiredY = screenPoint.Y + 6;
        var screens = (_owner ?? this).Screens;
        var screen = screens.ScreenFromPoint(screenPoint) ?? screens.Primary;
        if (screen is null)
        {
            return;
        }

        var working = screen.WorkingArea;
        var width = (int)Math.Max(1, Bounds.Width);
        var height = (int)Math.Max(1, Bounds.Height);

        var x = Math.Clamp(desiredX, working.X, working.Right - width);
        var y = Math.Clamp(desiredY, working.Y, working.Bottom - height);

        Position = new PixelPoint(x, y);
    }

    private void AttachOutsideCloseHandlers()
    {
        var ownerWindow = _owner;
        if (ownerWindow is null)
        {
            return;
        }

        ownerWindow.PointerPressed += OwnerPointerPressed;
        Closed += (_, _) => ownerWindow.PointerPressed -= OwnerPointerPressed;
        Deactivated += (_, _) => Close(null);
    }

    private void OwnerPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (IsPointerOver)
        {
            return;
        }

        ChoiceSelected?.Invoke(null);
        Close(null);
    }
}

public sealed class LayoutChoiceDialogViewModel
{
    public LayoutChoiceDialogViewModel(WorkspaceAvailability availability)
    {
        CanQuarter = availability.CanQuarter;
        CanHalf = availability.CanHalf;
        CanFull = availability.CanFull;
    }

    public bool CanQuarter { get; }
    public bool CanHalf { get; }
    public bool CanFull { get; }
}
