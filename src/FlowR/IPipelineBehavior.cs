namespace FlowR.Pipeline;

/// <summary>
/// Represents a step in the request pipeline.
/// Implement this to add cross-cutting concerns like logging, validation, caching, etc.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Called during request processing. Call <paramref name="next"/> to continue the pipeline.
    /// </summary>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Delegate for the next step in the pipeline.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Represents a step in the notification pipeline.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    Task HandleAsync(
        TNotification notification,
        NotificationHandlerDelegate next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Delegate for the next notification handler in the pipeline.
/// </summary>
public delegate Task NotificationHandlerDelegate();

/// <summary>
/// Strategy for how notification handlers are called when multiple exist.
/// </summary>
public enum NotificationPublishStrategy
{
    /// <summary>
    /// Handlers are called sequentially, one after another. Default.
    /// </summary>
    Sequential,

    /// <summary>
    /// All handlers are called in parallel using Task.WhenAll.
    /// </summary>
    Parallel,

    /// <summary>
    /// Handlers are called in parallel but failures do not stop others.
    /// All exceptions are collected and thrown as AggregateException.
    /// </summary>
    ParallelNoThrow,

    /// <summary>
    /// Fire and forget — handlers run in background, exceptions are swallowed.
    /// Useful for non-critical events.
    /// </summary>
    FireAndForget
}
