namespace FluentSourceGenerators;

/// <summary>
/// The main entry point for building incremental source generators fluently.
/// </summary>
public sealed class IncrementalGeneratorBuilder
{
    private readonly List<Action<IncrementalGeneratorInitializationContext>> _postInits = [];
    private readonly List<Action<IncrementalGeneratorInitializationContext>> _registrations = [];
    private readonly DiagnosticDescriptor? _defaultDiagnosticDescriptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncrementalGeneratorBuilder"/> class.
    /// </summary>
    /// <param name="defaultDiagnosticDescriptor">Default diagnostic descriptor to use for unhandled exceptions.</param>
    private IncrementalGeneratorBuilder(DiagnosticDescriptor? defaultDiagnosticDescriptor) =>
        _defaultDiagnosticDescriptor = defaultDiagnosticDescriptor;

    /// <summary>
    /// Creates a new instance of the <see cref="IncrementalGeneratorBuilder"/>.
    /// </summary>
    /// <param name="defaultDiagnosticDescriptor">Optional default diagnostic descriptor for reporting unhandled exceptions.</param>
    /// <returns>A new instance of <see cref="IncrementalGeneratorBuilder"/>.</returns>
    public static IncrementalGeneratorBuilder Create(DiagnosticDescriptor? defaultDiagnosticDescriptor = null) =>
        new(defaultDiagnosticDescriptor);

    /// <summary>
    /// Adds a post-initialization action that runs after the generator has been initialized.
    /// This is typically used to add additional files or resources to the compilation.
    /// </summary>
    /// <param name="action">The action to execute during post-initialization.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public IncrementalGeneratorBuilder AddPostInitialization(Action<IncrementalGeneratorPostInitializationContext> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _postInits.Add(ctx => ctx.RegisterPostInitializationOutput(action));
        return this;
    }

    /// <summary>
    /// Starts building a syntax-based pipeline that processes syntax nodes matching a predicate.
    /// </summary>
    /// <typeparam name="TModel">The type of the model that will be generated from the syntax nodes.</typeparam>
    /// <returns>A <see cref="SyntaxPipelineBuilder{TModel}"/> to configure the syntax pipeline.</returns>
    public SyntaxPipelineBuilder<TModel> StartSyntaxPipeline<TModel>()
        where TModel : IEquatable<TModel>
    {
        var pipeline = new SyntaxPipelineBuilder<TModel>(this, _defaultDiagnosticDescriptor);
        _registrations.Add(pipeline.Register);
        return pipeline;
    }

    /// <summary>
    /// Starts building a generic provider pipeline for processing any incremental value provider.
    /// </summary>
    /// <typeparam name="TProvider">The type of the value provided by the source.</typeparam>
    /// <typeparam name="TModel">The type of the model that will be generated.</typeparam>
    /// <param name="providerSelector">A function that selects the source incremental value provider.</param>
    /// <returns>A <see cref="ProviderPipelineBuilder{TProvider, TModel}"/> to configure the pipeline.</returns>
    public ProviderPipelineBuilder<TProvider, TModel> StartProviderPipeline<TProvider, TModel>(
        Func<IncrementalGeneratorInitializationContext, IncrementalValueProvider<TProvider>> providerSelector)
        where TModel : IEquatable<TModel>
    {
        if (providerSelector == null) throw new ArgumentNullException(nameof(providerSelector));
        var pipeline = new ProviderPipelineBuilder<TProvider, TModel>(this, providerSelector, _defaultDiagnosticDescriptor);
        _registrations.Add(pipeline.Register);
        return pipeline;
    }

    /// <summary>
    /// Starts building a pipeline that processes the compilation.
    /// </summary>
    /// <typeparam name="TModel">The type of the model that will be generated from the compilation.</typeparam>
    /// <returns>A <see cref="ProviderPipelineBuilder{Compilation, TModel}"/> to configure the compilation pipeline.</returns>
    public ProviderPipelineBuilder<Compilation, TModel> StartCompilationPipeline<TModel>()
        where TModel : IEquatable<TModel>
        => StartProviderPipeline<Compilation, TModel>(ctx => ctx.CompilationProvider);

    /// <summary>
    /// Starts building a pipeline that processes all additional text files.
    /// </summary>
    /// <typeparam name="TModel">The type of the model that will be generated from the additional files.</typeparam>
    /// <returns>A <see cref="ProviderPipelineBuilder{ImmutableArray{AdditionalText}, TModel}"/> to configure the pipeline.</returns>
    public ProviderPipelineBuilder<ImmutableArray<AdditionalText>, TModel> StartAdditionalTextsPipeline<TModel>()
        where TModel : IEquatable<TModel>
        => StartProviderPipeline<ImmutableArray<AdditionalText>, TModel>(ctx => ctx.AdditionalTextsProvider.Collect());

    /// <summary>
    /// Builds the incremental generator with all configured pipelines.
    /// </summary>
    /// <returns>An <see cref="IIncrementalGenerator"/> instance ready for registration.</returns>
    public IIncrementalGenerator Build() => new BuiltGenerator(_postInits, _registrations);

    /// <summary>
    /// Creates a diagnostic with the specified parameters.
    /// </summary>
    /// <param name="id">The diagnostic ID (e.g., "SG0001").</param>
    /// <param name="message">The message that describes the diagnostic.</param>
    /// <param name="location">The location where the diagnostic should be reported, or null for no specific location.</param>
    /// <param name="severity">The severity of the diagnostic.</param>
    /// <returns>A new <see cref="Diagnostic"/> instance.</returns>
    public static Diagnostic CreateDiagnostic(
        string id,
        string message,
        Location? location = null,
        DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("Diagnostic ID cannot be null or empty.", nameof(id));
        if (string.IsNullOrEmpty(message)) throw new ArgumentException("Diagnostic message cannot be null or empty.", nameof(message));

        var descriptor = new DiagnosticDescriptor(id, message, message, "SourceGenerator", severity, true);
        return Diagnostic.Create(descriptor, location ?? Location.None);
    }

    internal sealed class BuiltGenerator(
        IReadOnlyList<Action<IncrementalGeneratorInitializationContext>> postInits,
        IReadOnlyList<Action<IncrementalGeneratorInitializationContext>> registrations)
        : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            foreach (var postInit in postInits)
                postInit(context);

            foreach (var registration in registrations)
                registration(context);
        }
    }
}
