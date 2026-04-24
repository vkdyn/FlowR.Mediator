using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Notifications;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class EmailOrderCreatedNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly TestLog _log;

    public EmailOrderCreatedNotificationHandler(TestLog log)
    {
        _log = log;
    }

    public Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"email:{notification.OrderId}");

        return Task.CompletedTask;
    }
}
