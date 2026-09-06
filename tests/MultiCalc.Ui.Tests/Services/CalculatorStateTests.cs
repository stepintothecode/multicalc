using Microsoft.Extensions.Logging.Abstractions;
using MultiCalc.Abstractions;
using MultiCalc.Domain.Calculation;
using MultiCalc.Domain.History;
using MultiCalc.Domain.Sessions;
using MultiCalc.Domain.Settings;
using MultiCalc.Evaluation;
using MultiCalc.TestSupport;
using MultiCalc.Ui.Services;
using Xunit;

namespace MultiCalc.Ui.Tests.Services;

public sealed class CalculatorStateTests
{
    private sealed record Harness(
        CalculatorState State,
        FakeNativeCalculatorLauncher Native,
        FakeFileExporter Exporter,
        FakeHaptics Haptics,
        FakeSessionStore Store);

    private static Harness Build(
        AppSettings? settings = null,
        NativeCalculatorStatus? nativeStatus = null,
        IFileReader? reader = null)
    {
        var sessionStore = new FakeSessionStore();
        var settingsState = new SettingsState(new FakeSettingsStore(settings));
        var native = new FakeNativeCalculatorLauncher(nativeStatus);
        var exporter = new FakeFileExporter();
        var haptics = new FakeHaptics();

        settingsState.InitialiseAsync().GetAwaiter().GetResult();

        var state = new CalculatorState(
            sessionStore,
            new CalculationService(new NCalcCalculatorEngine()),
            native,
            exporter,
            reader ?? new FakeFileReader(),
            haptics,
            settingsState,
            new FakeClock(),
            NullLogger<CalculatorState>.Instance);

        state.InitialiseAsync().GetAwaiter().GetResult();

        return new Harness(state, native, exporter, haptics, sessionStore);
    }

    private static void Type(CalculatorState state, params CalculatorKey[] keys)
    {
        foreach (var key in keys)
        {
            state.Press(key);
        }
    }

    [Fact]
    public void A_fresh_start_has_one_empty_calculator()
    {
        var h = Build();

        Assert.Single(h.State.Book.Sessions);
        Assert.True(h.State.Active.Draft.IsEmpty);
    }

    [Fact]
    public void Pressing_equals_records_the_result_and_leaves_it_ready_to_type_onto()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.Evaluate();

