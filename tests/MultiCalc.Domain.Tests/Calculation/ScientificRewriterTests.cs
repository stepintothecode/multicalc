using MultiCalc.Domain.Calculation;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class ScientificRewriterTests
{
    [Fact]
    public void Plain_arithmetic_is_untouched()
    {
        Assert.Equal("2+3*4", ScientificRewriter.Rewrite("2+3*4"));
    }

    [Theory]
    [InlineData("sqrt(9)")]
    [InlineData("abs(0-4)")]
    [InlineData("sin(30)")]
    [InlineData("cos(0)")]
    [InlineData("tan(45)")]
    [InlineData("ln(1)")]
    [InlineData("exp(1)")]
    [InlineData("asin(1)")]
    [InlineData("acos(1)")]
    [InlineData("atan(1)")]
    public void Built_in_functions_go_through_untouched(string draft)
    {
        // The engine matches these regardless of case, so there is nothing to rename and
        // no chance of one pattern corrupting the result of another.
        Assert.Equal(draft, ScientificRewriter.Rewrite(draft));
    }

    [Fact]
    public void Log_means_base_ten_the_way_a_calculator_key_does()
    {
        Assert.Equal("Log10(100)", ScientificRewriter.Rewrite("log(100)"));
    }

    [Fact]
    public void Renaming_log_does_not_disturb_the_inverse_functions()
    {
        // An earlier version renamed every function and turned "asin(" into "ASin(",
        // because "sin(" still matched inside the "Asin(" it had just produced.
        Assert.Equal("asin(1)+Log10(10)", ScientificRewriter.Rewrite("asin(1)+log(10)"));
    }

    [Fact]
    public void Constants_become_numbers_precise_enough_not_to_be_the_limit()
    {
        var pi = ScientificRewriter.Rewrite("π");

        Assert.StartsWith("3.14159265358979", pi, StringComparison.Ordinal);
        Assert.StartsWith("2.71828182845904", ScientificRewriter.Rewrite("E"), StringComparison.Ordinal);
    }

    [Fact]
    public void A_constant_inside_an_expression_keeps_the_rest_of_it()
    {
        Assert.Equal("2*" + ScientificRewriter.Rewrite("π"), ScientificRewriter.Rewrite("2*π"));
    }

    [Theory]
    [InlineData("2^10", "Pow(2,10)")]
    [InlineData("5^2", "Pow(5,2)")]
    [InlineData("2.5^2", "Pow(2.5,2)")]
    public void Powers_become_a_function_because_the_engine_has_no_power_operator(string draft, string expected)
    {
        Assert.Equal(expected, ScientificRewriter.Rewrite(draft));
    }

    [Fact]
    public void A_power_keeps_what_is_around_it()
    {
        Assert.Equal("1+Pow(2,3)+4", ScientificRewriter.Rewrite("1+2^3+4"));
    }

    [Fact]
    public void A_bracketed_group_can_be_the_base_or_the_exponent()
    {
        Assert.Equal("Pow((1+2),(3+4))", ScientificRewriter.Rewrite("(1+2)^(3+4)"));
    }

    [Fact]
    public void A_function_call_can_be_the_base()
    {
        Assert.Equal("Pow(sqrt(9),2)", ScientificRewriter.Rewrite("sqrt(9)^2"));
    }

    [Fact]
    public void A_half_typed_power_is_dropped_rather_than_guessed_at()
    {
        Assert.Equal("5", ScientificRewriter.Rewrite("5^"));
    }
}
