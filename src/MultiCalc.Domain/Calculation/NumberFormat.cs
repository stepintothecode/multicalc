using System.Globalization;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// Turns computed numbers back into text, for the display and for further typing. Grouping
/// an expression rather than a single number belongs to <see cref="ExpressionDisplay"/>.
/// </summary>
public static class NumberFormat
{
    /// <summary>
    /// Plain digits, no grouping, so the text can be fed straight back into an expression.
    /// Trailing zeros after the point are dropped: 2.50 becomes "2.5", 2.00 becomes "2".
    /// </summary>
    public static string ForExpression(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);

    /// <summary>
    /// The same number with thousands separators, for reading rather than re-entering.
    /// </summary>
    public static string ForDisplay(decimal value) =>
        value.ToString("#,##0.############################", CultureInfo.InvariantCulture);

}
