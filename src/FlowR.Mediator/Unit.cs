namespace FlowR.Mediator;

/// <summary>
/// Represents a void return type. Use this for requests that have no return value.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>
    /// The singleton instance of <see cref="Unit"/>.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Returns a completed task with <see cref="Unit"/> value.
    /// </summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public int CompareTo(Unit other) => 0;
    public int CompareTo(object? obj) => 0;
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
    public override string ToString() => "()";
}
