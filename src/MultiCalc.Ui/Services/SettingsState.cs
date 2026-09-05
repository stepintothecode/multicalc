using MultiCalc.Domain.Settings;

namespace MultiCalc.Ui.Services;

/// <summary>Holds the settings for the running app and writes every change straight through.</summary>
public sealed class SettingsState
{
    private readonly ISettingsStore store;

    /// <summary>Creates the state over a store.</summary>
    public SettingsState(ISettingsStore store) => this.store = store;

    /// <summary>Raised after any change, so pages can repaint.</summary>
    public event Action? Changed;

    /// <summary>The settings as they stand.</summary>
    public AppSettings Current { get; private set; } = AppSettings.Default;

    /// <summary>Reads the saved settings. Called once at startup.</summary>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        Current = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        Changed?.Invoke();
    }

    /// <summary>Applies a change and saves it.</summary>
    public async Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken = default)
    {
        Current = change(Current);
        Changed?.Invoke();

        await store.SaveAsync(Current, cancellationToken).ConfigureAwait(false);
    }
}
