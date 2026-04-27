namespace FlowR.Mediator.Extensions;

/// <summary>
/// MediatR-compatible service factory delegate.
/// Used when you need to resolve services manually inside custom code.
/// Equivalent to MediatR's ServiceFactory.
/// </summary>
public delegate object? ServiceFactory(Type serviceType);

public static class ServiceFactoryExtensions
{
    public static T? GetInstance<T>(this ServiceFactory factory) where T : class
        => factory(typeof(T)) as T;

    public static IEnumerable<T> GetInstances<T>(this ServiceFactory factory) where T : class
        => (IEnumerable<T>)(factory(typeof(IEnumerable<T>)) ?? Array.Empty<T>());
}
