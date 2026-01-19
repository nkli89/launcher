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

public sealed class DashboardForm : Form
{
    private readonly DashboardConfig _config;
    private readonly Panel _container;
    private readonly List<DashboardPaneHost> _panes = new();
    private bool _initialized;

    public DashboardForm(DashboardConfig config, string? title)
    {
        _config = config;
        Text = string.IsNullOrWhiteSpace(title) ? "Dashboard" : title;
        // Keep the floating bar above the web dashboards.
        TopMost = false;
        ApplyWindowMode();
        BackColor = Color.Black;

        _container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        Controls.Add(_container);
        Shown += OnShown;
        Resize += (_, _) => ApplyLayout();
    }

    public IReadOnlyList<WebView2> WebViews => _panes.Select(p => p.View).ToList();

    private async void OnShown(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await InitializePanesAsync();
        ApplyLayout();
    }

    private async Task InitializePanesAsync()
    {
        var panes = ResolvePanes();
        foreach (var pane in panes)
        {
            var host = CreatePaneHost(pane);
            _container.Controls.Add(host.Container);
            _panes.Add(host);
            await InitializeWebViewAsync(host.View, pane);
        }
    }

    private List<DashboardPaneConfig> ResolvePanes()
    {
        var layoutId = (_config.Layout.Id ?? string.Empty).Trim().ToUpperInvariant();
        var required = layoutId switch
        {
            "HALF" => 2,
            "MIX_3" => 3,
            _ => 4
        };

        if (_config.Panes.Count <= required)
        {
            return _config.Panes.ToList();
        }

        return _config.Panes.Take(required).ToList();
    }

    private async Task InitializeWebViewAsync(WebView2 webView, DashboardPaneConfig pane)
    {
        try
        {
            var environment = await DashboardWebViewFactory.GetEnvironmentAsync(pane.UserDataFolder);
            await webView.EnsureCoreWebView2Async(environment);
            ApplyWebViewSettings(webView, pane);
            await ApplyInjectionAsync(webView, pane);
            Navigate(webView, pane);
        }
        catch
        {
            // Keep UI alive even if a pane fails to initialize.
        }
    }

    private void ApplyWebViewSettings(WebView2 webView, DashboardPaneConfig pane)
    {
        webView.ZoomFactor = pane.Zoom <= 0 ? 1.0 : pane.Zoom;

        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var settings = webView.CoreWebView2.Settings;
        var ui = pane.Ui;
        if (ui is not null)
        {
            settings.AreDefaultContextMenusEnabled = !ui.DisableContextMenu;
            if (ui.BlockPopups)
            {
                webView.CoreWebView2.NewWindowRequested += (_, args) => { args.Handled = true; };
            }
        }

        var navigation = pane.Navigation;
        if (navigation is not null && navigation.Hosts.Count > 0 &&
            navigation.Mode.Equals("whitelist", StringComparison.OrdinalIgnoreCase))
        {
            webView.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
                {
                    args.Cancel = true;
                    return;
                }

                var host = uri.Host;
                if (!navigation.Hosts.Any(h => host.Equals(h, StringComparison.OrdinalIgnoreCase)))
                {
                    args.Cancel = true;
                }
            };
        }

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

    private static async Task ApplyInjectionAsync(WebView2 webView, DashboardPaneConfig pane)
    {
        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var css = pane.Injection?.Css;
        if (!string.IsNullOrWhiteSpace(css))
        {
            var script = $"(function(){{const style=document.createElement('style');style.innerHTML={ToJsString(css)};document.head.appendChild(style);}})();";
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }

        var js = pane.Injection?.Js;
        if (!string.IsNullOrWhiteSpace(js))
        {
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js);
        }
    }

    private void Navigate(WebView2 webView, DashboardPaneConfig pane)
    {
        if (string.IsNullOrWhiteSpace(pane.Url))
        {
            return;
        }

        if (!Uri.TryCreate(pane.Url, UriKind.Absolute, out var uri))
        {
            var candidate = $"https://{pane.Url.Trim()}";
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri))
            {
                return;
            }
        }

        webView.Source = uri;
    }

    private void ApplyLayout()
    {
        if (_panes.Count == 0)
        {
            return;
        }

        var gap = Math.Max(0, _config.App.GapPx);
        var rects = DashboardLayoutEngine.Calculate(_config.Layout, _container.Width, _container.Height, gap, _panes.Count);
        for (var i = 0; i < _panes.Count && i < rects.Count; i++)
        {
            var rect = rects[i];
            _panes[i].Container.Bounds = rect;
        }
    }

    private void ApplyWindowMode()
    {
        if (_config.App.Fullscreen)
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

    private static string ToJsString(string value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value);
    }

    private DashboardPaneHost CreatePaneHost(DashboardPaneConfig pane)
    {
        var webView = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = pane.UserDataFolder
            }
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
        closeButton.Click += (_, _) => ClosePane(container, webView);

        container.Controls.Add(closeButton);
        closeButton.BringToFront();

        container.Resize += (_, _) =>
        {
            closeButton.Location = new Point(container.Width - closeButton.Width - 4, 4);
        };

        return new DashboardPaneHost(container, webView);
    }

    private void ClosePane(Panel container, WebView2 webView)
    {
        var host = _panes.FirstOrDefault(p => ReferenceEquals(p.Container, container));
        if (host is null)
        {
            return;
        }

        _panes.Remove(host);
        _container.Controls.Remove(container);
        webView.Dispose();
        container.Dispose();
        ApplyLayout();
    }

    private sealed class DashboardPaneHost
    {
        public DashboardPaneHost(Panel container, WebView2 view)
        {
            Container = container;
            View = view;
        }

        public Panel Container { get; }
        public WebView2 View { get; }
    }
}
