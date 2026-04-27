using FlowR.Mediator.Pipeline;

namespace FlowR.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that runs all registered IRequestPreProcessor&lt;TRequest&gt;
/// before the handler. Mirrors MediatR's RequestPreProcessorBehavior.
/// Add to your pipeline via:
///   services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(RequestPreProcessorBehavior&lt;,&gt;));
/// </summary>
public sealed class RequestPreProcessorBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly IEnumerable<IRequestPreProcessor<TRequest>> _preProcessors;

    public RequestPreProcessorBehavior(IEnumerable<IRequestPreProcessor<TRequest>> preProcessors)
        => _preProcessors = preProcessors;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (IRequestPreProcessor<TRequest> processor in _preProcessors)
            await processor.Process(request, cancellationToken).ConfigureAwait(false);

        return await next().ConfigureAwait(false);
    }
}
