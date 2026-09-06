using System.Text.Json;
using MultiCalc.Domain.Sessions;

namespace MultiCalc.Storage;

/// <summary>Keeps the open calculators in a JSON file under the app's private data directory.</summary>
public sealed class JsonSessionStore : ISessionStore
{
    private readonly string path;

    /// <summary>Creates the store. The file is created on first save.</summary>
    /// <param name="directory">Where to keep the file.</param>
    public JsonSessionStore(string directory) =>
        path = Path.Combine(directory, "sessions.json");

    /// <inheritdoc />
    public async Task<SessionBook?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize(json, StorageJsonContext.Default.SessionDocument);

            // A file from a newer build is refused rather than half read.
            if (document is null || document.Version > SessionDocument.CurrentVersion)
            {
                return null;
            }

            var sessions = document.Sessions
                .Where(record => !string.IsNullOrEmpty(record.Id))
                .Select(ToSession)
                .ToList();

            return sessions.Count == 0
                ? null
                : SessionBook.Restore(sessions, document.ActiveId, sessions[0].Id, sessions[0].CreatedAt);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // An unreadable file means a fresh start, not a crash on launch.
            return null;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(SessionBook book, CancellationToken cancellationToken = default)
    {
        var document = new SessionDocument
        {
            ActiveId = book.ActiveId,
            Sessions = [.. book.Sessions.Select(ToRecord)],
        };

        var json = JsonSerializer.Serialize(document, StorageJsonContext.Default.SessionDocument);

        return AtomicFile.WriteAsync(path, json, cancellationToken);
    }

    private static CalculatorSession ToSession(SessionRecord record) =>
        CalculatorSession.Restore(
            record.Id,
            string.IsNullOrWhiteSpace(record.Name) ? "Calculator" : record.Name,
            record.Expression,
            record.LastResult,
            [.. record.History.Select(e => new CalculationEntry(e.Expression, e.Result, e.At))],
            record.CreatedAt,
            TintOf(record),
            record.Scientific);

    /// <summary>
    /// The stored hue, or the named colour a file from before the picker would carry, or
    /// the default for a file from before either.
    /// </summary>
    private static CalculatorTint TintOf(SessionRecord record)
    {
        if (record.Hue is { } hue)
        {
            return CalculatorTint.FromHue(hue);
        }

        return record.Colour?.ToLowerInvariant() switch
        {
            "teal" => CalculatorTint.FromHue(170),
            "blue" => CalculatorTint.FromHue(215),
            "violet" => CalculatorTint.FromHue(260),
            "pink" => CalculatorTint.FromHue(325),
            "red" => CalculatorTint.FromHue(5),
            "orange" => CalculatorTint.FromHue(30),
            "green" => CalculatorTint.FromHue(100),
            "slate" => CalculatorTint.FromHue(210),
            _ => CalculatorTint.Default,
        };
    }

    private static SessionRecord ToRecord(CalculatorSession session) => new()
    {
        Id = session.Id,
        Name = session.Name,
        Hue = session.Tint.Hue,
        Scientific = session.Scientific,
        Expression = session.Draft.Expression,
        LastResult = session.LastResult,
        CreatedAt = session.CreatedAt,
        History = [.. session.History.Select(e => new EntryRecord
        {
            Expression = e.Expression,
            Result = e.Result,
            At = e.At,
        })],
    };
}
