using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaDateRenamer;

public static class RenameService
{
    public static string BuildBaseFileName(DateTime timestamp)
        => $"{timestamp:yyyy-MM-dd-HH'h'-mm'm'-ss's'}";

    public static string BuildUniqueFileName(
        string originalPath,
        DateTime timestamp,
        IReadOnlyCollection<string> reservedNames)
    {
        var ext = Path.GetExtension(originalPath);
        var dir = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var baseName = BuildBaseFileName(timestamp);

        var candidate = baseName + ext;
        var fullCandidate = Path.Combine(dir, candidate);

        if (!Conflicts(fullCandidate, originalPath, reservedNames))
            return candidate;

        for (int i = 1; i < 10000; i++)
        {
            candidate = $"{baseName}-{i:00}{ext}";
            fullCandidate = Path.Combine(dir, candidate);

            if (!Conflicts(fullCandidate, originalPath, reservedNames))
                return candidate;
        }

        throw new InvalidOperationException($"Unable to generate unique filename for {originalPath}");
    }

    private static bool Conflicts(string candidateFullPath, string originalPath, IReadOnlyCollection<string> reservedNames)
    {
        if (string.Equals(candidateFullPath, originalPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (File.Exists(candidateFullPath))
            return true;

        return reservedNames.Any(x => string.Equals(x, candidateFullPath, StringComparison.OrdinalIgnoreCase));
    }
}