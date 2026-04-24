// Optional compatibility aliases for code that imports `using MediatR;`.
// Do not include this file if your project still references the real MediatR package.
namespace MediatR;

public interface IBaseRequest : FlowR.Mediator.IBaseRequest { }
public interface IRequest<out TResponse> : FlowR.Mediator.IRequest<TResponse>, IBaseRequest { }
public interface IRequest : FlowR.Mediator.IRequest, IBaseRequest { }
public interface INotification : FlowR.Mediator.INotification { }
