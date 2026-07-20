using System;
using System.IO;
using TacticalGoap.Configuration;
using TacticalGoap.Configuration.Json;
using TacticalGoap.Configuration.Validation;
using TacticalGoap.Sample.Simulation;
using Xunit;

namespace TacticalGoap.IntegrationTests;

public sealed class BlockedDoorReplanningTests
{
    [Fact]
    public void BlockedDoorReplanning_CompletesSection46Flow_REQ_SAMPLE_001()
    {
        ScenarioRunner runner = new(ConfigurationBundle.CreateDefault());
        ScenarioResult result = runner.Run("BlockedDoorReplanning", seedOverride: 1046, traceDirectory: Path.Combine(Path.GetTempPath(), "tg-int"));
        Assert.True(result.Success, result.Summary);
        Assert.Contains("§46", result.Summary, StringComparison.Ordinal);
        Assert.True(result.TicksExecuted <= result.MaxTicks);
    }
}

public sealed class OrderOverrideTests
{
    [Fact]
    public void OrderOverriddenByDanger_GrenadeOverridesAdvance_REQ_SAMPLE_001()
    {
        ScenarioRunner runner = new(ConfigurationBundle.CreateDefault());
        ScenarioResult result = runner.Run("OrderOverriddenByDanger", seedOverride: 1047, traceDirectory: Path.Combine(Path.GetTempPath(), "tg-int"));
        Assert.True(result.Success, result.Summary);
        Assert.Contains("§47", result.Summary, StringComparison.Ordinal);
    }
}

public sealed class DeterministicReplayTests
{
    [Fact]
    public void BasicAttack_TwoRuns_IdenticalDigestAndGoalActionLog_REQ_DET_001()
    {
        ScenarioRunner runner = new(ConfigurationBundle.CreateDefault());
        string dir = Path.Combine(Path.GetTempPath(), "tg-replay");
        ScenarioResult a = runner.Run("BasicAttack", seedOverride: 1001, traceDirectory: dir);
        ScenarioResult b = runner.Run("BasicAttack", seedOverride: 1001, traceDirectory: dir);
        Assert.True(a.Success);
        Assert.True(b.Success);
        Assert.Equal(a.Digest, b.Digest);
        Assert.Equal(a.GoalActionLog.Count, b.GoalActionLog.Count);
        for (int i = 0; i < a.GoalActionLog.Count; i++)
        {
            Assert.Equal(a.GoalActionLog[i], b.GoalActionLog[i]);
        }
    }
}

public sealed class ZeroAllocationTests
{
    [Fact]
    public void ZeroAllocationSteadyState_MeasurableHarness_REQ_ALLOC_001()
    {
        ScenarioRunner runner = new(ConfigurationBundle.CreateDefault());
        ScenarioResult result = runner.Run(
            "ZeroAllocationSteadyState",
            seedOverride: 1012,
            traceDirectory: Path.Combine(Path.GetTempPath(), "tg-int"));
        Assert.True(result.Success, result.Summary);
    }
}

public sealed class ConfigurationLoadTests
{
    [Fact]
    public void JsonLoader_LoadsSampleDefaults_AndRejectsInvalid()
    {
        ConfigurationBundle defaults = ConfigurationBundle.CreateDefault();
        Assert.True(defaults.IsFrozen);
        Assert.Equal(8, defaults.Runtime.MaxAgents);

        string json = """
            {
              "runtime": { "maxAgents": 4, "maxSquads": 2, "tickDeltaMilliseconds": 16 },
              "squad": { "maxSquads": 2, "maxAgentsPerSquad": 4, "maxOrders": 16, "maxSearchSectors": 8, "reclusterIntervalTicks": 30, "defaultOrderDurationTicks": 45 },
              "sample": { "defaultMaxTicks": 100, "defaultSeed": 7, "defaultScenario": "TakeCover", "traceDirectory": "t" }
            }
            """;
        ConfigurationBundle loaded = JsonConfigurationLoader.LoadFromJson(json);
        Assert.Equal(4, loaded.Runtime.MaxAgents);
        Assert.Equal("TakeCover", loaded.Sample.DefaultScenario);

        Assert.Throws<ConfigValidationException>(() =>
            JsonConfigurationLoader.LoadFromJson("""{"runtime":{"maxAgents":0,"maxSquads":1,"tickDeltaMilliseconds":16}}"""));
    }
}
