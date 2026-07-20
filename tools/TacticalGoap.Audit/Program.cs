using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalGoap.Audit;

/// <summary>
/// Entry point for the TacticalGoap Power-of-Ten compliance audit CLI.
/// </summary>
public static class Program
{
    /// <summary>
    /// Audits production sources under <c>src/</c> and returns a non-zero exit code on errors.
    /// </summary>
    /// <param name="args">
    /// Optional repository root path. When omitted, walks upward from the current directory
    /// looking for <c>TacticalGoap.sln</c>, otherwise uses the current directory.
    /// </param>
    /// <returns>0 when no Error findings; 1 when one or more Error findings exist; 2 on usage failures.</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: TacticalGoap.Audit [repoRoot]");
            return 2;
        }

        string? optionalPath = args.Length == 1 ? args[0] : null;
        string repoRoot = RepoLocator.ResolveRoot(optionalPath);
        Console.WriteLine(
            string.Create(CultureInfo.InvariantCulture, $"Auditing production sources under: {repoRoot}"));

        SourceAuditor auditor = new SourceAuditor(repoRoot);
        IReadOnlyList<AuditFinding> findings = auditor.Run();
        WriteFindings(findings);
        WriteSummary(findings);
        return CountBySeverity(findings, AuditSeverity.Error) > 0 ? 1 : 0;
    }

    private static void WriteFindings(IReadOnlyList<AuditFinding> findings)
    {
        for (int i = 0; i < findings.Count; i++)
        {
            AuditFinding finding = findings[i];
            Console.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{finding.File}({finding.Line}): {finding.Severity} {finding.RuleId}: {finding.Description}"));
        }
    }

    private static void WriteSummary(IReadOnlyList<AuditFinding> findings)
    {
        int errors = CountBySeverity(findings, AuditSeverity.Error);
        int warnings = CountBySeverity(findings, AuditSeverity.Warning);
        int infos = CountBySeverity(findings, AuditSeverity.Info);

        Console.WriteLine();
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Summary: {findings.Count} finding(s) ({errors} error(s), {warnings} warning(s), {infos} info)."));
        Console.WriteLine("Limitations: syntax-only Roslyn walk; see SourceAuditor remarks and docs/POWER_OF_TEN_COMPLIANCE.md.");
        Console.WriteLine("False positives: POT001/007/008/010/011 heuristics; POT013 value-type name guesses.");
        Console.WriteLine("False negatives: indirect recursion, semantic aliases, cross-file partial method length.");
    }

    private static int CountBySeverity(IReadOnlyList<AuditFinding> findings, AuditSeverity severity)
    {
        int count = 0;
        for (int i = 0; i < findings.Count; i++)
        {
            if (findings[i].Severity == severity)
            {
                count++;
            }
        }

        return count;
    }
}
