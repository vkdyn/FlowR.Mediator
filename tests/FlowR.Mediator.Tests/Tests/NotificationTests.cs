using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class NotificationTests
{
    [TestMethod]
    public async Task Publish_ShouldCallAllNotificationHandlers()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        TestLog log = serviceProvider.GetRequiredService<TestLog>();

        await mediator.Publish(new OrderCreatedNotification("ORD-1")).ConfigureAwait(false);

        CollectionAssert.Contains(log.Messages.ToList(), "email:ORD-1");
        CollectionAssert.Contains(log.Messages.ToList(), "audit-notification:ORD-1");
    }

    [TestMethod]
    public async Task PublishAsync_ShouldDoNothing_WhenNotificationHasNoHandlers()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new NoHandlerNotification("nothing")).ConfigureAwait(false);

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task PublishAsync_WithParallelNoThrow_ShouldThrowAggregateException()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        AggregateException exception = await AssertEx.ThrowsAsync<AggregateException>(async () =>
        {
            await mediator.PublishAsync(
                    new FailingNotification(),
                    NotificationPublishStrategy.ParallelNoThrow)
                .ConfigureAwait(false);
        });

        Assert.IsTrue(exception.InnerExceptions.Count >= 1);
        Assert.IsTrue(exception.InnerExceptions.Any(inner => inner is InvalidOperationException));
    }

    [TestMethod]
    public async Task PublishAsync_WithSequential_ShouldPropagateHandlerException()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        InvalidOperationException exception = await AssertEx.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.PublishAsync(
                    new FailingNotification(),
                    NotificationPublishStrategy.Sequential)
                .ConfigureAwait(false);
        });

        Assert.AreEqual("Expected notification failure.", exception.Message);
    }

    [TestMethod]
    public async Task Publish_Should_Run_All_Handlers()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Publish(new OrderCreatedNotification("1"));

        // Filter to only the handler messages — notification pipeline behaviors
        // (registered by other tests via assembly scanning) add extra entries
        List<string> handlerMessages = log.Messages
            .Where(m => m == "email:1" || m == "audit-notification:1")
            .ToList();

        Assert.AreEqual(2, handlerMessages.Count);
    }
}
