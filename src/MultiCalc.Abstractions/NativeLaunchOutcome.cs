namespace MultiCalc.Abstractions;

/// <summary>What happened when we asked the system for another native calculator.</summary>
public enum NativeLaunchOutcome
{
    /// <summary>A new instance was started.</summary>
    Launched,

    /// <summary>This device cannot do it. The UI should fall back to an in-app instance.</summary>
    NotSupported,

    /// <summary>The system refused the start. Rare, and worth telling the person about.</summary>
    Failed,
}
