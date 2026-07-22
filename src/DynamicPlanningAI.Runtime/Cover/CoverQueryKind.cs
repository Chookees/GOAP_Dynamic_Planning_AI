namespace DynamicPlanningAI.Runtime.Cover;

/// <summary>
/// Cover query intent used to bias scoring weights.
/// </summary>
public enum CoverQueryKind : byte
{
    /// <summary>
    /// Best defensive cover against a threat.
    /// </summary>
    BestDefensiveCover = 0,

    /// <summary>
    /// Cover that advances toward the threat while remaining protected.
    /// </summary>
    BestAdvancingCover = 1,

    /// <summary>
    /// Cover that increases distance from the threat.
    /// </summary>
    BestRetreatCover = 2,

    /// <summary>
    /// Firing position optimized for suppression.
    /// </summary>
    BestSuppressionPoint = 3,

    /// <summary>
    /// Observation point for search / overwatch.
    /// </summary>
    BestSearchObservationPoint = 4,
}
