using FlowR.Mediator;
using FlowR.Mediator.Behaviors;
using FlowR.Mediator.Extensions;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ============================================================
// FlowR Sample Application
// Demonstrates: Requests, Void Commands, Notifications,
//               Streaming, Pipeline Behaviors, Validation
// ============================================================

var services = new ServiceCollection();

services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

services.AddFlowR(options =>
{
    options.MediatorLifetime = ServiceLifetime.Scoped;
    options.HandlerLifetime = ServiceLifetime.Transient;
},
typeof(Program).Assembly);

// Add open generic behaviors (apply to ALL requests)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Add validator
services.AddTransient<IFlowRValidator<GetProductQuery>, GetProductQueryValidator>();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

Console.WriteLine("=== FlowR Sample Application ===\n");

// 1. Request/Response
Console.WriteLine("--- Query ---");
var product = await mediator.SendAsync(new GetProductQuery(1));
Console.WriteLine($"Got product: {product.Name} @ ${product.Price}\n");

// 2. Void Command
Console.WriteLine("--- Command ---");
await mediator.SendAsync(new CreateProductCommand("Widget", 9.99m));
Console.WriteLine();

// 3. Notification (multiple handlers)
Console.WriteLine("--- Notification ---");
await mediator.PublishAsync(new ProductCreatedEvent(42, "Widget"));
Console.WriteLine();

// 4. Parallel notification
Console.WriteLine("--- Parallel Notification ---");
await mediator.PublishAsync(new ProductCreatedEvent(43, "Gadget"), NotificationPublishStrategy.Parallel);
Console.WriteLine();

// 5. Streaming
Console.WriteLine("--- Stream ---");
await foreach (var item in mediator.CreateStream(new GetTopProductsStream(3)))
{
    Console.WriteLine($"  Streamed: {item.Name}");
}
Console.WriteLine();

// 6. Validation failure
Console.WriteLine("--- Validation Failure ---");
try
{
    await mediator.SendAsync(new GetProductQuery(-1));
}
catch (FlowRValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Errors[0].ErrorMessage}");
}

Console.WriteLine("\n=== Done! ===");

// ============================================================
// Models
// ============================================================

public record GetProductQuery(int ProductId) : IRequest<ProductDto>;
public record ProductDto(int Id, string Name, decimal Price);

public record CreateProductCommand(string Name, decimal Price) : IRequest;

public record ProductCreatedEvent(int ProductId, string Name) : INotification;

public record GetTopProductsStream(int Count) : IStreamRequest<ProductDto>;

// ============================================================
// Handlers
// ============================================================

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    public Task<ProductDto> HandleAsync(GetProductQuery request, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProductDto(request.ProductId, "Awesome Widget", 29.99m));
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand>
{
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(ILogger<CreateProductCommandHandler> logger)
        => _logger = logger;

    public Task<Unit> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating product: {Name} @ {Price}", request.Name, request.Price);
        return Unit.Task;
    }
}

public class ProductCreatedEmailHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEmailHandler> _logger;
    public ProductCreatedEmailHandler(ILogger<ProductCreatedEmailHandler> logger) => _logger = logger;

    public Task HandleAsync(ProductCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("EMAIL: Product {Name} (ID:{Id}) created", notification.Name, notification.ProductId);
        return Task.CompletedTask;
    }
}

public class ProductCreatedAuditHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedAuditHandler> _logger;
    public ProductCreatedAuditHandler(ILogger<ProductCreatedAuditHandler> logger) => _logger = logger;

    public Task HandleAsync(ProductCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AUDIT: Logged product creation event for {Name}", notification.Name);
        return Task.CompletedTask;
    }
}

public class GetTopProductsStreamHandler : IStreamRequestHandler<GetTopProductsStream, ProductDto>
{
    private static readonly ProductDto[] Products =
    [
        new(1, "Widget Pro", 49.99m),
        new(2, "Gadget Max", 99.99m),
        new(3, "Super Thing", 19.99m),
        new(4, "Doohickey", 5.99m),
        new(5, "Thingamajig", 14.99m),
    ];

    public async IAsyncEnumerable<ProductDto> HandleAsync(GetTopProductsStream request, CancellationToken cancellationToken = default)
    {
        foreach (var p in Products.Take(request.Count))
        {
            await Task.Delay(10, cancellationToken);
            yield return p;
        }
    }
}

// ============================================================
// Validators
// ============================================================

public class GetProductQueryValidator : IFlowRValidator<GetProductQuery>
{
    public Task<IEnumerable<ValidationError>> ValidateAsync(GetProductQuery request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        if (request.ProductId <= 0)
            errors.Add(new ValidationError("ProductId", "ProductId must be greater than 0."));

        return Task.FromResult<IEnumerable<ValidationError>>(errors);
    }
}
