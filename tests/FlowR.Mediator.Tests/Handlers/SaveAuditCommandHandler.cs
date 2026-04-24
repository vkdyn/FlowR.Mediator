using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class SaveAuditCommandHandler : IRequestHandler<SaveAuditCommand>
{
    private readonly TestLog _log;

    public SaveAuditCommandHandler(TestLog log)
    {
        _log = log;
    }

    public Task HandleAsync(SaveAuditCommand request, CancellationToken cancellationToken)
    {
        _log.Add($"audit:{request.Value}");

        return Task.CompletedTask;
    }
}
