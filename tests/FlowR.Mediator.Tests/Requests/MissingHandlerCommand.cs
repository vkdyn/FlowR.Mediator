namespace FlowR.Mediator.Tests.Requests;

public sealed record MissingHandlerCommand(string Value) : IRequest<string>;
