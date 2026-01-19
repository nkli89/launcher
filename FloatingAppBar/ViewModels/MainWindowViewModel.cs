using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using FloatingAppBar.Models;
using FloatingAppBar.Services;

namespace FloatingAppBar.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<AppItemViewModel> Items { get; } = new();
    public ObservableCollection<AppItemViewModel> AllItems { get; } = new();
    public InfoPanelViewModel InfoPanel { get; }
    public Avalonia.CornerRadius BarCornerRadius { get; }
    private bool _isSquareView;
    private bool _isMinimized;
    private readonly Dictionary<string, List<AppItemViewModel>> _manifestItems = new();
    private readonly DispatcherTimer _runningCheckTimer;

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
    public RelayCommand ShowSquareCommand { get; }
    public RelayCommand ShowDefaultCommand { get; }
    public RelayCommand MinimizeCommand { get; }
    public RelayCommand RestoreCommand { get; }

    public bool IsMinimized
    {
        get => _isMinimized;
        set
        {
            if (SetProperty(ref _isMinimized, value))
            {
                NotifyPropertyChanged(nameof(IsNotMinimized));
            }
        }
    }
    public bool IsNotMinimized => !IsMinimized;

    public MainWindowViewModel()
    {
        var manifestsDirectory = Path.Combine(AppContext.BaseDirectory, "Manifests");
        var manifestBaseDirectory = AppContext.BaseDirectory;
        var loader = new ManifestLoader();
        var directoryLoader = new ManifestDirectoryLoader(loader);
        var manifests = directoryLoader.LoadAll(manifestsDirectory);
        var actionRunner = new ActionRunner();
        var actionManifestLoader = new ActionManifestLoader();
        var settingsLoader = new SettingsLoader();
        var settings = settingsLoader.Load(Path.Combine(AppContext.BaseDirectory, "settings.json"));
        var networkAdapter = ResolveNetworkAdapter(settings);
        InfoPanel = new InfoPanelViewModel(networkAdapter);
        BarCornerRadius = settings.BarShape.Equals("square", StringComparison.OrdinalIgnoreCase)
            ? new Avalonia.CornerRadius(0)
            : new Avalonia.CornerRadius(settings.CornerRadius);
        ShowSquareCommand = new RelayCommand(() => IsSquareView = true);
        ShowDefaultCommand = new RelayCommand(() => IsSquareView = false);
        MinimizeCommand = new RelayCommand(() => IsMinimized = true);
        RestoreCommand = new RelayCommand(() => IsMinimized = false);

        foreach (var entry in manifests)
        {
            foreach (var item in entry.Manifest.Items)
            {
                if (!IsPlatformMatch(item))
                {
                    continue;
                }

                var icon = IconLoader.Load(item.IconPath);
                var command = new RelayCommand(() =>
                {
                    actionRunner.Execute(item, manifestBaseDirectory);
                    UpdateRunningStates();
                });
                var menuActions = BuildMenuActions(item, manifestBaseDirectory, actionRunner, actionManifestLoader);
                var appItem = new AppItemViewModel(
                    item.Title,
                    icon,
                    command,
                    menuActions,
                    item.ShowInBar,
                    entry.Path,
                    DeleteItem,
                    item.Actions.Run,
                    item.Actions.OpenUrl,
                    item.Actions.OpenDashboard);
                appItem.PropertyChanged += OnItemPropertyChanged;
                AllItems.Add(appItem);
                if (!_manifestItems.TryGetValue(entry.Path, out var list))
                {
                    list = new List<AppItemViewModel>();
                    _manifestItems[entry.Path] = list;
                }
                list.Add(appItem);
                // Defer adding to Items until we enforce the display limit.
            }
        }

        RefreshVisibleItems();

        _runningCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _runningCheckTimer.Tick += (_, _) => UpdateRunningStates();
        _runningCheckTimer.Start();
        UpdateRunningStates();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppItemViewModel.IsPinned))
        {
            RefreshVisibleItems();
        }
    }

    private void RefreshVisibleItems()
    {
        Items.Clear();
        foreach (var item in AllItems.Where(i => i.IsPinned).Take(7))
        {
            Items.Add(item);
        }
    }

    private void UpdateRunningStates()
    {
        foreach (var item in AllItems)
        {
            item.IsRunning = IsItemRunning(item);
        }
    }

    private static bool IsItemRunning(AppItemViewModel item)
    {
        if (item.RunAction is { } runAction && !string.IsNullOrWhiteSpace(runAction.Command))
        {
            return IsProcessRunning(runAction.Command);
        }

        if (item.OpenUrlAction is { } openUrlAction && !string.IsNullOrWhiteSpace(openUrlAction.Url))
        {
            return IsUrlOpen(openUrlAction.Url, item.Title) || DashboardWorkspaceLauncher.IsUrlOpen(openUrlAction.Url);
        }

        if (item.OpenDashboardAction is { } openDashboardAction)
        {
            return DashboardLauncher.IsOpen(openDashboardAction, AppContext.BaseDirectory);
        }

        return false;
    }

    private static bool IsProcessRunning(string command)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(command.Trim());
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            return Process.GetProcessesByName(fileName).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUrlOpen(string url, string? titleHint)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var candidate = $"https://{url.Trim()}";
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri))
            {
                return false;
            }
        }

        var host = uri.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var candidates = BuildUrlTitleCandidates(host, url, titleHint);
        if (candidates.Length == 0)
        {
            return false;
        }

        try
        {
            foreach (var process in Process.GetProcesses())
            {
                var title = process.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                foreach (var candidate in candidates)
                {
                    if (title.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string[] BuildUrlTitleCandidates(string host, string url, string? titleHint)
    {
        var trimmedHost = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? host[4..]
            : host;

        var urlWithoutScheme = url;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            urlWithoutScheme = url[7..];
        }
        else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            urlWithoutScheme = url[8..];
        }

        var candidates = new List<string>
        {
            host,
            trimmedHost,
            $"www.{trimmedHost}",
            urlWithoutScheme,
            titleHint ?? string.Empty
        };

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => candidate.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void DeleteItem(AppItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(item.ManifestPath))
        {
            return;
        }

        if (_manifestItems.TryGetValue(item.ManifestPath, out var list))
        {
            foreach (var entry in list)
            {
                entry.PropertyChanged -= OnItemPropertyChanged;
                AllItems.Remove(entry);
                Items.Remove(entry);
            }

            _manifestItems.Remove(item.ManifestPath);
        }

        try
        {
            if (File.Exists(item.ManifestPath))
            {
                File.Delete(item.ManifestPath);
            }
        }
        catch
        {
            // Ignore delete failures; the UI is already updated.
        }
    }

    public void MovePinnedItem(AppItemViewModel source, AppItemViewModel target)
    {
        if (source == target)
        {
            return;
        }

        if (!Items.Contains(source) || !Items.Contains(target))
        {
            return;
        }

        var oldIndex = Items.IndexOf(source);
        var newIndex = Items.IndexOf(target);
        if (oldIndex < 0 || newIndex < 0)
        {
            return;
        }

        Items.Move(oldIndex, newIndex);

        var orderedPinned = Items.ToList();
        var unpinned = AllItems.Where(i => !i.IsPinned).ToList();
        AllItems.Clear();
        foreach (var item in orderedPinned)
        {
            AllItems.Add(item);
        }
        foreach (var item in unpinned)
        {
            AllItems.Add(item);
        }

        RefreshVisibleItems();
    }

    private static ObservableCollection<ActionMenuItemViewModel> BuildMenuActions(
        ManifestItem item,
        string manifestBaseDirectory,
        ActionRunner actionRunner,
        ActionManifestLoader actionManifestLoader)
    {
        var menu = new ObservableCollection<ActionMenuItemViewModel>();

        if (item.Actions.Run is not null)
        {
            var title = "הרץ";
            menu.Add(new ActionMenuItemViewModel(title, new RelayCommand(() => actionRunner.Run(item.Actions.Run, manifestBaseDirectory))));
        }

        if (item.Actions.OpenUrl is not null)
        {
            var title = "פתח קישור";
            menu.Add(new ActionMenuItemViewModel(title, new RelayCommand(() => actionRunner.OpenUrl(item.Actions.OpenUrl))));
        }

        foreach (var action in item.Actions.Menu)
        {
            var title = string.IsNullOrWhiteSpace(action.Title) ? "פעולה" : action.Title;
            menu.Add(new ActionMenuItemViewModel(title, new RelayCommand(() => actionRunner.Execute(action, manifestBaseDirectory))));
        }

        var sidecarPath = ResolveSidecarPath(item, manifestBaseDirectory);
        if (!string.IsNullOrWhiteSpace(sidecarPath) && File.Exists(sidecarPath))
        {
            var baseDirectory = Path.GetDirectoryName(sidecarPath) ?? manifestBaseDirectory;
            var sidecar = actionManifestLoader.Load(sidecarPath);
            foreach (var action in sidecar.Actions)
            {
                var title = string.IsNullOrWhiteSpace(action.Title) ? "פעולה" : action.Title;
                menu.Add(new ActionMenuItemViewModel(title, new RelayCommand(() => actionRunner.Execute(action, baseDirectory))));
            }
        }

        return menu;
    }

    private static string? ResolveSidecarPath(ManifestItem item, string manifestBaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(item.ActionsManifestPath))
        {
            return ResolvePath(item.ActionsManifestPath, manifestBaseDirectory);
        }

        if (item.Actions.Run is null || string.IsNullOrWhiteSpace(item.Actions.Run.Command))
        {
            return null;
        }

        var runCommand = ResolvePath(item.Actions.Run.Command, manifestBaseDirectory);
        if (!File.Exists(runCommand))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(runCommand);
        var fileName = Path.GetFileNameWithoutExtension(runCommand);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return Path.Combine(directory, $"{fileName}.actions.json");
    }

    private static string? ResolveNetworkAdapter(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.NetworkAdapter))
        {
            return settings.NetworkAdapter;
        }

        if (!string.IsNullOrWhiteSpace(settings.NetworkAdapterWifi))
        {
            return settings.NetworkAdapterWifi;
        }

        if (!string.IsNullOrWhiteSpace(settings.NetworkAdapterLan))
        {
            return settings.NetworkAdapterLan;
        }

        return null;
    }

    private static string ResolvePath(string path, string baseDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static bool IsPlatformMatch(ManifestItem item)
    {
        if (string.Equals(item.Platform, "any", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (OperatingSystem.IsWindows() && string.Equals(item.Platform, "windows", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (OperatingSystem.IsLinux() && string.Equals(item.Platform, "linux", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
