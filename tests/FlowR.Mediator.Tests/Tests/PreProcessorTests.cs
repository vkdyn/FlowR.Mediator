using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

[TestClass]
public sealed class PreProcessorTests
{
    [TestMethod]
    public async Task PreProcessor_RunsBeforeHandler()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("hello"));

        List<string> messages = log.Messages.ToList();
        int preIndex     = messages.IndexOf("pre:hello");
        int handlerIndex = messages.IndexOf("handler:hello");

        Assert.IsTrue(preIndex >= 0,     "Pre-processor did not run.");
        Assert.IsTrue(handlerIndex >= 0, "Handler did not run.");
        Assert.IsTrue(preIndex < handlerIndex, "Pre-processor must run before handler.");
    }

    [TestMethod]
    public async Task PreProcessor_RunsEvenWhenNoPostProcessor()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("world"));

        CollectionAssert.Contains(log.Messages.ToList(), "pre:world");
    }

    [TestMethod]
    public async Task PreProcessor_RegisteredViaOptions_RunsBeforeHandler()
    {
        // Verify the RegisterProcessorBehaviors opt-in flag works identically
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviorsViaOptions();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("opts"));

        List<string> messages = log.Messages.ToList();
        Assert.IsTrue(messages.IndexOf("pre:opts") < messages.IndexOf("handler:opts"),
            "Pre-processor registered via options must run before handler.");
    }

    [TestMethod]
    public async Task PreProcessor_DoesNotRunWhenNotRegisteredInPipeline()
    {
        // CreateDefault() does NOT register pre/post processor behaviors
        ServiceProvider sp = TestServiceFactory.CreateDefault();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("nopre"));

        // Pre-processor should NOT appear since the behavior was not registered
        CollectionAssert.DoesNotContain(log.Messages.ToList(), "pre:nopre");
    }

    [TestMethod]
    public async Task PreProcessor_ReceivesCorrectRequest()
    {
        ServiceProvider sp = TestServiceFactory.CreateWithProcessorBehaviors();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Send(new ProcessedCommand("specific-value"));

        CollectionAssert.Contains(log.Messages.ToList(), "pre:specific-value");
    }
}
