namespace DynamicPlanningAI.Audit;

/// <summary>
/// Severity of an audit finding.
/// </summary>
public enum AuditSeverity
{
    /// <summary>
    /// Informational note; does not fail the process.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Heuristic or style concern; does not fail the process.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Clear Power-of-Ten or allocation-path violation; fails the process.
    /// </summary>
    Error = 2,
}
