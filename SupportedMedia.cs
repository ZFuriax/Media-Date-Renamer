using System;
using System.Collections.Generic;
using System.IO;

namespace MediaDateRenamer;

public static class SupportedMedia
{
    public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp", ".heic"
    };

    public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".m4v", ".avi", ".mts", ".m2ts", ".3gp", ".wmv", ".mkv"
    };

    public static bool IsImage(string path)
        => ImageExtensions.Contains(Path.GetExtension(path));

    public static bool IsVideo(string path)
        => VideoExtensions.Contains(Path.GetExtension(path));

    public static bool IsSupported(string path)
        => IsImage(path) || IsVideo(path);
}