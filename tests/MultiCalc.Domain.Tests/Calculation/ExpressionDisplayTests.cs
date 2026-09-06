using MultiCalc.Domain.Calculation;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class ExpressionDisplayTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1.5", "1.5")]
    [InlineData("0.123456", "0.123456")]
    [InlineData("1234+5", "1,234+5")]
    [InlineData("1234567*2", "1,234,567×2")]
    [InlineData("12345.678901", "12,345.678901")]
    public void Numbers_are_grouped_and_operators_are_written_properly(string expression, string expected) =>
        Assert.Equal(expected, ExpressionDisplay.For(expression).Text);

    [Fact]
    public void Eulers_number_is_written_lowercase_but_a_function_name_is_left_alone() =>
        Assert.Equal("2×e+exp(1)", ExpressionDisplay.For("2*E+exp(1)").Text);

    [Fact]
    public void A_caret_on_screen_maps_to_the_same_place_in_the_expression()
    {
        var display = ExpressionDisplay.For("1234+5");

        // "1,234+5": the separator pushes everything after it along by one.
        Assert.Equal(0, display.RawIndex(0));
        Assert.Equal(1, display.RawIndex(1));
        Assert.Equal(4, display.RawIndex(5));
        Assert.Equal(6, display.RawIndex(7));
    }

    [Fact]
    public void A_caret_in_the_expression_maps_back_to_the_screen()
    {
        var display = ExpressionDisplay.For("1234+5");

        Assert.Equal(0, display.DisplayOffset(0));
        Assert.Equal(5, display.DisplayOffset(4));
        Assert.Equal(7, display.DisplayOffset(6));
    }

    [Fact]
    public void A_caret_past_either_end_lands_on_the_nearest_one()
    {
        var display = ExpressionDisplay.For("12");

        Assert.Equal(0, display.RawIndex(-5));
        Assert.Equal(2, display.RawIndex(99));
        Assert.Equal(2, display.DisplayOffset(99));
    }

    [Fact]
    public void Text_from_the_display_comes_back_as_an_expression() =>
        Assert.Equal("1234*5-2", ExpressionDisplay.FromDisplay("1,234×5−2", 0).Expression);

    [Fact]
    public void A_paste_keeps_the_sum_and_drops_the_prose_around_it() =>
        Assert.Equal("12+3", ExpressionDisplay.FromDisplay("total: 12 + 3 please", 0).Expression);

    [Fact]
    public void A_lone_e_is_eulers_number_and_one_inside_a_word_is_not() =>
        Assert.Equal("2*E+exp(1)", ExpressionDisplay.FromDisplay("2×e+exp(1)", 0).Expression);

    [Fact]
    public void The_caret_moves_with_the_characters_that_were_dropped()
    {
        // Caret after the "4" of "1,234": four characters survive before it.
        var (_, caret) = ExpressionDisplay.FromDisplay("1,234×5", 5);

        Assert.Equal(4, caret);
    }

    [Fact]
    public void A_round_trip_through_the_display_leaves_the_expression_alone()
    {
        const string expression = "1234*sin(30)+0.5-π";

        var display = ExpressionDisplay.For(expression);

        Assert.Equal(expression, ExpressionDisplay.FromDisplay(display.Text, 0).Expression);
    }
}
