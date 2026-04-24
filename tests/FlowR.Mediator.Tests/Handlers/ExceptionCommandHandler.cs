using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class ExceptionCommandHandler : IRequestHandler<ExceptionCommand, string>
{
    public Task<string> HandleAsync(ExceptionCommand request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Expected command failure.");
    }
}
