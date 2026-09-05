using MultiCalc.Domain.History;
using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.History;

public sealed class HistoryMergeTests
{
    private static readonly DateTimeOffset Noon = new(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);

    private static CalculationEntry Entry(string expression, string result, int minutes) =>
        new(expression, result, Noon.AddMinutes(minutes));

    [Fact]
    public void Both_sides_survive_and_come_back_newest_first()
    {
        var combined = HistoryMerge.Combine(
            [Entry("2+2", "4", 10)],
            [Entry("1+1", "2", 5), Entry("3+3", "6", 20)]);

        Assert.Equal(3, combined.Count);
        Assert.Equal("3+3", combined[0].Expression);
        Assert.Equal("1+1", combined[2].Expression);
    }

    [Fact]
    public void Importing_the_same_calculation_twice_keeps_one()
    {
        var entry = Entry("2+2", "4", 10);

        var combined = HistoryMerge.Combine([entry], [entry]);

        Assert.Single(combined);
    }

    [Fact]
    public void The_same_sum_done_at_a_different_time_is_a_different_calculation()
    {
        var combined = HistoryMerge.Combine([Entry("2+2", "4", 10)], [Entry("2+2", "4", 30)]);

        Assert.Equal(2, combined.Count);
    }

    [Fact]
    public void The_combined_history_still_stops_at_the_limit()
    {
        var existing = Enumerable.Range(0, CalculatorSession.HistoryLimit)
            .Select(i => Entry($"{i}+0", $"{i}", i))
            .ToList();

        var incoming = Enumerable.Range(1000, 50)
            .Select(i => Entry($"{i}+0", $"{i}", i))
            .ToList();

        var combined = HistoryMerge.Combine(existing, incoming);

        Assert.Equal(CalculatorSession.HistoryLimit, combined.Count);

        // The newest survive the cull.
        Assert.Equal("1049+0", combined[0].Expression);
    }

    [Fact]
    public void Merging_into_nothing_just_gives_the_incoming_side()
    {
        var combined = HistoryMerge.Combine([], [Entry("2+2", "4", 10)]);

        Assert.Single(combined);
    }
}
