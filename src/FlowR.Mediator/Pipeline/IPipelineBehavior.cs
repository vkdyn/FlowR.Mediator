namespace FlowR.Mediator.Pipeline;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
public delegate Task NotificationHandlerDelegate();

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IBaseRequest
{
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

public interface INotificationPipelineBehavior<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, NotificationHandlerDelegate next, CancellationToken cancellationToken);
}
