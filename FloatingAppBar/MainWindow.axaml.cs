using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FloatingAppBar.ViewModels;

namespace FloatingAppBar;

public partial class MainWindow : Window
{
    private AppItemViewModel? _dragItem;
    private bool _dragInProgress;
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

        _dragInProgress = true;
        var data = new DataObject();
        data.Set("app-item", _dragItem);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _dragItem = null;
        _dragInProgress = false;
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

        _dragInProgress = true;
        var data = new DataObject();
        data.Set("app-item", _dragItem);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _dragItem = null;
        _dragInProgress = false;
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