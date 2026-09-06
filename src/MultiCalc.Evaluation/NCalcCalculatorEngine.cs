using MultiCalc.Abstractions;
using NCalc;
using NCalc.Handlers;

namespace MultiCalc.Evaluation;

/// <summary>
/// Arithmetic on top of NCalc. Decimal is the default so 0.1 + 0.2 comes to 0.3,
/// which is the answer a person holding a calculator expects.
/// </summary>
public sealed class NCalcCalculatorEngine : ICalculatorEngine
{
    private const double DegreesPerRadian = 180.0 / Math.PI;

    private const ExpressionOptions Options =
        ExpressionOptions.DecimalAsDefault | ExpressionOptions.IgnoreCaseAtBuiltInFunctions;

    /// <summary>
    /// The six trigonometric functions, taken over from NCalc so the angle mode can be
    /// honoured. Everything else NCalc provides is left alone.
    /// </summary>
    private static readonly string[] Trigonometric =
        ["sin", "cos", "tan", "asin", "acos", "atan"];

    /// <inheritdoc />
    public EvaluationOutcome Evaluate(string expression, AngleMode angles = AngleMode.Degrees)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return EvaluationOutcome.Failure(EvaluationError.Empty);
        }

        try
        {
            var parsed = new Expression(expression, Options);
            parsed.EvaluateFunction += (name, args) => Trigonometry(name, args, angles);

            return Convert(parsed.Evaluate());
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

    /// <summary>
    /// Handles the trigonometric functions so degrees work. NCalc's own take radians, and
    /// a calculator that answered 0.0175 for sin(1) would be wrong for most people.
    /// </summary>
    private static void Trigonometry(string name, FunctionEventArgs args, AngleMode angles)
    {
        if (!Trigonometric.Contains(name, StringComparer.OrdinalIgnoreCase) || args.Parameters.Count != 1)
        {
            return;
        }

        var value = System.Convert.ToDouble(args.Parameters.Evaluate(0), null);
        var inDegrees = angles == AngleMode.Degrees;
        var isInverse = name.StartsWith('a') || name.StartsWith('A');

        // Forward functions take an angle, so degrees are converted going in.
        // Inverse functions return one, so they are converted coming out.
        var argument = inDegrees && !isInverse ? value / DegreesPerRadian : value;

        var result = name.ToLowerInvariant() switch
        {
            "sin" => Math.Sin(argument),
            "cos" => Math.Cos(argument),
            "tan" => Math.Tan(argument),
            "asin" => Math.Asin(argument),
            "acos" => Math.Acos(argument),
            "atan" => Math.Atan(argument),
            _ => double.NaN,
        };

        args.Result = inDegrees && isInverse ? result * DegreesPerRadian : result;
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

        // Trigonometry lands a hair off a round number, and "0.9999999999999999" for cos(0)
        // reads as a bug. Fifteen places is well inside double's honest precision.
        return EvaluationOutcome.Success(Math.Round((decimal)value, 15));
    }
}
