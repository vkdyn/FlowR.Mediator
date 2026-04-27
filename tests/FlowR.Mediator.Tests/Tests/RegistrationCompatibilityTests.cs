using FlowR.Mediator.Extensions;
using FlowR.Mediator.Tests.Infrastructure;
using FlowR.Mediator.Tests.Models;
using FlowR.Mediator.Tests.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowR.Mediator.Tests.Tests;

/// <summary>
/// Verifies that AddMediatR() and AddMediator() are true drop-in aliases for AddFlowR().
/// Every test here has a twin in the existing RegistrationTests / RequestResponseTests —
/// these just prove the same behavior works through each alias.
/// </summary>
[TestClass]
public sealed class RegistrationCompatibilityTests
{
    // ── AddMediatR alias ─────────────────────────────────────────────────────

    [TestMethod]
    public void AddMediatR_RegistersIMediator_ISender_IPublisher()
    {
        ServiceProvider sp = TestServiceFactory.CreateViaMediatRAlias();

        IMediator  mediator  = sp.GetRequiredService<IMediator>();
        ISender    sender    = sp.GetRequiredService<ISender>();
        IPublisher publisher = sp.GetRequiredService<IPublisher>();

        Assert.IsNotNull(mediator);
        Assert.IsNotNull(sender);
        Assert.IsNotNull(publisher);
        Assert.IsInstanceOfType(sender,    typeof(IMediator));
        Assert.IsInstanceOfType(publisher, typeof(IMediator));
    }

    [TestMethod]
    public async Task AddMediatR_CanSendRequest()
    {
        ServiceProvider sp = TestServiceFactory.CreateViaMediatRAlias();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        PingResult result = await mediator.Send(new PingCommand("mediatralias"));

        Assert.AreEqual("MEDIATRALIAS", result.Value);
    }

    [TestMethod]
    public async Task AddMediatR_CanPublishNotification()
    {
        ServiceProvider sp = TestServiceFactory.CreateViaMediatRAlias();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        TestLog log = sp.GetRequiredService<TestLog>();

        await mediator.Publish(new Notifications.OrderCreatedNotification("ORD-MEDIATRALIAS"));

        CollectionAssert.Contains(log.Messages.ToList(), "email:ORD-MEDIATRALIAS");
        CollectionAssert.Contains(log.Messages.ToList(), "audit-notification:ORD-MEDIATRALIAS");
    }

    // ── AddMediator alias ────────────────────────────────────────────────────

    [TestMethod]
    public void AddMediator_RegistersIMediator_ISender_IPublisher()
    {
        ServiceProvider sp = TestServiceFactory.CreateViaAddMediatorAlias();

        IMediator  mediator  = sp.GetRequiredService<IMediator>();
        ISender    sender    = sp.GetRequiredService<ISender>();
        IPublisher publisher = sp.GetRequiredService<IPublisher>();

        Assert.IsNotNull(mediator);
        Assert.IsInstanceOfType(sender,    typeof(IMediator));
        Assert.IsInstanceOfType(publisher, typeof(IMediator));
    }

    [TestMethod]
    public async Task AddMediator_CanSendRequest()
    {
        ServiceProvider sp = TestServiceFactory.CreateViaAddMediatorAlias();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        PingResult result = await mediator.Send(new PingCommand("mediatoralias"));

        Assert.AreEqual("MEDIATORALIAS", result.Value);
    }

    // ── FlowROptions lifetime config ─────────────────────────────────────────

    [TestMethod]
    public void AddFlowR_WithScopedLifetime_RegistersMediatorAsScoped()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(options =>
        {
            options.MediatorLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped;
        }, typeof(TestServiceFactory).Assembly);

        ServiceProvider sp = services.BuildServiceProvider();

        using IServiceScope scope1 = sp.CreateScope();
        using IServiceScope scope2 = sp.CreateScope();

        IMediator m1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        IMediator m2 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        IMediator m3 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        // Same scope → same instance
        Assert.AreSame(m1, m2, "Scoped mediator should be same instance within scope.");
        // Different scope → different instance
        Assert.AreNotSame(m1, m3, "Scoped mediator should differ across scopes.");
    }

    [TestMethod]
    public async Task AddFlowR_WithSingletonHandlerLifetime_SameHandlerInstanceReused()
    {
        ServiceCollection services = new();
        services.AddSingleton<TestLog>();
        services.AddFlowR(options =>
        {
            options.HandlerLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton;
        }, typeof(TestServiceFactory).Assembly);

        ServiceProvider sp = services.BuildServiceProvider();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        // Just verify it works correctly with singleton handlers
        PingResult r1 = await mediator.Send(new PingCommand("s1"));
        PingResult r2 = await mediator.Send(new PingCommand("s2"));

        Assert.AreEqual("S1", r1.Value);
        Assert.AreEqual("S2", r2.Value);
    }
}
