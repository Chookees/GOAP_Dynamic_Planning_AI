using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace DynamicPlanningAI.ArchitectureTests;

/// <summary>
/// Enforces solution layering and Power-of-Ten spot checks.
/// </summary>
public sealed class LayeringAndComplianceTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    /// <summary>
    /// Runtime must not reference the Sample project.
    /// </summary>
    [Fact]
    public void Runtime_DoesNotReference_Sample()
    {
        string csproj = Path.Combine(RepoRoot, "src", "DynamicPlanningAI.Runtime", "DynamicPlanningAI.Runtime.csproj");
        string text = File.ReadAllText(csproj);
        Assert.DoesNotContain("DynamicPlanningAI.Sample", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Abstractions must not reference Runtime.
    /// </summary>
    [Fact]
    public void Abstractions_DoesNotReference_Runtime()
    {
        string csproj = Path.Combine(RepoRoot, "src", "DynamicPlanningAI.Abstractions", "DynamicPlanningAI.Abstractions.csproj");
        string text = File.ReadAllText(csproj);
        Assert.DoesNotContain("DynamicPlanningAI.Runtime", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Diagnostics may reference Abstractions only (not Runtime or Sample).
    /// </summary>
    [Fact]
    public void Diagnostics_DoesNotReference_RuntimeOrSample()
    {
        string csproj = Path.Combine(RepoRoot, "src", "DynamicPlanningAI.Diagnostics", "DynamicPlanningAI.Diagnostics.csproj");
        string text = File.ReadAllText(csproj);
        Assert.DoesNotContain("DynamicPlanningAI.Runtime", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DynamicPlanningAI.Sample", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DynamicPlanningAI.Abstractions", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Production projects must keep AllowUnsafeBlocks disabled.
    /// </summary>
    [Fact]
    public void ProductionProjects_DisallowUnsafe()
    {
        string directoryBuild = Path.Combine(RepoRoot, "Directory.Build.props");
        string text = File.ReadAllText(directoryBuild);
        Assert.Contains("<AllowUnsafeBlocks>false</AllowUnsafeBlocks>", text, StringComparison.Ordinal);

        string srcRoot = Path.Combine(RepoRoot, "src");
        foreach (string csproj in Directory.EnumerateFiles(srcRoot, "*.csproj", SearchOption.AllDirectories))
        {
            string projectText = File.ReadAllText(csproj);
            Assert.DoesNotContain("<AllowUnsafeBlocks>true</AllowUnsafeBlocks>", projectText, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Spot-check: Runtime sources must not import System.Linq.
    /// </summary>
    [Fact]
    public void RuntimeSources_DoNotImport_SystemLinq()
    {
        string runtimeRoot = Path.Combine(RepoRoot, "src", "DynamicPlanningAI.Runtime");
        foreach (string file in Directory.EnumerateFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            Assert.False(
                text.Contains("using System.Linq;", StringComparison.Ordinal),
                $"System.Linq import found in {file}");
        }
    }

    /// <summary>
    /// Spot-check: Runtime sources must not contain the unsafe keyword as a language construct.
    /// </summary>
    [Fact]
    public void RuntimeSources_DoNotContainUnsafeKeyword()
    {
        string runtimeRoot = Path.Combine(RepoRoot, "src", "DynamicPlanningAI.Runtime");
        foreach (string file in Directory.EnumerateFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            Assert.DoesNotContain(" unsafe ", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\tunsafe ", text, StringComparison.Ordinal);
            Assert.False(text.StartsWith("unsafe ", StringComparison.Ordinal), file);
        }
    }

    /// <summary>
    /// Loaded Runtime assembly should resolve for architecture smoke coverage.
    /// </summary>
    [Fact]
    public void RuntimeAssembly_Loads()
    {
        Assembly runtime = typeof(DynamicPlanningAI.Runtime.Planning.GoapPlanner).Assembly;
        Assert.Contains("DynamicPlanningAI.Runtime", runtime.GetName().Name, StringComparison.Ordinal);
    }

    private static bool IsBuildArtifact(string file)
    {
        string normalized = file.Replace('\\', '/');
        return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    private static string LocateRepoRoot()
    {
        string? cursor = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(cursor))
        {
            if (File.Exists(Path.Combine(cursor, "DynamicPlanningAI.sln")))
            {
                return cursor;
            }

            cursor = Directory.GetParent(cursor)?.FullName;
        }

        cursor = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (File.Exists(Path.Combine(cursor, "DynamicPlanningAI.sln")))
        {
            return cursor;
        }

        throw new InvalidOperationException("Unable to locate DynamicPlanningAI.sln from test base directory.");
    }
}
