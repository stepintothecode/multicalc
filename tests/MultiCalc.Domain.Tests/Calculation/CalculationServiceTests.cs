using MultiCalc.Abstractions;
using MultiCalc.Domain.Calculation;
using MultiCalc.TestSupport;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class CalculationServiceTests
{
    [Fact]
    public void An_empty_draft_never_reaches_the_engine()
    {
        var engine = new StubCalculatorEngine();
        var service = new CalculationService(engine);

        var outcome = service.Evaluate(ExpressionDraft.Empty);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(EvaluationError.Empty, outcome.Error);
        Assert.Null(engine.LastExpression);
    }

    [Fact]
    public void Brackets_are_closed_before_the_engine_sees_the_expression()
    {
        var engine = new StubCalculatorEngine();
        var service = new CalculationService(engine);

        service.Evaluate(ExpressionDraft.FromExpression("(2+3"));

        Assert.Equal("(2+3)", engine.LastExpression);
    }

    [Fact]
    public void Percent_is_expanded_before_the_engine_sees_the_expression()
    {
        var engine = new StubCalculatorEngine();
        var service = new CalculationService(engine);

        service.Evaluate(ExpressionDraft.FromExpression("50+10%"));

        Assert.Equal("50+((50)*(10)/100)", engine.LastExpression);
    }

    [Fact]
    public void The_engine_outcome_is_passed_straight_back()
    {
        var expected = EvaluationOutcome.Failure(EvaluationError.DivideByZero);
        var service = new CalculationService(new StubCalculatorEngine(expected));

        Assert.Equal(expected, service.Evaluate(ExpressionDraft.FromExpression("1/0")));
    }

    [Fact]
    public void A_running_answer_shows_while_the_expression_is_being_typed()
    {
        var service = new CalculationService(new StubCalculatorEngine(EvaluationOutcome.Success(12.5m)));

        Assert.Equal("12.5", service.Preview(ExpressionDraft.FromExpression("25/2")));
    }

    [Fact]
    public void A_running_answer_is_grouped_the_way_the_display_groups_it()
    {
        var service = new CalculationService(new StubCalculatorEngine(EvaluationOutcome.Success(1234567m)));

        Assert.Equal("1,234,567", service.Preview(ExpressionDraft.FromExpression("1000000+234567")));
    }

    [Fact]
    public void A_half_typed_expression_shows_no_running_answer()
    {
        var service = new CalculationService(
            new StubCalculatorEngine(EvaluationOutcome.Failure(EvaluationError.Syntax)));

        Assert.Null(service.Preview(ExpressionDraft.FromExpression("5*")));
    }

    [Fact]
    public void An_empty_display_shows_no_running_answer()
    {
        var service = new CalculationService(new StubCalculatorEngine());

        Assert.Null(service.Preview(ExpressionDraft.Empty));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("0.5")]
    [InlineData("-0.8191520442889")]
    public void A_plain_number_shows_no_running_answer(string typed)
    {
        // Would only repeat the line above it. A negative one used to slip through, because
        // the answer was written with an ASCII hyphen and the display uses a minus sign.
        var engine = new StubCalculatorEngine(EvaluationOutcome.Success(1234m));
        var service = new CalculationService(engine);

        Assert.Null(service.Preview(ExpressionDraft.FromExpression(typed)));
        Assert.Null(engine.LastExpression);
    }

    [Fact]
    public void A_negative_running_answer_is_written_with_the_display_minus_sign()
    {
        var service = new CalculationService(new StubCalculatorEngine(EvaluationOutcome.Success(-2.5m)));

        Assert.Equal("−2.5", service.Preview(ExpressionDraft.FromExpression("2-4.5")));
    }

    [Fact]
    public void The_running_answer_is_measured_in_the_angle_unit_it_is_given()
    {
        var engine = new StubCalculatorEngine();
        var service = new CalculationService(engine);

        service.Preview(ExpressionDraft.FromExpression("sin(30"), AngleMode.Radians);

        Assert.Equal(AngleMode.Radians, engine.LastAngleMode);
        Assert.Equal("sin(30)", engine.LastExpression);
    }
}
