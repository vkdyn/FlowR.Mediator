using FlowR.Mediator.Behaviors;
using FlowR.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowR.Mediator.Tests;

// ---- Test Models ----

public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record UserDto(int Id, string Name);

public record CreateUserCommand(string Name) : IRequest;

public record PingRequest : IRequest<string>;

public record UserCreatedNotification(int UserId, string Name) : INotification;

public record GetNumbersStream(int Count) : IStreamRequest<int>;

public record MissingHandlerQuery(int Id) : IRequest<string>;

// ---- Test Handlers ----

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public static int CallCount;

    public Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(new UserDto(request.UserId, $"User{request.UserId}"));
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
{
    public static bool Called;

    public Task<Unit> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        Called = true;
        return Unit.Task;
    }
}

public sealed class PingHandler : IRequestHandler<PingRequest, string>
{
    public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Pong");
    }
}

public sealed class UserCreatedHandler1 : INotificationHandler<UserCreatedNotification>
{
    public static List<string> Calls { get; } = [];

    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"Handler1:{notification.UserId}");
        return Task.CompletedTask;
    }
}

public sealed class UserCreatedHandler2 : INotificationHandler<UserCreatedNotification>
{
    public static List<string> Calls { get; } = [];

    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"Handler2:{notification.UserId}");
        return Task.CompletedTask;
    }
}

public sealed class NumberStreamHandler : IStreamRequestHandler<GetNumbersStream, int>
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

// ---- Test Behaviour ----

public sealed class TestBehavior : IPipelineBehavior<PingRequest, string>
{
    public static List<string> Calls { get; } = [];

    public async Task<string> HandleAsync(
        PingRequest request,
        RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken = default)
    {
        Calls.Add("Before:PingRequest");
        string result = await next().ConfigureAwait(false);
        Calls.Add("After:PingRequest");
        return result;
    }
}

// ---- Validator ----

public sealed class RejectAllValidator : IFlowRValidator<GetUserQuery>
{
    public Task<IEnumerable<ValidationError>> ValidateAsync(
        GetUserQuery request,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ValidationError> errors =
        [
            new ValidationError("UserId", "UserId must be positive.")
        ];

        return Task.FromResult(errors);
    }
}

// ---- Tests ----

public sealed class MediatorTests
{
    private static IMediator BuildMediator(Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        services.AddTransient<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
        services.AddTransient<IRequestHandler<CreateUserCommand, Unit>, CreateUserCommandHandler>();
        services.AddTransient<IRequestHandler<PingRequest, string>, PingHandler>();
        services.AddTransient<INotificationHandler<UserCreatedNotification>, UserCreatedHandler1>();
        services.AddTransient<INotificationHandler<UserCreatedNotification>, UserCreatedHandler2>();
        services.AddTransient<IStreamRequestHandler<GetNumbersStream, int>, NumberStreamHandler>();

        configure?.Invoke(services);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IMediator>();
    }

    private static IMediator BuildMediatorWithoutHandlers()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_Request_ReturnsResponse()
    {
        GetUserQueryHandler.CallCount = 0;

        IMediator mediator = BuildMediator();
        UserDto result = await mediator.SendAsync(new GetUserQuery(42));

        Assert.Equal(42, result.Id);
        Assert.Equal("User42", result.Name);
        Assert.Equal(1, GetUserQueryHandler.CallCount);
    }

    [Fact]
    public async Task Send_VoidRequest_Succeeds()
    {
        CreateUserCommandHandler.Called = false;

        IMediator mediator = BuildMediator();
        await mediator.SendAsync(new CreateUserCommand("Alice"));

        Assert.True(CreateUserCommandHandler.Called);
    }

    [Fact]
    public async Task Send_PingRequest_ReturnsPong()
    {
        IMediator mediator = BuildMediator();

        string result = await mediator.SendAsync(new PingRequest());

        Assert.Equal("Pong", result);
    }

    [Fact]
    public async Task Publish_Notification_CallsAllHandlers()
    {
        UserCreatedHandler1.Calls.Clear();
        UserCreatedHandler2.Calls.Clear();

        IMediator mediator = BuildMediator();
        await mediator.PublishAsync(new UserCreatedNotification(1, "Alice"));

        Assert.Contains("Handler1:1", UserCreatedHandler1.Calls);
        Assert.Contains("Handler2:1", UserCreatedHandler2.Calls);
    }

    [Fact]
    public async Task Publish_Parallel_CallsAllHandlers()
    {
        UserCreatedHandler1.Calls.Clear();
        UserCreatedHandler2.Calls.Clear();

        IMediator mediator = BuildMediator();
        await mediator.PublishAsync(
            new UserCreatedNotification(2, "Bob"),
            NotificationPublishStrategy.Parallel);

        Assert.Contains("Handler1:2", UserCreatedHandler1.Calls);
        Assert.Contains("Handler2:2", UserCreatedHandler2.Calls);
    }

    [Fact]
    public async Task Stream_Request_YieldsCorrectValues()
    {
        IMediator mediator = BuildMediator();
        List<int> results = [];

        await foreach (int num in mediator.CreateStream(new GetNumbersStream(5)))
        {
            results.Add(num);
        }

        Assert.Equal([1, 2, 3, 4, 5], results);
    }

    [Fact]
    public async Task Pipeline_Behavior_IsCalledInOrder()
    {
        TestBehavior.Calls.Clear();

        IMediator mediator = BuildMediator(services =>
        {
            services.AddTransient<IPipelineBehavior<PingRequest, string>, TestBehavior>();
        });

        string result = await mediator.SendAsync(new PingRequest());

        Assert.Equal("Pong", result);
        Assert.Equal(2, TestBehavior.Calls.Count);
        Assert.Equal("Before:PingRequest", TestBehavior.Calls[0]);
        Assert.Equal("After:PingRequest", TestBehavior.Calls[1]);
    }

    [Fact]
    public async Task Send_NoHandler_ThrowsInvalidOperation()
    {
        IMediator mediator = BuildMediatorWithoutHandlers();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new MissingHandlerQuery(1)));

        Assert.Contains("No handler registered", exception.Message);
    }

    [Fact]
    public async Task ValidationBehavior_WithErrors_ThrowsValidationException()
    {
        IMediator mediator = BuildMediator(services =>
        {
            services.AddTransient<IFlowRValidator<GetUserQuery>, RejectAllValidator>();
            services.AddTransient<IPipelineBehavior<GetUserQuery, UserDto>, ValidationBehavior<GetUserQuery, UserDto>>();
        });

        FlowRValidationException exception = await Assert.ThrowsAsync<FlowRValidationException>(() =>
            mediator.SendAsync(new GetUserQuery(-1)));

        Assert.Single(exception.Errors);
        Assert.Equal("UserId", exception.Errors[0].PropertyName);
        Assert.Equal("UserId must be positive.", exception.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Unit_Equality_Works()
    {
        Assert.Equal(Unit.Value, Unit.Value);
        Assert.True(Unit.Value == Unit.Value);
        Assert.False(Unit.Value != Unit.Value);
        Assert.Equal("()", Unit.Value.ToString());
    }
}