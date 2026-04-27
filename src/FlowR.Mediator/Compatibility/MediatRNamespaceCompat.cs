// ============================================================================
// MediatR Namespace Compatibility Layer
//
// This file re-exports all FlowR.Mediator types under the MediatR namespace
// so code with "using MediatR;" compiles without changes.
//
// HOW TO USE:
//   Include this file in your project (do NOT reference the real MediatR package).
//   All existing "using MediatR;" imports will resolve to FlowR.Mediator types.
// ============================================================================

// ReSharper disable all
#pragma warning disable CS0108 // hides inherited member

namespace MediatR
{
    // Core interfaces
    public interface IBaseRequest : FlowR.Mediator.IBaseRequest { }
    public interface IRequest<out TResponse> : FlowR.Mediator.IRequest<TResponse>, IBaseRequest { }
    public interface IRequest : FlowR.Mediator.IRequest, IBaseRequest { }
    public interface IStreamRequest<out TResponse> : FlowR.Mediator.IStreamRequest<TResponse>, IBaseRequest { }
    public interface INotification : FlowR.Mediator.INotification { }

    // Handler interfaces — using HandleAsync (FlowR name)
    public interface IRequestHandler<in TRequest, TResponse>
        : FlowR.Mediator.IRequestHandler<TRequest, TResponse>
        where TRequest : FlowR.Mediator.IRequest<TResponse> { }

    public interface IRequestHandler<in TRequest>
        : FlowR.Mediator.IRequestHandler<TRequest>
        where TRequest : FlowR.Mediator.IRequest { }

    public interface INotificationHandler<in TNotification>
        : FlowR.Mediator.INotificationHandler<TNotification>
        where TNotification : FlowR.Mediator.INotification { }

    public interface IStreamRequestHandler<in TRequest, out TResponse>
        : FlowR.Mediator.IStreamRequestHandler<TRequest, TResponse>
        where TRequest : FlowR.Mediator.IStreamRequest<TResponse> { }

    // Pre/Post processors
    public interface IRequestPreProcessor<in TRequest>
        : FlowR.Mediator.Pipeline.IRequestPreProcessor<TRequest>
        where TRequest : notnull { }

    public interface IRequestPostProcessor<in TRequest, in TResponse>
        : FlowR.Mediator.Pipeline.IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull { }

    // Pipeline
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
    public delegate Task NotificationHandlerDelegate();

    public interface IPipelineBehavior<in TRequest, TResponse>
        : FlowR.Mediator.Pipeline.IPipelineBehavior<TRequest, TResponse>
        where TRequest : FlowR.Mediator.IBaseRequest { }

    // Mediator interfaces
    public interface IMediator : FlowR.Mediator.IMediator { }
    public interface ISender : FlowR.Mediator.ISender { }
    public interface IPublisher : FlowR.Mediator.IPublisher { }

    // Unit
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new();
        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";

        // Implicit conversion to/from FlowR.Mediator.Unit
        public static implicit operator FlowR.Mediator.Unit(Unit _) => FlowR.Mediator.Unit.Value;
        public static implicit operator Unit(FlowR.Mediator.Unit _) => Value;
    }

    // ServiceFactory delegate (used in some MediatR patterns)
    public delegate object? ServiceFactory(Type serviceType);
}