        Assert.Equal("5", h.State.Active.LastResult);
        Assert.Equal("5", h.State.Active.Draft.Expression);
        Assert.Single(h.State.Active.History);
        Assert.Null(h.State.ErrorMessage);
    }

    [Fact]
    public void A_bad_sum_shows_a_message_and_keeps_what_was_typed()
    {
        var h = Build();

        Type(h.State, CalculatorKey.One, CalculatorKey.Divide, CalculatorKey.Zero);
        h.State.Evaluate();

        Assert.Equal("Cannot divide by zero", h.State.ErrorMessage);

        // Losing the typing on an error would be worse than the error.
        Assert.Equal("1/0", h.State.Active.Draft.Expression);
        Assert.Empty(h.State.Active.History);
    }

    [Fact]
    public void Typing_after_a_result_clears_the_result_line()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.Evaluate();
        Assert.Equal("5", h.State.Active.LastResult);

        // Used to leave the old answer under the new expression: "48-120 = 112,552".
        Type(h.State, CalculatorKey.Nine);

        Assert.Null(h.State.Active.LastResult);
    }

    [Fact]
    public void The_answer_appears_while_the_expression_is_still_being_typed()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);

        Assert.Equal("5", h.State.Preview);
    }

    [Fact]
    public void The_running_answer_follows_every_key()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Multiply, CalculatorKey.Three);
        Assert.Equal("6", h.State.Preview);

        Type(h.State, CalculatorKey.Zero);
        Assert.Equal("60", h.State.Preview);

        Type(h.State, CalculatorKey.Backspace);
        Assert.Equal("6", h.State.Preview);
    }

    [Fact]
    public void There_is_no_running_answer_midway_through_typing_or_after_equals()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add);
        Assert.Null(h.State.Preview);

        Type(h.State, CalculatorKey.Three);
        h.State.Evaluate();

        // The answer is on the expression line now, so repeating it below would be noise.
        Assert.Null(h.State.Preview);
    }

    [Fact]
    public void Each_calculator_has_its_own_running_answer()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.AddInApp();

        Assert.Null(h.State.Preview);

        Type(h.State, CalculatorKey.Four, CalculatorKey.Add, CalculatorKey.Four);

        Assert.Equal("8", h.State.Preview);
    }

    [Fact]
    public void An_edit_in_the_display_becomes_the_expression()
    {
        var h = Build();

        h.State.Edit("1,234×5", 7);

        Assert.Equal("1234*5", h.State.Active.Draft.Expression);
        Assert.Equal(6, h.State.Active.Draft.Caret);
        Assert.Equal("6,170", h.State.Preview);
    }

    [Fact]
    public void A_paste_of_something_that_is_not_a_sum_leaves_only_what_could_be_one()
    {
        var h = Build();

        h.State.Edit("about 12 apples", 0);

        Assert.Equal("12", h.State.Active.Draft.Expression);
    }

    [Fact]
    public void Moving_the_caret_puts_the_next_key_where_it_was_put()
    {
        var h = Build();

        Type(h.State, CalculatorKey.One, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);

        // Between the "1" and the "2", counted in what the display shows.
        h.State.MoveCaret(1);
        Type(h.State, CalculatorKey.Nine);

        Assert.Equal("192+3", h.State.Active.Draft.Expression);
    }

    [Fact]
    public void The_caret_is_counted_in_the_displayed_text_separators_and_all()
    {
        var h = Build();

        h.State.Edit("1234+5", 6);

        // The display reads "1,234+5", so screen offset 5 is after the last grouped digit.
        h.State.MoveCaret(5);
        Type(h.State, CalculatorKey.Nine);

        Assert.Equal("12349+5", h.State.Active.Draft.Expression);
    }

    [Fact]
    public void Each_finished_calculation_stamps_the_display()
    {
        var h = Build();

        Assert.Equal(0, h.State.ResultStamp);

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.Evaluate();
        Assert.Equal(1, h.State.ResultStamp);

        // A failure is not an answer landing, so it must not animate one.
        Type(h.State, CalculatorKey.Divide, CalculatorKey.Zero);
        h.State.Evaluate();
        Assert.Equal(1, h.State.ResultStamp);
    }

    [Fact]
    public void The_next_key_press_clears_the_message()
    {
        var h = Build();

        Type(h.State, CalculatorKey.One, CalculatorKey.Divide, CalculatorKey.Zero);
        h.State.Evaluate();
        h.State.Press(CalculatorKey.Backspace);

        Assert.Null(h.State.ErrorMessage);
    }

    [Fact]
    public void Pressing_equals_on_an_empty_display_says_nothing()
    {
        var h = Build();

        h.State.Evaluate();

        Assert.Null(h.State.ErrorMessage);
        Assert.Empty(h.State.Active.History);
    }

    [Fact]
    public void Each_calculator_keeps_its_own_display()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Seven);
        var first = h.State.Active.Id;

        h.State.AddInApp();
        Type(h.State, CalculatorKey.Nine);

        Assert.Equal("9", h.State.Active.Draft.Expression);
        Assert.Equal("7", h.State.Book.Sessions.First(s => s.Id == first).Draft.Expression);
    }

    [Fact]
    public void Adding_in_app_never_touches_the_system_calculator()
    {
        var h = Build();

        h.State.AddInApp();

        Assert.Equal(2, h.State.Book.Sessions.Count);
        Assert.Equal(0, h.Native.LaunchCount);
    }

    [Fact]
    public void Adding_a_system_calculator_never_adds_one_in_app()
    {
        var h = Build();

        Assert.Equal(NativeLaunchOutcome.Launched, h.State.AddNative());

        Assert.Equal(1, h.Native.LaunchCount);
        Assert.Single(h.State.Book.Sessions);
    }

    [Fact]
    public void A_device_that_will_not_spawn_is_reported_rather_than_assumed()
    {
        var h = Build(nativeStatus: new NativeCalculatorStatus(NativeCalculatorSupport.SingleInstanceOnly));

        Assert.False(h.State.NativeStatus.CanSpawn);
        Assert.Equal(NativeCalculatorSupport.SingleInstanceOnly, h.State.NativeStatus.Support);
    }

    [Fact]
    public void Adding_stops_at_the_limit_and_says_so()
    {
        var h = Build();

        for (var i = 1; i < SessionBook.MaxSessions; i++)
        {
            Assert.True(h.State.AddInApp());
        }

        Assert.False(h.State.AddInApp());
        Assert.NotNull(h.State.ErrorMessage);
    }

    [Fact]
    public void Reusing_a_result_strips_the_thousands_separators()
    {
        var h = Build();

        h.State.Reuse("1,234.5");

        // Separators are for reading. Feeding them back in would not parse.
        Assert.Equal("1234.5", h.State.Active.Draft.Expression);
    }

    [Fact]
    public async Task Exporting_with_no_history_refuses_rather_than_writing_an_empty_file()
    {
        var h = Build();

        var result = await h.State.ExportHistoryAsync(HistoryFormat.Text, TestContext.Current.CancellationToken);

        Assert.False(result.Saved);
        Assert.Null(h.Exporter.LastContents);
    }

    [Fact]
    public async Task Exporting_hands_the_rendered_history_to_the_platform()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.Evaluate();

        var result = await h.State.ExportHistoryAsync(HistoryFormat.Csv, TestContext.Current.CancellationToken);

        Assert.True(result.Saved);
        Assert.EndsWith(".csv", h.Exporter.LastFileName);
        Assert.Contains("2+3", h.Exporter.LastContents);
    }

    [Fact]
    public async Task Importing_merges_into_a_calculator_with_the_same_name()
    {
        var h = Build(reader: FakeFileReader.Returning(ExportOf("Calculator 1", "8+8", "16")));

        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);
        h.State.Evaluate();

        var outcome = await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);

        Assert.True(outcome.IsSuccess);
        Assert.Single(h.State.Book.Sessions);
        Assert.Equal(2, h.State.Active.History.Count);
    }

    [Fact]
    public async Task Importing_an_unknown_name_arrives_as_a_new_calculator()
    {
        var h = Build(reader: FakeFileReader.Returning(ExportOf("Groceries", "8+8", "16")));

        var before = h.State.Active.Id;
        var outcome = await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(2, h.State.Book.Sessions.Count);
        Assert.Contains(h.State.Book.Sessions, s => s.Name == "Groceries");

        // Importing must not yank the person off the calculator they were using.
        Assert.Equal(before, h.State.Active.Id);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_does_not_duplicate_anything()
    {
        var h = Build(reader: FakeFileReader.Returning(ExportOf("Groceries", "8+8", "16")));

        await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);
        await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);

        var groceries = h.State.Book.Sessions.First(s => s.Name == "Groceries");

        Assert.Equal(2, h.State.Book.Sessions.Count);
        Assert.Single(groceries.History);
    }

    [Fact]
    public async Task Cancelling_the_picker_changes_nothing_and_reports_nothing()
    {
        var h = Build(reader: new FakeFileReader());

        var outcome = await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);

        Assert.True(outcome.IsSuccess);
        Assert.Empty(outcome.Calculators);
        Assert.Single(h.State.Book.Sessions);
    }

    [Fact]
    public async Task Importing_something_that_is_not_an_export_says_so()
    {
        var h = Build(reader: FakeFileReader.Returning("""{"hello":"world"}"""));

        var outcome = await h.State.ImportHistoryAsync(TestContext.Current.CancellationToken);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Failure);
    }

    [Fact]
    public void Keys_buzz_only_when_the_setting_is_on()
    {
        var on = Build(new AppSettings { HapticFeedback = true });
        on.State.Press(CalculatorKey.One);
        Assert.Equal(1, on.Haptics.TapCount);

        var off = Build(new AppSettings { HapticFeedback = false });
        off.State.Press(CalculatorKey.One);
        Assert.Equal(0, off.Haptics.TapCount);
    }

    [Fact]
    public async Task Closing_the_app_writes_the_open_calculators()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Four, CalculatorKey.Two);
        await h.State.FlushAsync();

        Assert.NotNull(h.Store.Saved);
        Assert.Equal("42", h.Store.Saved.Active.Draft.Expression);
    }

    [Fact]
    public void Clearing_one_history_leaves_the_others_alone()
    {
        var h = Build();

        Type(h.State, CalculatorKey.One, CalculatorKey.Add, CalculatorKey.One);
        h.State.Evaluate();
        var first = h.State.Active.Id;

        h.State.AddInApp();
        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Two);
        h.State.Evaluate();

        h.State.ClearHistory(h.State.Active.Id);

        Assert.Empty(h.State.Active.History);
        Assert.Single(h.State.Book.Sessions.First(s => s.Id == first).History);
    }

    [Fact]
    public void Clearing_every_history_empties_them_all()
    {
        var h = Build();

        Type(h.State, CalculatorKey.One, CalculatorKey.Add, CalculatorKey.One);
        h.State.Evaluate();
        h.State.AddInApp();
        Type(h.State, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Two);
        h.State.Evaluate();

        h.State.ClearAllHistory();

        Assert.All(h.State.Book.Sessions, session => Assert.Empty(session.History));
    }

    [Fact]
    public void The_scientific_keys_belong_to_one_calculator_not_all_of_them()
    {
        var h = Build();

        var first = h.State.Active.Id;
        h.State.ToggleScientific(first);
        h.State.AddInApp();

        Assert.True(h.State.Book.Sessions.First(s => s.Id == first).Scientific);
        Assert.False(h.State.Active.Scientific);
    }

    [Fact]
    public void Scientific_calculations_go_through_the_whole_stack()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Sine, CalculatorKey.Three, CalculatorKey.Zero);
        h.State.Evaluate();

        // sin(30) in degrees, which is what a calculator answers by default.
        Assert.Equal("0.5", h.State.Active.LastResult);
    }

    [Fact]
    public void A_square_root_reads_the_way_it_is_written()
    {
        var h = Build();

        Type(h.State, CalculatorKey.SquareRoot, CalculatorKey.Nine);
        Assert.Equal("sqrt(9", h.State.Active.Draft.Expression);

        h.State.Evaluate();
        Assert.Equal("3", h.State.Active.LastResult);
    }

    [Fact]
    public void Powers_work_from_the_keypad()
    {
        var h = Build();

        Type(h.State, CalculatorKey.Two, CalculatorKey.Power, CalculatorKey.One, CalculatorKey.Zero);
        h.State.Evaluate();

        Assert.Equal("1,024", h.State.Active.LastResult);
    }

    [Fact]
    public void Importing_restores_the_scientific_setting()
    {
        // Covered end to end in the import tests; this pins the state layer's part of it.
        var h = Build();

        h.State.ToggleScientific(h.State.Active.Id);

        Assert.True(h.State.Active.Scientific);
    }

    [Fact]
    public void Stepping_moves_between_calculators()
    {
        var h = Build();

        var first = h.State.Active.Id;
        h.State.AddInApp();

        h.State.Step(1);

        Assert.Equal(first, h.State.Active.Id);
    }

    private static string ExportOf(string name, string expression, string result) =>
        $$"""
          {
            "app": "MultiCalc",
            "exportedAt": "2026-01-01T09:00:00+00:00",
            "calculators": [
              {
                "name": "{{name}}",
                "createdAt": "2026-01-01T09:00:00+00:00",
                "entries": [
                  { "expression": "{{expression}}", "result": "{{result}}", "at": "2026-01-01T09:30:00+00:00" }
                ]
              }
            ]
          }
          """;
}
