using FlowR.Mediator.Pipeline;

namespace FlowR.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that runs all registered IRequestPostProcessor&lt;TRequest, TResponse&gt;
/// after the handler. Mirrors MediatR's RequestPostProcessorBehavior.
/// Add to your pipeline via:
///   services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(RequestPostProcessorBehavior&lt;,&gt;));
/// </summary>
public sealed class RequestPostProcessorBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> _postProcessors;

    public RequestPostProcessorBehavior(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors)
        => _postProcessors = postProcessors;

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response = await next().ConfigureAwait(false);

        foreach (IRequestPostProcessor<TRequest, TResponse> processor in _postProcessors)
            await processor.Process(request, response, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
