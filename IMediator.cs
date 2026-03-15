namespace FlowR;

/// <summary>
/// Defines the FlowR mediator. Use this to send requests and publish notifications.
/// </summary>
public interface IMediator : ISender, IPublisher { }

/// <summary>
/// Sends requests and gets responses.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request and returns a response.
    /// </summary>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a void request (returns Unit).
    /// </summary>
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a streaming response from a streaming request.
    /// </summary>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes notifications to all registered handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Publishes a notification using a specific strategy.
    /// </summary>
    Task PublishAsync<TNotification>(TNotification notification, NotificationPublishStrategy strategy, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
