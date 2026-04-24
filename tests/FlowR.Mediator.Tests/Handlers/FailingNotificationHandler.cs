using FlowR.Mediator.Tests.Notifications;

namespace FlowR.Mediator.Tests.Handlers;

public sealed class FailingNotificationHandler : INotificationHandler<FailingNotification>
{
    public Task HandleAsync(FailingNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Expected notification failure.");
    }
}
