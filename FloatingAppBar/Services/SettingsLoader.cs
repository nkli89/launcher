using System.IO;
using System.Text.Json;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class SettingsLoader
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public AppSettings Load(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(settingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json, _options) ?? new AppSettings();
    }
}
