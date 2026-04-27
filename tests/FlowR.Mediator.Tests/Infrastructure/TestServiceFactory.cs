using FlowR.Mediator.Behaviors;
using FlowR.Mediator.Extensions;
using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator.Tests.Infrastructure;

public static class TestServiceFactory
{
    /// <summary>Default setup — all handlers in this assembly, no extra behaviors.</summary>
    public static ServiceProvider CreateDefault()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(typeof(TestServiceFactory).Assembly);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Includes exactly one TestPipelineBehaviour for pipeline order tests.
    /// Does NOT use assembly scanning to avoid double-registration of behaviors
    /// that are discovered both by the scan and the explicit AddTransient call.
    /// </summary>
    public static ServiceProvider CreateWithPipelineBehaviours()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();

        // Register mediator + handlers only (no behavior scanning)
        services.AddFlowR(options =>
        {
            options.BehaviorLifetime = ServiceLifetime.Transient;
        }, typeof(TestServiceFactory).Assembly);

        // Remove any behaviors the scan may have picked up, then add exactly one
        ServiceDescriptor[] scannedBehaviors = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .ToArray();
        foreach (ServiceDescriptor b in scannedBehaviors)
            services.Remove(b);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestPipelineBehaviour<,>));

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registers RequestPreProcessorBehavior and RequestPostProcessorBehavior explicitly.
    /// </summary>
    public static ServiceProvider CreateWithProcessorBehaviors()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(typeof(TestServiceFactory).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
        return services.BuildServiceProvider();
    }

    /// <summary>Uses the RegisterProcessorBehaviors opt-in flag on FlowROptions.</summary>
    public static ServiceProvider CreateWithProcessorBehaviorsViaOptions()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(options =>
        {
            options.RegisterProcessorBehaviors = true;
        }, typeof(TestServiceFactory).Assembly);
        return services.BuildServiceProvider();
    }

    /// <summary>Mediator only, no handlers — for error-path tests.</summary>
    public static ServiceProvider CreateEmptyMediator()
    {
        ServiceCollection services = new();
        services.AddFlowR();
        return services.BuildServiceProvider();
    }

    /// <summary>Uses AddMediatR alias — verifies drop-in compat registration.</summary>
    public static ServiceProvider CreateViaMediatRAlias()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddMediatR(typeof(TestServiceFactory).Assembly);
        return services.BuildServiceProvider();
    }

    /// <summary>Uses AddMediator alias — verifies FlowR v1 compat registration.</summary>
    public static ServiceProvider CreateViaAddMediatorAlias()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddMediator(typeof(TestServiceFactory).Assembly);
        return services.BuildServiceProvider();
    }
}
