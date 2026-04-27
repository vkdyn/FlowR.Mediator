using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;

namespace FlowR.Mediator.Tests.Behaviours;

public sealed class TestNotificationPipelineBehavior<TNotification>
    : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    private readonly TestLog _log;

    public TestNotificationPipelineBehavior(TestLog log) => _log = log;

    public async Task HandleAsync(
        TNotification notification,
        NotificationHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        _log.Add($"notification-before:{typeof(TNotification).Name}");
        await next();
        _log.Add($"notification-after:{typeof(TNotification).Name}");
    }
}
