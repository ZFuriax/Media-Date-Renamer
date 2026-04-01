using System;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using TagLibFile = TagLib.File;

namespace MediaDateRenamer;

public class MetadataResult
{
    public DateTime? Timestamp { get; set; }
    public string Source { get; set; } = "";
}

public static class MetadataService
{
    public static MetadataResult GetBestTimestamp(string path)
    {
        return SupportedMedia.IsImage(path)
            ? GetBestTimestampForImage(path)
            : GetBestTimestampForVideo(path);
    }

    private static MetadataResult GetBestTimestampForImage(string imagePath)
    {
        var imageTaken = TryGetImageDateTaken(imagePath);
        if (imageTaken.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = imageTaken.Value,
                Source = "Image Date Taken"
            };
        }

        var matchedVideoTaken = TryGetMatchingVideoDateTaken(imagePath);
        if (matchedVideoTaken.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = matchedVideoTaken.Value,
                Source = "Matched Video Date Taken"
            };
        }

        var created = TryGetCreationTime(imagePath);
        if (created.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = created.Value,
                Source = "Image Date Created"
            };
        }

        var modified = TryGetModifiedTime(imagePath);
        if (modified.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = modified.Value,
                Source = "Image Date Modified"
            };
        }

        return new MetadataResult
        {
            Timestamp = null,
            Source = "No usable date"
        };
    }

    private static MetadataResult GetBestTimestampForVideo(string videoPath)
    {
        var taken = TryGetVideoDateTaken(videoPath);
        if (taken.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = taken.Value,
                Source = "Video Date Taken"
            };
        }

        var created = TryGetCreationTime(videoPath);
        if (created.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = created.Value,
                Source = "Video Date Created"
            };
        }

        var modified = TryGetModifiedTime(videoPath);
        if (modified.HasValue)
        {
            return new MetadataResult
            {
                Timestamp = modified.Value,
                Source = "Video Date Modified"
            };
        }

        return new MetadataResult
        {
            Timestamp = null,
            Source = "No usable date"
        };
    }

    private static DateTime? TryGetImageDateTaken(string path)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(path);
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd == null)
                return null;

            if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dtOriginal))
                return Normalize(dtOriginal);

            if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dtDigitized))
                return Normalize(dtDigitized);

            if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dtGeneric))
                return Normalize(dtGeneric);
        }
        catch
        {
        }

        return null;
    }

    private static DateTime? TryGetMatchingVideoDateTaken(string imagePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(imagePath);
            var baseName = Path.GetFileNameWithoutExtension(imagePath);

            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
                return null;

            foreach (var ext in SupportedMedia.VideoExtensions)
            {
                var candidate = Path.Combine(directory, baseName + ext);
                if (!File.Exists(candidate))
                    continue;

                var dt = TryGetVideoDateTaken(candidate);
                if (dt.HasValue)
                    return dt.Value;
            }
        }
        catch
        {
        }

        return null;
    }

    private static DateTime? TryGetVideoDateTaken(string path)
    {
        // Important: this is metadata only.
        // For the matched-video fallback, we must use the video's Date Taken only,
        // never its created/modified filesystem dates.

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(path);
            var qtMovieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();

            if (qtMovieHeader != null &&
                qtMovieHeader.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var qtCreated))
            {
                return Normalize(qtCreated);
            }
        }
        catch
        {
        }

        try
        {
            using var file = TagLibFile.Create(path);
            var dateTagged = file.Tag.DateTagged;

            if (dateTagged.HasValue)
            {
                var value = dateTagged.Value;
                var month = value.Month < 1 ? 1 : value.Month;
                var day = value.Day < 1 ? 1 : value.Day;

                var dt = new DateTime(value.Year, month, day, 0, 0, 0, DateTimeKind.Local);
                return Normalize(dt);
            }
        }
        catch
        {
        }

        return null;
    }

    private static DateTime? TryGetCreationTime(string path)
    {
        try
        {
            return Normalize(File.GetCreationTime(path));
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? TryGetModifiedTime(string path)
    {
        try
        {
            return Normalize(File.GetLastWriteTime(path));
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? Normalize(DateTime? dt)
    {
        if (!dt.HasValue)
            return null;

        if (dt.Value.Year < 1900)
            return null;

        return dt.Value;
    }
}