namespace FlowR.Mediator.Tests.Infrastructure;

public sealed class TestLog
{
    private readonly List<string> _messages = new();

    public IReadOnlyList<string> Messages => _messages;

    public void Add(string message)
    {
        _messages.Add(message);
    }

    public void Clear()
    {
        _messages.Clear();
    }
}
