using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;

namespace FloatingAppBar.Services;

public static class IconLoader
{
    public static IImage? Load(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        var fullPath = Path.IsPathRooted(iconPath)
            ? iconPath
            : Path.Combine(AppContext.BaseDirectory, iconPath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        return extension switch
        {
            ".svg" => TryLoadSvg(fullPath),
            _ => new Bitmap(fullPath)
        };
    }

    private static IImage? TryLoadSvg(string fullPath)
    {
        try
        {
            return new SvgImage
            {
                Source = SvgSource.Load(fullPath)
            };
        }
        catch
        {
            return null;
        }
    }
}
