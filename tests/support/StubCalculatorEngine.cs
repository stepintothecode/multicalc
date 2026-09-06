using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>
/// An engine that records what it was asked and returns whatever the test set up.
/// Lets CalculationService be tested for what it does to an expression rather than
/// for whether the arithmetic is right.
/// </summary>
public sealed class StubCalculatorEngine : ICalculatorEngine
{
    private readonly EvaluationOutcome outcome;

    public StubCalculatorEngine(EvaluationOutcome? outcome = null) =>
        this.outcome = outcome ?? EvaluationOutcome.Success(0m);

    /// <summary>The last expression handed to the engine.</summary>
    public string? LastExpression { get; private set; }

    /// <summary>The angle mode the engine was asked for.</summary>
    public AngleMode LastAngleMode { get; private set; }

    public EvaluationOutcome Evaluate(string expression, AngleMode angles = AngleMode.Degrees)
    {
        LastExpression = expression;
        LastAngleMode = angles;

        return outcome;
    }
}
