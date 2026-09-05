using MultiCalc.Domain.History;
using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.History;

public sealed class HistoryImportTests
{
    private static readonly DateTimeOffset At = new(2026, 3, 4, 14, 32, 0, TimeSpan.Zero);

    private static string RoundTrip(params CalculatorSession[] sessions) =>
        HistoryExport.Render(sessions, HistoryFormat.Json, At);

    [Fact]
    public void What_the_exporter_writes_is_what_the_importer_reads()
    {
        var session = CalculatorSession.Create("a", "Groceries", At)
            .WithResult(new CalculationEntry("2+3", "5", At), 5m)
            .WithResult(new CalculationEntry("5*4", "20", At), 20m);

        var outcome = HistoryImport.Parse(RoundTrip(session));

        Assert.True(outcome.IsSuccess);
        var imported = Assert.Single(outcome.Calculators);
        Assert.Equal("Groceries", imported.Name);
        Assert.Equal(2, imported.Entries.Count);
        Assert.Equal(2, outcome.EntryCount);
    }

    [Fact]
    public void Tag_colours_survive_the_round_trip()
    {
        // An export should restore what the person had, not a list of default-coloured
        // strangers, so the hue travels in the file too.
        var session = CalculatorSession.Create("a", "Groceries", At, CalculatorTint.FromHue(285))
            .WithResult(new CalculationEntry("2+3", "5", At), 5m);

        var imported = HistoryImport.Parse(RoundTrip(session)).Calculators[0];

        Assert.Equal(285, imported.Tint?.Hue);
    }

    [Fact]
    public void A_file_without_colours_imports_without_inventing_one()
    {
        var json = """
                   {
                     "calculators": [
                       { "name": "Old", "entries": [
                         { "expression": "1+1", "result": "2", "at": "2026-03-04T14:32:00+00:00" }
                       ] }
                     ]
                   }
                   """;

        Assert.Null(HistoryImport.Parse(json).Calculators[0].Tint);
    }

    [Fact]
    public void Timestamps_survive_the_round_trip()
    {
        var session = CalculatorSession.Create("a", "Rent", At)
            .WithResult(new CalculationEntry("1+1", "2", At), 2m);

        var imported = HistoryImport.Parse(RoundTrip(session)).Calculators[0];

        Assert.Equal(At, imported.Entries[0].At);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("""{"hello":"world"}""")]
    [InlineData("""{"calculators":"nope"}""")]
    public void Anything_that_is_not_an_export_fails_with_something_to_show(string json)
    {
        var outcome = HistoryImport.Parse(json);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Failure);
        Assert.Empty(outcome.Calculators);
    }

    [Fact]
    public void An_export_with_no_calculations_in_it_is_a_failure_not_a_silent_success()
    {
        var outcome = HistoryImport.Parse(RoundTrip(CalculatorSession.Create("a", "Empty", At)));

        Assert.False(outcome.IsSuccess);
    }

    [Fact]
    public void A_half_written_entry_is_dropped_rather_than_invented()
    {
        var json = """
                   {
                     "calculators": [
                       { "name": "Odd", "entries": [
                         { "expression": "2+2" },
                         { "expression": "3+3", "result": "6", "at": "2026-03-04T14:32:00+00:00" }
                       ] }
                     ]
                   }
                   """;

        var imported = HistoryImport.Parse(json).Calculators[0];

        Assert.Single(imported.Entries);
        Assert.Equal("3+3", imported.Entries[0].Expression);
    }

    [Fact]
    public void An_entry_with_no_name_still_lands_somewhere_findable()
    {
        var json = """
                   {
                     "calculators": [
                       { "entries": [
                         { "expression": "1+1", "result": "2", "at": "2026-03-04T14:32:00+00:00" }
                       ] }
                     ]
                   }
                   """;

        Assert.Equal("Imported", HistoryImport.Parse(json).Calculators[0].Name);
    }
}
