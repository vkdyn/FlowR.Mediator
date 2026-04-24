using FlowR.Mediator.Internal;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return SendAsync(request, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync(request, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, cancellationToken);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>> handler =
            HandlerCache.GetOrCreateRequestHandler<TResponse>(request.GetType());

        return await handler(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await SendAsync<Unit>(request, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>> handler =
            HandlerCache.GetOrCreateStreamHandler<TResponse>(request.GetType());

        return handler(request, _serviceProvider, cancellationToken);
    }

    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
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
                _ = Task.Run(async () => await PublishSequentialAsync(notification, handlers, behaviours, CancellationToken.None).ConfigureAwait(false), CancellationToken.None);
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
            NotificationHandlerDelegate pipeline = BuildNotificationPipeline(notification, handler, behaviours, cancellationToken);
            await pipeline().ConfigureAwait(false);
        }
    }

    private static async Task PublishParallelAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Task> tasks = new();
        foreach (INotificationHandler<TNotification> handler in handlers)
        {
            NotificationHandlerDelegate pipeline = BuildNotificationPipeline(notification, handler, behaviours, cancellationToken);
            tasks.Add(pipeline());
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task PublishParallelNoThrowAsync<TNotification>(
        TNotification notification,
        List<INotificationHandler<TNotification>> handlers,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Task<Exception?>> tasks = new();
        foreach (INotificationHandler<TNotification> handler in handlers)
        {
            NotificationHandlerDelegate pipeline = BuildNotificationPipeline(notification, handler, behaviours, cancellationToken);
            tasks.Add(CaptureExceptionAsync(pipeline));
        }

        Exception?[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        List<Exception> exceptions = results.Where(exception => exception is not null).Select(exception => exception!).ToList();

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more notification handlers failed.", exceptions);
        }
    }

    private static async Task<Exception?> CaptureExceptionAsync(NotificationHandlerDelegate pipeline)
    {
        try
        {
            await pipeline().ConfigureAwait(false);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static NotificationHandlerDelegate BuildNotificationPipeline<TNotification>(
        TNotification notification,
        INotificationHandler<TNotification> handler,
        List<INotificationPipelineBehavior<TNotification>> behaviours,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        NotificationHandlerDelegate pipeline = () => handler.HandleAsync(notification, cancellationToken);

        foreach (INotificationPipelineBehavior<TNotification> behaviour in behaviours)
        {
            NotificationHandlerDelegate next = pipeline;
            pipeline = () => behaviour.HandleAsync(notification, next, cancellationToken);
        }

        return pipeline;
    }
}
