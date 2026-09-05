using MultiCalc.Domain.Calculation;
using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.Sessions;

public sealed class CalculatorSessionTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static CalculatorSession New() => CalculatorSession.Create("a", "Calculator 1", At);

    [Fact]
    public void A_new_session_is_empty()
    {
        var session = New();

        Assert.True(session.Draft.IsEmpty);
        Assert.Null(session.LastResult);
        Assert.Empty(session.History);
    }

    [Fact]
    public void Recording_a_result_puts_it_on_the_display_ready_to_type_onto()
    {
        var session = New().WithResult(new CalculationEntry("2+3", "5", At), 5m);

        Assert.Equal("5", session.Draft.Expression);
        Assert.Equal("5", session.LastResult);
    }

    [Fact]
    public void History_is_newest_first()
    {
        var session = New()
            .WithResult(new CalculationEntry("1+1", "2", At), 2m)
            .WithResult(new CalculationEntry("2+2", "4", At), 4m);

        Assert.Equal("2+2", session.History[0].Expression);
        Assert.Equal("1+1", session.History[1].Expression);
    }

    [Fact]
    public void History_stops_growing_at_the_limit()
    {
        var session = New();

        for (var i = 0; i < CalculatorSession.HistoryLimit + 20; i++)
        {
            session = session.WithResult(new CalculationEntry($"{i}+0", $"{i}", At), i);
        }

        Assert.Equal(CalculatorSession.HistoryLimit, session.History.Count);

        // The newest survives, the oldest is gone.
        Assert.Equal($"{CalculatorSession.HistoryLimit + 19}+0", session.History[0].Expression);
    }

    [Fact]
    public void Clearing_history_leaves_the_display_alone()
    {
        var session = New()
            .WithResult(new CalculationEntry("2+3", "5", At), 5m)
            .WithoutHistory();

        Assert.Empty(session.History);
        Assert.Equal("5", session.Draft.Expression);
    }

    [Fact]
    public void Renaming_trims_and_ignores_blanks()
    {
        Assert.Equal("Groceries", New().Rename("  Groceries  ").Name);
        Assert.Equal("Calculator 1", New().Rename("   ").Name);
    }

    [Fact]
    public void A_restored_session_keeps_what_was_typed()
    {
        var session = CalculatorSession.Restore("a", "Rent", "12+3", "15", [], At);

        Assert.Equal("12+3", session.Draft.Expression);
        Assert.Equal("15", session.LastResult);
    }

    [Fact]
    public void Typing_over_a_result_drops_it_but_keeps_the_history()
    {
        // Used to keep LastResult, so the display showed the old answer under the new
        // expression: "48-120 = 112,552".
        var session = New().WithResult(new CalculationEntry("2+3", "5", At), 5m);

        var typed = session.WithDraft(ExpressionDraft.FromExpression("9"));

        Assert.Equal("9", typed.Draft.Expression);
        Assert.Null(typed.LastResult);
        Assert.Single(typed.History);
    }

    [Fact]
    public void A_result_stays_on_screen_until_something_is_typed()
    {
        var session = New().WithResult(new CalculationEntry("2+3", "5", At), 5m);

        Assert.Equal("5", session.LastResult);
    }
}
