namespace FlowR.Mediator.Tests.Requests;

public sealed record ProcessedCommand(string Value) : IRequest<string>;
