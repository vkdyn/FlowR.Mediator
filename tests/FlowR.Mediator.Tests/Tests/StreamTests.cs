using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class StreamTests
{
    [TestMethod]
    public async Task CreateStream_ShouldReturnAllItems()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        List<int> values = new();

        await foreach (int value in mediator.CreateStream(new CountToStreamRequest(4)))
        {
            values.Add(value);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, values);
    }

    [TestMethod]
    public async Task CreateStream_ShouldReturnEmpty_WhenCountIsZero()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        List<int> values = new();

        await foreach (int value in mediator.CreateStream(new CountToStreamRequest(0)))
        {
            values.Add(value);
        }

        Assert.AreEqual(0, values.Count);
    }
}
