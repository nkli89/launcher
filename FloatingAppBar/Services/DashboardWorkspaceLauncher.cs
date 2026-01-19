using System;
using System.IO;
using FloatingAppBar.Dashboard;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public enum WorkspaceLayoutChoice
{
    Quarter,
    Half,
    Full
}

public sealed class WorkspaceAvailability
{
    public bool CanQuarter { get; init; }
    public bool CanHalf { get; init; }
    public bool CanFull { get; init; }
}

public static class DashboardWorkspaceLauncher
{
    private static DashboardWorkspaceForm? _form;

    public static WorkspaceAvailability GetAvailability()
    {
        if (_form is null || _form.IsDisposed)
        {
            return new WorkspaceAvailability
            {
                CanQuarter = true,
                CanHalf = true,
                CanFull = true
            };
        }

        return _form.GetAvailability();
    }

    public static bool TryOpen(string title, string url, WorkspaceLayoutChoice choice)
    {
        if (!TryNormalizeUrl(url, out var normalized))
        {
            return false;
        }

        EnsureForm();
        if (_form is null)
        {
            return false;
        }

        return _form.TryAddPane(title, normalized, choice);
    }

    public static bool IsUrlOpen(string url)
    {
        if (_form is null || _form.IsDisposed)
        {
            return false;
        }

        if (!TryNormalizeUrl(url, out var normalized))
        {
            return false;
        }

        return _form.ContainsUrl(normalized);
    }

    private static void EnsureForm()
    {
        if (_form is not null && !_form.IsDisposed)
        {
            return;
        }

        var defaults = new DashboardAppConfig();
        _form = new DashboardWorkspaceForm(defaults);
        _form.FormClosed += (_, _) => _form = null;
        _form.Show();
    }

    private static bool TryNormalizeUrl(string url, out string normalized)
    {
        normalized = string.Empty;
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

        normalized = uri.ToString();
        return true;
    }
}
