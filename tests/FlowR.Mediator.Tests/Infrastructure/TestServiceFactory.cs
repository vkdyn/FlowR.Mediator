using FlowR.Mediator.Extensions;
using FlowR.Mediator.Tests.Behaviours;
using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator.Tests.Infrastructure;

public static class TestServiceFactory
{
    public static ServiceProvider CreateDefault()
    {
        ServiceCollection services = new();

        services.AddSingleton<TestLog>();
        services.AddFlowR(typeof(TestServiceFactory).Assembly);

        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithPipelineBehaviours()
    {
        ServiceCollection services = new();

        services.AddSingleton<TestLog>();
        services.AddFlowR(typeof(TestServiceFactory).Assembly);

        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateEmptyMediator()
    {
        ServiceCollection services = new();

        services.AddFlowR();

        return services.BuildServiceProvider();
    }
}
