using System.Collections.Immutable;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Options;
using Saritasa.Tools.CodeAnalyzers.Analyzers;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// Program class.
/// </summary>
internal class Program
{
    /// <summary>
    /// Entry point.
    /// </summary>
    public static async Task Main(string[] args)
    {
        MSBuildLocator.RegisterDefaults();

        var config = ManualConfig.CreateEmpty()
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithIterationCount(15))
            .AddLogger(ConsoleLogger.Default)
            .WithOption(ConfigOptions.DisableLogFile, true)
            .AddExporter(JsonExporter.BriefCompressed)
            .AddColumnProvider(DefaultColumnProviders.Instance);

        BenchmarkRunner.Run<CodeAnalyzersBenchmarks>(config, args);
    }
}
