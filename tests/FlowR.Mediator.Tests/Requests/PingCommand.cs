using FlowR.Mediator.Tests.Models;

namespace FlowR.Mediator.Tests.Requests;

public sealed record PingCommand(string Value) : IRequest<PingResult>;
