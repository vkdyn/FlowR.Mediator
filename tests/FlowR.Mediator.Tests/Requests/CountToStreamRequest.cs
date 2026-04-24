namespace FlowR.Mediator.Tests.Requests;

public sealed record CountToStreamRequest(int Count) : IStreamRequest<int>;
