using MultiCalc.Domain.Calculation;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class PercentExpanderTests
{
    [Fact]
    public void An_expression_without_a_percent_is_untouched()
    {
        Assert.Equal("2+3", PercentExpander.Expand("2+3"));
    }

    [Fact]
    public void A_bare_percent_is_a_hundredth()
    {
        Assert.Equal("((10)/100)", PercentExpander.Expand("10%"));
    }

    [Fact]
    public void After_plus_a_percent_is_a_share_of_the_left_hand_side()
    {
        Assert.Equal("50+((50)*(10)/100)", PercentExpander.Expand("50+10%"));
    }

    [Fact]
    public void After_minus_a_percent_is_a_share_of_the_left_hand_side()
    {
        Assert.Equal("50-((50)*(10)/100)", PercentExpander.Expand("50-10%"));
    }

    [Fact]
    public void The_share_covers_everything_to_the_left_not_just_the_last_number()
    {
        Assert.Equal("2+3+((2+3)*(10)/100)", PercentExpander.Expand("2+3+10%"));
    }

    [Fact]
    public void After_times_a_percent_is_a_hundredth()
    {
        Assert.Equal("200*((10)/100)", PercentExpander.Expand("200*10%"));
    }

    [Fact]
    public void A_percent_on_a_bracketed_group_takes_the_whole_group()
    {
        // The operand is always wrapped, so a group that already has brackets gets a
        // redundant pair. Harmless, and cheaper than working out when it is safe to skip.
        Assert.Equal("(((2+3))/100)", PercentExpander.Expand("(2+3)%"));
    }

    [Fact]
    public void Several_percents_are_all_expanded()
    {
        var expanded = PercentExpander.Expand("10%*20%");

        Assert.DoesNotContain('%', expanded);
    }

    [Fact]
    public void A_marker_with_nothing_in_front_of_it_is_dropped_rather_than_guessed_at()
    {
        Assert.Equal("2+3", PercentExpander.Expand("%2+3"));
    }
}
