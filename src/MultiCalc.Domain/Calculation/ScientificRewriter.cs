using System.Globalization;
using System.Text;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// Turns the scientific parts of a draft into something the engine understands.
/// <para>
/// The display keeps what a person would write: "sqrt(", "log(", "2^10", "π". The engine
/// wants its own spelling and has no power operator, so the swap happens here, once, on
/// the way in. Same arrangement as <see cref="PercentExpander"/>.
/// </para>
/// </summary>
public static class ScientificRewriter
{
    /// <summary>Enough digits to outlast decimal's precision, so the constant never limits the answer.</summary>
    private const string PiLiteral = "3.14159265358979323846264338328";

    private const string ELiteral = "2.71828182845904523536028747135";

    /// <summary>
    /// The only function that needs renaming. Everything else the keypad can type is a
    /// built-in under the same name, and the engine is asked to ignore case, so sqrt, sin,
    /// asin, ln and the rest go through untouched.
    /// <para>
    /// Renaming them all was tried first and was quietly wrong: replacing "sin(" after
    /// "asin(" had already become "Asin(" turned it into "ASin(", because the second
    /// pattern still matched inside the result of the first.
    /// </para>
    /// </summary>
    private static readonly (string Draft, string Engine)[] FunctionNames =
    [
        ("log(", "Log10("),
    ];

    // One pass removes exactly one operator, so this only trips on absurd input.
    private const int MaxPasses = 64;

    /// <summary>Rewrites <paramref name="expression"/> into engine syntax.</summary>
    public static string Rewrite(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return expression;
        }

        return ExpandPowers(ReplaceNames(ReplaceConstants(expression)));
    }

    private static string ReplaceConstants(string expression)
    {
        if (!expression.Contains('π') && !expression.Contains('E'))
        {
            return expression;
        }

        var output = new StringBuilder(expression.Length + 32);

        foreach (var c in expression)
        {
            output.Append(c switch
            {
                'π' => PiLiteral,
                'E' => ELiteral,
                _ => c.ToString(CultureInfo.InvariantCulture),
            });
        }

        return output.ToString();
    }

    private static string ReplaceNames(string expression)
    {
        foreach (var (draft, engine) in FunctionNames)
        {
            expression = expression.Replace(draft, engine, StringComparison.Ordinal);
        }

        return expression;
    }

    /// <summary>
    /// Turns "a^b" into "Pow(a,b)". The engine has no power operator, and the one character
    /// that looks like it means something else entirely there.
    /// </summary>
    private static string ExpandPowers(string expression)
    {
        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var caret = expression.IndexOf('^', StringComparison.Ordinal);

            if (caret < 0)
            {
                return expression;
            }

            var baseStart = OperandStart(expression, caret);
            var exponentEnd = OperandEnd(expression, caret + 1);

            var baseText = expression[baseStart..caret];
            var exponentText = expression[(caret + 1)..exponentEnd];

            if (baseText.Length == 0 || exponentText.Length == 0)
            {
                // A half typed power. Drop the caret rather than invent an exponent.
                expression = expression.Remove(caret, 1);
                continue;
            }

            expression = string.Concat(
                expression[..baseStart],
                $"Pow({baseText},{exponentText})",
                expression[exponentEnd..]);
        }

        return expression;
    }

    /// <summary>Walks back from an operator to the start of the value it applies to.</summary>
    private static int OperandStart(string expression, int at)
    {
        if (at == 0)
        {
            return 0;
        }

        if (expression[at - 1] == ')')
        {
            var depth = 0;

            for (var i = at - 1; i >= 0; i--)
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
                        var start = i;
                        while (start > 0 && char.IsAsciiLetter(expression[start - 1]))
                        {
                            start--;
                        }

                        return start;
                    }
                }
            }

            return 0;
        }

        var digits = at;
        while (digits > 0 && (char.IsAsciiDigit(expression[digits - 1]) || expression[digits - 1] == '.'))
        {
            digits--;
        }

        return digits;
    }

    /// <summary>Walks forward from an operator to the end of the value it applies to.</summary>
    private static int OperandEnd(string expression, int from)
    {
        if (from >= expression.Length)
        {
            return from;
        }

        // A negative exponent, or a function call, or a bracketed group.
        var i = from;

        if (expression[i] == '-')
        {
            i++;
        }

        while (i < expression.Length && char.IsAsciiLetter(expression[i]))
        {
            i++;
        }

        if (i < expression.Length && expression[i] == '(')
        {
            var depth = 0;

            for (; i < expression.Length; i++)
            {
                if (expression[i] == '(')
                {
                    depth++;
                }
                else if (expression[i] == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return i + 1;
                    }
                }
            }

            return expression.Length;
        }

        while (i < expression.Length && (char.IsAsciiDigit(expression[i]) || expression[i] == '.'))
        {
            i++;
        }

        return i;
    }
}
