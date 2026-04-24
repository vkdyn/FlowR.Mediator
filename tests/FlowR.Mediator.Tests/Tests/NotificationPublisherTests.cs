using FlowR.Mediator;
using FlowR.Mediator.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class NotificationPublisherTests
{
    [TestMethod]
    public async Task NotificationPublisher_Should_Invoke_All_Handlers()
    {
        List<string> calls = new();

        List<NotificationHandlerExecutor> handlers =
        [
            new((notification, cancellationToken) =>
            {
                calls.Add("handler1");
                return Task.CompletedTask;
            }),
            new((notification, cancellationToken) =>
            {
                calls.Add("handler2");
                return Task.CompletedTask;
            })
        ];

        INotificationPublisher publisher = new TestNotificationPublisher();

        await publisher.Publish(
            handlers,
            new TestNotification(),
            CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { "handler1", "handler2" },
            calls);
    }

    [TestMethod]
    public async Task NotificationPublisher_Should_Throw_When_Handler_Fails()
    {
        List<NotificationHandlerExecutor> handlers =
        [
            new((notification, cancellationToken) =>
            {
                throw new InvalidOperationException("boom");
            })
        ];

        INotificationPublisher publisher = new TestNotificationPublisher();

        await AssertEx.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await publisher.Publish(
                handlers,
                new TestNotification(),
                CancellationToken.None);
        });
    }

    private sealed class TestNotificationPublisher : INotificationPublisher
    {
        public async Task Publish(
            IEnumerable<NotificationHandlerExecutor> handlers,
            INotification notification,
            CancellationToken cancellationToken)
        {
            foreach (NotificationHandlerExecutor handler in handlers)
            {
                await handler.Execute(notification, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private sealed record TestNotification() : INotification;
}