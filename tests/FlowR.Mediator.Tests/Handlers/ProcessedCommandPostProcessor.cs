using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class ProcessedCommandPostProcessor : IRequestPostProcessor<ProcessedCommand, string>
{
    private readonly TestLog _log;

    public ProcessedCommandPostProcessor(TestLog log) => _log = log;

    public Task Process(ProcessedCommand request, string response, CancellationToken cancellationToken)
    {
        _log.Add($"post:{response}");
        return Task.CompletedTask;
    }
}
