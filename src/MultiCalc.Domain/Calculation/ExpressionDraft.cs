namespace MultiCalc.Domain.Calculation;

/// <summary>
/// The expression as it is being typed, and the rules about which key may follow which.
/// Immutable: every key press returns a new draft, and a key that would produce nonsense
/// returns the draft unchanged rather than a broken expression.
/// </summary>
public sealed record ExpressionDraft
{
    private const string Operators = "+-*/";

    private static readonly IReadOnlyDictionary<CalculatorKey, string> Functions =
        ExpressionVocabulary.Functions;

    private static readonly IReadOnlyDictionary<CalculatorKey, string> Constants =
        ExpressionVocabulary.Constants;

    private ExpressionDraft(string expression, int caret)
    {
        Expression = expression;
        Caret = Math.Clamp(caret, 0, expression.Length);
    }

    private ExpressionDraft(string expression)
        : this(expression, expression.Length)
    {
    }

    /// <summary>An empty draft.</summary>
    public static ExpressionDraft Empty { get; } = new(string.Empty);

    /// <summary>
    /// Where the next key press goes, as an index into <see cref="Expression"/>. Normally the
    /// end, and somewhere else once someone has put the caret there.
    /// </summary>
    public int Caret { get; }

    /// <summary>
    /// The raw expression, using ASCII operators. Percent stays as a "%" marker here and is
    /// expanded by <see cref="PercentExpander"/> only when the expression is evaluated.
    /// </summary>
    public string Expression { get; }

    /// <summary>True when nothing has been typed.</summary>
    public bool IsEmpty => Expression.Length == 0;

    /// <summary>
    /// True when what is typed is nothing but a number, sign and decimal point included.
    /// There is no sum here, so there is nothing to work out and nothing to preview.
    /// </summary>
    public bool IsPlainNumber
    {
        get
        {
            if (Expression.Length == 0)
            {
                return false;
            }

            var digits = 0;
            var points = 0;

            for (var i = Expression[0] == '-' ? 1 : 0; i < Expression.Length; i++)
            {
                var c = Expression[i];

                if (char.IsAsciiDigit(c))
                {
                    digits++;
                }
                else if (c == '.' && points++ == 0)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return digits > 0;
        }
    }

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

    /// <summary>Returns a copy with the caret somewhere else. Out of range values are clamped.</summary>
    public ExpressionDraft WithCaret(int caret) =>
        caret == Caret ? this : new ExpressionDraft(Expression, caret);

    /// <summary>
    /// Replaces the whole expression, for an edit made in the display itself rather than on
    /// the keypad: a paste, a deleted selection, a backspace from a hardware keyboard.
    /// </summary>
    public static ExpressionDraft FromEdit(string expression, int caret) =>
        new(expression ?? string.Empty, caret);

    /// <summary>
    /// Applies a key press and returns the resulting draft.
    /// <para>
    /// The rules only ever look at what comes before the caret, and whatever follows it is
    /// carried along untouched. With the caret at the end, which is where it usually is, that
    /// is exactly typing; with the caret in the middle it is inserting, under the same rules.
    /// </para>
    /// </summary>
    public ExpressionDraft Press(CalculatorKey key)
    {
        if (Caret == Expression.Length)
        {
            return PressAtEnd(key);
        }

        if (key == CalculatorKey.Clear)
        {
            return Empty;
        }

        var edited = new ExpressionDraft(Expression[..Caret]).PressAtEnd(key);

        return new ExpressionDraft(edited.Expression + Expression[Caret..], edited.Expression.Length);
    }

    private ExpressionDraft PressAtEnd(CalculatorKey key)
    {
        if (Functions.TryGetValue(key, out var function))
        {
            return AppendValueLike(function);
        }

        if (Constants.TryGetValue(key, out var constant))
        {
            return AppendValueLike(constant);
        }

        return key switch
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
            CalculatorKey.Square => AppendPower("2"),
            CalculatorKey.Power => AppendPower(string.Empty),
            CalculatorKey.Reciprocal => Reciprocal(),
            _ => AppendDigit(DigitOf(key)),
        };
    }

    /// <summary>
    /// Appends something that stands where a number would: a function opener or a
    /// constant. Following a value it implies a multiply, the same as an open bracket.
    /// </summary>
    private ExpressionDraft AppendValueLike(string token) =>
        LastChar is char last && EndsAValue(last)
            ? With(Expression + '*' + token)
            : With(Expression + token);

    /// <summary>
    /// Raises whatever came before it to a power. "2" for the square key, and nothing for
    /// the x-to-the-y key, which leaves the exponent to be typed.
    /// </summary>
    private ExpressionDraft AppendPower(string exponent)
    {
        if (LastChar is not char last || !EndsAValue(last))
        {
            return this;
        }

        return With(Expression + '^' + exponent);
    }

    /// <summary>
    /// One over what is on the display. With something already typed it wraps that,
    /// because a reciprocal of nothing is not a thing anyone means.
    /// </summary>
    private ExpressionDraft Reciprocal()
    {
        if (Expression.Length == 0)
        {
            return With("1/(");
        }

        var start = TrailingOperandStart();

        return start < 0
            ? With(Expression + "1/(")
            : With(Expression[..start] + "1/(" + Expression[start..] + ")");
    }

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

    private static bool EndsAValue(char c) =>
        char.IsAsciiDigit(c) || c == ')' || c == '%' || c == 'π' || c == 'E';

    /// <summary>
    /// Where the value at the end of the expression starts, counting a bracketed group as
    /// one value. Returns -1 when the expression does not end in a value at all.
    /// </summary>
    private int TrailingOperandStart()
    {
        if (Expression.Length == 0)
        {
            return -1;
        }

        var last = Expression[^1];

        if (last == ')')
        {
            var depth = 0;

            for (var i = Expression.Length - 1; i >= 0; i--)
            {
                if (Expression[i] == ')')
                {
                    depth++;
                }
                else if (Expression[i] == '(')
                {
                    depth--;

                    if (depth == 0)
                    {
                        // Take any function name sitting in front of the bracket with it.
                        var start = i;
                        while (start > 0 && char.IsAsciiLetter(Expression[start - 1]))
                        {
                            start--;
                        }

                        return start;
                    }
                }
            }

            return 0;
        }

        if (last is 'π' or 'E')
        {
            return Expression.Length - 1;
        }

        return TrailingNumberStart();
    }

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

    /// <summary>The expression as the display should show it, and the map back to it.</summary>
    public ExpressionDisplay Display => ExpressionDisplay.For(Expression);

    /// <summary>The expression as the display should show it: grouped digits and real operator glyphs.</summary>
    public string ToDisplay() => Display.Text;
}
