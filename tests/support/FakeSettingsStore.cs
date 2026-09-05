using MultiCalc.Domain.Settings;

namespace MultiCalc.TestSupport;

/// <summary>Keeps settings in memory.</summary>
public sealed class FakeSettingsStore : ISettingsStore
{
    private AppSettings current;

    public FakeSettingsStore(AppSettings? initial = null) => current = initial ?? AppSettings.Default;

    /// <summary>The settings as last written.</summary>
    public AppSettings Saved => current;

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(current);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        current = settings;
        return Task.CompletedTask;
    }
}
