using System.Globalization;

namespace MultiCalc.Domain.Calculation;

/// <summary>Turns computed numbers back into text, for the display and for further typing.</summary>
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

    /// <summary>
    /// Adds thousands separators to every number inside an expression, leaving operators
    /// and any half typed trailing point alone.
    /// </summary>
    public static string GroupExpression(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return expression;
        }

        var output = new System.Text.StringBuilder(expression.Length + 8);
        var index = 0;

        while (index < expression.Length)
        {
            if (!char.IsAsciiDigit(expression[index]))
            {
                output.Append(expression[index]);
                index++;
                continue;
            }

            var start = index;
            while (index < expression.Length && char.IsAsciiDigit(expression[index]))
            {
                index++;
            }

            var integerPart = expression[start..index];

            // Only the part before the point is grouped, and only when it is a whole segment
            // rather than the digits that follow a point.
            var followsPoint = start > 0 && expression[start - 1] == '.';
            output.Append(followsPoint ? integerPart : Group(integerPart));

            if (index < expression.Length && expression[index] == '.')
            {
                output.Append('.');
                index++;
            }
        }

        return output.ToString();
    }

    private static string Group(string digits)
    {
        if (digits.Length <= 3)
        {
            return digits;
        }

        var output = new System.Text.StringBuilder(digits.Length + (digits.Length / 3));

        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0)
            {
                output.Append(',');
            }

            output.Append(digits[i]);
        }

        return output.ToString();
    }
}
