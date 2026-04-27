using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class FireAndForgetPublishTests
{
    [TestMethod]
    public async Task PublishAsync_FireAndForget_DoesNotAwait_ButEventuallyRuns()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        // Fire and forget — returns immediately without awaiting handlers
        await mediator.PublishAsync(
            new OrderCreatedNotification("ORD-FF"),
            NotificationPublishStrategy.FireAndForget);

        // Give the background task time to complete
        await Task.Delay(200);

        CollectionAssert.Contains(log.Messages.ToList(), "email:ORD-FF");
        CollectionAssert.Contains(log.Messages.ToList(), "audit-notification:ORD-FF");
    }

    [TestMethod]
    public async Task PublishAsync_Sequential_AwaitsAllHandlers_BeforeReturning()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.PublishAsync(
            new OrderCreatedNotification("ORD-SEQ"),
            NotificationPublishStrategy.Sequential);

        // Sequential publish returns only after all handlers have completed
        CollectionAssert.Contains(log.Messages.ToList(), "email:ORD-SEQ");
        CollectionAssert.Contains(log.Messages.ToList(), "audit-notification:ORD-SEQ");
    }

    [TestMethod]
    public async Task PublishAsync_Parallel_RunsAllHandlers()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.PublishAsync(
            new OrderCreatedNotification("ORD-PAR"),
            NotificationPublishStrategy.Parallel);

        CollectionAssert.Contains(log.Messages.ToList(), "email:ORD-PAR");
        CollectionAssert.Contains(log.Messages.ToList(), "audit-notification:ORD-PAR");
    }

    [TestMethod]
    public async Task PublishAsync_ParallelNoThrow_CollectsAllExceptionsInAggregateException()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        AggregateException ex = await AssertEx.ThrowsAsync<AggregateException>(async () =>
        {
            await mediator.PublishAsync(
                new FailingNotification(),
                NotificationPublishStrategy.ParallelNoThrow);
        });

        Assert.IsTrue(ex.InnerExceptions.Count >= 1);
        Assert.IsTrue(ex.InnerExceptions.OfType<InvalidOperationException>().Any());
    }
}
