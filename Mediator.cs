using FlowR.Pipeline;
using FlowR.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace FlowR;

/// <summary>
/// The FlowR mediator implementation.
/// Resolves handlers and behaviors from the DI container and orchestrates the pipeline.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handler = HandlerCache.GetOrCreateRequestHandler<TResponse>(_serviceProvider, requestType);
        return await handler(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await SendAsync<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handler = HandlerCache.GetOrCreateStreamHandler<TResponse>(_serviceProvider, requestType);
        return handler(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => PublishAsync(notification, NotificationPublishStrategy.Sequential, cancellationToken);

    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(TNotification notification, NotificationPublishStrategy strategy, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = _serviceProvider
            .GetServices<INotificationHandler<TNotification>>()
            .ToList();

        if (handlers.Count == 0) return;

        var behaviors = _serviceProvider
            .GetServices<INotificationPipelineBehavior<TNotification>>()
            .Reverse()
            .ToList();

        switch (strategy)
        {
            case NotificationPublishStrategy.Sequential:
                await PublishSequentialAsync(notification, handlers, behaviors, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.Parallel:
                await PublishParallelAsync(notification, handlers, behaviors, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.ParallelNoThrow:
                await PublishParallelNoThrowAsync(notification, handlers, behaviors, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.FireAndForget:
                _ = Task.Run(() => PublishSequentialAsync(notification, handlers, behaviors, CancellationToken.None), CancellationToken.None);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }
    }

    private static async Task PublishSequentialAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviors,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        foreach (var handler in handlers)
        {
            await BuildNotificationPipeline(notification, handler, behaviors, cancellationToken)().ConfigureAwait(false);
        }
    }

    private static async Task PublishParallelAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviors,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = handlers.Select(h => BuildNotificationPipeline(notification, h, behaviors, cancellationToken)());
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task PublishParallelNoThrowAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviors,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = handlers.Select(h => BuildNotificationPipeline(notification, h, behaviors, cancellationToken)());
        var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(x => x.Exception, TaskContinuationOptions.None))).ConfigureAwait(false);
        var exceptions = results.Where(e => e != null).SelectMany(e => e!.InnerExceptions).ToList();
        if (exceptions.Count > 0)
            throw new AggregateException("One or more notification handlers failed.", exceptions);
    }

    private static NotificationHandlerDelegate BuildNotificationPipeline<TNotification>(
        TNotification notification,
        INotificationHandler<TNotification> handler,
        List<INotificationPipelineBehavior<TNotification>> behaviors,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        NotificationHandlerDelegate core = () => handler.HandleAsync(notification, cancellationToken);

        return behaviors.Aggregate(core, (next, behavior) =>
            () => behavior.HandleAsync(notification, next, cancellationToken));
    }
}
