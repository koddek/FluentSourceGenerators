using FluentSourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using TUnit.Core;

namespace FluentSourceGenerators.Tests;

public record ConfigModel(string Key);

[Generator]
public class ConfigGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InvalidConfigId =
        new("CFG001", "Invalid Config", "Config key must be uppercase", "Usage", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalGeneratorBuilder.Create()
            .StartSyntaxPipeline<ConfigModel>()
            .WithPredicate((n, _) => n is ClassDeclarationSyntax c && c.Identifier.Text == "Config")
            .WithTransform((ctx, _) =>
            {
                var cls = (ClassDeclarationSyntax)ctx.Node;

                // FIX 1: Cast ISymbol to INamedTypeSymbol to access .GetMembers()
                if (ctx.SemanticModel.GetDeclaredSymbol(cls) is not INamedTypeSymbol symbol)
                    return GenResult<ConfigModel>.Fail(Diagnostic.Create(InvalidConfigId, cls.GetLocation()));

                var keyField = symbol.GetMembers("Key").OfType<IFieldSymbol>().FirstOrDefault();

                if (keyField == null || keyField.ConstantValue is not string val)
                    return GenResult<ConfigModel>.Fail(Diagnostic.Create(InvalidConfigId, cls.GetLocation()));

                if (val != val.ToUpperInvariant())
                    return GenResult<ConfigModel>.Fail(Diagnostic.Create(InvalidConfigId,
                        keyField.DeclaringSyntaxReferences[0].GetSyntax().GetLocation()));

                return new ConfigModel(val);
            })
            // FIX 2: Add 'ct' (CancellationToken) argument
            .WithOutput((spc, model, ct) => { spc.AddSource("Config.g.cs", $"// Validated Key: {model.Key}"); })
            .And()
            .Build()
            .Initialize(context);
    }
}

public class ConfigGeneratorTest
{
    [Test]
    public async Task Config_ReturnsError_WhenLowercase()
    {
        var input = """
                    public partial class Config {
                        public const string Key = "lowercase"; 
                    }
                    """;

        var (diags, output) = TestHelpers.RunGenerator<ConfigGenerator>(input);

        await Assert.That(diags).HasSingleItem();
        await Assert.That(diags[0].Id).IsEqualTo("CFG001");
        await Assert.That(output).IsEmpty(); // No code generated
    }
}
