using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class ServiceFactoryCommandHandler : IRequestHandler<ServiceFactoryCommand, string>
{
    public Task<string> HandleAsync(ServiceFactoryCommand request, CancellationToken cancellationToken)
        => Task.FromResult($"resolved:{request.Value}");
}
