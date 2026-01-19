using System.Collections.Generic;

namespace FloatingAppBar.Models;

public sealed class DashboardConfig
{
    public DashboardAppConfig App { get; set; } = new();
    public DashboardLayoutConfig Layout { get; set; } = new();
    public List<DashboardPaneConfig> Panes { get; set; } = new();
}

public sealed class DashboardAppConfig
{
    public bool Fullscreen { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public string OverlayHotkey { get; set; } = "Ctrl+Shift+O";
    public int GapPx { get; set; } = 6;
}

public sealed class DashboardLayoutConfig
{
    public string Id { get; set; } = "QUAD";
    public string Orientation { get; set; } = "Vertical";
}

public sealed class DashboardPaneConfig
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double Zoom { get; set; } = 1.0;
    public DashboardNavigationConfig? Navigation { get; set; }
    public DashboardUiConfig? Ui { get; set; }
    public DashboardInjectionConfig? Injection { get; set; }
    public string? UserDataFolder { get; set; }
}

public sealed class DashboardNavigationConfig
{
    public string Mode { get; set; } = "allow";
    public List<string> Hosts { get; set; } = new();
}

public sealed class DashboardUiConfig
{
    public bool DisableContextMenu { get; set; }
    public bool BlockPopups { get; set; }
}

public sealed class DashboardInjectionConfig
{
    public string? Css { get; set; }
    public string? Js { get; set; }
}
