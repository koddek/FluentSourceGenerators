# FluentSourceGenerators

**The Pit of Success™ for Roslyn Incremental Source Generators**

Write beautiful, type-safe, testable source generators in **~30 lines** instead of 300.

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalGeneratorBuilder
            .Create()
            .StartSyntaxPipeline<MyModel>()
            .WithPredicate((node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 })
            .WithTransform((ctx, ct) =>
            {
                if (!HasMyAttribute(ctx)) return Diagnostic.Create(MyError, ctx.Node.GetLocation());
                return new MyModel(ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!);
            })
            .WithOutput((spc, model, ct) =>
                spc.AddSource($"{model.Name}.g.cs", GenerateCode(model)))
            .And()
            .Build()
            .Initialize(context);
    }
}
```

### Features

- Full type safety — no `object` boxing
- `GenResult<T>` — return `Diagnostic` directly → auto-reported
- Zero boilerplate — no manual `Collect()`, `Combine()`, or null-filtering
- Built-in exception fallback
- Supports syntax, compilation, additional texts
- Chain multiple pipelines with `.And()`
- 100% unit-testable transforms

### Install

```bash
dotnet add package FluentSourceGenerators
```

### Why This Is the Best

This is the **only** fluent API that combines true generics + `GenResult<T>` diagnostics + production readiness.

MIT License • Contributions welcome!
