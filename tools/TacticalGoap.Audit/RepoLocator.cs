using System;
using System.Collections.Generic;
using System.IO;

namespace TacticalGoap.Audit;

/// <summary>
/// Locates the repository root and enumerates production C# sources under <c>src/</c>.
/// </summary>
internal static class RepoLocator
{
    private const string SolutionFileName = "TacticalGoap.sln";

    /// <summary>
    /// Resolves the repository root from an optional path argument or the current directory.
    /// </summary>
    /// <param name="optionalPath">Optional path provided on the command line.</param>
    /// <returns>Absolute repository root containing the solution file when found.</returns>
    public static string ResolveRoot(string? optionalPath)
    {
        string start = string.IsNullOrWhiteSpace(optionalPath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(optionalPath);

        if (File.Exists(start) && start.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(start) ?? start;
        }

        string? cursor = Directory.Exists(start) ? start : Path.GetDirectoryName(start);
        while (!string.IsNullOrEmpty(cursor))
        {
            string candidate = Path.Combine(cursor, SolutionFileName);
            if (File.Exists(candidate))
            {
                return cursor;
            }

            DirectoryInfo? parent = Directory.GetParent(cursor);
            cursor = parent?.FullName;
        }

        return Directory.Exists(start) ? start : Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Enumerates production <c>.cs</c> files under <c>src/</c>, excluding build outputs.
    /// </summary>
    /// <param name="repoRoot">Repository root.</param>
    /// <returns>Absolute source file paths.</returns>
    public static IReadOnlyList<string> EnumerateProductionSources(string repoRoot)
    {
        string srcRoot = Path.Combine(repoRoot, "src");
        List<string> files = new List<string>();
        if (!Directory.Exists(srcRoot))
        {
            return files;
        }

        foreach (string path in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(path))
            {
                continue;
            }

            files.Add(path);
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static bool IsExcluded(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Generated/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a path relative to the repository root when possible.
    /// </summary>
    /// <param name="repoRoot">Repository root.</param>
    /// <param name="absolutePath">Absolute file path.</param>
    /// <returns>Relative path using forward slashes.</returns>
    public static string ToRelative(string repoRoot, string absolutePath)
    {
        string relative = Path.GetRelativePath(repoRoot, absolutePath);
        return relative.Replace('\\', '/');
    }

    /// <summary>
    /// Determines whether a relative source path belongs to the Runtime project tree.
    /// </summary>
    /// <param name="relativePath">Repo-relative path using forward slashes.</param>
    /// <returns><see langword="true"/> when the path is under Runtime.</returns>
    public static bool IsRuntimePath(string relativePath)
    {
        return relativePath.StartsWith("src/TacticalGoap.Runtime/", StringComparison.OrdinalIgnoreCase);
    }
}
