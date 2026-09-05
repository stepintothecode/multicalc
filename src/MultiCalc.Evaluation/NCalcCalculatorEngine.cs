using MultiCalc.Abstractions;
using NCalc;

namespace MultiCalc.Evaluation;

/// <summary>
/// Arithmetic on top of NCalc. Decimal is the default so 0.1 + 0.2 comes to 0.3,
/// which is the answer a person holding a calculator expects.
/// </summary>
public sealed class NCalcCalculatorEngine : ICalculatorEngine
{
    private const ExpressionOptions Options = ExpressionOptions.DecimalAsDefault;

    /// <inheritdoc />
    public EvaluationOutcome Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return EvaluationOutcome.Failure(EvaluationError.Empty);
        }

        try
        {
            return Convert(new Expression(expression, Options).Evaluate());
        }
        catch (DivideByZeroException)
        {
            return EvaluationOutcome.Failure(EvaluationError.DivideByZero);
        }
        catch (OverflowException)
        {
            return EvaluationOutcome.Failure(EvaluationError.Overflow);
        }
        catch (Exception)
        {
            // The contract is that bad input comes back as a failed outcome rather than an
            // exception, and a parser has many ways to object. Anything unrecognised is a
            // syntax problem as far as the person typing is concerned.
            return EvaluationOutcome.Failure(EvaluationError.Syntax);
        }
    }

    private static EvaluationOutcome Convert(object? raw) => raw switch
    {
        decimal value => EvaluationOutcome.Success(value),
        double value => FromDouble(value),
        float value => FromDouble(value),
        int value => EvaluationOutcome.Success(value),
        long value => EvaluationOutcome.Success(value),
        _ => EvaluationOutcome.Failure(EvaluationError.Syntax),
    };

    /// <summary>
    /// Functions such as square root come back as doubles. Infinity here almost always means
    /// a division by zero that floating point swallowed rather than threw.
    /// </summary>
    private static EvaluationOutcome FromDouble(double value)
    {
        if (double.IsNaN(value))
        {
            return EvaluationOutcome.Failure(EvaluationError.Undefined);
        }

        if (double.IsInfinity(value))
        {
            return EvaluationOutcome.Failure(EvaluationError.DivideByZero);
        }

        if (Math.Abs(value) > (double)decimal.MaxValue)
        {
            return EvaluationOutcome.Failure(EvaluationError.Overflow);
        }

        return EvaluationOutcome.Success((decimal)value);
    }
}
