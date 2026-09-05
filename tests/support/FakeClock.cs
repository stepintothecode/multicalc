using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>A clock that only moves when a test tells it to.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset? start = null) =>
        UtcNow = start ?? new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
