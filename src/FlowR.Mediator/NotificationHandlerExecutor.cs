namespace FlowR.Mediator;

public sealed class NotificationHandlerExecutor
{
    private readonly Func<INotification, CancellationToken, Task> _handler;

    public NotificationHandlerExecutor(
        Func<INotification, CancellationToken, Task> handler)
    {
        _handler = handler;
    }

    public Task Execute(
        INotification notification,
        CancellationToken cancellationToken)
    {
        return _handler(notification, cancellationToken);
    }
}