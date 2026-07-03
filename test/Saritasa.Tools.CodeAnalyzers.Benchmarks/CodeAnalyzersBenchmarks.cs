using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Saritasa.Tools.CodeAnalyzers.Analyzers;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// Benchmarks for Roslyn diagnostic analyzers.
/// </summary>
public class CodeAnalyzersBenchmarks
{
    private static readonly List<Compilation> compilations = new();

    private static readonly LineLengthAnalyzer lineLengthAnalyzer = new();
    private static readonly ExceptionMessageDotAnalyzer exceptionMessageDotAnalyzer = new();
    private static readonly RequestHandlersAnalyzer requestHandlersAnalyzer = new();
    private static readonly SingularTypeNameAnalyzer singularTypeNameAnalyzer = new();

    private static readonly ImmutableArray<DiagnosticAnalyzer> analyzers =
    [
        lineLengthAnalyzer,
        exceptionMessageDotAnalyzer,
        requestHandlersAnalyzer,
        singularTypeNameAnalyzer
    ];

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

    private static async Task RunAnalyzer(DiagnosticAnalyzer analyzer)
    {
        foreach (var compilation in compilations)
        {
            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
