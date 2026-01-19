using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Forms;
using FloatingAppBar.Dashboard;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public static class DashboardLauncher
{
    private static readonly ConcurrentDictionary<string, DashboardForm> OpenForms = new(StringComparer.OrdinalIgnoreCase);
    private static readonly DashboardConfigLoader Loader = new();

    public static void Open(OpenDashboardAction action, string? baseDirectory)
    {
        var configPath = ResolvePath(action.ConfigPath, baseDirectory);
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return;
        }

        if (OpenForms.TryGetValue(configPath, out var existing))
        {
            TryActivate(existing);
            return;
        }

        var config = Loader.Load(configPath);
        ApplyOverrides(config, action);

        var form = new DashboardForm(config, Path.GetFileNameWithoutExtension(configPath));
        form.FormClosed += (_, _) => OpenForms.TryRemove(configPath, out _);
        OpenForms[configPath] = form;
        form.Show();
        TryActivate(form);
    }

    public static bool IsOpen(OpenDashboardAction action, string? baseDirectory)
    {
        var configPath = ResolvePath(action.ConfigPath, baseDirectory);
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return false;
        }

        return OpenForms.ContainsKey(configPath);
    }

    private static void ApplyOverrides(DashboardConfig config, OpenDashboardAction action)
    {
        if (action.AlwaysOnTop.HasValue)
        {
            config.App.AlwaysOnTop = action.AlwaysOnTop.Value;
        }

        if (action.Fullscreen.HasValue)
        {
            config.App.Fullscreen = action.Fullscreen.Value;
        }
    }

    private static void TryActivate(Form form)
    {
        try
        {
            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
            }

            form.Activate();
        }
        catch
        {
            // Ignore focus failures.
        }
    }

    private static string ResolvePath(string? path, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(baseDirectory))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}
