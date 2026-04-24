namespace FlowR.Mediator.Pipeline;

public enum NotificationPublishStrategy
{
    Sequential = 0,
    Parallel = 1,
    ParallelNoThrow = 2,
    FireAndForget = 3
}
