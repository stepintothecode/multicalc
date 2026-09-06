using MultiCalc.Domain.Calculation;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class NumberFormatTests
{
    [Theory]
    [InlineData(2.50, "2.5")]
    [InlineData(2.00, "2")]
    [InlineData(0, "0")]
    [InlineData(-1234.5, "-1234.5")]
    [InlineData(1234567, "1234567")]
    public void For_expression_drops_trailing_zeros_and_grouping(decimal value, string expected)
    {
        Assert.Equal(expected, NumberFormat.ForExpression(value));
    }

    [Theory]
    [InlineData(1234567, "1,234,567")]
    [InlineData(1234.5, "1,234.5")]
    [InlineData(999, "999")]
    [InlineData(0.25, "0.25")]
    public void For_display_groups_thousands(decimal value, string expected)
    {
        Assert.Equal(expected, NumberFormat.ForDisplay(value));
    }

}
