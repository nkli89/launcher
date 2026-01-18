using System.Collections.Generic;

namespace FloatingAppBar.Models;

public sealed class Manifest
{
    public int ManifestVersion { get; set; } = 1;
    public List<ManifestItem> Items { get; set; } = new();
}
