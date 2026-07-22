using System;
using System.Globalization;
using System.IO;
using DynamicPlanningAI.Configuration;
using DynamicPlanningAI.Configuration.Json;
using DynamicPlanningAI.Configuration.Validation;
using DynamicPlanningAI.Sample.Simulation;

namespace DynamicPlanningAI.Sample;

/// <summary>
/// Deterministic console entry point for DynamicPlanningAI sample scenarios.
/// </summary>
public static class Program
{
    /// <summary>
    /// Parses CLI arguments and runs a named scenario.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Zero on success; non-zero on failure.</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        try
        {
            return Run(args);
        }
        catch (ConfigValidationException ex)
        {
            Console.Error.WriteLine("CONFIG: " + ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine("ARGS: " + ex.Message);
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine("FATAL: " + ex.Message);
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine("IO: " + ex.Message);
            return 2;
        }
    }

    private static int Run(string[] args)
    {
        ConfigurationBundle config = LoadConfiguration();
        string scenario = config.Sample.DefaultScenario;
        int? seed = null;
        string? traceDir = null;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--scenario" && i + 1 < args.Length)
            {
                scenario = args[++i];
                continue;
            }

            if (arg == "--seed" && i + 1 < args.Length)
            {
                if (!int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    Console.Error.WriteLine("Invalid --seed value.");
                    return 2;
                }

                seed = parsed;
                continue;
            }

            if (arg == "--trace-dir" && i + 1 < args.Length)
            {
                traceDir = args[++i];
                continue;
            }

            if (arg == "--help" || arg == "-h")
            {
                PrintHelp();
                return 0;
            }
        }

        ScenarioRunner runner = new(config);
        ScenarioResult result = runner.Run(scenario, seed, traceDir);
        Console.WriteLine(
            string.Concat(
                result.ScenarioName,
                " seed=",
                result.Seed.ToString(CultureInfo.InvariantCulture),
                " ticks=",
                result.TicksExecuted.ToString(CultureInfo.InvariantCulture),
                "/",
                result.MaxTicks.ToString(CultureInfo.InvariantCulture),
                " success=",
                result.Success ? "true" : "false",
                " :: ",
                result.Summary));
        Console.WriteLine("trace=" + result.TracePath);
        Console.WriteLine("digest=" + result.Digest);
        return result.Success ? 0 : 1;
    }

    private static ConfigurationBundle LoadConfiguration()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "sample-defaults.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DynamicPlanningAI.Configuration", "Data", "sample-defaults.json"),
            Path.Combine("src", "DynamicPlanningAI.Configuration", "Data", "sample-defaults.json"),
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string path = Path.GetFullPath(candidates[i]);
            if (File.Exists(path))
            {
                return JsonConfigurationLoader.LoadFromFile(path);
            }
        }

        return ConfigurationBundle.CreateDefault();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("DynamicPlanningAI Sample");
        Console.WriteLine("  --scenario <Name>   Scenario to run (default BasicAttack)");
        Console.WriteLine("  --seed <int>        Deterministic seed override");
        Console.WriteLine("  --trace-dir <path>  Trace output directory");
    }
}
