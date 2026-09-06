namespace MultiCalc.Domain.Calculation;

/// <summary>
/// What an expression is allowed to be made of. One place, because the keypad writes these
/// tokens, the display has to recognise them coming back, and the engine has to be handed
/// exactly what it understands.
/// </summary>
public static class ExpressionVocabulary
{
    /// <summary>
    /// Keys that open a function. The draft carries the everyday spelling and
    /// <see cref="ScientificRewriter"/> turns it into what the engine wants, so what is on
    /// the display is what a person would write.
    /// </summary>
    public static readonly IReadOnlyDictionary<CalculatorKey, string> Functions =
        new Dictionary<CalculatorKey, string>
        {
            [CalculatorKey.SquareRoot] = "sqrt(",
            [CalculatorKey.AbsoluteValue] = "abs(",
            [CalculatorKey.Sine] = "sin(",
            [CalculatorKey.Cosine] = "cos(",
            [CalculatorKey.Tangent] = "tan(",
            [CalculatorKey.ArcSine] = "asin(",
            [CalculatorKey.ArcCosine] = "acos(",
            [CalculatorKey.ArcTangent] = "atan(",
            [CalculatorKey.NaturalLog] = "ln(",
            [CalculatorKey.Log10] = "log(",
            [CalculatorKey.Exponential] = "exp(",
        };

    /// <summary>
    /// Keys that stand where a value would. The constants are single characters so nothing
    /// else can match them; ten-to-the-power starts with its own literal, so it behaves the
    /// same way and needs no special case.
    /// </summary>
    public static readonly IReadOnlyDictionary<CalculatorKey, string> Constants =
        new Dictionary<CalculatorKey, string>
        {
            [CalculatorKey.Pi] = "π",
            [CalculatorKey.Euler] = "E",
            [CalculatorKey.PowerOfTen] = "10^",
        };

    private static readonly HashSet<string> Names =
        [.. Functions.Values.Select(token => token.TrimEnd('('))];

    /// <summary>True when these letters spell a function this calculator knows.</summary>
    public static bool IsFunctionName(string word) => Names.Contains(word);
}
