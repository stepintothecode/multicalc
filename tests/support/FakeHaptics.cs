using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>Counts buzzes instead of making them.</summary>
public sealed class FakeHaptics : IHaptics
{
    /// <summary>How many times a tap was asked for.</summary>
    public int TapCount { get; private set; }

    public void Tap() => TapCount++;
}
