using System;
using System.Collections.Generic;
using System.IO;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public sealed class ManifestDirectoryLoader
{
    private readonly ManifestLoader _manifestLoader;

    public ManifestDirectoryLoader(ManifestLoader manifestLoader)
    {
        _manifestLoader = manifestLoader;
    }

    public List<ManifestEntry> LoadAll(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new List<ManifestEntry>();
        }

        var manifests = new List<ManifestEntry>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                manifests.Add(new ManifestEntry(file, _manifestLoader.Load(file)));
            }
            catch
            {
                // Skip invalid manifests.
            }
        }

        return manifests;
    }
}

public sealed record ManifestEntry(string Path, Manifest Manifest);
