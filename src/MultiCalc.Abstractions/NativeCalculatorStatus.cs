namespace MultiCalc.Abstractions;

/// <summary>What the probe found out about the device's built in calculator.</summary>
/// <param name="Support">Whether instances can be spawned, and if not, why not.</param>
/// <param name="PackageName">The calculator's package, when one was found.</param>
/// <param name="ActivityName">The activity to start, when one was found.</param>
/// <param name="DisplayName">The calculator's own name, for showing in the UI.</param>
public sealed record NativeCalculatorStatus(
    NativeCalculatorSupport Support,
    string? PackageName = null,
    string? ActivityName = null,
    string? DisplayName = null)
{
    /// <summary>True when spawning a native instance is worth offering.</summary>
    public bool CanSpawn => Support == NativeCalculatorSupport.Supported;

    /// <summary>A status meaning "nothing here", used by platforms with no such concept.</summary>
    public static NativeCalculatorStatus Unsupported { get; } =
        new(NativeCalculatorSupport.PlatformNotSupported);
}
