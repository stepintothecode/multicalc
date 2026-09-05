namespace MultiCalc.Domain.Settings;

/// <summary>Keeps <see cref="AppSettings"/> across restarts.</summary>
public interface ISettingsStore
{
    /// <summary>Reads the saved settings, or the defaults when nothing is saved yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the settings.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
