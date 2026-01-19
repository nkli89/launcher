using System;
using System.Collections.Generic;
using System.Drawing;
using FloatingAppBar.Models;

namespace FloatingAppBar.Services;

public static class DashboardLayoutEngine
{
    public static IReadOnlyList<Rectangle> Calculate(DashboardLayoutConfig layout, int width, int height, int gapPx, int paneCount)
    {
        var layoutId = (layout.Id ?? string.Empty).Trim().ToUpperInvariant();
        var orientation = (layout.Orientation ?? string.Empty).Trim().ToUpperInvariant();
        var gap = Math.Max(0, gapPx);

        if (width <= 0 || height <= 0 || paneCount <= 0)
        {
            return Array.Empty<Rectangle>();
        }

        return layoutId switch
        {
            "HALF" => BuildHalf(width, height, gap, paneCount, orientation),
            "MIX_3" => BuildMixThree(width, height, gap, paneCount),
            _ => BuildQuad(width, height, gap, paneCount)
        };
    }

    private static IReadOnlyList<Rectangle> BuildQuad(int width, int height, int gap, int paneCount)
    {
        var rects = new List<Rectangle>();
        var insetWidth = Math.Max(0, width - gap * 2);
        var insetHeight = Math.Max(0, height - gap * 2);
        var cellWidth = Math.Max(1, (insetWidth - gap) / 2);
        var cellHeight = Math.Max(1, (insetHeight - gap) / 2);
        var x0 = gap;
        var y0 = gap;

        rects.Add(new Rectangle(x0, y0, cellWidth, cellHeight));
        rects.Add(new Rectangle(x0 + cellWidth + gap, y0, cellWidth, cellHeight));
        rects.Add(new Rectangle(x0, y0 + cellHeight + gap, cellWidth, cellHeight));
        rects.Add(new Rectangle(x0 + cellWidth + gap, y0 + cellHeight + gap, cellWidth, cellHeight));

        return Trim(rects, paneCount);
    }

    private static IReadOnlyList<Rectangle> BuildHalf(int width, int height, int gap, int paneCount, string orientation)
    {
        var rects = new List<Rectangle>();
        var insetWidth = Math.Max(0, width - gap * 2);
        var insetHeight = Math.Max(0, height - gap * 2);
        var x0 = gap;
        var y0 = gap;

        if (string.Equals(orientation, "HORIZONTAL", StringComparison.OrdinalIgnoreCase))
        {
            var paneHeight = Math.Max(1, (insetHeight - gap) / 2);
            rects.Add(new Rectangle(x0, y0, insetWidth, paneHeight));
            rects.Add(new Rectangle(x0, y0 + paneHeight + gap, insetWidth, paneHeight));
        }
        else
        {
            var paneWidth = Math.Max(1, (insetWidth - gap) / 2);
            rects.Add(new Rectangle(x0, y0, paneWidth, insetHeight));
            rects.Add(new Rectangle(x0 + paneWidth + gap, y0, paneWidth, insetHeight));
        }

        return Trim(rects, paneCount);
    }

    private static IReadOnlyList<Rectangle> BuildMixThree(int width, int height, int gap, int paneCount)
    {
        var rects = new List<Rectangle>();
        var insetWidth = Math.Max(0, width - gap * 2);
        var insetHeight = Math.Max(0, height - gap * 2);
        var x0 = gap;
        var y0 = gap;
        var leftWidth = Math.Max(1, (insetWidth - gap) / 2);
        var rightWidth = Math.Max(1, insetWidth - leftWidth - gap);
        var rightHeight = Math.Max(1, (insetHeight - gap) / 2);

        rects.Add(new Rectangle(x0, y0, leftWidth, insetHeight));
        rects.Add(new Rectangle(x0 + leftWidth + gap, y0, rightWidth, rightHeight));
        rects.Add(new Rectangle(x0 + leftWidth + gap, y0 + rightHeight + gap, rightWidth, rightHeight));

        return Trim(rects, paneCount);
    }

    private static IReadOnlyList<Rectangle> Trim(List<Rectangle> rects, int paneCount)
    {
        if (rects.Count <= paneCount)
        {
            return rects;
        }

        return rects.GetRange(0, paneCount);
    }
}
