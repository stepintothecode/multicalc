namespace MultiCalc.Abstractions;

/// <summary>Why an expression could not be turned into a number.</summary>
public enum EvaluationError
{
    /// <summary>Nothing to evaluate.</summary>
    Empty,

    /// <summary>The expression does not parse.</summary>
    Syntax,

    /// <summary>Division by zero.</summary>
    DivideByZero,

    /// <summary>The result is outside the range this calculator can hold.</summary>
    Overflow,

    /// <summary>The expression parses but has no defined value, such as the square root of a negative.</summary>
    Undefined,
}
