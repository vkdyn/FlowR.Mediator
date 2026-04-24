namespace FlowR.Mediator.Tests.Notifications;

public sealed record OrderCreatedNotification(string OrderId) : INotification;
