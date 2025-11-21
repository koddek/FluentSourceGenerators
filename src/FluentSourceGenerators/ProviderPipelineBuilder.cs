namespace FluentSourceGenerators;

/// <summary>
/// Fluent builder for creating and configuring pipelines that process single-value providers
/// such as Compilation, AnalyzerConfigOptions, or custom incremental value providers.
/// </summary>
/// <typeparam name="TProvider">The type of the value provided by the source.</typeparam>
/// <typeparam name="TModel">The type of the model that will be generated.</typeparam>
public sealed class ProviderPipelineBuilder<TProvider, TModel>(
    IncrementalGeneratorBuilder parent,
    Func<IncrementalGeneratorInitializationContext, IncrementalValueProvider<TProvider>> providerSelector,
    DiagnosticDescriptor? defaultDescriptor)
    where TModel : IEquatable<TModel>
{
    private Func<TProvider, CancellationToken, GenResult<TModel>>? _transform;
    private Action<SourceProductionContext, TModel, CancellationToken>? _output;

    /// <summary>
    /// Sets the transformation function that converts the provider value into a model.
    /// </summary>
    /// <param name="transform">A function that transforms the provider value into a <see cref="GenResult{TModel}"/>.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/> is null.</exception>
    public ProviderPipelineBuilder<TProvider, TModel> WithTransform(
        Func<TProvider, CancellationToken, GenResult<TModel>> transform)
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
    public ProviderPipelineBuilder<TProvider, TModel> WithOutput(
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
        if (_transform is null) throw new ArgumentNullException(nameof(_transform));
        if (_output is null) throw new ArgumentNullException(nameof(_output));

        var resultProvider = providerSelector(context)
            .Select((input, ct) => _transform(input, ct));

        // One RegisterSourceOutput – branch inside (this is the official pattern)
        context.RegisterSourceOutput(resultProvider, (spc, result) =>
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
