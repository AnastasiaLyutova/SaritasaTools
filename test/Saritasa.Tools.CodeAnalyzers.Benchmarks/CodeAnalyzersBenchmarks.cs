using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Saritasa.Tools.CodeAnalyzers.Analyzers;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// Wraps a DiagnosticAnalyzer to provide a short display name for BenchmarkDotNet.
/// Without this, ToString() returns the fully qualified type name, which pbreporter
/// either truncates or omits from the comparison table.
/// </summary>
public class AnalyzerParam(DiagnosticAnalyzer analyzer)
{
    public DiagnosticAnalyzer Analyzer { get; } = analyzer;

    // BenchmarkDotNet uses ToString() to build the benchmark case name, e.g. RunAnalyzer(LineLengthAnalyzer).
    public override string ToString() => Analyzer.GetType().Name;
}

/// <summary>
/// Benchmarks for Roslyn diagnostic analyzers.
/// Analyzers are discovered dynamically via reflection from Saritasa.Tools.CodeAnalyzers assembly,
/// so no changes to this file are needed when a new analyzer is added.
/// </summary>
public class CodeAnalyzersBenchmarks
{
    private static readonly List<Compilation> compilations = new();

    // Discovered at startup via reflection so that new analyzers are benchmarked automatically.
    // Using typeof(LineLengthAnalyzer) only to resolve the target assembly — no other types
    // are referenced directly, so this file compiles on any branch regardless of which
    // analyzers exist there.
    public static IEnumerable<AnalyzerParam> AnalyzerSource { get; } =
        typeof(LineLengthAnalyzer).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => new AnalyzerParam((DiagnosticAnalyzer)Activator.CreateInstance(t)!))
            .ToList();

    static CodeAnalyzersBenchmarks()
    {
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (sender, args) => Console.WriteLine($"[MSBuild Error] {args.Diagnostic.Message}");

        var solution = workspace.OpenSolutionAsync(CodeAnalyzersBenchmarkSettings.TestProjectPath).GetAwaiter().GetResult();

        foreach (var project in solution.Projects)
        {
            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation == null)
            {
                continue;
            }
            compilations.Add(compilation);
        }

        // Warm up each analyzer individually to JIT-compile the single-analyzer execution path
        // in Roslyn, which differs from the multi-analyzer path used when running all at once.
        //foreach (var param in AnalyzerSource)
        //{
            //RunAnalyzer(param).GetAwaiter().GetResult();
        //}
    }

    /// <summary>
    /// Runs a single analyzer against all compiled projects.
    /// BenchmarkDotNet generates one benchmark case per analyzer found in AnalyzerSource.
    /// </summary>
    [Benchmark]
    [ArgumentsSource(nameof(AnalyzerSource))]
    public async Task RunAnalyzer(AnalyzerParam param)
    {
        foreach (var compilation in compilations)
        {
            // A new instance must be created each iteration: CompilationWithAnalyzers caches
            // results internally, so reusing it would measure cache retrieval, not actual analysis.
            var compilationWithAnalyzers = compilation.WithAnalyzers([param.Analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}

/// <summary>
/// Benchmarks for Roslyn diagnostic analyzers.
/// </summary>
public class CodeAnalyzersBenchmarks1
{
    private static readonly List<Compilation> compilations = new();

    private static readonly LineLengthAnalyzer lineLengthAnalyzer = new();
    private static readonly ExceptionMessageDotAnalyzer exceptionMessageDotAnalyzer = new();
    private static readonly RequestHandlersAnalyzer requestHandlersAnalyzer = new();
    private static readonly SingularTypeNameAnalyzer singularTypeNameAnalyzer = new();
    private static readonly EarlyExitAnalyzer earlyExitAnalyzer = new();

    private static readonly ImmutableArray<DiagnosticAnalyzer> analyzers =
    [
        lineLengthAnalyzer,
        exceptionMessageDotAnalyzer,
        requestHandlersAnalyzer,
        singularTypeNameAnalyzer
    ];

    static CodeAnalyzersBenchmarks1()
    {
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (sender, args) => Console.WriteLine($"[MSBuild Error] {args.Diagnostic.Message}");

        var solution = workspace.OpenSolutionAsync(CodeAnalyzersBenchmarkSettings.TestProjectPath).GetAwaiter().GetResult();

        foreach (var project in solution.Projects)
        {
            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation == null)
            {
                continue;
            }
            compilations.Add(compilation);
        }

        // Warm up each analyzer individually to JIT-compile the single-analyzer execution path
        // in Roslyn, which differs from the multi-analyzer path used when running all at once.
        //foreach (var analyzer in analyzers)
        //{
        //    RunAnalyzer(analyzer).GetAwaiter().GetResult();
        //}
    }

    /// <summary>
    /// Run line lenght analyzer.
    /// </summary>
    [Benchmark]
    public async Task RunLineLengthAnalyzer()
    {
        await RunAnalyzer(lineLengthAnalyzer);
    }

    /// <summary>
    /// Run exception message dot analyzer.
    /// </summary>
    [Benchmark]
    public async Task RunExceptionMessageDotAnalyzer()
    {
        await RunAnalyzer(exceptionMessageDotAnalyzer);
    }

    /// <summary>
    /// Run request handlers analyzer.
    /// </summary>
    [Benchmark]
    public async Task RunRequestHandlersAnalyzer()
    {
        await RunAnalyzer(requestHandlersAnalyzer);
    }

    /// <summary>
    /// Run singular type name analyzer.
    /// </summary>
    [Benchmark]
    public async Task RunSingularTypeNameAnalyzer()
    {
        await RunAnalyzer(singularTypeNameAnalyzer);
    }

    /// <summary>
    /// Run singular type name analyzer.
    /// </summary>
    [Benchmark]
    public async Task RunEarlyExitAnalyzer()
    {
        await RunAnalyzer(earlyExitAnalyzer);
    }

    private static async Task RunAnalyzer(DiagnosticAnalyzer analyzer)
    {
        foreach (var compilation in compilations)
        {
            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
