using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class ProcessedCommandPreProcessor : IRequestPreProcessor<ProcessedCommand>
{
    private readonly TestLog _log;

    public ProcessedCommandPreProcessor(TestLog log) => _log = log;

    public Task Process(ProcessedCommand request, CancellationToken cancellationToken)
    {
        _log.Add($"pre:{request.Value}");
        return Task.CompletedTask;
    }
}
