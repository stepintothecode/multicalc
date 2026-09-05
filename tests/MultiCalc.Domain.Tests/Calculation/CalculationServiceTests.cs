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
}
