using System.Collections.Generic;

namespace FloatingAppBar.Models;

public sealed class ManifestItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Platform { get; set; } = "any";
    public bool ShowInBar { get; set; } = true;
    public string? ActionsManifestPath { get; set; }
    public ManifestActions Actions { get; set; } = new();
}

public sealed class ManifestActions
{
    public RunAction? Run { get; set; }
    public OpenUrlAction? OpenUrl { get; set; }
    public OpenDashboardAction? OpenDashboard { get; set; }
    public List<ActionDefinition> Menu { get; set; } = new();
}

public sealed class RunAction
{
    public string Command { get; set; } = string.Empty;
    public string? Args { get; set; }
    public string? WorkingDirectory { get; set; }
}

public sealed class OpenUrlAction
{
    public string Url { get; set; } = string.Empty;
}

public sealed class OpenDashboardAction
{
    public string ConfigPath { get; set; } = string.Empty;
    public bool? AlwaysOnTop { get; set; }
    public bool? Fullscreen { get; set; }
}

public sealed class ActionManifest
{
    public List<ActionDefinition> Actions { get; set; } = new();
}

public sealed class ActionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RunAction? Run { get; set; }
    public OpenUrlAction? OpenUrl { get; set; }
    public OpenDashboardAction? OpenDashboard { get; set; }
}
