using System.IO;
using System.Text.Json;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class ManifestLoader
{
    private readonly JsonSerializerOptions _options;

    public ManifestLoader()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public Manifest Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new Manifest();
        }

        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<Manifest>(json, _options) ?? new Manifest();
    }
}
