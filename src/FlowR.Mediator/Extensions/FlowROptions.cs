using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator.Extensions;

/// <summary>
/// Configuration options for FlowR.Mediator registration.
/// </summary>
public sealed class FlowROptions
{
    /// <summary>Lifetime for IMediator/ISender/IPublisher. Default: Transient.</summary>
    public ServiceLifetime MediatorLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>Lifetime for all request/notification/stream handlers. Default: Transient.</summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>Lifetime for all pipeline behaviors. Default: Transient.</summary>
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// When true, automatically registers RequestPreProcessorBehavior and
    /// RequestPostProcessorBehavior in the pipeline so IRequestPreProcessor
    /// and IRequestPostProcessor implementations run automatically.
    /// Default: false (opt-in, mirrors MediatR behavior).
    /// </summary>
    public bool RegisterProcessorBehaviors { get; set; } = false;
}
