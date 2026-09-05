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
}
