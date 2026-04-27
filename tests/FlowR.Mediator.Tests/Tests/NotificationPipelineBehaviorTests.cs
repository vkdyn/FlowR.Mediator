using FlowR.Mediator.Extensions;
using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Behaviours;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class NotificationPipelineBehaviorTests
{
    private static ServiceProvider CreateWithNotificationBehavior()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(typeof(TestServiceFactory).Assembly);
        services.AddTransient(
            typeof(INotificationPipelineBehavior<>),
            typeof(TestNotificationPipelineBehavior<>));
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task NotificationPipelineBehavior_RunsAroundHandlers()
    {
        ServiceProvider sp = CreateWithNotificationBehavior();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Publish(new OrderCreatedNotification("ORD-BEH"));

        List<string> messages = log.Messages.ToList();

        CollectionAssert.Contains(messages, "notification-before:OrderCreatedNotification");
        CollectionAssert.Contains(messages, "notification-after:OrderCreatedNotification");
    }

    [TestMethod]
    public async Task NotificationPipelineBehavior_RunsBeforeAndAfterEachHandler()
    {
        ServiceProvider sp = CreateWithNotificationBehavior();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Publish(new OrderCreatedNotification("ORD-ORDER"));

        List<string> messages = log.Messages.ToList();
        int beforeIdx = messages.IndexOf("notification-before:OrderCreatedNotification");
        int afterIdx  = messages.LastIndexOf("notification-after:OrderCreatedNotification");

        Assert.IsTrue(beforeIdx >= 0, "before not found");
        Assert.IsTrue(afterIdx  >= 0, "after not found");
        Assert.IsTrue(beforeIdx < afterIdx, "before must precede after");
    }
}
