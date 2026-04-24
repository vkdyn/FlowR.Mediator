using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace FlowR.Mediator.Internal;

internal static class HandlerCache
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> RequestHandlers = new();
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> StreamHandlers = new();

    public static Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>> GetOrCreateRequestHandler<TResponse>(Type requestType)
    {
        object factory = RequestHandlers.GetOrAdd((requestType, typeof(TResponse)), static key =>
        {
            MethodInfo method = typeof(HandlerCache)
                .GetMethod(nameof(CreateRequestHandler), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(key.RequestType, key.ResponseType);

            return method.Invoke(null, null)!;
        });

        return (Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>>)factory;
    }

    public static Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>> GetOrCreateStreamHandler<TResponse>(Type requestType)
    {
        object factory = StreamHandlers.GetOrAdd((requestType, typeof(TResponse)), static key =>
        {
            MethodInfo method = typeof(HandlerCache)
                .GetMethod(nameof(CreateStreamHandler), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(key.RequestType, key.ResponseType);

            return method.Invoke(null, null)!;
        });

        return (Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>>)factory;
    }

    private static Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>> CreateRequestHandler<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        return async (request, serviceProvider, cancellationToken) =>
        {
            TRequest typedRequest = (TRequest)request;

            RequestHandlerDelegate<TResponse> pipeline = () => InvokeRequestHandler<TRequest, TResponse>(typedRequest, serviceProvider, cancellationToken);

            List<IPipelineBehavior<TRequest, TResponse>> behaviours = serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse()
                .ToList();

            foreach (IPipelineBehavior<TRequest, TResponse> behaviour in behaviours)
            {
                RequestHandlerDelegate<TResponse> next = pipeline;
                pipeline = () => behaviour.HandleAsync(typedRequest, next, cancellationToken);
            }

            return await pipeline().ConfigureAwait(false);
        };
    }

    private static async Task<TResponse> InvokeRequestHandler<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        IRequestHandler<TRequest, TResponse>? handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();

        if (handler is not null)
        {
            return await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"No request handler registered for request '{typeof(TRequest).FullName}' and response '{typeof(TResponse).FullName}'.");
    }

    private static Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>> CreateStreamHandler<TRequest, TResponse>()
        where TRequest : IStreamRequest<TResponse>
    {
        return (request, serviceProvider, cancellationToken) =>
        {
            TRequest typedRequest = (TRequest)request;
            IStreamRequestHandler<TRequest, TResponse> handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
            return handler.HandleAsync(typedRequest, cancellationToken);
        };
    }
}
