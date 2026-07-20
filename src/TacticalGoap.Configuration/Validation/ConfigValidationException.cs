using System;
using System.Diagnostics.CodeAnalysis;

namespace TacticalGoap.Configuration.Validation;

/// <summary>
/// Thrown when a configuration group fails validation against hard limits.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Domain exception always carries group metadata; standard ctors forward to defaults.")]
public sealed class ConfigValidationException : Exception
{
    /// <summary>
    /// Initializes a new validation exception with default metadata.
    /// </summary>
    public ConfigValidationException()
        : this("Configuration", "Configuration validation failed.", 0)
    {
    }

    /// <summary>
    /// Initializes a new validation exception with a message.
    /// </summary>
    /// <param name="message">Human-readable failure detail.</param>
    public ConfigValidationException(string message)
        : this("Configuration", message, 0)
    {
    }

    /// <summary>
    /// Initializes a new validation exception with a message and inner exception.
    /// </summary>
    /// <param name="message">Human-readable failure detail.</param>
    /// <param name="innerException">Inner exception.</param>
    public ConfigValidationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        GroupName = "Configuration";
        ReasonCode = 0;
        ContextId = 0;
    }

    /// <summary>
    /// Initializes a new validation exception.
    /// </summary>
    /// <param name="groupName">Configuration group name.</param>
    /// <param name="message">Human-readable failure detail.</param>
    /// <param name="reasonCode">Stable reason code.</param>
    /// <param name="contextId">Optional failing element id.</param>
    public ConfigValidationException(string groupName, string message, int reasonCode, int contextId = 0)
        : base(message)
    {
        GroupName = groupName;
        ReasonCode = reasonCode;
        ContextId = contextId;
    }

    /// <summary>Gets the configuration group name.</summary>
    public string GroupName { get; }

    /// <summary>Gets the stable reason code.</summary>
    public int ReasonCode { get; }

    /// <summary>Gets the optional failing element identifier.</summary>
    public int ContextId { get; }
}
