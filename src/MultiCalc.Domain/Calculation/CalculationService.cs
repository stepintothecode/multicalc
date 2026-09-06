using MultiCalc.Abstractions;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// Turns a draft into a number. Owns everything the arithmetic engine should not know
/// about: unclosed brackets, what percent means on a calculator, and the everyday
/// spelling of the scientific functions.
/// </summary>
public sealed class CalculationService
{
    private readonly ICalculatorEngine engine;

    /// <summary>Creates the service over an arithmetic engine.</summary>
    public CalculationService(ICalculatorEngine engine) => this.engine = engine;

    /// <summary>Evaluates what is currently typed.</summary>
    /// <param name="draft">What is on the display.</param>
    /// <param name="angles">What a trigonometric argument is measured in.</param>
    public EvaluationOutcome Evaluate(ExpressionDraft draft, AngleMode angles = AngleMode.Degrees)
    {
        if (draft.IsEmpty)
        {
            return EvaluationOutcome.Failure(EvaluationError.Empty);
        }

        // Percent first: it copies the left hand side, and copying already rewritten
        // function calls would be harder to read and no more correct.
        var expanded = PercentExpander.Expand(draft.ClosedExpression);

        return engine.Evaluate(ScientificRewriter.Rewrite(expanded), angles);
    }

    /// <summary>
    /// The running answer to show under the expression while it is still being typed, or
    /// null when there is nothing worth showing.
    /// </summary>
    /// <remarks>
    /// Half typed expressions and impossible ones simply produce nothing: an error belongs
    /// to pressing equals, not to being midway through "5/" on the way to "5/2". A plain
    /// number produces nothing either, because repeating the line above it says nothing.
    /// </remarks>
    public string? Preview(ExpressionDraft draft, AngleMode angles = AngleMode.Degrees)
    {
        if (draft.IsPlainNumber)
        {
            return null;
        }

        var outcome = Evaluate(draft, angles);

        // Rendered the way the expression above it is rendered, so the answer to a negative
        // sum is not written with a different minus sign from the one that produced it.
        return outcome.IsSuccess ? ExpressionDraft.FromValue(outcome.Value).ToDisplay() : null;
    }
}
