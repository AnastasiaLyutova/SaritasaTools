using System.CommandLine;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Build.Locator;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// Program class.
/// </summary>
internal class Program
{
    // Static state to temporarily hold parsed values
    public static string File { get; private set; } = string.Empty;

    /// <summary>
    /// Entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        Option<string> fileOption = new("--test")
        {
            Description = "The file to read and display on the console"
        };

        var rootCommand = new RootCommand();
        rootCommand.Options.Add(fileOption);
        rootCommand.TreatUnmatchedTokensAsErrors = false;

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count == 0 && parseResult.GetValue(fileOption) is string parsedFile)
        {
            File = parsedFile;

            MSBuildLocator.RegisterDefaults();

            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Default
                    .WithToolchain(InProcessEmitToolchain.Instance))
                    .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                    .WithOption(ConfigOptions.StopOnFirstError, true)
                .AddLogger(ConsoleLogger.Default)
                    .WithOption(ConfigOptions.DisableLogFile, true)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(JsonExporter.BriefCompressed)
                .AddColumnProvider(DefaultColumnProviders.Instance);

            BenchmarkRunner.Run<CodeAnalyzersBenchmarks>(config, parseResult.UnmatchedTokens.ToArray());
            return 0;
        }

        foreach (var parseError in parseResult.Errors)
        {
            Console.Error.WriteLine(parseError.Message);
        }

        return 1;
    }
}
