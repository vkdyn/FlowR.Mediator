namespace FlowR.Mediator.Diagnostics;

public sealed class ExecutionLog
{
    private readonly List<string> _messages = new();

    public IReadOnlyList<string> Messages => _messages;

    public void Add(string message)
    {
        _messages.Add(message);
    }
}
