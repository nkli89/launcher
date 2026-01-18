using System.IO;
using System.Text.Json;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class ActionManifestLoader
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public ActionManifest Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new ActionManifest();
        }

        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<ActionManifest>(json, _options) ?? new ActionManifest();
    }
}
