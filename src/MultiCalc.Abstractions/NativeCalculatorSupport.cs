namespace MultiCalc.Abstractions;

/// <summary>What this device will let us do with its built in calculator.</summary>
public enum NativeCalculatorSupport
{
    /// <summary>A calculator was found and it accepts a second task, so instances can be spawned.</summary>
    Supported,

    /// <summary>
    /// A calculator was found, but its activity is declared single task or single instance.
    /// Launching again reuses the one window instead of adding another.
    /// </summary>
    SingleInstanceOnly,

    /// <summary>No app on the device answers as a calculator.</summary>
    NoCalculatorFound,

    /// <summary>This platform has no notion of launching another app's calculator.</summary>
    PlatformNotSupported,
}
