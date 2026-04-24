using FlowR.Mediator;
using FlowR.Mediator.Diagnostics;
using FlowR.Mediator.Extensions;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

ServiceCollection services = new();
services.AddSingleton<ExecutionLog>();
services.AddFlowR(typeof(Program).Assembly);

using ServiceProvider provider = services.BuildServiceProvider();
IMediator mediator = provider.GetRequiredService<IMediator>();

CreateOrderResult result = await mediator.Send(new CreateOrderCommand("ORD-1001", 125.50m));
Console.WriteLine($"Created order: {result.OrderId}, total: {result.Total:C}");

string status = await mediator.SendAsync(new GetOrderStatusQuery("ORD-1001"));
Console.WriteLine($"Status: {status}");

await mediator.Publish(new OrderCreatedNotification("ORD-1001"));

await foreach (int number in mediator.CreateStream(new CountToStreamRequest(5)))
{
    Console.WriteLine($"Stream value: {number}");
}

ExecutionLog log = provider.GetRequiredService<ExecutionLog>();
Console.WriteLine("Pipeline log:");
foreach (string item in log.Messages)
{
    Console.WriteLine($"- {item}");
}

public sealed record CreateOrderCommand(string OrderId, decimal Total) : IRequest<CreateOrderResult>;
public sealed record CreateOrderResult(string OrderId, decimal Total);

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public Task<CreateOrderResult> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CreateOrderResult(request.OrderId, request.Total));
    }
}

public sealed record GetOrderStatusQuery(string OrderId) : IRequest<string>;

public sealed class GetOrderStatusQueryHandler : IRequestHandler<GetOrderStatusQuery, string>
{
    public Task<string> HandleAsync(GetOrderStatusQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Order {request.OrderId} is Ready");
    }
}

public sealed record OrderCreatedNotification(string OrderId) : INotification;

public sealed class SendOrderEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ExecutionLog _log;

    public SendOrderEmailHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"Email sent for {notification.OrderId}");
        return Task.CompletedTask;
    }
}

public sealed class AuditOrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ExecutionLog _log;

    public AuditOrderCreatedHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        _log.Add($"Audit saved for {notification.OrderId}");
        return Task.CompletedTask;
    }
}

public sealed record CountToStreamRequest(int Count) : IStreamRequest<int>;

public sealed class CountToStreamRequestHandler : IStreamRequestHandler<CountToStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        CountToStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken);
            yield return i;
        }
    }
}

public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ExecutionLog _log;

    public LoggingBehaviour(ExecutionLog log)
    {
        _log = log;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _log.Add($"Before {typeof(TRequest).Name}");
        TResponse response = await next().ConfigureAwait(false);
        _log.Add($"After {typeof(TRequest).Name}");
        return response;
    }
}
