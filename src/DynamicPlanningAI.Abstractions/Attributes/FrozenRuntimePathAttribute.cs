using System;

namespace DynamicPlanningAI.Abstractions.Attributes;

/// <summary>
/// Marks a method or type as belonging to the frozen runtime path.
/// </summary>
/// <remarks>
/// After <c>AiRuntime.Freeze()</c> succeeds, code on the frozen runtime path
/// must not deliberately allocate managed objects. The audit tool applies
/// stricter Power-of-Ten checks to members annotated with this attribute.
/// Initialization, configuration loading, and on-demand diagnostic formatting
/// must not be marked with this attribute.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = true)]
public sealed class FrozenRuntimePathAttribute : Attribute
{
}
