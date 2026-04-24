using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Models;
using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class PingCommandHandler : IRequestHandler<PingCommand, PingResult>
{
    private readonly TestLog _log;

    public PingCommandHandler(TestLog log)
    {
        _log = log;
    }

    public Task<PingResult> HandleAsync(PingCommand request, CancellationToken cancellationToken)
    {
        _log.Add("handler:PingCommand");

        return Task.FromResult(new PingResult(request.Value.ToUpperInvariant()));
    }
}
