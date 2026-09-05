namespace MultiCalc.Domain.Sessions;

/// <summary>Keeps the open calculators across restarts.</summary>
public interface ISessionStore
{
    /// <summary>
    /// Reads the saved book. Returns null when nothing is saved or the file cannot be read,
    /// so the caller decides what a fresh start looks like rather than getting a guess.
    /// </summary>
    Task<SessionBook?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the book.</summary>
    Task SaveAsync(SessionBook book, CancellationToken cancellationToken = default);
}
