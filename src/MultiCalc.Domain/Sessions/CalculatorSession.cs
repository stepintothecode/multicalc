using MultiCalc.Domain.Calculation;

namespace MultiCalc.Domain.Sessions;

/// <summary>
/// One in-app calculator: what is on its display and everything it has worked out.
/// Sessions are independent, which is the whole point of the app.
/// </summary>
public sealed record CalculatorSession
{
    /// <summary>How many entries one session keeps before the oldest are dropped.</summary>
    public const int HistoryLimit = 200;

    private CalculatorSession(
        string id,
        string name,
        CalculatorTint tint,
        bool scientific,
        ExpressionDraft draft,
        string? lastResult,
        IReadOnlyList<CalculationEntry> history,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Tint = tint;
        Scientific = scientific;
        Draft = draft;
        LastResult = lastResult;
        History = history;
        CreatedAt = createdAt;
    }

    /// <summary>Stable identifier, used for ordering and for restoring the active session.</summary>
    public string Id { get; }

    /// <summary>The label shown in the calculator list. Renameable.</summary>
    public string Name { get; }

    /// <summary>The tag colour, so this calculator is recognisable without reading its name.</summary>
    public CalculatorTint Tint { get; }

    /// <summary>Whether this calculator shows the scientific keys.</summary>
    public bool Scientific { get; }

    /// <summary>What is currently typed.</summary>
    public ExpressionDraft Draft { get; }

    /// <summary>The last computed result in display form, or null if nothing has been worked out yet.</summary>
    public string? LastResult { get; }

    /// <summary>Newest first.</summary>
    public IReadOnlyList<CalculationEntry> History { get; }

    /// <summary>When this calculator was opened.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Creates an empty session.</summary>
    /// <remarks>
    /// The tint is nullable rather than defaulted, because <c>default(CalculatorTint)</c>
    /// is hue zero, which is a real colour and not the one meant by "unspecified".
    /// </remarks>
    public static CalculatorSession Create(
        string id,
        string name,
        DateTimeOffset createdAt,
        CalculatorTint? tint = null,
        bool scientific = false) =>
        new(id, name, tint ?? CalculatorTint.Default, scientific, ExpressionDraft.Empty, null, [], createdAt);

    /// <summary>Rebuilds a session from stored values.</summary>
    public static CalculatorSession Restore(
        string id,
        string name,
        string? expression,
        string? lastResult,
        IReadOnlyList<CalculationEntry> history,
        DateTimeOffset createdAt,
        CalculatorTint? tint = null,
        bool scientific = false) =>
        new(
            id,
            name,
            tint ?? CalculatorTint.Default,
            scientific,
            ExpressionDraft.FromExpression(expression),
            lastResult,
            history,
            createdAt);

    /// <summary>
    /// Returns a copy with a different draft.
    /// <para>
    /// The last result is dropped, because it described the old draft. Keeping it puts a
    /// stale answer under a fresh expression, so the display reads "48-120 = 112,552".
    /// </para>
    /// </summary>
    public CalculatorSession WithDraft(ExpressionDraft draft) =>
        new(Id, Name, Tint, Scientific, draft, null, History, CreatedAt);

    /// <summary>Returns a copy with a different name. A blank name is ignored.</summary>
    public CalculatorSession Rename(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? this
            : new CalculatorSession(Id, name.Trim(), Tint, Scientific, Draft, LastResult, History, CreatedAt);

    /// <summary>Returns a copy tagged with a different colour.</summary>
    public CalculatorSession WithTint(CalculatorTint tint) =>
        new(Id, Name, tint, Scientific, Draft, LastResult, History, CreatedAt);

    /// <summary>
    /// Returns a copy showing or hiding the scientific keys. Per calculator, so a scratch
    /// pad can stay simple while the one next to it does trigonometry.
    /// </summary>
    public CalculatorSession WithScientific(bool scientific) =>
        new(Id, Name, Tint, scientific, Draft, LastResult, History, CreatedAt);

    /// <summary>
    /// Records a finished calculation: the result becomes the new draft so it can be
    /// typed onto, and the entry goes to the top of the history.
    /// </summary>
    public CalculatorSession WithResult(CalculationEntry entry, decimal value)
    {
        var history = new List<CalculationEntry>(History.Count + 1) { entry };
        history.AddRange(History.Count > HistoryLimit - 1 ? History.Take(HistoryLimit - 1) : History);

        return new CalculatorSession(
            Id, Name, Tint, Scientific, ExpressionDraft.FromValue(value), entry.Result, history, CreatedAt);
    }

    /// <summary>Returns a copy with an empty history. The current draft is left alone.</summary>
    public CalculatorSession WithoutHistory() =>
        new(Id, Name, Tint, Scientific, Draft, LastResult, [], CreatedAt);

    /// <summary>
    /// Returns a copy carrying a different history, for restoring an import. The draft and
    /// the result on screen are left alone: importing old calculations should not disturb
    /// the one in progress.
    /// </summary>
    public CalculatorSession WithHistory(IReadOnlyList<CalculationEntry> history) =>
        new(Id, Name, Tint, Scientific, Draft, LastResult, history, CreatedAt);
}
