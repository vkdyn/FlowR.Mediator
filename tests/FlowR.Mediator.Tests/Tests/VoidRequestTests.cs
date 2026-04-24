using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class VoidRequestTests
{
    [TestMethod]
    public async Task Send_ShouldSupportVoidRequest()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        TestLog log = serviceProvider.GetRequiredService<TestLog>();

        await mediator.Send(new SaveAuditCommand("A-100")).ConfigureAwait(false);

        CollectionAssert.Contains(log.Messages.ToList(), "audit:A-100");
    }

    [TestMethod]
    public async Task SendAsync_ShouldSupportVoidRequest()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        TestLog log = serviceProvider.GetRequiredService<TestLog>();

        await mediator.SendAsync(new SaveAuditCommand("A-200")).ConfigureAwait(false);

        CollectionAssert.Contains(log.Messages.ToList(), "audit:A-200");
    }
}
