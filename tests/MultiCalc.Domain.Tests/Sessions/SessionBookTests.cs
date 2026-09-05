using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.Sessions;

public sealed class SessionBookTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static SessionBook Start() => SessionBook.Start("a", At);

    [Fact]
    public void A_new_book_holds_one_active_calculator()
    {
        var book = Start();

        Assert.Single(book.Sessions);
        Assert.Equal("a", book.ActiveId);
        Assert.Equal("Calculator 1", book.Active.Name);
    }

    [Fact]
    public void Adding_appends_and_activates()
    {
        var book = Start().AddNew("b", At);

        Assert.Equal(2, book.Sessions.Count);
        Assert.Equal("b", book.ActiveId);
        Assert.Equal("Calculator 2", book.Active.Name);
    }

    [Fact]
    public void Names_reuse_the_lowest_free_number()
    {
        var book = Start().AddNew("b", At).AddNew("c", At);

        // Closing the middle one frees "Calculator 2", which the next one takes back.
        var reduced = book.Remove("b", "replacement", At).AddNew("d", At);

        Assert.Equal("Calculator 2", reduced.Active.Name);
    }

    [Fact]
    public void Closing_the_only_calculator_leaves_a_fresh_one()
    {
        var book = Start().Remove("a", "fresh", At);

        Assert.Single(book.Sessions);
        Assert.Equal("fresh", book.ActiveId);
        Assert.True(book.Active.Draft.IsEmpty);
    }

    [Fact]
    public void Closing_the_active_calculator_moves_to_its_neighbour()
    {
        var book = Start().AddNew("b", At).AddNew("c", At).Activate("b");

        var reduced = book.Remove("b", "unused", At);

        Assert.Equal("c", reduced.ActiveId);
    }

    [Fact]
    public void Closing_the_last_calculator_in_the_list_steps_back()
    {
        var book = Start().AddNew("b", At);

        Assert.Equal("a", book.Remove("b", "unused", At).ActiveId);
    }

    [Fact]
    public void Closing_a_background_calculator_leaves_the_active_one_alone()
    {
        var book = Start().AddNew("b", At);

        var reduced = book.Remove("a", "unused", At);

        Assert.Equal("b", reduced.ActiveId);
        Assert.Single(reduced.Sessions);
    }

    [Fact]
    public void An_unknown_id_is_ignored_rather_than_throwing()
    {
        var book = Start();

        Assert.Equal(book.ActiveId, book.Activate("missing").ActiveId);
        Assert.Equal(book.Sessions.Count, book.Remove("missing", "unused", At).Sessions.Count);
    }

    [Fact]
    public void Stepping_wraps_at_both_ends()
    {
        var book = Start().AddNew("b", At).AddNew("c", At);

        // Active is "c", the last one, so forwards wraps to the first.
        Assert.Equal("a", book.Step(1).ActiveId);

        // From the first, backwards wraps to the last.
        Assert.Equal("c", book.Activate("a").Step(-1).ActiveId);
    }

    [Fact]
    public void Stepping_a_single_calculator_does_nothing()
    {
        Assert.Equal("a", Start().Step(1).ActiveId);
    }

    [Fact]
    public void Replacing_swaps_in_the_updated_copy()
    {
        var book = Start();
        var renamed = book.Active.Rename("Groceries");

        Assert.Equal("Groceries", book.Replace(renamed).Active.Name);
    }

    [Fact]
    public void The_book_stops_growing_at_the_limit()
    {
        var book = Start();

        for (var i = 1; i < SessionBook.MaxSessions + 5; i++)
        {
            book = book.AddNew($"id-{i}", At);
        }

        Assert.Equal(SessionBook.MaxSessions, book.Sessions.Count);
        Assert.True(book.IsFull);
    }

    [Fact]
    public void Restoring_an_empty_list_gives_a_working_book_rather_than_an_invalid_one()
    {
        var book = SessionBook.Restore([], null, "fallback", At);

        Assert.Single(book.Sessions);
        Assert.Equal("fallback", book.ActiveId);
    }

    [Fact]
    public void Restoring_an_unknown_active_id_falls_back_to_the_first()
    {
        var sessions = new[] { CalculatorSession.Create("x", "Calculator 1", At) };

        Assert.Equal("x", SessionBook.Restore(sessions, "gone", "unused", At).ActiveId);
    }
}
