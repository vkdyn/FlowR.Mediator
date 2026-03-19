using FlowR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowR.Extensions;

/// <summary>
/// Extension methods to register FlowR with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class FlowRServiceCollectionExtensions
{
    /// <summary>
    /// Registers FlowR and scans the provided assemblies for handlers and behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for handlers. Defaults to entry assembly if none provided.</param>
    public static IServiceCollection AddFlowR(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddFlowR(configure => { }, assemblies);
    }

    /// <summary>
    /// Registers FlowR with configuration options.
    /// </summary>
    public static IServiceCollection AddFlowR(
        this IServiceCollection services,
        Action<FlowROptions> configure,
        params Assembly[] assemblies)
    {
        var options = new FlowROptions();
        configure(options);

        // Register mediator
        services.Add(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), options.MediatorLifetime));

        if (assemblies.Length == 0)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
                assemblies = [entryAssembly];
        }

        foreach (var assembly in assemblies.Distinct())
        {
            services.RegisterHandlersFromAssembly(assembly, options);
        }

        return services;
    }

    private static void RegisterHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        FlowROptions options)
    {
        var types = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in types)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType) continue;
                var genericDef = @interface.GetGenericTypeDefinition();

                // IRequestHandler<TRequest, TResponse>
                if (genericDef == typeof(IRequestHandler<,>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.HandlerLifetime));
                }
                // IRequestHandler<TRequest> (void)
                else if (genericDef == typeof(IRequestHandler<>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.HandlerLifetime));
                    // Also register as IRequestHandler<TRequest, Unit>
                    var requestType = @interface.GetGenericArguments()[0];
                    var unitHandlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(Unit));
                    services.Add(new ServiceDescriptor(unitHandlerInterface, type, options.HandlerLifetime));
                }
                // INotificationHandler<TNotification>
                else if (genericDef == typeof(INotificationHandler<>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.HandlerLifetime));
                }
                // IStreamRequestHandler<TRequest, TResponse>
                else if (genericDef == typeof(IStreamRequestHandler<,>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.HandlerLifetime));
                }
                // IPipelineBehavior<TRequest, TResponse>
                else if (genericDef == typeof(IPipelineBehavior<,>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.BehaviorLifetime));
                }
                // INotificationPipelineBehavior<TNotification>
                else if (genericDef == typeof(INotificationPipelineBehavior<>))
                {
                    services.Add(new ServiceDescriptor(@interface, type, options.BehaviorLifetime));
                }
            }
        }
    }

    /// <summary>
    /// Manually adds a specific pipeline behavior (open generic).
    /// Example: services.AddFlowRBehavior(typeof(LoggingBehavior&lt;,&gt;))
    /// </summary>
    public static IServiceCollection AddFlowRBehavior(
        this IServiceCollection services,
        Type behaviorType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, lifetime));
        return services;
    }
}

/// <summary>
/// Configuration options for FlowR.
/// </summary>
public sealed class FlowROptions
{
    /// <summary>
    /// Lifetime of the IMediator service. Default: Transient.
    /// </summary>
    public ServiceLifetime MediatorLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Lifetime of request and notification handlers. Default: Transient.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Lifetime of pipeline behaviors. Default: Transient.
    /// </summary>
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Transient;
}
