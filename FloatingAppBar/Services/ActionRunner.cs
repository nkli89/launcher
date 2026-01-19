using System;
using System.Diagnostics;
using System.IO;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class ActionRunner
{
    public void Execute(ManifestItem item, string? baseDirectory = null)
    {
        try
        {
            if (item.Actions.OpenDashboard is { } openDashboard)
            {
                DashboardLauncher.Open(openDashboard, baseDirectory);
                return;
            }

            if (item.Actions.OpenUrl is { } openUrl)
            {
                OpenUrl(openUrl);
                return;
            }

            if (item.Actions.Run is { } run)
            {
                Run(run, baseDirectory);
            }
        }
        catch
        {
            // Avoid crashing the UI on bad commands/paths.
        }
    }

    public void Execute(ActionDefinition action, string? baseDirectory = null)
    {
        try
        {
            if (action.OpenDashboard is { } openDashboard)
            {
                DashboardLauncher.Open(openDashboard, baseDirectory);
                return;
            }

            if (action.OpenUrl is { } openUrl)
            {
                OpenUrl(openUrl);
                return;
            }

            if (action.Run is { } run)
            {
                Run(run, baseDirectory);
            }
        }
        catch
        {
            // Avoid crashing the UI on bad commands/paths.
        }
    }

    public void Run(RunAction action, string? baseDirectory = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(action.Command))
            {
                return;
            }

            var command = ResolvePath(action.Command, baseDirectory);
            var workingDirectory = ResolvePath(action.WorkingDirectory, baseDirectory);

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = action.Args ?? string.Empty,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            Process.Start(startInfo);
        }
        catch
        {
            // Ignore failed process start to keep the app running.
        }
    }

    public void OpenUrl(OpenUrlAction action)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(action.Url))
            {
                return;
            }

            if (!Uri.TryCreate(action.Url, UriKind.Absolute, out var uri))
            {
                var candidate = $"https://{action.Url.Trim()}";
                if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri))
                {
                    return;
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch
        {
            // Ignore invalid URLs to keep UI stable.
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
