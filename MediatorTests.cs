using FlowR;
using FlowR.Behaviors;
using FlowR.Extensions;
using FlowR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlowR.Tests;

// ---- Test Models ----

public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

public record CreateUserCommand(string Name) : IRequest;

public record PingRequest : IRequest<string>;

public record UserCreatedNotification(int UserId, string Name) : INotification;

public record GetNumbersStream(int Count) : IStreamRequest<int>;

// ---- Test Handlers ----

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public static int CallCount;
    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(new UserDto(request.UserId, $"User{request.UserId}"));
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public static bool Called;
    public Task<Unit> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        Called = true;
        return Unit.Task;
    }
}

public class PingHandler : IRequestHandler<PingRequest, string>
{
    public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult("Pong");
}

public class UserCreatedHandler1 : INotificationHandler<UserCreatedNotification>
{
    public static List<string> Calls { get; } = [];
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"Handler1:{notification.UserId}");
        return Task.CompletedTask;
    }
}

public class UserCreatedHandler2 : INotificationHandler<UserCreatedNotification>
{
    public static List<string> Calls { get; } = [];
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"Handler2:{notification.UserId}");
        return Task.CompletedTask;
    }
}

public class NumberStreamHandler : IStreamRequestHandler<GetNumbersStream, int>
{
    public async IAsyncEnumerable<int> HandleAsync(GetNumbersStream request, CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}

// ---- Test Behavior ----

public class TestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Calls { get; } = [];

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        Calls.Add($"Before:{typeof(TRequest).Name}");
        var result = await next();
        Calls.Add($"After:{typeof(TRequest).Name}");
        return result;
    }
}

// ---- Tests ----

public class MediatorTests
{
    private IMediator BuildMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowR(typeof(MediatorTests).Assembly);
        configure?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_Request_ReturnsResponse()
    {
        var mediator = BuildMediator();
        var result = await mediator.SendAsync(new GetUserQuery(42));
        Assert.Equal(42, result.Id);
        Assert.Equal("User42", result.Name);
    }

    [Fact]
    public async Task Send_VoidRequest_Succeeds()
    {
        CreateUserCommandHandler.Called = false;
        var mediator = BuildMediator();
        await mediator.SendAsync(new CreateUserCommand("Alice"));
        Assert.True(CreateUserCommandHandler.Called);
    }

    [Fact]
    public async Task Send_PingRequest_ReturnsPong()
    {
        var mediator = BuildMediator();
        var result = await mediator.SendAsync(new PingRequest());
        Assert.Equal("Pong", result);
    }

    [Fact]
    public async Task Publish_Notification_CallsAllHandlers()
    {
        UserCreatedHandler1.Calls.Clear();
        UserCreatedHandler2.Calls.Clear();

        var mediator = BuildMediator();
        await mediator.PublishAsync(new UserCreatedNotification(1, "Alice"));

        Assert.Contains("Handler1:1", UserCreatedHandler1.Calls);
        Assert.Contains("Handler2:1", UserCreatedHandler2.Calls);
    }

    [Fact]
    public async Task Publish_Parallel_CallsAllHandlers()
    {
        UserCreatedHandler1.Calls.Clear();
        UserCreatedHandler2.Calls.Clear();

        var mediator = BuildMediator();
        await mediator.PublishAsync(new UserCreatedNotification(2, "Bob"), NotificationPublishStrategy.Parallel);

        Assert.Contains("Handler1:2", UserCreatedHandler1.Calls);
        Assert.Contains("Handler2:2", UserCreatedHandler2.Calls);
    }

    [Fact]
    public async Task Stream_Request_YieldsCorrectValues()
    {
        var mediator = BuildMediator();
        var results = new List<int>();
        await foreach (var num in mediator.CreateStream(new GetNumbersStream(5)))
            results.Add(num);

        Assert.Equal([1, 2, 3, 4, 5], results);
    }

    [Fact]
    public async Task Pipeline_Behavior_IsCalledInOrder()
    {
        TestBehavior<PingRequest, string>.Calls.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowR(typeof(MediatorTests).Assembly);
        services.AddTransient<IPipelineBehavior<PingRequest, string>, TestBehavior<PingRequest, string>>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.SendAsync(new PingRequest());

        Assert.Contains("Before:PingRequest", TestBehavior<PingRequest, string>.Calls);
        Assert.Contains("After:PingRequest", TestBehavior<PingRequest, string>.Calls);
    }

    [Fact]
    public async Task Send_NoHandler_ThrowsInvalidOperation()
    {
        var services = new ServiceCollection();
        services.AddFlowR(); // No handlers registered
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new GetUserQuery(1)));
    }

    [Fact]
    public async Task ValidationBehavior_WithErrors_ThrowsValidationException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowR(typeof(MediatorTests).Assembly);
        services.AddTransient<IFlowRValidator<GetUserQuery>, RejectAllValidator>();
        services.AddTransient<IPipelineBehavior<GetUserQuery, UserDto>, ValidationBehavior<GetUserQuery, UserDto>>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<FlowRValidationException>(() =>
            mediator.SendAsync(new GetUserQuery(-1)));
    }

    [Fact]
    public void Unit_Equality_Works()
    {
        Assert.Equal(Unit.Value, Unit.Value);
        Assert.True(Unit.Value == Unit.Value);
    }
}

public class RejectAllValidator : IFlowRValidator<GetUserQuery>
{
    public Task<IEnumerable<ValidationError>> ValidateAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        IEnumerable<ValidationError> errors = [new("UserId", "UserId must be positive.")];
        return Task.FromResult(errors);
    }
}
