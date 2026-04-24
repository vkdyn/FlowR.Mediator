using FlowR.Mediator.Pipeline;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Models;
using FlowR.Mediator.Tests.Notifications;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class PipelineBehaviourTests
{
    [TestMethod]
    public async Task Send_ShouldRunPipelineBehaviourAroundHandler()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateWithPipelineBehaviours();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        TestLog log = serviceProvider.GetRequiredService<TestLog>();

        PingResult result = await mediator.Send(new PingCommand("pipeline")).ConfigureAwait(false);

        Assert.AreEqual("PIPELINE", result.Value);

        List<string> messages = log.Messages.ToList();

        CollectionAssert.Contains(messages, "before:PingCommand");
        CollectionAssert.Contains(messages, "handler:PingCommand");
        CollectionAssert.Contains(messages, "after:PingCommand");

        Assert.IsTrue(
            messages.IndexOf("before:PingCommand") < messages.IndexOf("handler:PingCommand"));

        Assert.IsTrue(
            messages.IndexOf("handler:PingCommand") < messages.IndexOf("after:PingCommand"));
    }

    [TestMethod]
    public async Task Pipeline_Should_Run_In_Correct_Order()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateWithPipelineBehaviours();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        TestLog log = serviceProvider.GetRequiredService<TestLog>();

        await mediator.Send(new PingCommand("x")).ConfigureAwait(false);

        List<string> messages = log.Messages
            .Where(message =>
                message == "before:PingCommand" ||
                message == "handler:PingCommand" ||
                message == "after:PingCommand")
            .ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                "before:PingCommand",
                "handler:PingCommand",
                "after:PingCommand"
            },
            messages);
    }

    [TestMethod]
    public async Task Stream_Should_Respect_Cancellation()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await AssertEx.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (int _ in mediator.CreateStream(
                               new CountToStreamRequest(10),
                               cancellationTokenSource.Token))
            {
            }
        });
    }

    [TestMethod]
    public async Task PublishAsync_Parallel_Should_Throw_Immediately()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        await AssertEx.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.PublishAsync(
                    new FailingNotification(),
                    NotificationPublishStrategy.Parallel)
                .ConfigureAwait(false);
        });
    }
}