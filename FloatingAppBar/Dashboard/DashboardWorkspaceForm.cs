using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FloatingAppBar.Models;
using FloatingAppBar.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace FloatingAppBar.Dashboard;

public sealed class DashboardWorkspaceForm : Form
{
    private readonly DashboardAppConfig _options;
    private readonly Panel _container;
    private readonly List<WorkspacePane> _panes = new();
    private readonly bool[] _occupied = new bool[4];

    public DashboardWorkspaceForm(DashboardAppConfig options)
    {
        _options = options;
        Text = "Workspace";
        // Keep the floating bar above the web workspace.
        TopMost = false;
        ApplyWindowMode();
        BackColor = Color.Black;

        _container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        Controls.Add(_container);
        Resize += (_, _) => ApplyLayout();
    }

    public WorkspaceAvailability GetAvailability()
    {
        var freeCount = _occupied.Count(slot => !slot);
        return new WorkspaceAvailability
        {
            CanQuarter = freeCount >= 1,
            CanHalf = HasHalfSpace(),
            CanFull = freeCount == 4
        };
    }

    public bool TryAddPane(string title, string url, WorkspaceLayoutChoice choice)
    {
        if (!TryReserveSlots(choice, out var slots))
        {
            return false;
        }

        var host = CreatePaneHost();
        _container.Controls.Add(host.Container);
        var pane = new WorkspacePane(host, slots, url);
        _panes.Add(pane);
        ApplyLayout();
        _ = InitializeWebViewAsync(host.View, title, url);
        return true;
    }

    public bool ContainsUrl(string url)
    {
        return _panes.Any(p => string.Equals(p.Url, url, StringComparison.OrdinalIgnoreCase));
    }

    private async Task InitializeWebViewAsync(WebView2 webView, string title, string url)
    {
        try
        {
            var environment = await DashboardWebViewFactory.GetEnvironmentAsync(null);
            await webView.EnsureCoreWebView2Async(environment);
            ApplyWebViewSettings(webView);
            Navigate(webView, url);
        }
        catch
        {
            // Keep UI alive even if a pane fails to initialize.
        }
    }

    private static void ApplyWebViewSettings(WebView2 webView)
    {
        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var settings = webView.CoreWebView2.Settings;
        settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.NewWindowRequested += (_, args) => { args.Handled = true; };
        webView.CoreWebView2.ProcessFailed += (_, _) =>
        {
            try
            {
                webView.Reload();
            }
            catch
            {
                // Ignore reload failures.
            }
        };
    }

    private static void Navigate(WebView2 webView, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        webView.Source = uri;
    }

    private void ApplyLayout()
    {
        if (_panes.Count == 0)
        {
            return;
        }

        var gap = Math.Max(0, _options.GapPx);
        var slotRects = BuildSlotRects(_container.Width, _container.Height, gap);
        foreach (var pane in _panes)
        {
            var bounds = Union(slotRects, pane.Slots);
            pane.Host.Container.Bounds = bounds;
        }
    }

    private Rectangle[] BuildSlotRects(int width, int height, int gap)
    {
        var insetWidth = Math.Max(0, width - gap * 2);
        var insetHeight = Math.Max(0, height - gap * 2);
        var cellWidth = Math.Max(1, (insetWidth - gap) / 2);
        var cellHeight = Math.Max(1, (insetHeight - gap) / 2);
        var x0 = gap;
        var y0 = gap;

        return new[]
        {
            new Rectangle(x0, y0, cellWidth, cellHeight),
            new Rectangle(x0 + cellWidth + gap, y0, cellWidth, cellHeight),
            new Rectangle(x0, y0 + cellHeight + gap, cellWidth, cellHeight),
            new Rectangle(x0 + cellWidth + gap, y0 + cellHeight + gap, cellWidth, cellHeight)
        };
    }

