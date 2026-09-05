namespace MultiCalc.Abstractions;

/// <summary>
/// The result of evaluating an expression: either a number or a named reason it failed.
/// There is deliberately no "best guess" case. A wrong number on a calculator is worse
/// than an error the person can see.
/// </summary>
public sealed record EvaluationOutcome
{
    private EvaluationOutcome(bool isSuccess, decimal value, EvaluationError error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>True when <see cref="Value"/> holds a number.</summary>
    public bool IsSuccess { get; }

    /// <summary>The computed value. Only meaningful when <see cref="IsSuccess"/> is true.</summary>
    public decimal Value { get; }

    /// <summary>Why it failed. Only meaningful when <see cref="IsSuccess"/> is false.</summary>
    public EvaluationError Error { get; }

    /// <summary>Creates a successful outcome.</summary>
    public static EvaluationOutcome Success(decimal value) => new(true, value, default);

    /// <summary>Creates a failed outcome.</summary>
    public static EvaluationOutcome Failure(EvaluationError error) => new(false, 0m, error);
}
