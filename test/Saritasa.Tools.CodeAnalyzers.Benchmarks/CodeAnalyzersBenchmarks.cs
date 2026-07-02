using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Saritasa.Tools.CodeAnalyzers.Analyzers;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// 
/// </summary>
public class CodeAnalyzersBenchmarks
{
    private static readonly List<Compilation> compilations = new();

    private static readonly DiagnosticAnalyzer lineLengthAnalyzer = new LineLengthAnalyzer();
    private static readonly DiagnosticAnalyzer exceptionMessageDotAnalyzer = new ExceptionMessageDotAnalyzer();
    private static readonly DiagnosticAnalyzer requestHandlersAnalyzer = new RequestHandlersAnalyzer();
    private static readonly DiagnosticAnalyzer singularTypeNameAnalyzer = new SingularTypeNameAnalyzer();

    static CodeAnalyzersBenchmarks()
    {
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (sender, args) => Console.WriteLine($"[MSBuild Error] {args.Diagnostic.Message}");

        var solution = workspace.OpenSolutionAsync(Program.File).GetAwaiter().GetResult();

        foreach (var project in solution.Projects)
        {
            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation == null)
            {
                continue;
            }
            compilations.Add(compilation);
        }

        var analyzers = ImmutableArray.Create(
            lineLengthAnalyzer,
            exceptionMessageDotAnalyzer,
            requestHandlersAnalyzer,
            singularTypeNameAnalyzer);

        foreach (var analyzer in analyzers)
        {
            foreach (var compilation in compilations)
            {
                var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
                _ = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Benchmark]
    public async Task RunLineLengthAnalyzer()
    {
        await RunAnalyzer(lineLengthAnalyzer);
    }

    /// <summary>
    /// 
    /// </summary>
    [Benchmark]
    public async Task RunExceptionMessageDotAnalyzer()
    {
        await RunAnalyzer(exceptionMessageDotAnalyzer);
    }

    /// <summary>
    /// 
    /// </summary>
    [Benchmark]
    public async Task RunRequestHandlersAnalyzer()
    {
        await RunAnalyzer(requestHandlersAnalyzer);
    }

    /// <summary>
    /// 
    /// </summary>
    [Benchmark]
    public async Task RunSingularTypeNameAnalyzer()
    {
        await RunAnalyzer(singularTypeNameAnalyzer);
    }

    private async Task RunAnalyzer(DiagnosticAnalyzer analyzer)
    {
        foreach (var compilation in compilations)
        {
            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
