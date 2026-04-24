using FlowR.Mediator.Pipeline;

namespace FlowR.Mediator.Behaviors;

/// <summary>
/// Marker interface for validators. Implement with FluentValidation or your own validation logic.
/// </summary>
public interface IFlowRValidator<in TRequest>
{
    /// <summary>
    /// Validates the request. Return empty enumerable for valid, errors otherwise.
    /// </summary>
    Task<IEnumerable<ValidationError>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a validation error.
/// </summary>
public sealed record ValidationError(string PropertyName, string ErrorMessage);

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public sealed class FlowRValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public FlowRValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public override string ToString() =>
        $"FlowRValidationException: {string.Join("; ", Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}";
}

/// <summary>
/// Pipeline behavior that runs all registered validators for a request.
/// Throws <see cref="FlowRValidationException"/> if any validators fail.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IFlowRValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IFlowRValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var validationTasks = _validators.Select(v => v.ValidateAsync(request, cancellationToken));
        var results = await Task.WhenAll(validationTasks).ConfigureAwait(false);

        var errors = results
            .SelectMany(r => r)
            .Where(e => e != null)
            .ToList();

        if (errors.Count > 0)
            throw new FlowRValidationException(errors);

        return await next().ConfigureAwait(false);
    }
}
