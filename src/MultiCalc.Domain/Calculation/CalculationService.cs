using MultiCalc.Abstractions;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// Turns a draft into a number. Owns the two things the arithmetic engine should not know
/// about: unclosed brackets, and what percent means on a calculator.
/// </summary>
public sealed class CalculationService
{
    private readonly ICalculatorEngine engine;

    /// <summary>Creates the service over an arithmetic engine.</summary>
    public CalculationService(ICalculatorEngine engine) => this.engine = engine;

    /// <summary>Evaluates what is currently typed.</summary>
    public EvaluationOutcome Evaluate(ExpressionDraft draft)
    {
        if (draft.IsEmpty)
        {
            return EvaluationOutcome.Failure(EvaluationError.Empty);
        }

        return engine.Evaluate(PercentExpander.Expand(draft.ClosedExpression));
    }
}
