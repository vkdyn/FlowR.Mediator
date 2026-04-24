using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Notifications;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class AuditOrderCreatedNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly TestLog _log;

    public AuditOrderCreatedNotificationHandler(TestLog log)
    {
        _log = log;
    }

    public Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"audit-notification:{notification.OrderId}");

        return Task.CompletedTask;
    }
}
