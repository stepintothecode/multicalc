using System.Text;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// The expression as it is being typed, and the rules about which key may follow which.
/// Immutable: every key press returns a new draft, and a key that would produce nonsense
/// returns the draft unchanged rather than a broken expression.
/// </summary>
public sealed record ExpressionDraft
{
    private const string Operators = "+-*/";

    private ExpressionDraft(string expression) => Expression = expression;

    /// <summary>An empty draft.</summary>
    public static ExpressionDraft Empty { get; } = new(string.Empty);

    /// <summary>
    /// The raw expression, using ASCII operators. Percent stays as a "%" marker here and is
    /// expanded by <see cref="PercentExpander"/> only when the expression is evaluated.
    /// </summary>
    public string Expression { get; }

    /// <summary>True when nothing has been typed.</summary>
    public bool IsEmpty => Expression.Length == 0;

    /// <summary>
    /// The expression with any unclosed brackets closed, which is what actually gets evaluated.
    /// Typing "(2+3" and pressing equals should give 5, not a syntax error.
    /// </summary>
    public string ClosedExpression
    {
        get
        {
            var missing = OpenBracketCount;
            return missing == 0 ? Expression : Expression + new string(')', missing);
        }
    }

    /// <summary>Starts a fresh draft holding a number, so a result can be typed onto.</summary>
    public static ExpressionDraft FromValue(decimal value) => new(NumberFormat.ForExpression(value));

    /// <summary>Starts a draft from raw text. Used when restoring a saved session.</summary>
    public static ExpressionDraft FromExpression(string? expression) =>
        string.IsNullOrEmpty(expression) ? Empty : new ExpressionDraft(expression);

    /// <summary>Applies a key press and returns the resulting draft.</summary>
    public ExpressionDraft Press(CalculatorKey key) => key switch
    {
        CalculatorKey.Clear => Empty,
        CalculatorKey.Backspace => Backspace(),
        CalculatorKey.Decimal => AppendDecimal(),
        CalculatorKey.OpenParen => AppendOpenBracket(),
        CalculatorKey.CloseParen => AppendCloseBracket(),
        CalculatorKey.Percent => AppendPercent(),
        CalculatorKey.ToggleSign => ToggleSign(),
        CalculatorKey.Add => AppendOperator('+'),
        CalculatorKey.Subtract => AppendOperator('-'),
        CalculatorKey.Multiply => AppendOperator('*'),
        CalculatorKey.Divide => AppendOperator('/'),
        _ => AppendDigit(DigitOf(key)),
    };

    private static char DigitOf(CalculatorKey key) => (char)('0' + (int)key);

    private char? LastChar => Expression.Length == 0 ? null : Expression[^1];

    private int OpenBracketCount
    {
        get
        {
            var depth = 0;
            foreach (var c in Expression)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
            }

            return depth < 0 ? 0 : depth;
        }
    }

    private static bool IsOperator(char c) => Operators.Contains(c);

    private static bool EndsAValue(char c) => char.IsAsciiDigit(c) || c == ')' || c == '%';

    /// <summary>Index where the number at the end of the expression starts, or -1 if none.</summary>
    private int TrailingNumberStart()
    {
        var i = Expression.Length;
        while (i > 0 && (char.IsAsciiDigit(Expression[i - 1]) || Expression[i - 1] == '.'))
        {
            i--;
        }

        return i == Expression.Length ? -1 : i;
    }

    private string TrailingNumber()
    {
        var start = TrailingNumberStart();
        return start < 0 ? string.Empty : Expression[start..];
    }

    private ExpressionDraft With(string expression) => new(expression);

    private ExpressionDraft AppendDigit(char digit)
    {
        // A number cannot follow a bracket or a percent, so treat it as an implied multiply.
        if (LastChar is char last && (last == ')' || last == '%'))
        {
            return With(Expression + '*' + digit);
        }

        var number = TrailingNumber();

        // A leading zero is a placeholder, not a digit: 0 then 5 is 5, not 05.
        if (number == "0")
        {
            return With(Expression[..^1] + digit);
        }

        return With(Expression + digit);
    }

    private ExpressionDraft AppendDecimal()
    {
        if (LastChar is char last && (last == ')' || last == '%'))
        {
            return With(Expression + "*0.");
        }

        var start = TrailingNumberStart();

        if (start < 0)
        {
            return With(Expression + "0.");
        }

        // One point per number.
        return Expression[start..].Contains('.') ? this : With(Expression + '.');
    }

    private ExpressionDraft AppendOperator(char op)
    {
        if (Expression.Length == 0)
        {
            // Only a minus can open an expression.
            return op == '-' ? With("-") : this;
        }

        var last = Expression[^1];

        // A half typed number such as "5." loses its point before an operator.
        if (last == '.')
        {
            return With(Expression[..^1] + op);
        }

        if (last == '(')
        {
            return op == '-' ? With(Expression + op) : this;
        }

        if (IsOperator(last))
        {
            // "5*-" is a negative operand and stays. Anything else replaces the operator.
            if (op == '-' && (last == '*' || last == '/'))
            {
                return With(Expression + op);
            }

            var trimmed = Expression.TrimEnd(Operators.ToCharArray());
            return trimmed.Length == 0
                ? (op == '-' ? With("-") : this)
                : With(trimmed + op);
        }

        return With(Expression + op);
    }

    private ExpressionDraft AppendOpenBracket()
    {
        if (LastChar is char last && EndsAValue(last))
        {
            return With(Expression + "*(");
        }

        return With(Expression + '(');
    }

    private ExpressionDraft AppendCloseBracket()
    {
        if (OpenBracketCount == 0 || LastChar is not char last || !EndsAValue(last))
        {
            return this;
        }

        return With(Expression + ')');
    }

    private ExpressionDraft AppendPercent()
    {
        // Percent applies to a value, and only once.
        if (LastChar is not char last || last == '%' || !EndsAValue(last))
        {
            return this;
        }

        return With(Expression + '%');
    }

    private ExpressionDraft ToggleSign()
    {
        var start = TrailingNumberStart();

        if (start < 0)
        {
            return this;
        }

        // Already negated by a minus that is acting as a sign rather than a subtraction.
        if (start > 0 && Expression[start - 1] == '-'
            && (start - 1 == 0 || Expression[start - 2] == '(' || IsOperator(Expression[start - 2])))
        {
            return With(Expression.Remove(start - 1, 1));
        }

        return With(Expression.Insert(start, "-"));
    }

    private ExpressionDraft Backspace() =>
        Expression.Length == 0 ? this : With(Expression[..^1]);

    /// <summary>The expression as the display should show it: grouped digits and real operator glyphs.</summary>
    public string ToDisplay()
    {
        if (Expression.Length == 0)
        {
            return string.Empty;
        }

        var grouped = NumberFormat.GroupExpression(Expression);
        var output = new StringBuilder(grouped.Length);

        foreach (var c in grouped)
        {
            output.Append(c switch
            {
                '*' => '×',
                '/' => '÷',
                '-' => '−',
                _ => c,
            });
        }

        return output.ToString();
    }
}
