namespace FluentSourceGenerators;

/// <summary>
/// Fluent builder for creating and configuring syntax-based pipelines using CreateSyntaxProvider.
/// This builder helps process syntax nodes that match a predicate and transform them into models.
/// </summary>
/// <typeparam name="TModel">The type of the model that will be generated from matching syntax nodes.</typeparam>
public sealed class SyntaxPipelineBuilder<TModel>(
    IncrementalGeneratorBuilder parent,
    DiagnosticDescriptor? defaultDescriptor)
    where TModel : IEquatable<TModel>
{
    private Func<SyntaxNode, CancellationToken, bool>? _predicate;
    private Func<GeneratorSyntaxContext, CancellationToken, GenResult<TModel>>? _transform;
    private Action<SourceProductionContext, TModel, CancellationToken>? _output;

    /// <summary>
    /// Sets the predicate used to filter syntax nodes for processing.
    /// </summary>
    /// <param name="predicate">A function that determines if a syntax node should be processed.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    public SyntaxPipelineBuilder<TModel> WithPredicate(Func<SyntaxNode, CancellationToken, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        return this;
    }

    /// <summary>
    /// Sets the transformation function that converts matching syntax nodes into models.
    /// </summary>
    /// <param name="transform">A function that transforms a syntax context into a <see cref="GenResult{TModel}"/>.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/> is null.</exception>
    public SyntaxPipelineBuilder<TModel> WithTransform(
        Func<GeneratorSyntaxContext, CancellationToken, GenResult<TModel>> transform)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        return this;
    }

    /// <summary>
    /// Sets the output action that generates source code from the transformed models.
    /// </summary>
    /// <param name="output">An action that generates source code for a given model.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="output"/> is null.</exception>
    public SyntaxPipelineBuilder<TModel> WithOutput(
        Action<SourceProductionContext, TModel, CancellationToken> output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        return this;
    }

    /// <summary>
    /// Returns to the parent builder to continue configuration.
    /// </summary>
    /// <returns>The parent <see cref="IncrementalGeneratorBuilder"/> instance.</returns>
    public IncrementalGeneratorBuilder And() => parent;

    internal void Register(IncrementalGeneratorInitializationContext context)
    {
        if (_predicate is null) throw new ArgumentNullException(nameof(_predicate));
        if (_transform is null) throw new ArgumentNullException(nameof(_transform));
        if (_output is null) throw new ArgumentNullException(nameof(_output));

        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(_predicate, (ctx, ct) => _transform(ctx, ct));

        context.RegisterSourceOutput(provider, (spc, result) =>
        {
            if (!result.IsSuccess)
            {
                if (result.Error is { } diag)
                    spc.ReportDiagnostic(diag);
                return;
            }

            var model = result.Value!;

            try
            {
                _output(spc, model, spc.CancellationToken);
            }
            catch (Exception ex) when (defaultDescriptor is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(defaultDescriptor, null, ex.Message));
            }
        });
    }
}
