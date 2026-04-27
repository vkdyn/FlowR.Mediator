using FlowR.Mediator.Extensions;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class ServiceFactoryTests
{
    [TestMethod]
    public void ServiceFactory_IsRegistered_WhenAddFlowRCalled()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();

        ServiceFactory factory = sp.GetRequiredService<ServiceFactory>();

        Assert.IsNotNull(factory);
    }

    [TestMethod]
    public void ServiceFactory_CanResolveRegisteredService()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();

        ServiceFactory factory = sp.GetRequiredService<ServiceFactory>();

        object? mediator = factory(typeof(IMediator));

        Assert.IsNotNull(mediator);
        Assert.IsInstanceOfType(mediator, typeof(IMediator));
    }

    [TestMethod]
    public void ServiceFactory_ReturnsNull_ForUnregisteredService()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();

        ServiceFactory factory = sp.GetRequiredService<ServiceFactory>();

        // IDisposable is not registered — should return null, not throw
        object? result = factory(typeof(IDisposable));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ServiceFactory_GetInstance_Extension_ResolvesTypedService()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();

        ServiceFactory factory = sp.GetRequiredService<ServiceFactory>();

        IMediator? mediator = factory.GetInstance<IMediator>();

        Assert.IsNotNull(mediator);
    }

    [TestMethod]
    public void ServiceFactory_GetInstances_Extension_ResolvesMultipleHandlers()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();

        ServiceFactory factory = sp.GetRequiredService<ServiceFactory>();

        // GetInstances should return all registered IRequestHandler<ProcessedCommand, string>
        IEnumerable<IRequestHandler<ProcessedCommand, string>> handlers =
            factory.GetInstances<IRequestHandler<ProcessedCommand, string>>();

        Assert.IsNotNull(handlers);
        Assert.IsTrue(handlers.Any(), "Expected at least one handler to be resolved.");
    }
}
