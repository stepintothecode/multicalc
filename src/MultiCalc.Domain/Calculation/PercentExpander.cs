namespace MultiCalc.Domain.Calculation;

/// <summary>
/// Rewrites the "%" marker into arithmetic, just before evaluation.
/// <para>
/// The display keeps "50+10%" because that is what was typed. Evaluation needs the meaning,
/// and on a calculator that meaning depends on the operator in front of it:
/// after plus or minus a percent is a share of the left hand side, so 50+10% is 55;
/// after times or divide, or on its own, it is simply a hundredth, so 200*10% is 20.
/// </para>
/// </summary>
public static class PercentExpander
{
    // One pass removes exactly one marker, so this only ever trips on absurd input.
    private const int MaxPasses = 64;

    /// <summary>Returns <paramref name="expression"/> with every percent marker expanded.</summary>
    public static string Expand(string expression)
    {
        if (string.IsNullOrEmpty(expression) || !expression.Contains('%'))
        {
            return expression;
        }

        var current = expression;

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var marker = current.IndexOf('%', StringComparison.Ordinal);

            if (marker < 0)
            {
                return current;
            }

            current = ExpandOne(current, marker);
        }

        return current;
    }

    private static string ExpandOne(string expression, int marker)
    {
        var operandStart = OperandStart(expression, marker);
        var operand = expression[operandStart..marker];
        var before = expression[..operandStart];
        var after = expression[(marker + 1)..];

        if (operand.Length == 0)
        {
            // A stray marker with nothing in front of it. Drop it rather than invent a value.
            return before + after;
        }

        var isShareOfLeft = before.Length > 1 && (before[^1] == '+' || before[^1] == '-');

        if (isShareOfLeft)
        {
            var op = before[^1];
            var lhs = before[..^1];

            return $"{lhs}{op}(({lhs})*({operand})/100){after}";
        }

        return $"{before}(({operand})/100){after}";
    }

    /// <summary>Walks back from the marker to the start of the value it applies to.</summary>
    private static int OperandStart(string expression, int marker)
    {
        if (marker == 0)
        {
            return 0;
        }

        if (expression[marker - 1] == ')')
        {
            var depth = 0;

            for (var i = marker - 1; i >= 0; i--)
            {
                if (expression[i] == ')')
                {
                    depth++;
                }
                else if (expression[i] == '(')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        var start = marker;

        while (start > 0 && (char.IsAsciiDigit(expression[start - 1]) || expression[start - 1] == '.'))
        {
            start--;
        }

        return start;
    }
}
