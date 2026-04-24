namespace FlowR.Mediator;

public interface IBaseRequest { }

public interface IRequest<out TResponse> : IBaseRequest { }

public interface IRequest : IRequest<Unit> { }

public interface IStreamRequest<out TResponse> : IBaseRequest { }
