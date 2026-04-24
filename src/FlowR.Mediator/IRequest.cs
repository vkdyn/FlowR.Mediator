namespace FlowR.Mediator;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary>
/// Marker interface for requests that return no response (void/unit).
/// </summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>
/// Base marker interface for all requests.
/// </summary>
public interface IBaseRequest { }

/// <summary>
/// Handles a request and returns a response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a request that returns no response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest<Unit>
{
}

/// <summary>
/// Marker interface for notifications (events).
/// Notifications can be handled by multiple handlers.
/// </summary>
public interface INotification { }

/// <summary>
/// Handles a notification.
/// Multiple handlers can be registered for the same notification.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for streaming requests.
/// </summary>
/// <typeparam name="TResponse">The type of each streamed item.</typeparam>
public interface IStreamRequest<out TResponse> : IBaseRequest { }

/// <summary>
/// Handles a streaming request, yielding multiple results.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The type of each streamed item.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
