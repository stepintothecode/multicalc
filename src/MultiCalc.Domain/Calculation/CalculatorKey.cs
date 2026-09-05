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
}
