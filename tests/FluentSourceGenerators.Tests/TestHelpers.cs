using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FluentSourceGenerators.Tests;

public static class TestHelpers
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) RunGenerator<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("Tests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new T();

        // The GeneratorDriver runs the generator against the compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();

        var output = runResult.GeneratedTrees.Length > 0
            ? runResult.GeneratedTrees.Last().ToString()
            : "";

        return (runResult.Diagnostics, output);
    }
}
