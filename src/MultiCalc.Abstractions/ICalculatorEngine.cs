namespace MultiCalc.Abstractions;

/// <summary>Turns an infix expression such as "12 + 4 * 2" into a number.</summary>
public interface ICalculatorEngine
{
    /// <summary>
    /// Evaluates <paramref name="expression"/>. Never throws for bad input: an unparseable
    /// expression comes back as a failed <see cref="EvaluationOutcome"/>.
    /// </summary>
    /// <param name="expression">The expression, already free of display-only markers.</param>
    /// <param name="angles">What any trigonometric argument is measured in.</param>
    EvaluationOutcome Evaluate(string expression, AngleMode angles = AngleMode.Degrees);
}
