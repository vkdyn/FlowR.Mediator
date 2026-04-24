# FlowR.Mediator 🌊

[![NuGet](https://img.shields.io/nuget/v/FlowR.svg)](https://www.nuget.org/packages/FlowR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FlowR.svg)](https://www.nuget.org/packages/FlowR)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![CI](https://github.com/yourusername/FlowR/actions/workflows/ci.yml/badge.svg)](https://github.com/yourusername/FlowR/actions)

**A free, high-performance, open-source mediator pattern library for .NET — forever.**

FlowR brings the mediator/CQRS pattern to .NET with zero licensing fees, excellent performance through compiled expression trees, and more features out of the box than alternatives.

---

## Why FlowR?

| Feature | FlowR | MediatR |
|---|---|---|
| License | ✅ MIT (free forever) | ❌ Paid for commercial |
| Request/Response | ✅ | ✅ |
| Void Commands | ✅ | ✅ |
| Notifications | ✅ | ✅ |
| Streaming | ✅ | ✅ |
| Pipeline Behaviors | ✅ | ✅ |
| Notification Strategies | ✅ Sequential, Parallel, FireAndForget | ❌ |
| Built-in Logging Behavior | ✅ | ❌ |
| Built-in Validation Behavior | ✅ | ❌ |
| Built-in Retry Behavior | ✅ | ❌ |
| Built-in Performance Behavior | ✅ | ❌ |
| Notification Pipeline Behaviors | ✅ | ❌ |
| Compiled Expression Trees (fast!) | ✅ | ✅ |

---

## Installation

```bash
dotnet add package FlowR.Mediator
```

---

## Quick Start

### 1. Register FlowR

```csharp
// Program.cs
builder.Services.AddFlowR(typeof(Program).Assembly);
```

### 2. Define a Request

```csharp
// Request (returns a response)
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

// Command (no response)
public record CreateUserCommand(string Name, string Email) : IRequest;
```

### 3. Create a Handler

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repo;
    public GetUserQueryHandler(IUserRepository repo) => _repo = repo;

    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        var user = await _repo.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto(user.Id, user.Name);
    }
}
```

### 4. Send It

```csharp
public class UserController : ControllerBase
{
    private readonly ISender _sender;
    public UserController(ISender sender) => _sender = sender;

    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(int id)
        => await _sender.SendAsync(new GetUserQuery(id));
}
```

---

## Notifications

Publish events to multiple handlers:

```csharp
// Define
public record UserRegisteredEvent(int UserId) : INotification;

// Multiple handlers
public class SendWelcomeEmailHandler : INotificationHandler<UserRegisteredEvent> { ... }
public class CreateDefaultProfileHandler : INotificationHandler<UserRegisteredEvent> { ... }

// Publish
await mediator.PublishAsync(new UserRegisteredEvent(userId));

// Publish in parallel
await mediator.PublishAsync(new UserRegisteredEvent(userId), NotificationPublishStrategy.Parallel);

// Fire and forget (background, non-critical)
await mediator.PublishAsync(new UserRegisteredEvent(userId), NotificationPublishStrategy.FireAndForget);
```

---

## Streaming

```csharp
public record GetOrdersStream(int CustomerId) : IStreamRequest<OrderDto>;

public class GetOrdersStreamHandler : IStreamRequestHandler<GetOrdersStream, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> HandleAsync(GetOrdersStream request, CancellationToken cancellationToken = default)
    {
        await foreach (var order in _db.GetOrdersAsync(request.CustomerId, cancellationToken))
            yield return new OrderDto(order.Id, order.Total);
    }
}

// Use
await foreach (var order in sender.CreateStream(new GetOrdersStream(customerId)))
    Console.WriteLine(order);
```

---

## Pipeline Behaviors

Add cross-cutting concerns to your request pipeline:

```csharp
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;
    public AuthorizationBehavior(ICurrentUser currentUser) => _currentUser = currentUser;

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException();

        return await next();
    }
}

// Register for all requests (open generic)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
```

### Built-in Behaviors

FlowR ships with ready-to-use behaviors:

```csharp
// Logging (logs request name and duration)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Performance (warns on slow requests > 500ms)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

// Validation (integrates with any validator)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient<IFlowRValidator<CreateUserCommand>, CreateUserCommandValidator>();

// Retry (retries failed requests up to N times)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
```

---

## Validation

```csharp
public class CreateUserCommandValidator : IFlowRValidator<CreateUserCommand>
{
    public Task<IEnumerable<ValidationError>> ValidateAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError("Name", "Name is required."));
        if (!request.Email.Contains('@'))
            errors.Add(new ValidationError("Email", "Invalid email address."));

        return Task.FromResult<IEnumerable<ValidationError>>(errors);
    }
}
```

When validation fails, `FlowRValidationException` is thrown with all errors:

```csharp
catch (FlowRValidationException ex)
{
    foreach (var error in ex.Errors)
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
```

---

## Configuration

```csharp
services.AddFlowR(options =>
{
    options.MediatorLifetime = ServiceLifetime.Scoped;   // default: Transient
    options.HandlerLifetime = ServiceLifetime.Transient; // default: Transient
    options.BehaviorLifetime = ServiceLifetime.Transient; // default: Transient
},
typeof(Program).Assembly,
typeof(OtherAssemblyMarker).Assembly);
```

---

## Performance

FlowR uses **compiled expression trees** to build handler delegates once and cache them. After the first call, handler resolution is essentially a dictionary lookup — no reflection overhead on hot paths.

---

## License

MIT — free for personal and commercial use, forever.

---

## Contributing

PRs welcome! See [CONTRIBUTING.md](CONTRIBUTING.md).

Star ⭐ the repo if FlowR helps you!
