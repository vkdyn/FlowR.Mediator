// ------------------------------------------------------------------------------------------
// MediatR Compatibility Layer for FlowR.Mediator
// This allows existing MediatR-based code to compile without refactoring
// ------------------------------------------------------------------------------------------
namespace FlowR.Mediator.Abstractions
{
    // Marker interface
    public interface IBaseRequest { }

    // Request with response
    public interface IRequest<out TResponse> : IBaseRequest { }

    // Request without response
    public interface IRequest : IBaseRequest { }

    // Notification (event)
    public interface INotification { }

    // Mediator interface
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

        Task Send(IRequest request, CancellationToken cancellationToken = default);

        Task Publish(INotification notification, CancellationToken cancellationToken = default);
    }

    // Handler with response
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    // Handler without response
    public interface IRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken);
    }

    // Notification handler
    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }

    // Optional publisher abstraction
    public interface INotificationPublisher
    {
        Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors,
            INotification notification,
            CancellationToken cancellationToken);
    }

    // Executor wrapper
    public class NotificationHandlerExecutor
    {
        public Func<INotification, CancellationToken, Task> HandlerCallback { get; }

        public NotificationHandlerExecutor(Func<INotification, CancellationToken, Task> handlerCallback)
        {
            HandlerCallback = handlerCallback;
        }

        public Task Execute(INotification notification, CancellationToken cancellationToken)
        {
            return HandlerCallback(notification, cancellationToken);
        }
    }
}
