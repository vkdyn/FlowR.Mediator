using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;

namespace FlowR.Mediator.Tests.Behaviours;

public sealed class TestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly TestLog _log;

    public TestPipelineBehaviour(TestLog log)
    {
        _log = log;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _log.Add($"before:{typeof(TRequest).Name}");

        TResponse response = await next().ConfigureAwait(false);

        _log.Add($"after:{typeof(TRequest).Name}");

        return response;
    }
}
