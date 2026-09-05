using System.Text.Json.Serialization;

namespace MultiCalc.Storage;

/// <summary>The on disk shape of the open calculators.</summary>
/// <remarks>
/// Versioned. Adding a property is safe; changing or removing one means raising
/// <see cref="CurrentVersion"/> and handling the older shape on load.
/// </remarks>
internal sealed class SessionDocument
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public string? ActiveId { get; set; }

    public List<SessionRecord> Sessions { get; set; } = [];
}

internal sealed class SessionRecord
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? Hue { get; set; }

    /// <summary>
    /// The old named colour, kept only so a file written before the picker still opens
    /// with the colour its owner chose. Never written any more.
    /// </summary>
    public string? Colour { get; set; }

    public string? Expression { get; set; }

    public string? LastResult { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<EntryRecord> History { get; set; } = [];
}

internal sealed class EntryRecord
{
    public string Expression { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public DateTimeOffset At { get; set; }
}

/// <summary>The on disk shape of the settings. Enums are stored by name, not by number.</summary>
internal sealed class SettingsDocument
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public string Theme { get; set; } = string.Empty;

    public bool HapticFeedback { get; set; } = true;
}

/// <summary>
/// Source generated serialisation. The Release build for Android trims unused metadata,
/// which breaks reflection based serialisation but leaves this intact.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SessionDocument))]
[JsonSerializable(typeof(SettingsDocument))]
internal sealed partial class StorageJsonContext : JsonSerializerContext;
