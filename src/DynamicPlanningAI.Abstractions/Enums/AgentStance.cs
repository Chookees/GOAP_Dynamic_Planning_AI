namespace DynamicPlanningAI.Abstractions.Enums;

/// <summary>
/// Agent posture reported by the host body adapter.
/// </summary>
public enum AgentStance : byte
{
    /// <summary>
    /// Standing upright posture.
    /// </summary>
    Standing = 0,

    /// <summary>
    /// Crouched posture.
    /// </summary>
    Crouching = 1,

    /// <summary>
    /// Prone posture.
    /// </summary>
    Prone = 2,

    /// <summary>
    /// In cover lean or similar defensive posture.
    /// </summary>
    InCover = 3,
}
