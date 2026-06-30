using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using Saritasa.Tools.CodeAnalyzers.Analyzers;

namespace Saritasa.Tools.CodeAnalyzers.Benchmarks;

/// <summary>
/// 
/// </summary>
public class CodeAnalyzersBenchmarks
{
    private static readonly List<Compilation> _compilations = new();

    static CodeAnalyzersBenchmarks()
    {
        using var workspace = MSBuildWorkspace.Create();

        workspace.WorkspaceFailed += (sender, args) => Console.WriteLine($"[MSBuild Error] {args.Diagnostic.Message}");

        var project = workspace.OpenProjectAsync(GetProjectPath()).GetAwaiter().GetResult();

        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
        if (compilation == null)
        {
            return;
        }
        _compilations.Add(compilation);

        var analyzer1 = new ExceptionMessageDotAnalyzer();
        var analyzer2 = new LineLengthAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer1, analyzer2);

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

    private static string GetProjectPath()
    {
        var rootPath = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(rootPath) && Directory.GetFiles(rootPath, "*.sln").Length == 0)
        {
            rootPath = Path.GetDirectoryName(rootPath);
        }

        if (string.IsNullOrEmpty(rootPath))
        {
            throw new DirectoryNotFoundException("Not.");
        }

        return Path.Combine(rootPath, "src", "Saritasa.Tools.Domain", "Saritasa.Tools.Domain.csproj");
    }
}
