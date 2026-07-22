namespace DynamicPlanningAI.Audit;

/// <summary>
/// One audit finding reported for a production source location.
/// </summary>
/// <param name="File">Absolute or repo-relative source file path.</param>
/// <param name="Line">One-based line number.</param>
/// <param name="RuleId">Stable rule identifier (for example POT001).</param>
/// <param name="Severity">Finding severity.</param>
/// <param name="Description">Human-readable description.</param>
public readonly record struct AuditFinding(
    string File,
    int Line,
    string RuleId,
    AuditSeverity Severity,
    string Description);
