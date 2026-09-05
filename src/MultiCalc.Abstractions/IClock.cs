namespace MultiCalc.Abstractions;

/// <summary>The current time, behind a seam so tests can hold it still.</summary>
public interface IClock
{
    /// <summary>Now, in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>The real clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
