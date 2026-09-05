using MultiCalc.Domain.Sessions;

namespace MultiCalc.TestSupport;

/// <summary>Keeps the book in memory and counts writes.</summary>
public sealed class FakeSessionStore : ISessionStore
{
    private SessionBook? saved;

    public FakeSessionStore(SessionBook? initial = null) => saved = initial;

    /// <summary>How many times the book has been written.</summary>
    public int SaveCount { get; private set; }

    /// <summary>The book as last written, or the one the store was seeded with.</summary>
    public SessionBook? Saved => saved;

    public Task<SessionBook?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(saved);

    public Task SaveAsync(SessionBook book, CancellationToken cancellationToken = default)
    {
        saved = book;
        SaveCount++;

        return Task.CompletedTask;
    }
}
