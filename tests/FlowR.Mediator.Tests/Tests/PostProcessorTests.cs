using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class PostProcessorTests
{
    [TestMethod]
    public async Task PostProcessor_RunsAfterHandler()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("hello"));

        List<string> messages = log.Messages.ToList();
        int handlerIndex = messages.IndexOf("handler:hello");
        int postIndex    = messages.IndexOf("post:HELLO");   // handler uppercases the value

        Assert.IsTrue(handlerIndex >= 0, "Handler did not run.");
        Assert.IsTrue(postIndex >= 0,    "Post-processor did not run.");
        Assert.IsTrue(handlerIndex < postIndex, "Handler must run before post-processor.");
    }

    [TestMethod]
    public async Task PostProcessor_ReceivesHandlerResponse()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        string result = await mediator.Send(new ProcessedCommand("test"));

        // Handler returns "TEST" (uppercased), post-processor logs "post:TEST"
        Assert.AreEqual("TEST", result);
        CollectionAssert.Contains(log.Messages.ToList(), "post:TEST");
    }

    [TestMethod]
    public async Task PostProcessor_RegisteredViaOptions_RunsAfterHandler()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviorsViaOptions();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("opts"));

        List<string> messages = log.Messages.ToList();
        Assert.IsTrue(messages.IndexOf("handler:opts") < messages.IndexOf("post:OPTS"),
            "Post-processor registered via options must run after handler.");
    }

    [TestMethod]
    public async Task PostProcessor_DoesNotRunWhenNotRegisteredInPipeline()
    {
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("nopost"));

        CollectionAssert.DoesNotContain(log.Messages.ToList(), "post:NOPOST");
    }

    [TestMethod]
    public async Task PreAndPostProcessor_RunInCorrectOrder()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("order"));

        List<string> messages = log.Messages.ToList();
        int preIndex     = messages.IndexOf("pre:order");
        int handlerIndex = messages.IndexOf("handler:order");
        int postIndex    = messages.IndexOf("post:ORDER");

        Assert.IsTrue(preIndex     >= 0, "Pre-processor missing.");
        Assert.IsTrue(handlerIndex >= 0, "Handler missing.");
        Assert.IsTrue(postIndex    >= 0, "Post-processor missing.");

        Assert.IsTrue(preIndex < handlerIndex,  "pre → handler order wrong.");
        Assert.IsTrue(handlerIndex < postIndex, "handler → post order wrong.");
    }
}
