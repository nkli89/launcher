using System.IO;
using System.Text.Json;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class DashboardConfigLoader
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public DashboardConfig Load(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return new DashboardConfig();
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<DashboardConfig>(json, _options) ?? new DashboardConfig();
    }
}
