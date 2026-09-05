namespace MultiCalc.Abstractions;

/// <summary>What the About page needs to know about the running build.</summary>
public interface IAppBuildInfo
{
    /// <summary>The version people see, such as "1.0".</summary>
    string Version { get; }

    /// <summary>The build number behind that version.</summary>
    string Build { get; }

    /// <summary>The package identifier, such as "com.stepintothecode.multicalc".</summary>
    string PackageName { get; }
}
