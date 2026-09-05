using System.Globalization;

namespace MultiCalc.Domain.Sessions;

/// <summary>
/// Every open in-app calculator, in the order they appear on the rail, plus which one is showing.
/// The book is never empty: removing the last calculator leaves a fresh blank one, because a
/// calculator app with nothing on screen has nowhere to go.
/// </summary>
public sealed record SessionBook
{
    /// <summary>The naming pattern for calculators the app creates.</summary>
    public const string NamePrefix = "Calculator";

    /// <summary>How many calculators can be open at once.</summary>
    public const int MaxSessions = 25;

    private SessionBook(IReadOnlyList<CalculatorSession> sessions, string activeId)
    {
        Sessions = sessions;
        ActiveId = activeId;
    }

    /// <summary>Open calculators, oldest first.</summary>
    public IReadOnlyList<CalculatorSession> Sessions { get; }

    /// <summary>The calculator currently on screen.</summary>
    public string ActiveId { get; }

    /// <summary>The calculator currently on screen.</summary>
    public CalculatorSession Active =>
        Sessions.FirstOrDefault(s => s.Id == ActiveId) ?? Sessions[0];

    /// <summary>True when another calculator would exceed <see cref="MaxSessions"/>.</summary>
    public bool IsFull => Sessions.Count >= MaxSessions;

    /// <summary>Starts a book holding one empty calculator.</summary>
    public static SessionBook Start(string id, DateTimeOffset createdAt)
    {
        var first = CalculatorSession.Create(id, $"{NamePrefix} 1", createdAt, CalculatorTint.Default);
        return new SessionBook([first], first.Id);
    }

    /// <summary>
    /// Rebuilds a book from stored sessions. An empty or unknown active id falls back to the
    /// first session, and an empty list produces a fresh book rather than an invalid one.
    /// </summary>
    public static SessionBook Restore(
        IReadOnlyList<CalculatorSession> sessions,
        string? activeId,
        string fallbackId,
        DateTimeOffset createdAt)
    {
        if (sessions.Count == 0)
        {
            return Start(fallbackId, createdAt);
        }

        var active = sessions.Any(s => s.Id == activeId) ? activeId! : sessions[0].Id;
        return new SessionBook(sessions, active);
    }

    /// <summary>
    /// Adds a calculator and makes it active. Returns the book unchanged once
    /// <see cref="MaxSessions"/> is reached.
    /// </summary>
    public SessionBook AddNew(string id, DateTimeOffset createdAt)
    {
        if (IsFull)
        {
            return this;
        }

        var session = CalculatorSession.Create(id, NextName(), createdAt, NextTint());
        return new SessionBook([.. Sessions, session], session.Id);
    }

    /// <summary>
    /// Closes a calculator. Closing the active one moves to its neighbour, and closing the
    /// last one leaves a fresh blank calculator in its place.
    /// </summary>
    public SessionBook Remove(string id, string replacementId, DateTimeOffset createdAt)
    {
        var index = IndexOf(id);

        if (index < 0)
        {
            return this;
        }

        if (Sessions.Count == 1)
        {
            return Start(replacementId, createdAt);
        }

        var remaining = Sessions.Where(s => s.Id != id).ToList();

        if (ActiveId != id)
        {
            return new SessionBook(remaining, ActiveId);
        }

        // Step to the one that took its place, or to the new last one if it was at the end.
        var nextIndex = Math.Min(index, remaining.Count - 1);
        return new SessionBook(remaining, remaining[nextIndex].Id);
    }

    /// <summary>Brings a calculator to the front. An unknown id is ignored.</summary>
    public SessionBook Activate(string id) =>
        IndexOf(id) < 0 ? this : new SessionBook(Sessions, id);

    /// <summary>Moves to the next or previous calculator, wrapping at the ends.</summary>
    public SessionBook Step(int offset)
    {
        if (Sessions.Count < 2 || offset == 0)
        {
            return this;
        }

        var index = IndexOf(ActiveId);
        var count = Sessions.Count;
        var next = ((index + offset) % count + count) % count;

        return new SessionBook(Sessions, Sessions[next].Id);
    }

    /// <summary>Swaps in an updated copy of a calculator. An unknown id is ignored.</summary>
    public SessionBook Replace(CalculatorSession session)
    {
        var index = IndexOf(session.Id);

        if (index < 0)
        {
            return this;
        }

        var updated = Sessions.ToArray();
        updated[index] = session;

        return new SessionBook(updated, ActiveId);
    }

    private int IndexOf(string id)
    {
        for (var i = 0; i < Sessions.Count; i++)
        {
            if (Sessions[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// The first quick colour nobody is using, so a new calculator is distinguishable from
    /// the ones already open. The palette has one entry for every calculator the app
    /// allows, so under normal use this never has to repeat.
    /// </summary>
    private CalculatorTint NextTint()
    {
        var taken = Sessions.Select(session => session.Tint).ToHashSet();

        foreach (var tint in CalculatorTint.Palette)
        {
            if (!taken.Contains(tint))
            {
                return tint;
            }
        }

        return CalculatorTint.Palette[Sessions.Count % CalculatorTint.Palette.Count];
    }

    /// <summary>
    /// The lowest unused number, so closing "Calculator 2" and adding one gives back
    /// "Calculator 2" rather than climbing forever.
    /// </summary>
    private string NextName()
    {
        var taken = new HashSet<int>();

        foreach (var session in Sessions)
        {
            if (session.Name.StartsWith(NamePrefix + " ", StringComparison.Ordinal)
                && int.TryParse(
                    session.Name[(NamePrefix.Length + 1)..],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                taken.Add(number);
            }
        }

        var candidate = 1;
        while (taken.Contains(candidate))
        {
            candidate++;
        }

        return $"{NamePrefix} {candidate}";
    }
}
