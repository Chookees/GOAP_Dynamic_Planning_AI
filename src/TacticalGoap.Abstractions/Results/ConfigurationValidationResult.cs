namespace TacticalGoap.Abstractions.Results;

/// <summary>
/// Explicit configuration validation outcome used before freeze.
/// </summary>
/// <remarks>
/// Validation runs only during configuration and initialization. Reason codes are
/// stable integers understood by diagnostic tooling; this type never allocates
/// managed strings on the hot path.
/// </remarks>
public readonly struct ConfigurationValidationResult
{
    /// <summary>
    /// Initializes a new configuration validation result.
    /// </summary>
    /// <param name="isSuccess">Whether validation succeeded.</param>
    /// <param name="reasonCode">Stable failure reason code; zero on success.</param>
    /// <param name="contextId">Optional identifier locating the failing element.</param>
    public ConfigurationValidationResult(bool isSuccess, int reasonCode, int contextId)
    {
        IsSuccess = isSuccess;
        ReasonCode = reasonCode;
        ContextId = contextId;
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the stable failure reason code; zero on success.
    /// </summary>
    public int ReasonCode { get; }

    /// <summary>
    /// Gets an optional identifier locating the failing configuration element.
    /// </summary>
    public int ContextId { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A success result with zero reason and context.</returns>
    public static ConfigurationValidationResult Success()
    {
        return new ConfigurationValidationResult(true, 0, 0);
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="reasonCode">Stable failure reason code.</param>
    /// <param name="contextId">Optional failing element identifier.</param>
    /// <returns>A failure result.</returns>
    public static ConfigurationValidationResult Failure(int reasonCode, int contextId = 0)
    {
        return new ConfigurationValidationResult(false, reasonCode, contextId);
    }
}
