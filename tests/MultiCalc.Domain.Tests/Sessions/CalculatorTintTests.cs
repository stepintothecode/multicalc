using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.Sessions;

public sealed class CalculatorTintTests
{
    private static readonly DateTimeOffset At = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(359, 359)]
    [InlineData(360, 0)]
    [InlineData(400, 40)]
    [InlineData(-10, 350)]
    [InlineData(-370, 350)]
    public void Any_number_lands_somewhere_on_the_wheel(int given, int expected)
    {
        Assert.Equal(expected, CalculatorTint.FromHue(given).Hue);
    }

    [Fact]
    public void The_palette_covers_the_wheel_without_repeating()
    {
        var hues = CalculatorTint.Palette.Select(t => t.Hue).ToList();

        Assert.Equal(hues.Count, hues.Distinct().Count());
        Assert.All(hues, hue => Assert.InRange(hue, 0, CalculatorTint.Wheel - 1));
    }

    [Fact]
    public void The_palette_has_a_colour_for_every_calculator_the_app_allows()
    {
        // Otherwise two calculators start out the same colour, which is the whole point.
        Assert.True(CalculatorTint.Palette.Count >= SessionBook.MaxSessions - 1);
    }

    [Fact]
    public void The_style_is_the_custom_property_the_stylesheet_reads()
    {
        Assert.Equal("--hue:210", CalculatorTint.FromHue(210).Style);
    }

    [Fact]
    public void New_calculators_each_get_a_colour_nobody_is_using()
    {
        var book = SessionBook.Start("a", At);

        for (var i = 1; i < 10; i++)
        {
            book = book.AddNew($"id-{i}", At);
        }

        var used = book.Sessions.Select(s => s.Tint).ToList();

        Assert.Equal(used.Count, used.Distinct().Count());
    }

    [Fact]
    public void Closing_a_calculator_frees_its_colour_for_the_next_one()
    {
        var book = SessionBook.Start("a", At).AddNew("b", At).AddNew("c", At);

        var freed = book.Sessions[1].Tint;
        var reduced = book.Remove("b", "unused", At).AddNew("d", At);

        Assert.Equal(freed, reduced.Active.Tint);
    }

    [Fact]
    public void Recolouring_leaves_everything_else_alone()
    {
        var session = CalculatorSession.Create("a", "Rent", At)
            .WithResult(new CalculationEntry("2+3", "5", At), 5m);

        var recoloured = session.WithTint(CalculatorTint.FromHue(300));

        Assert.Equal(300, recoloured.Tint.Hue);
        Assert.Equal("Rent", recoloured.Name);
        Assert.Single(recoloured.History);
        Assert.Equal("5", recoloured.Draft.Expression);
    }

    [Fact]
    public void Renaming_keeps_the_colour()
    {
        var session = CalculatorSession.Create("a", "Rent", At, CalculatorTint.FromHue(45));

        Assert.Equal(45, session.Rename("Groceries").Tint.Hue);
    }

    [Fact]
    public void An_unspecified_tint_is_the_default_rather_than_hue_zero()
    {
        // Hue zero is red, a real choice. "Unspecified" has to mean something else.
        Assert.Equal(CalculatorTint.Default, CalculatorSession.Create("a", "Rent", At).Tint);
    }
}
