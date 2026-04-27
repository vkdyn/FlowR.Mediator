namespace FlowR.Mediator.Tests.Requests;

public sealed record ServiceFactoryCommand(string Value) : IRequest<string>;
