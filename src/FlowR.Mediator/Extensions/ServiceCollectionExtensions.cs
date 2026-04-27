using FlowR.Mediator.Behaviors;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowR.Mediator.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register FlowR.Mediator — drop-in for MediatR's AddMediatR(assemblies).
    /// </summary>
    public static IServiceCollection AddFlowR(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddFlowR(_ => { }, assemblies);

    /// <summary>
    /// Register FlowR.Mediator with full options control.
    /// </summary>
    public static IServiceCollection AddFlowR(
        this IServiceCollection services,
        Action<FlowROptions> configure,
        params Assembly[] assemblies)
    {
        FlowROptions options = new();
        configure(options);

        services.Add(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(ISender),    sp => sp.GetRequiredService<IMediator>(), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), options.MediatorLifetime));

        // ServiceFactory for manual service resolution (MediatR compat pattern)
        services.AddTransient<ServiceFactory>(sp => sp.GetService!);

        if (assemblies.Length == 0)
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry is not null) assemblies = [entry];
        }

        foreach (Assembly assembly in assemblies.Distinct())
            services.RegisterHandlersFromAssembly(assembly, options);

        // Auto-register pre/post processor pipeline behaviors when opted in
        if (options.RegisterProcessorBehaviors)
        {
            services.Add(new ServiceDescriptor(
                typeof(IPipelineBehavior<,>),
                typeof(RequestPreProcessorBehavior<,>),
                options.BehaviorLifetime));

            services.Add(new ServiceDescriptor(
                typeof(IPipelineBehavior<,>),
                typeof(RequestPostProcessorBehavior<,>),
                options.BehaviorLifetime));
        }

        return services;
    }

    // ── MediatR drop-in aliases ───────────────────────────────────────────────

    /// <summary>Exact drop-in for: services.AddMediatR(assemblies)</summary>
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddFlowR(assemblies);

    /// <summary>Exact drop-in for: services.AddMediatR(cfg => { ... }, assemblies)</summary>
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Action<FlowROptions> configure,
        params Assembly[] assemblies)
        => services.AddFlowR(configure, assemblies);

    /// <summary>FlowR v1 alias.</summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddFlowR(assemblies);

    // ── Behavior registration helpers ────────────────────────────────────────

    /// <summary>
    /// Register a closed or open-generic pipeline behavior.
    /// Equivalent to MediatR's cfg.AddBehavior() / cfg.AddOpenBehavior().
    /// </summary>
    public static IServiceCollection AddFlowRBehavior(
        this IServiceCollection services,
        Type behaviorType,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, lifetime));
        return services;
    }

    // ── Assembly scanning ─────────────────────────────────────────────────────

    private static void RegisterHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        FlowROptions options)
    {
        IEnumerable<Type> types = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (Type type in types)
        {
            foreach (Type iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                Type def = iface.GetGenericTypeDefinition();

                // Handlers
                if (def == typeof(IRequestHandler<,>)       ||
                    def == typeof(IRequestHandler<>)         ||
                    def == typeof(INotificationHandler<>)    ||
                    def == typeof(IStreamRequestHandler<,>)  ||
                    def == typeof(IRequestPreProcessor<>)    ||
                    def == typeof(IRequestPostProcessor<,>))
                {
                    services.Add(new ServiceDescriptor(iface, type, options.HandlerLifetime));
                }
                // Behaviors
                else if (def == typeof(IPipelineBehavior<,>) ||
                         def == typeof(INotificationPipelineBehavior<>))
                {
                    Type serviceType = type.ContainsGenericParameters ? def : iface;
                    services.Add(new ServiceDescriptor(serviceType, type, options.BehaviorLifetime));
                }
            }
        }
    }
}
