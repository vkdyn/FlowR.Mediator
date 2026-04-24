using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowR.Mediator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowR(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddFlowR(_ => { }, assemblies);
    }

    public static IServiceCollection AddFlowR(this IServiceCollection services, Action<FlowROptions> configure, params Assembly[] assemblies)
    {
        FlowROptions options = new();
        configure(options);

        services.Add(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(ISender), serviceProvider => serviceProvider.GetRequiredService<IMediator>(), options.MediatorLifetime));
        services.Add(new ServiceDescriptor(typeof(IPublisher), serviceProvider => serviceProvider.GetRequiredService<IMediator>(), options.MediatorLifetime));

        if (assemblies.Length == 0)
        {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
            {
                assemblies = [entryAssembly];
            }
        }

        foreach (Assembly assembly in assemblies.Distinct())
        {
            services.RegisterHandlersFromAssembly(assembly, options);
        }

        return services;
    }

    public static IServiceCollection AddMediatR(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddFlowR(assemblies);
    }

    public static IServiceCollection AddFlowRBehavior(this IServiceCollection services, Type behaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, lifetime));
        return services;
    }

    private static void RegisterHandlersFromAssembly(this IServiceCollection services, Assembly assembly, FlowROptions options)
    {
        IEnumerable<Type> types = assembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (Type type in types)
        {
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericDefinition = interfaceType.GetGenericTypeDefinition();

                if (genericDefinition == typeof(IRequestHandler<,>) ||
                    genericDefinition == typeof(IRequestHandler<>) ||
                    genericDefinition == typeof(INotificationHandler<>) ||
                    genericDefinition == typeof(IStreamRequestHandler<,>))
                {
                    services.Add(new ServiceDescriptor(interfaceType, type, options.HandlerLifetime));
                }
                else if (genericDefinition == typeof(IPipelineBehavior<,>) ||
                         genericDefinition == typeof(INotificationPipelineBehavior<>))
                {
                    Type serviceType = type.ContainsGenericParameters ? genericDefinition : interfaceType;
                    services.Add(new ServiceDescriptor(serviceType, type, options.BehaviorLifetime));
                }
            }
        }
    }
}
