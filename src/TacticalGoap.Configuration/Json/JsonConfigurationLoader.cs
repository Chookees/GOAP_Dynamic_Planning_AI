using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using TacticalGoap.Configuration.Groups;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Json;

/// <summary>
/// Loads a <see cref="ConfigurationBundle"/> from JSON during initialization only.
/// </summary>
/// <remarks>
/// After freeze, hosts must not call this loader again on the combat path.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "DTO types are instantiated by System.Text.Json deserialization.")]
public static class JsonConfigurationLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads and validates a configuration bundle from a JSON file path.
    /// </summary>
    /// <param name="path">Absolute or relative JSON path.</param>
    /// <returns>Validated immutable bundle.</returns>
    public static ConfigurationBundle LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Loads and validates a configuration bundle from a JSON string.
    /// </summary>
    /// <param name="json">JSON document text.</param>
    /// <returns>Validated immutable bundle.</returns>
    public static ConfigurationBundle LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        JsonConfigDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<JsonConfigDocument>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new ConfigValidationException(
                nameof(JsonConfigurationLoader),
                "JSON parse failed: " + ex.Message,
                4001);
        }

        if (document is null)
        {
            throw new ConfigValidationException(
                nameof(JsonConfigurationLoader),
                "JSON document deserialized to null.",
                4002);
        }

        return Map(document);
    }

    private static ConfigurationBundle Map(JsonConfigDocument document)
    {
        RuntimeConfiguration runtime = RuntimeConfiguration.Create(
            document.Runtime?.MaxAgents ?? 8,
            document.Runtime?.MaxSquads ?? 4,
            document.Runtime?.TickDeltaMilliseconds ?? 16);

        PlannerConfiguration planner = PlannerConfiguration.Create(
            document.Planner?.MaxNodes ?? 512,
            document.Planner?.MaxExpansionsPerStep ?? 64,
            document.Planner?.MaxStepsPerTick ?? 8,
            document.Planner?.MaxPlanLength ?? 16,
            document.Planner?.MaxReplanningAttempts ?? 8);

        PerceptionConfiguration perception = PerceptionConfiguration.Create(
            document.Perception?.MaxCandidates ?? 32,
            document.Perception?.MaxSoundEvents ?? 32,
            document.Perception?.MaxDamageEvents ?? 16,
            document.Perception?.VisionRangeCells ?? 12);

        MemoryConfiguration memory = MemoryConfiguration.Create(
            document.Memory?.MaxRecords ?? 64,
            document.Memory?.DefaultConfidence ?? 800);

        GoalConfiguration goals = GoalConfiguration.Create(
            document.Goals?.MaxGoalsPerAgent ?? 16,
            document.Goals?.HysteresisMargin ?? 10,
            document.Goals?.DangerPriorityFloor ?? 9000);

        ActionConfiguration actions = ActionConfiguration.Create(
            document.Actions?.MaxDefinitions ?? 128,
            document.Actions?.MaxCandidates ?? 256,
            document.Actions?.MaxActionTicks ?? 1024);

        CoverConfiguration cover = CoverConfiguration.Create(
            document.Cover?.MaxCandidates ?? 32,
            document.Cover?.MaxTacticalPoints ?? 256,
            document.Cover?.FlankAngleWeight ?? 40,
            document.Cover?.PathCostWeight ?? 2,
            document.Cover?.QualityWeight ?? 10);

        NavigationConfiguration navigation = NavigationConfiguration.Create(
            document.Navigation?.MaxPathNodes ?? 128,
            document.Navigation?.MaxExpansions ?? 512,
            document.Navigation?.GridWidth ?? 24,
            document.Navigation?.GridHeight ?? 16);

        SquadConfiguration squad = SquadConfiguration.Create(
            document.Squad?.MaxSquads ?? 16,
            document.Squad?.MaxAgentsPerSquad ?? 8,
            document.Squad?.MaxOrders ?? 64,
            document.Squad?.MaxSearchSectors ?? 32,
            document.Squad?.ReclusterIntervalTicks ?? 30,
            document.Squad?.DefaultOrderDurationTicks ?? 45);

        CommunicationConfiguration communication = CommunicationConfiguration.Create(
            document.Communication?.MaxPendingRequests ?? 32);

        DiagnosticsConfiguration diagnostics = DiagnosticsConfiguration.Create(
            document.Diagnostics?.RingBufferCapacity ?? 1024,
            document.Diagnostics?.Enabled ?? true);

        SampleConfiguration sample = SampleConfiguration.Create(
            document.Sample?.DefaultMaxTicks ?? 256,
            document.Sample?.DefaultSeed ?? 42,
            document.Sample?.DefaultScenario ?? "BasicAttack",
            document.Sample?.TraceDirectory ?? "traces");

        return ConfigurationBundle.Create(
            runtime,
            planner,
            perception,
            memory,
            goals,
            actions,
            cover,
            navigation,
            squad,
            communication,
            diagnostics,
            sample);
    }

    private sealed class JsonConfigDocument
    {
        public RuntimeDto? Runtime { get; set; }
        public PlannerDto? Planner { get; set; }
        public PerceptionDto? Perception { get; set; }
        public MemoryDto? Memory { get; set; }
        public GoalsDto? Goals { get; set; }
        public ActionsDto? Actions { get; set; }
        public CoverDto? Cover { get; set; }
        public NavigationDto? Navigation { get; set; }
        public SquadDto? Squad { get; set; }
        public CommunicationDto? Communication { get; set; }
        public DiagnosticsDto? Diagnostics { get; set; }
        public SampleDto? Sample { get; set; }
    }

    private sealed class RuntimeDto
    {
        public int? MaxAgents { get; set; }
        public int? MaxSquads { get; set; }
        public int? TickDeltaMilliseconds { get; set; }
    }

    private sealed class PlannerDto
    {
        public int? MaxNodes { get; set; }
        public int? MaxExpansionsPerStep { get; set; }
        public int? MaxStepsPerTick { get; set; }
        public int? MaxPlanLength { get; set; }
        public int? MaxReplanningAttempts { get; set; }
    }

    private sealed class PerceptionDto
    {
        public int? MaxCandidates { get; set; }
        public int? MaxSoundEvents { get; set; }
        public int? MaxDamageEvents { get; set; }
        public int? VisionRangeCells { get; set; }
    }

    private sealed class MemoryDto
    {
        public int? MaxRecords { get; set; }
        public int? DefaultConfidence { get; set; }
    }

    private sealed class GoalsDto
    {
        public int? MaxGoalsPerAgent { get; set; }
        public int? HysteresisMargin { get; set; }
        public int? DangerPriorityFloor { get; set; }
    }

    private sealed class ActionsDto
    {
        public int? MaxDefinitions { get; set; }
        public int? MaxCandidates { get; set; }
        public int? MaxActionTicks { get; set; }
    }

    private sealed class CoverDto
    {
        public int? MaxCandidates { get; set; }
        public int? MaxTacticalPoints { get; set; }
        public int? FlankAngleWeight { get; set; }
        public int? PathCostWeight { get; set; }
        public int? QualityWeight { get; set; }
    }

    private sealed class NavigationDto
    {
        public int? MaxPathNodes { get; set; }
        public int? MaxExpansions { get; set; }
        public int? GridWidth { get; set; }
        public int? GridHeight { get; set; }
    }

    private sealed class SquadDto
    {
        public int? MaxSquads { get; set; }
        public int? MaxAgentsPerSquad { get; set; }
        public int? MaxOrders { get; set; }
        public int? MaxSearchSectors { get; set; }
        public int? ReclusterIntervalTicks { get; set; }
        public int? DefaultOrderDurationTicks { get; set; }
    }

    private sealed class CommunicationDto
    {
        public int? MaxPendingRequests { get; set; }
    }

    private sealed class DiagnosticsDto
    {
        public int? RingBufferCapacity { get; set; }
        public bool? Enabled { get; set; }
    }

    private sealed class SampleDto
    {
        public int? DefaultMaxTicks { get; set; }
        public int? DefaultSeed { get; set; }
        public string? DefaultScenario { get; set; }
        public string? TraceDirectory { get; set; }
    }
}
