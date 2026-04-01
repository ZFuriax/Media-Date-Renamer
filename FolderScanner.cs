using System;
using System.Collections.Generic;
using System.IO;

namespace MediaDateRenamer;

public static class FolderScanner
{
    public static IEnumerable<string> ExpandPaths(IEnumerable<string> inputPaths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputPaths)
        {
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (File.Exists(input))
            {
                var full = Path.GetFullPath(input);
                if (SupportedMedia.IsSupported(full) && seen.Add(full))
                    yield return full;

                continue;
            }

            if (Directory.Exists(input))
            {
                IEnumerable<string> files;

                try
                {
                    files = Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    var full = Path.GetFullPath(file);
                    if (SupportedMedia.IsSupported(full) && seen.Add(full))
                        yield return full;
                }
            }
        }
    }
}