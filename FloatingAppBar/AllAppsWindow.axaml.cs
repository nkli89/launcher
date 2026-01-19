using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FloatingAppBar.ViewModels;

namespace FloatingAppBar;

public partial class AllAppsWindow : Window
{
    private Control? _anchor;
    private Window? _owner;

    public AllAppsWindow(AllAppsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void AttachAnchor(Control? anchor, Window? owner)
    {
        _anchor = anchor;
        _owner = owner;
        if (IsVisible)
        {
            PositionUnderAnchor();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionUnderAnchor();
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

    private void PositionUnderAnchor()
    {
        if (_anchor is null)
        {
            return;
        }

        var anchorPoint = _anchor.PointToScreen(new Point(0, _anchor.Bounds.Height));
        var desiredX = anchorPoint.X;
        var desiredY = anchorPoint.Y + 6;

        if (_owner is not null)
        {
            var ownerBottom = _owner.PointToScreen(new Point(0, _owner.Bounds.Height));
            desiredY = ownerBottom.Y + 6;
        }
        var screens = (_owner ?? this).Screens;
        var screen = screens.ScreenFromPoint(anchorPoint) ?? screens.Primary;
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
}
