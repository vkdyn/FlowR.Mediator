using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Models;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class RequestResponseTests
{
    [TestMethod]
    public async Task Send_ShouldResolveRequestHandlerAndReturnResponse()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        PingResult result = await mediator.Send(new PingCommand("hello"));

        Assert.AreEqual("HELLO", result.Value);
    }

    [TestMethod]
    public async Task SendAsync_ShouldResolveRequestHandlerAndReturnResponse()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        PingResult result = await mediator.SendAsync(new PingCommand("async"));

        Assert.AreEqual("ASYNC", result.Value);
    }

    [TestMethod]
    public async Task Send_ShouldThrowClearException_WhenRequestHandlerIsMissing()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        InvalidOperationException exception = await AssertEx.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.Send(new MissingHandlerCommand("missing")).ConfigureAwait(false);
        });

        StringAssert.Contains(exception.Message, "No request handler registered");
    }

    [TestMethod]
    public async Task Send_ShouldPropagateHandlerException()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

        InvalidOperationException exception = await AssertEx.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.Send(new ExceptionCommand()).ConfigureAwait(false);
        });

        Assert.AreEqual("Expected command failure.", exception.Message);
    }

    [TestMethod]
    public async Task Send_ShouldThrowOperationCancelled_WhenCancellationTokenIsCancelled()
    {
        ServiceProvider serviceProvider = TestServiceFactory.CreateDefault();
        IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
        CancellationTokenSource cancellationTokenSource = new();

        cancellationTokenSource.Cancel();

        await AssertEx.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await mediator.Send(new CancellableCommand(), cancellationTokenSource.Token).ConfigureAwait(false);
        });
    }
}
