namespace FlowR.Mediator.Pipeline;

/// <summary>
/// Runs after the request handler and all pipeline behaviors.
/// Mirrors MediatR's IRequestPostProcessor&lt;TRequest, TResponse&gt;.
/// </summary>
public interface IRequestPostProcessor<in TRequest, in TResponse>
    where TRequest : notnull
{
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
