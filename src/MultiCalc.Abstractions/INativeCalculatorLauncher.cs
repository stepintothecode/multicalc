namespace MultiCalc.Abstractions;

/// <summary>Starts extra copies of whatever calculator the device shipped with.</summary>
public interface INativeCalculatorLauncher
{
    /// <summary>
    /// Asks the system what it will allow. Cheap enough to call on every settings visit,
    /// and the answer can change when the user installs a different calculator.
    /// </summary>
    NativeCalculatorStatus Probe();

    /// <summary>Starts one more instance of the device calculator.</summary>
    NativeLaunchOutcome LaunchNewInstance();
}
