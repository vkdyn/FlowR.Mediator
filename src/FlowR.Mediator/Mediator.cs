using FlowR.Mediator.Internal;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator;

/// <summary>
/// The FlowR mediator implementation.
/// Resolves handlers and behaviours from the DI container and orchestrates the pipeline.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(request, cancellationToken);
    }

    public Task Send(
        IRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(request, cancellationToken);
    }

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, cancellationToken);
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();

        Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>> handler =
            HandlerCache.GetOrCreateRequestHandler<TResponse>(_serviceProvider, requestType);

        return await handler(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(
        IRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await SendAsync<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();

        Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>> handler =
            HandlerCache.GetOrCreateStreamHandler<TResponse>(_serviceProvider, requestType);

        return handler(request, _serviceProvider, cancellationToken);
    }

    public Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, NotificationPublishStrategy.Sequential, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(
        TNotification notification,
        NotificationPublishStrategy strategy,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        List<INotificationHandler<TNotification>> handlers = _serviceProvider
            .GetServices<INotificationHandler<TNotification>>()
            .ToList();

        if (handlers.Count == 0)
        {
            return;
        }

        List<INotificationPipelineBehavior<TNotification>> behaviours = _serviceProvider
            .GetServices<INotificationPipelineBehavior<TNotification>>()
            .Reverse()
            .ToList();

        switch (strategy)
        {
            case NotificationPublishStrategy.Sequential:
                await PublishSequentialAsync(notification, handlers, behaviours, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.Parallel:
                await PublishParallelAsync(notification, handlers, behaviours, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.ParallelNoThrow:
                await PublishParallelNoThrowAsync(notification, handlers, behaviours, cancellationToken).ConfigureAwait(false);
                break;

            case NotificationPublishStrategy.FireAndForget:
                _ = Task.Run(
                    () => PublishSequentialAsync(
                        notification,
                        handlers,
                        behaviours,
                        CancellationToken.None),
                    CancellationToken.None);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }
    }

    private static async Task PublishSequentialAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        foreach (INotificationHandler<TNotification> handler in handlers)
        {
            await BuildNotificationPipeline(notification, handler, behaviours, cancellationToken)()
                .ConfigureAwait(false);
        }
    }

    private static async Task PublishParallelAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        IEnumerable<Task> tasks = handlers.Select(handler =>
            BuildNotificationPipeline(notification, handler, behaviours, cancellationToken)());

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task PublishParallelNoThrowAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        IEnumerable<Task> tasks = handlers.Select(handler =>
            BuildNotificationPipeline(notification, handler, behaviours, cancellationToken)());

        Task<Exception?>[] wrappedTasks = tasks
            .Select(async task =>
            {
                try
                {
                    await task.ConfigureAwait(false);
                    return null;
                }
                catch (Exception exception)
                {
                    return exception;
                }
            })
            .ToArray();

        Exception?[] results = await Task.WhenAll(wrappedTasks).ConfigureAwait(false);

        List<Exception> exceptions = results
            .Where(exception => exception != null)
            .Select(exception => exception!)
            .ToList();

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more notification handlers failed.", exceptions);
        }
    }

    private static NotificationHandlerDelegate BuildNotificationPipeline<TNotification>(
        TNotification notification,
        INotificationHandler<TNotification> handler,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        NotificationHandlerDelegate core = () => handler.HandleAsync(notification, cancellationToken);

        return behaviours.Aggregate(
            core,
            (next, behaviour) => () => behaviour.HandleAsync(notification, next, cancellationToken));
    }
}