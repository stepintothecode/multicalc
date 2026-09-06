using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;
using MultiCalc.Domain.Calculation;
using MultiCalc.Domain.History;
using MultiCalc.Domain.Sessions;

namespace MultiCalc.Ui.Services;

/// <summary>
/// The open calculators and everything the UI does to them. One instance for the whole app,
/// so the keypad, the calculator list and the history sheet are always looking at the same
/// thing.
/// </summary>
public sealed class CalculatorState
{
    private const int SaveDelayMilliseconds = 600;

    private readonly ISessionStore store;
    private readonly CalculationService calculator;
    private readonly INativeCalculatorLauncher nativeLauncher;
    private readonly IFileExporter exporter;
    private readonly IFileReader reader;
    private readonly IHaptics haptics;
    private readonly SettingsState settings;
    private readonly IClock clock;
    private readonly ILogger<CalculatorState> logger;

    private CancellationTokenSource? pendingSave;
    private string? previewOf;
    private AngleMode previewAngles;
    private string? preview;

    /// <summary>Creates the state. Nothing is read from storage until <see cref="InitialiseAsync"/>.</summary>
    public CalculatorState(
        ISessionStore store,
        CalculationService calculator,
        INativeCalculatorLauncher nativeLauncher,
        IFileExporter exporter,
        IFileReader reader,
        IHaptics haptics,
        SettingsState settings,
        IClock clock,
        ILogger<CalculatorState> logger)
    {
        this.store = store;
        this.calculator = calculator;
        this.nativeLauncher = nativeLauncher;
        this.exporter = exporter;
        this.reader = reader;
        this.haptics = haptics;
        this.settings = settings;
        this.clock = clock;
        this.logger = logger;

        Book = SessionBook.Start(NewId(), clock.UtcNow);
        NativeStatus = NativeCalculatorStatus.Unsupported;
    }

    /// <summary>Raised whenever anything on screen would change.</summary>
    public event Action? Changed;

    /// <summary>Every open calculator, and which one is showing.</summary>
    public SessionBook Book { get; private set; }

    /// <summary>The calculator currently on screen.</summary>
    public CalculatorSession Active => Book.Active;

    /// <summary>What the device will let us do with its own calculator.</summary>
    public NativeCalculatorStatus NativeStatus { get; private set; }

    /// <summary>The message under the display, or null when there is nothing wrong.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Counts finished calculations. The display uses it to know that an answer has just
    /// landed, so it can play the same animation twice in a row when the answer is the same.
    /// </summary>
    public int ResultStamp { get; private set; }

    /// <summary>
    /// The running answer for the active calculator, or null when there is nothing worth
    /// showing yet. Waiting for equals to find out what "18*7.5" comes to is most of the
    /// waiting a calculator ever asks anyone to do.
    /// </summary>
    /// <remarks>
    /// Worked out on demand and remembered, rather than recomputed on every render: the
    /// answer only changes when the expression or the angle unit does.
    /// </remarks>
    public string? Preview
    {
        get
        {
            var expression = Active.Draft.Expression;
            var angles = settings.Current.Angles;

            if (previewOf == expression && previewAngles == angles)
            {
                return preview;
            }

            previewOf = expression;
            previewAngles = angles;
            preview = calculator.Preview(Active.Draft, angles);

            return preview;
        }
    }

    /// <summary>Restores the saved calculators and probes the device calculator.</summary>
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        var restored = await store.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (restored is not null)
        {
            Book = restored;
        }

