using MultiCalc.Domain.Calculation;
using Xunit;

namespace MultiCalc.Domain.Tests.Calculation;

public sealed class ExpressionDraftTests
{
    private static ExpressionDraft Type(params CalculatorKey[] keys) =>
        keys.Aggregate(ExpressionDraft.Empty, (draft, key) => draft.Press(key));

    [Fact]
    public void Empty_draft_has_no_expression()
    {
        Assert.True(ExpressionDraft.Empty.IsEmpty);
        Assert.Equal(string.Empty, ExpressionDraft.Empty.Expression);
    }

    [Fact]
    public void Digits_append()
    {
        Assert.Equal("123", Type(CalculatorKey.One, CalculatorKey.Two, CalculatorKey.Three).Expression);
    }

    [Fact]
    public void Leading_zero_is_replaced_rather_than_kept()
    {
        Assert.Equal("5", Type(CalculatorKey.Zero, CalculatorKey.Five).Expression);
    }

    [Fact]
    public void Zero_inside_a_number_is_kept()
    {
        Assert.Equal("105", Type(CalculatorKey.One, CalculatorKey.Zero, CalculatorKey.Five).Expression);
    }

    [Fact]
    public void Decimal_on_an_empty_draft_opens_with_a_zero()
    {
        Assert.Equal("0.", Type(CalculatorKey.Decimal).Expression);
    }

    [Fact]
    public void A_number_takes_only_one_decimal_point()
    {
        var draft = Type(CalculatorKey.One, CalculatorKey.Decimal, CalculatorKey.Five, CalculatorKey.Decimal);

        Assert.Equal("1.5", draft.Expression);
    }

    [Fact]
    public void Decimal_after_an_operator_opens_a_new_number()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.Add, CalculatorKey.Decimal);

        Assert.Equal("5+0.", draft.Expression);
    }

    [Fact]
    public void Only_minus_may_open_an_expression()
    {
        Assert.Equal("-", Type(CalculatorKey.Subtract).Expression);
        Assert.Equal(string.Empty, Type(CalculatorKey.Add).Expression);
        Assert.Equal(string.Empty, Type(CalculatorKey.Multiply).Expression);
    }

    [Fact]
    public void A_second_operator_replaces_the_first()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.Add, CalculatorKey.Multiply);

        Assert.Equal("5*", draft.Expression);
    }

    [Fact]
    public void Minus_after_times_is_a_negative_operand_and_is_kept()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.Multiply, CalculatorKey.Subtract);

        Assert.Equal("5*-", draft.Expression);
    }

    [Fact]
    public void A_half_typed_decimal_loses_its_point_before_an_operator()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.Decimal, CalculatorKey.Add);

        Assert.Equal("5+", draft.Expression);
    }

    [Fact]
    public void An_open_bracket_after_a_value_implies_a_multiply()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.OpenParen);

        Assert.Equal("5*(", draft.Expression);
    }

    [Fact]
    public void A_close_bracket_needs_something_to_close()
    {
        Assert.Equal("5", Type(CalculatorKey.Five, CalculatorKey.CloseParen).Expression);
    }

    [Fact]
    public void A_close_bracket_is_accepted_once_a_bracket_is_open()
    {
        var draft = Type(CalculatorKey.OpenParen, CalculatorKey.Two, CalculatorKey.CloseParen);

        Assert.Equal("(2)", draft.Expression);
    }

    [Fact]
    public void Unclosed_brackets_are_closed_for_evaluation_only()
    {
        var draft = Type(CalculatorKey.OpenParen, CalculatorKey.Two, CalculatorKey.Add, CalculatorKey.Three);

        Assert.Equal("(2+3", draft.Expression);
        Assert.Equal("(2+3)", draft.ClosedExpression);
    }

    [Fact]
    public void Percent_follows_a_value_and_only_once()
    {
        var once = Type(CalculatorKey.Five, CalculatorKey.Percent, CalculatorKey.Percent);

        Assert.Equal("5%", once.Expression);
        Assert.Equal(string.Empty, Type(CalculatorKey.Percent).Expression);
    }

    [Fact]
    public void Toggling_the_sign_of_the_first_number_adds_and_removes_a_minus()
    {
        var negative = Type(CalculatorKey.Five, CalculatorKey.ToggleSign);
        Assert.Equal("-5", negative.Expression);

        Assert.Equal("5", negative.Press(CalculatorKey.ToggleSign).Expression);
    }

    [Fact]
    public void Toggling_the_sign_of_a_later_operand_leaves_the_operator_alone()
    {
        var draft = Type(CalculatorKey.Five, CalculatorKey.Add, CalculatorKey.Three, CalculatorKey.ToggleSign);

        Assert.Equal("5+-3", draft.Expression);
        Assert.Equal("5+3", draft.Press(CalculatorKey.ToggleSign).Expression);
    }

    [Fact]
    public void Toggling_the_sign_with_nothing_typed_does_nothing()
    {
        Assert.Equal(string.Empty, Type(CalculatorKey.ToggleSign).Expression);
    }

    [Fact]
    public void Backspace_removes_one_character_and_stops_at_empty()
    {
        var draft = Type(CalculatorKey.One, CalculatorKey.Two).Press(CalculatorKey.Backspace);

        Assert.Equal("1", draft.Expression);
        Assert.Equal(string.Empty, draft.Press(CalculatorKey.Backspace).Press(CalculatorKey.Backspace).Expression);
    }

    [Fact]
    public void Clear_empties_the_draft()
    {
        Assert.True(Type(CalculatorKey.One, CalculatorKey.Two).Press(CalculatorKey.Clear).IsEmpty);
    }

    [Fact]
    public void A_result_can_be_typed_onto()
    {
        var draft = ExpressionDraft.FromValue(12.5m).Press(CalculatorKey.Add).Press(CalculatorKey.One);

        Assert.Equal("12.5+1", draft.Expression);
    }

    [Fact]
    public void Display_groups_digits_and_uses_real_operator_glyphs()
    {
        var draft = ExpressionDraft.FromExpression("1234*5/2-1");

        Assert.Equal("1,234×5÷2−1", draft.ToDisplay());
    }
}
