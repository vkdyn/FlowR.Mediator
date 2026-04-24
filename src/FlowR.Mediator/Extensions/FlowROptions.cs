using Microsoft.Extensions.DependencyInjection;

namespace FlowR.Mediator.Extensions;

public sealed class FlowROptions
{
    public ServiceLifetime MediatorLifetime { get; set; } = ServiceLifetime.Transient;
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
    public ServiceLifetime BehaviorLifetime { get; set; } = ServiceLifetime.Transient;
}