        RefreshNativeStatus();
        Changed?.Invoke();
    }

    /// <summary>
    /// Asks the system again what it allows. Cheap enough to call on every visit to the
    /// calculator list, because the answer changes if a different calculator is installed.
    /// </summary>
    public void RefreshNativeStatus()
    {
        try
        {
            NativeStatus = nativeLauncher.Probe();
        }
        catch (Exception ex)
        {
            // A probe that throws must not stop the app opening.
            logger.LogWarning(ex, "Could not probe the device calculator");
            NativeStatus = NativeCalculatorStatus.Unsupported;
        }
    }

    /// <summary>Applies a key press to the active calculator.</summary>
    public void Press(CalculatorKey key)
    {
        Buzz();
        ErrorMessage = null;

        Update(Active.WithDraft(Active.Draft.Press(key)));
    }

    /// <summary>
    /// Takes an edit made in the display itself: a paste, a deleted selection, a hardware
    /// keyboard. The text arrives as the display writes it, separators and all.
    /// </summary>
    /// <param name="text">What the display now reads.</param>
    /// <param name="caret">Where the caret sits in that text.</param>
    public void Edit(string text, int caret)
    {
        ErrorMessage = null;

        var (expression, at) = ExpressionDisplay.FromDisplay(text, caret);

        if (expression == Active.Draft.Expression)
        {
            // Only the caret moved, or the edit was entirely characters we drop. Either way
            // there is nothing to save and nothing to redraw beyond the caret.
            MoveCaret(caret);
            return;
        }

        Update(Active.WithDraft(ExpressionDraft.FromEdit(expression, at)));
    }

    /// <summary>
    /// Moves the caret to a place in the displayed text. Nothing is saved: where the caret
    /// sits is not worth a write, and it does not survive closing the app.
    /// </summary>
    public void MoveCaret(int displayOffset)
    {
        var moved = Active.Draft.WithCaret(Active.Draft.Display.RawIndex(displayOffset));

        if (!ReferenceEquals(moved, Active.Draft))
        {
            Book = Book.Replace(Active.WithDraft(moved));
        }
    }

    /// <summary>
    /// Works out what is typed. A failure leaves the expression alone so it can be corrected,
    /// rather than clearing the display and losing the typing.
    /// </summary>
    public void Evaluate()
    {
        Buzz();

        var outcome = calculator.Evaluate(Active.Draft, settings.Current.Angles);

        if (!outcome.IsSuccess)
        {
            ErrorMessage = EvaluationMessages.For(outcome.Error);
            Changed?.Invoke();
            return;
        }

        ErrorMessage = null;
        ResultStamp++;

        var entry = new CalculationEntry(
            Active.Draft.ToDisplay(),
            NumberFormat.ForDisplay(outcome.Value),
            clock.UtcNow);

        Update(Active.WithResult(entry, outcome.Value));
    }

    /// <summary>Adds a calculator inside this app and shows it.</summary>
    /// <returns>False when the limit has been reached.</returns>
    public bool AddInApp()
    {
        Buzz();

        if (Book.IsFull)
        {
            ErrorMessage = $"That is the limit of {SessionBook.MaxSessions} calculators";
            Changed?.Invoke();
            return false;
        }

        ErrorMessage = null;
        Book = Book.AddNew(NewId(), clock.UtcNow);
        Persist(immediate: true);

        return true;
    }

    /// <summary>Spawns another copy of the device's own calculator.</summary>
    public NativeLaunchOutcome AddNative()
    {
        Buzz();

        try
        {
            var outcome = nativeLauncher.LaunchNewInstance();

            if (outcome != NativeLaunchOutcome.Launched)
            {
                // The device changed its mind since the last probe. Ask again so the UI
                // stops offering something that no longer works.
                RefreshNativeStatus();
                ErrorMessage = "This device would not open another system calculator";
                Changed?.Invoke();
            }

            return outcome;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not start a native calculator");
            ErrorMessage = "This device would not open another system calculator";
            Changed?.Invoke();

            return NativeLaunchOutcome.Failed;
        }
    }

    /// <summary>Brings a calculator to the front.</summary>
    public void Activate(string id)
    {
        ErrorMessage = null;
        Book = Book.Activate(id);
        Persist(immediate: true);
    }

    /// <summary>Moves to the next or previous calculator.</summary>
    public void Step(int offset)
    {
        ErrorMessage = null;
        Book = Book.Step(offset);
        Persist(immediate: true);
    }

    /// <summary>Closes a calculator.</summary>
    public void Close(string id)
    {
        ErrorMessage = null;
        Book = Book.Remove(id, NewId(), clock.UtcNow);
        Persist(immediate: true);
    }

    /// <summary>Renames a calculator. A blank name is ignored.</summary>
    public void Rename(string id, string name)
    {
        var session = Book.Sessions.FirstOrDefault(s => s.Id == id);

        if (session is null)
        {
            return;
        }

        Book = Book.Replace(session.Rename(name));
        Persist(immediate: true);
    }

    /// <summary>Tags a calculator with a colour. An unknown id is ignored.</summary>
    public void SetTint(string id, CalculatorTint tint)
    {
        var session = Book.Sessions.FirstOrDefault(s => s.Id == id);

        if (session is null || session.Tint == tint)
        {
            return;
        }

        Buzz();
        Book = Book.Replace(session.WithTint(tint));
        Persist(immediate: true);
    }

    /// <summary>Shows or hides the scientific keys on one calculator.</summary>
    public void ToggleScientific(string id)
    {
        var session = Book.Sessions.FirstOrDefault(s => s.Id == id);

        if (session is null)
        {
            return;
        }

        Buzz();
        Book = Book.Replace(session.WithScientific(!session.Scientific));
        Persist(immediate: true);
    }

    /// <summary>Empties one calculator's history. The display is left alone.</summary>
    public void ClearHistory(string id)
    {
        var session = Book.Sessions.FirstOrDefault(s => s.Id == id);

        if (session is null)
        {
            return;
        }

        Book = Book.Replace(session.WithoutHistory());
        Persist(immediate: true);
    }

    /// <summary>Empties every calculator's history at once.</summary>
    public void ClearAllHistory()
    {
        foreach (var session in Book.Sessions)
        {
            Book = Book.Replace(session.WithoutHistory());
        }

        Persist(immediate: true);
    }

    /// <summary>Puts a past result back on the display of the active calculator.</summary>
    public void Reuse(string value)
    {
        Buzz();
        ErrorMessage = null;

        // The stored result carries thousands separators, which are not valid input.
        var plain = value.Replace(",", string.Empty, StringComparison.Ordinal);

        Update(Active.WithDraft(ExpressionDraft.FromExpression(plain)));
    }

    /// <summary>Writes every calculator's history to a file the person chooses.</summary>
    public async Task<FileExportResult> ExportHistoryAsync(
        HistoryFormat format,
        CancellationToken cancellationToken = default)
    {
        if (HistoryExport.IsEmpty(Book.Sessions))
        {
            return FileExportResult.Error("There is no history to export yet");
        }

        var at = clock.UtcNow;
        var contents = HistoryExport.Render(Book.Sessions, format, at);

        try
        {
            return await exporter
                .SaveAsync(HistoryExport.BuildFileName(format, at), contents, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not export history");
            return FileExportResult.Error("Could not save the file");
        }
    }

    /// <summary>
    /// Reads a previously exported JSON file back in. A calculator whose name matches one
    /// that is open has its entries merged; anything else arrives as a new calculator.
    /// Nothing is ever overwritten or removed.
    /// </summary>
    public async Task<ImportOutcome> ImportHistoryAsync(CancellationToken cancellationToken = default)
    {
        FileReadResult file;

        try
        {
            file = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read a history file");
            return ImportOutcome.Error("Could not read that file");
        }

        if (file.Failure is { } failure)
        {
            return ImportOutcome.Error(failure);
        }

        if (file.Contents is not { } contents)
        {
            // Cancelled. Nothing to say and nothing to do.
            return new ImportOutcome([]);
        }

        var parsed = HistoryImport.Parse(contents);

        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        Apply(parsed);
        Persist(immediate: true);

        return parsed;
    }

    private void Apply(ImportOutcome parsed)
    {
        foreach (var imported in parsed.Calculators)
        {
            var existing = Book.Sessions.FirstOrDefault(s =>
                string.Equals(s.Name, imported.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                Book = Book.Replace(
                    existing.WithHistory(HistoryMerge.Combine(existing.History, imported.Entries)));

                continue;
            }

            if (Book.IsFull)
            {
                logger.LogInformation("Skipped importing {Name}: no room for another calculator", imported.Name);
                continue;
            }

            var id = NewId();
            var before = Book.ActiveId;

            Book = Book.AddNew(id, clock.UtcNow);

            var added = Book.Sessions.First(s => s.Id == id);
            var restored = added
                .Rename(imported.Name)
                .WithHistory(imported.Entries)
                .WithScientific(imported.Scientific);

            // A file that carried colours restores them; an older one keeps the colour the
            // new calculator was just handed.
            Book = Book.Replace(imported.Tint is { } tint ? restored.WithTint(tint) : restored);

            // Importing should not yank the person off the calculator they were using.
            Book = Book.Activate(before);
        }
    }

    /// <summary>Writes the open calculators now. Called when the app goes to the background.</summary>
    public async Task FlushAsync()
    {
        pendingSave?.Cancel();

        try
        {
            await store.SaveAsync(Book, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not save the open calculators");
        }
    }

    private void Update(CalculatorSession session)
    {
        Book = Book.Replace(session);
        Persist(immediate: false);
    }

    private void Persist(bool immediate)
    {
        Changed?.Invoke();

        pendingSave?.Cancel();
        pendingSave = new CancellationTokenSource();

        var token = pendingSave.Token;
        var snapshot = Book;
        var delay = immediate ? 0 : SaveDelayMilliseconds;

        // Typing fires this on every key, so writes are coalesced rather than run per press.
        _ = Task.Run(
            async () =>
            {
                try
                {
                    if (delay > 0)
                    {
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }

                    await store.SaveAsync(snapshot, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Superseded by a later change. Nothing to do.
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not save the open calculators");
                }
            },
            CancellationToken.None);
    }

    private void Buzz()
    {
        if (!settings.Current.HapticFeedback)
        {
            return;
        }

        try
        {
            haptics.Tap();
        }
        catch (Exception ex)
        {
            // A device with no vibrator is not a reason to drop the key press.
            logger.LogDebug(ex, "Haptics unavailable");
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("n");
}