    private static Rectangle Union(Rectangle[] slots, IReadOnlyList<int> indices)
    {
        var rect = slots[indices[0]];
        var minX = rect.Left;
        var minY = rect.Top;
        var maxX = rect.Right;
        var maxY = rect.Bottom;

        for (var i = 1; i < indices.Count; i++)
        {
            var r = slots[indices[i]];
            minX = Math.Min(minX, r.Left);
            minY = Math.Min(minY, r.Top);
            maxX = Math.Max(maxX, r.Right);
            maxY = Math.Max(maxY, r.Bottom);
        }

        return new Rectangle(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
    }

    private bool TryReserveSlots(WorkspaceLayoutChoice choice, out int[] slots)
    {
        slots = Array.Empty<int>();
        switch (choice)
        {
            case WorkspaceLayoutChoice.Full:
                if (_occupied.All(slot => !slot))
                {
                    slots = new[] { 0, 1, 2, 3 };
                    MarkOccupied(slots);
                    return true;
                }
                return false;
            case WorkspaceLayoutChoice.Half:
                return TryReserveHalf(out slots);
            case WorkspaceLayoutChoice.Quarter:
                return TryReserveQuarter(out slots);
            default:
                return false;
        }
    }

    private bool TryReserveQuarter(out int[] slots)
    {
        slots = Array.Empty<int>();
        for (var i = 0; i < _occupied.Length; i++)
        {
            if (!_occupied[i])
            {
                slots = new[] { i };
                MarkOccupied(slots);
                return true;
            }
        }

        return false;
    }

    private bool TryReserveHalf(out int[] slots)
    {
        slots = Array.Empty<int>();
        var pairs = new[]
        {
            new[] { 0, 2 }, // left
            new[] { 1, 3 }, // right
            new[] { 0, 1 }, // top
            new[] { 2, 3 }  // bottom
        };

        foreach (var pair in pairs)
        {
            if (!_occupied[pair[0]] && !_occupied[pair[1]])
            {
                slots = pair;
                MarkOccupied(slots);
                return true;
            }
        }

        return false;
    }

    private bool HasHalfSpace()
    {
        return (!_occupied[0] && !_occupied[2]) ||
               (!_occupied[1] && !_occupied[3]) ||
               (!_occupied[0] && !_occupied[1]) ||
               (!_occupied[2] && !_occupied[3]);
    }

    private void MarkOccupied(IEnumerable<int> slots)
    {
        foreach (var slot in slots)
        {
            _occupied[slot] = true;
        }
    }

    private void ApplyWindowMode()
    {
        if (_options.Fullscreen)
        {
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            return;
        }

        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        Size = new Size(1280, 720);
    }

    private sealed class WorkspacePane
    {
        public WorkspacePane(PaneHost host, int[] slots, string url)
        {
            Host = host;
            Slots = slots;
            Url = url;
        }

        public PaneHost Host { get; }
        public int[] Slots { get; }
        public string Url { get; }
    }

    private PaneHost CreatePaneHost()
    {
        var webView = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties()
        };

        var container = new Panel
        {
            BackColor = Color.Black
        };

        webView.Dock = DockStyle.Fill;
        container.Controls.Add(webView);

        var closeButton = new Button
        {
            Text = "X",
            Width = 24,
            Height = 24,
            BackColor = Color.FromArgb(180, 30, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        closeButton.Location = new Point(container.Width - closeButton.Width - 4, 4);
        closeButton.Click += (_, _) => ClosePane(container);

        container.Controls.Add(closeButton);
        closeButton.BringToFront();

        container.Resize += (_, _) =>
        {
            closeButton.Location = new Point(container.Width - closeButton.Width - 4, 4);
        };

        return new PaneHost(container, webView);
    }

    private void ClosePane(Panel container)
    {
        var pane = _panes.FirstOrDefault(p => ReferenceEquals(p.Host.Container, container));
        if (pane is null)
        {
            return;
        }

        _panes.Remove(pane);
        ReleaseSlots(pane.Slots);
        _container.Controls.Remove(container);
        pane.Host.View.Dispose();
        container.Dispose();
        ApplyLayout();
    }

    private void ReleaseSlots(IEnumerable<int> slots)
    {
        foreach (var slot in slots)
        {
            _occupied[slot] = false;
        }
    }

    private sealed class PaneHost
    {
        public PaneHost(Panel container, WebView2 view)
        {
            Container = container;
            View = view;
        }

        public Panel Container { get; }
        public WebView2 View { get; }
    }
}
