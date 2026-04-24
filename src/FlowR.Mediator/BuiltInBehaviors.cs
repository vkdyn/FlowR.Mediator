using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FlowR.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution time and details.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[FlowR] Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[FlowR] Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[FlowR] Error handling {RequestName} after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Pipeline behavior that measures performance and warns on slow requests.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _thresholdMs;

    /// <param name="logger">Logger instance.</param>
    /// <param name="thresholdMs">Threshold in milliseconds before a warning is logged. Default: 500ms.</param>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int thresholdMs = 500)
    {
        _logger = logger;
        _thresholdMs = thresholdMs;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        sw.Stop();

        if (sw.ElapsedMilliseconds > _thresholdMs)
        {
            _logger.LogWarning(
                "[FlowR] Slow request detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). Request: {@Request}",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, _thresholdMs, request);
        }

        return response;
    }
}

/// <summary>
/// Pipeline behavior that retries failed requests.
/// </summary>
public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    /// <param name="logger">Logger.</param>
    /// <param name="maxRetries">Maximum number of retries. Default: 3.</param>
    /// <param name="delayMs">Delay between retries in ms. Default: 100ms.</param>
    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger, int maxRetries = 3, int delayMs = 100)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _delay = TimeSpan.FromMilliseconds(delayMs);
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                _logger.LogWarning(ex, "[FlowR] Retry attempt {Attempt}/{MaxRetries} for {RequestName}",
                    attempt, _maxRetries, typeof(TRequest).Name);
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

/// <summary>
/// Pipeline behavior that catches exceptions and wraps them in a result type.
/// Implement IExceptionHandler to customize exception handling.
/// </summary>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IFlowRExceptionHandler<TRequest, TResponse>> _exceptionHandlers;

    public ExceptionHandlingBehavior(
        ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger,
        IEnumerable<IFlowRExceptionHandler<TRequest, TResponse>> exceptionHandlers)
    {
        _logger = logger;
        _exceptionHandlers = exceptionHandlers;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            foreach (var handler in _exceptionHandlers)
            {
                var (handled, response) = await handler.TryHandleAsync(request, ex, cancellationToken).ConfigureAwait(false);
                if (handled)
                    return response!;
            }

            _logger.LogError(ex, "[FlowR] Unhandled exception in {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}

/// <summary>
/// Implement this to handle specific exceptions in the pipeline.
/// </summary>
public interface IFlowRExceptionHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<(bool Handled, TResponse? Response)> TryHandleAsync(
        TRequest request,
        Exception exception,
        CancellationToken cancellationToken);
}
