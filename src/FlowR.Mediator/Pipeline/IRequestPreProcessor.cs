namespace FlowR.Mediator.Pipeline;

/// <summary>
/// Runs before the request handler and all pipeline behaviors.
/// Mirrors MediatR's IRequestPreProcessor&lt;TRequest&gt;.
/// </summary>
public interface IRequestPreProcessor<in TRequest>
    where TRequest : notnull
{
    Task Process(TRequest request, CancellationToken cancellationToken);
}
