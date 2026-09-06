namespace MultiCalc.Domain.Calculation;

/// <summary>Every key the keypad can send. Nothing else reaches <see cref="ExpressionDraft"/>.</summary>
public enum CalculatorKey
{
    /// <summary>Digit 0.</summary>
    Zero,

    /// <summary>Digit 1.</summary>
    One,

    /// <summary>Digit 2.</summary>
    Two,

    /// <summary>Digit 3.</summary>
    Three,

    /// <summary>Digit 4.</summary>
    Four,

    /// <summary>Digit 5.</summary>
    Five,

    /// <summary>Digit 6.</summary>
    Six,

    /// <summary>Digit 7.</summary>
    Seven,

    /// <summary>Digit 8.</summary>
    Eight,

    /// <summary>Digit 9.</summary>
    Nine,

    /// <summary>The decimal separator.</summary>
    Decimal,

    /// <summary>Plus.</summary>
    Add,

    /// <summary>Minus.</summary>
    Subtract,

    /// <summary>Times.</summary>
    Multiply,

    /// <summary>Divide.</summary>
    Divide,

    /// <summary>Opening bracket.</summary>
    OpenParen,

    /// <summary>Closing bracket.</summary>
    CloseParen,

    /// <summary>Percent, applied to the number just typed.</summary>
    Percent,

    /// <summary>Flips the sign of the number just typed.</summary>
    ToggleSign,

    /// <summary>Deletes one character.</summary>
    Backspace,

    /// <summary>Clears everything.</summary>
    Clear,

    // Scientific keys. Everything below opens a function or drops in a constant, so the
    // draft stays an ordinary infix string and the rewriter turns it into engine syntax.

    /// <summary>Square root, opens "sqrt(".</summary>
    SquareRoot,

    /// <summary>Absolute value, opens "abs(".</summary>
    AbsoluteValue,

    /// <summary>Sine, opens "sin(" or "asin(" when inverse is on.</summary>
    Sine,

    /// <summary>Cosine.</summary>
    Cosine,

    /// <summary>Tangent.</summary>
    Tangent,

    /// <summary>Natural logarithm, opens "ln(".</summary>
    NaturalLog,

    /// <summary>Logarithm base ten, opens "log(".</summary>
    Log10,

    /// <summary>Reciprocal, opens "1/(".</summary>
    Reciprocal,

    /// <summary>Squares what came before it.</summary>
    Square,

    /// <summary>Raises what came before it to a power you then type.</summary>
    Power,

    /// <summary>e raised to a power, opens "exp(".</summary>
    Exponential,

    /// <summary>Ten raised to a power you then type. The partner to the log key.</summary>
    PowerOfTen,

    /// <summary>The constant pi.</summary>
    Pi,

    /// <summary>Euler's number.</summary>
    Euler,

    /// <summary>Inverse sine. The keypad sends this instead of <see cref="Sine"/> when Inv is on.</summary>
    ArcSine,

    /// <summary>Inverse cosine.</summary>
    ArcCosine,

    /// <summary>Inverse tangent.</summary>
    ArcTangent,
}
