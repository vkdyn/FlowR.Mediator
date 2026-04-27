using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class ProcessedCommandHandler : IRequestHandler<ProcessedCommand, string>
{
    private readonly TestLog _log;

    public ProcessedCommandHandler(TestLog log) => _log = log;

    public Task<string> HandleAsync(ProcessedCommand request, CancellationToken cancellationToken)
    {
        _log.Add($"handler:{request.Value}");
        return Task.FromResult(request.Value.ToUpperInvariant());
    }
}
