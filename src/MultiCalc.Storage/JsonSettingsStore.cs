using System.Text.Json;
using MultiCalc.Domain.Settings;

namespace MultiCalc.Storage;

/// <summary>Keeps the settings in a JSON file under the app's private data directory.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string path;

    /// <summary>Creates the store. The file is created on first save.</summary>
    /// <param name="directory">Where to keep the file.</param>
    public JsonSettingsStore(string directory) =>
        path = Path.Combine(directory, "settings.json");

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return AppSettings.Default;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize(json, StorageJsonContext.Default.SettingsDocument);

            if (document is null || document.Version > SettingsDocument.CurrentVersion)
            {
                return AppSettings.Default;
            }

            return new AppSettings
            {
                Theme = Parse(document.Theme, AppSettings.Default.Theme),
                HapticFeedback = document.HapticFeedback,
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var document = new SettingsDocument
        {
            Theme = settings.Theme.ToString(),
            HapticFeedback = settings.HapticFeedback,
        };

        var json = JsonSerializer.Serialize(document, StorageJsonContext.Default.SettingsDocument);

        return AtomicFile.WriteAsync(path, json, cancellationToken);
    }

    /// <summary>An unrecognised name falls back to the default rather than to whatever is first.</summary>
    private static TEnum Parse<TEnum>(string value, TEnum fallback)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
