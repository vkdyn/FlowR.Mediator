using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class CancellableCommandHandler : IRequestHandler<CancellableCommand, string>
{
    public Task<string> HandleAsync(CancellableCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult("not-cancelled");
    }
}
