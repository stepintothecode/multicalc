using MultiCalc.Abstractions;

namespace MultiCalc.Ui.Services;

/// <summary>Turns an evaluation failure into something worth showing on the display.</summary>
public static class EvaluationMessages
{
    /// <summary>
    /// The message for a failure, or null when there is nothing to say. Pressing equals on an
    /// empty display should do nothing, not scold.
    /// </summary>
    public static string? For(EvaluationError error) => error switch
    {
        EvaluationError.Empty => null,
        EvaluationError.DivideByZero => "Cannot divide by zero",
        EvaluationError.Overflow => "Number too large",
        EvaluationError.Undefined => "No result",
        _ => "Check the expression",
    };
}
