using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace FloatingAppBar.Services;

public static class DashboardWebViewFactory
{
    private static readonly ConcurrentDictionary<string, Task<CoreWebView2Environment>> Environments = new(
        StringComparer.OrdinalIgnoreCase);

    public static Task<CoreWebView2Environment> GetEnvironmentAsync(string? userDataFolder)
    {
        var key = string.IsNullOrWhiteSpace(userDataFolder) ? "__shared__" : userDataFolder;
        return Environments.GetOrAdd(key, CreateEnvironmentAsync);
    }

    private static Task<CoreWebView2Environment> CreateEnvironmentAsync(string key)
    {
        if (string.Equals(key, "__shared__", StringComparison.OrdinalIgnoreCase))
        {
            return CoreWebView2Environment.CreateAsync();
        }

        return CoreWebView2Environment.CreateAsync(null, key);
    }
}
