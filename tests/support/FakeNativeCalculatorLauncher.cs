using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>Stands in for the device calculator, so the app logic can be tested off a phone.</summary>
public sealed class FakeNativeCalculatorLauncher : INativeCalculatorLauncher
{
    private readonly NativeCalculatorStatus status;
    private readonly NativeLaunchOutcome launchResult;

    public FakeNativeCalculatorLauncher(
        NativeCalculatorStatus? status = null,
        NativeLaunchOutcome launchResult = NativeLaunchOutcome.Launched)
    {
        this.status = status ?? new NativeCalculatorStatus(
            NativeCalculatorSupport.Supported,
            "com.example.calculator",
            "com.example.calculator.Main",
            "Calculator");

        this.launchResult = launchResult;
    }

    /// <summary>How many times a new native instance was asked for.</summary>
    public int LaunchCount { get; private set; }

    public NativeCalculatorStatus Probe() => status;

    public NativeLaunchOutcome LaunchNewInstance()
    {
        LaunchCount++;
        return launchResult;
    }
}
