using MultiCalc.Abstractions;

namespace MultiCalc.App.Services;

/// <summary>Build details for the About page.</summary>
public sealed class MauiAppBuildInfo : IAppBuildInfo
{
    /// <inheritdoc />
    public string Version => AppInfo.Current.VersionString;

    /// <inheritdoc />
    public string Build => AppInfo.Current.BuildString;

    /// <inheritdoc />
    public string PackageName => AppInfo.Current.PackageName;
}
