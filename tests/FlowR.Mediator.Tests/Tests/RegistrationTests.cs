using FlowR.Mediator.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class RegistrationTests
{
    [TestMethod]
    public void AddFlowR_ShouldRegisterMediatorSenderAndPublisher()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();

        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        ISender sender = serviceProvider.GetRequiredService<ISender>();
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Assert.IsNotNull(mediator);
        Assert.IsNotNull(sender);
        Assert.IsNotNull(publisher);

        Assert.IsInstanceOfType(sender, typeof(IMediator));
        Assert.IsInstanceOfType(publisher, typeof(IMediator));
    }
}