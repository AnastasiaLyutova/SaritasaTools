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
[MemoryDiagnoser]
public class CodeAnalyzersBenchmarks
{
    private static readonly List<Compilation> _compilations = new();

    static CodeAnalyzersBenchmarks()
    {
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (sender, args) => Console.WriteLine($"[MSBuild Error] {args.Diagnostic.Message}");

        var solution = workspace.OpenSolutionAsync(Program.File).GetAwaiter().GetResult();

        var analyzer1 = new ExceptionMessageDotAnalyzer();
        var analyzer2 = new LineLengthAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer1, analyzer2);

        foreach (var project in solution.Projects)
        {
            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation == null)
            {
                continue;
            }
            _compilations.Add(compilation);
        }

        foreach (var c in _compilations)
        {
            var compilationWithAnalyzers = c.WithAnalyzers(analyzers);
            _ = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public async Task RunLineLengthAnalyzer()
    {
        foreach (var compilation in _compilations)
        {
            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(typeof(LineLengthAnalyzer));
            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }

    [Benchmark]
    public async Task RunExceptionMessageDotAnalyzer()
    {
        foreach (var compilation in _compilations)
        {
            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(typeof(ExceptionMessageDotAnalyzer));
            var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
            _ = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
