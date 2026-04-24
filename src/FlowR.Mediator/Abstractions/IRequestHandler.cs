namespace FlowR.Mediator;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest
{
    async Task<Unit> IRequestHandler<TRequest, Unit>.HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        await HandleAsync(request, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    new Task HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}