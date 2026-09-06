using MultiCalc.Abstractions;
using MultiCalc.Evaluation;
using Xunit;

namespace MultiCalc.Evaluation.Tests;

public sealed class NCalcCalculatorEngineTests
{
    private readonly NCalcCalculatorEngine engine = new();

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("10-4", 6)]
    [InlineData("6*7", 42)]
    [InlineData("9/3", 3)]
    [InlineData("-5+2", -3)]
    public void Basic_arithmetic(string expression, decimal expected)
    {
        var outcome = engine.Evaluate(expression);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(expected, outcome.Value);
    }

    [Fact]
    public void Times_binds_tighter_than_plus()
    {
        Assert.Equal(14m, engine.Evaluate("2+3*4").Value);
    }

    [Fact]
    public void Brackets_win()
    {
        Assert.Equal(20m, engine.Evaluate("(2+3)*4").Value);
    }

    [Fact]
    public void Decimals_add_up_the_way_a_calculator_should()
    {
        // The whole reason the engine runs in decimal rather than double.
        Assert.Equal(0.3m, engine.Evaluate("0.1+0.2").Value);
    }

    [Fact]
    public void Dividing_by_zero_is_named_rather_than_guessed_at()
    {
        var outcome = engine.Evaluate("1/0");

        Assert.False(outcome.IsSuccess);
        Assert.Equal(EvaluationError.DivideByZero, outcome.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nothing_to_evaluate_is_its_own_answer(string expression)
    {
        Assert.Equal(EvaluationError.Empty, engine.Evaluate(expression).Error);
    }

    [Theory]
    [InlineData("2+")]
    [InlineData("((2+3)")]
    [InlineData("hello")]
    [InlineData("*5")]
    public void Unparseable_input_comes_back_as_a_failure_rather_than_an_exception(string expression)
    {
        var outcome = engine.Evaluate(expression);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(EvaluationError.Syntax, outcome.Error);
    }

    [Fact]
    public void A_true_or_false_expression_is_not_a_number()
    {
        Assert.False(engine.Evaluate("1 == 1").IsSuccess);
    }

    [Theory]
    [InlineData("Sqrt(9)", 3)]
    [InlineData("Abs(0-4)", 4)]
    [InlineData("Pow(2,10)", 1024)]
    [InlineData("Log10(100)", 2)]
    [InlineData("Exp(0)", 1)]
    public void Scientific_functions(string expression, decimal expected)
    {
        var outcome = engine.Evaluate(expression);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(expected, outcome.Value);
    }

    [Theory]
    [InlineData("Sin(30)", 0.5)]
    [InlineData("Sin(0)", 0)]
    [InlineData("Cos(0)", 1)]
    [InlineData("Cos(60)", 0.5)]
    [InlineData("Tan(45)", 1)]
    public void Trigonometry_works_in_degrees_because_that_is_what_a_calculator_does(
        string expression, decimal expected)
    {
        var outcome = engine.Evaluate(expression, AngleMode.Degrees);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(expected, outcome.Value);
    }

    [Fact]
    public void Radians_are_available_for_anyone_who_wants_them()
    {
        // Sin of pi over six is a half, the same angle as thirty degrees.
        var outcome = engine.Evaluate("Sin(0.5235987755982988)", AngleMode.Radians);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(0.5m, outcome.Value);
    }

    [Fact]
    public void The_inverse_functions_answer_in_the_same_unit_they_were_asked_in()
    {
        Assert.Equal(30m, engine.Evaluate("Asin(0.5)", AngleMode.Degrees).Value);
        Assert.Equal(45m, engine.Evaluate("Atan(1)", AngleMode.Degrees).Value);
    }

    [Fact]
    public void The_square_root_of_a_negative_has_no_answer_rather_than_a_wrong_one()
    {
        var outcome = engine.Evaluate("Sqrt(0-1)");

        Assert.False(outcome.IsSuccess);
        Assert.Equal(EvaluationError.Undefined, outcome.Error);
    }

    [Fact]
    public void Rounding_keeps_trigonometry_from_reading_as_a_bug()
    {
        // Without the round this comes back as 0.99999999999999989, which looks broken.
        Assert.Equal(1m, engine.Evaluate("Cos(0)").Value);
        Assert.Equal(0m, engine.Evaluate("Sin(180)").Value);
    }
}
