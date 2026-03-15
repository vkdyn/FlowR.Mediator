using FlowR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FlowR.Internal;

/// <summary>
/// High-performance handler cache using compiled expression trees.
/// Handlers are resolved once and cached — subsequent calls are near zero overhead.
/// </summary>
internal static class HandlerCache
{
    // Cache delegates: requestType -> compiled pipeline func
    private static readonly ConcurrentDictionary<Type, Delegate> _requestHandlers = new();
    private static readonly ConcurrentDictionary<Type, Delegate> _streamHandlers = new();

    internal static Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>> GetOrCreateRequestHandler<TResponse>(
        IServiceProvider serviceProvider,
        Type requestType)
    {
        var cached = _requestHandlers.GetOrAdd(requestType, static (rt, sp) =>
        {
            // Build: (request, sp, ct) => ExecutePipeline<TRequest, TResponse>(request, sp, ct)
            var method = typeof(HandlerCache)
                .GetMethod(nameof(ExecutePipeline), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(rt, GetResponseType(rt));

            var requestParam = Expression.Parameter(typeof(IBaseRequest), "request");
            var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var call = Expression.Call(method,
                Expression.Convert(requestParam, rt),
                spParam,
                ctParam);

            return Expression.Lambda(call, requestParam, spParam, ctParam).Compile();
        }, serviceProvider);

        return (Func<IBaseRequest, IServiceProvider, CancellationToken, Task<TResponse>>)cached;
    }

    internal static Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>> GetOrCreateStreamHandler<TResponse>(
        IServiceProvider serviceProvider,
        Type requestType)
    {
        var cached = _streamHandlers.GetOrAdd(requestType, static (rt, sp) =>
        {
            var method = typeof(HandlerCache)
                .GetMethod(nameof(ExecuteStream), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(rt, GetStreamResponseType(rt));

            var requestParam = Expression.Parameter(typeof(IBaseRequest), "request");
            var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var call = Expression.Call(method,
                Expression.Convert(requestParam, rt),
                spParam,
                ctParam);

            return Expression.Lambda(call, requestParam, spParam, ctParam).Compile();
        }, serviceProvider);

        return (Func<IBaseRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TResponse>>)cached;
    }

    private static async Task<TResponse> ExecutePipeline<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{typeof(TRequest).FullName}'. " +
                $"Register it via AddFlowR() and ensure your handler implements IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToList();

        if (behaviors.Count == 0)
            return await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);

        // Build pipeline from innermost (handler) to outermost (first behavior)
        RequestHandlerDelegate<TResponse> pipeline = () =>
            handler.HandleAsync(request, cancellationToken);

        pipeline = behaviors.Aggregate(pipeline, (next, behavior) =>
            () => behavior.HandleAsync(request, next, cancellationToken));

        return await pipeline().ConfigureAwait(false);
    }

    private static IAsyncEnumerable<TResponse> ExecuteStream<TRequest, TResponse>(
        TRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IStreamRequest<TResponse>
    {
        var handler = serviceProvider.GetService<IStreamRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No stream handler registered for '{typeof(TRequest).FullName}'. " +
                $"Register a handler implementing IStreamRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");

        return handler.HandleAsync(request, cancellationToken);
    }

    private static Type GetResponseType(Type requestType)
    {
        return requestType
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];
    }

    private static Type GetStreamResponseType(Type requestType)
    {
        return requestType
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>))
            .GetGenericArguments()[0];
    }
}
