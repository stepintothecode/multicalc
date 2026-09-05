using System.Text.Json;
using MultiCalc.Domain.History;
using MultiCalc.Domain.Sessions;
using Xunit;

namespace MultiCalc.Domain.Tests.History;

public sealed class HistoryExportTests
{
    private static readonly DateTimeOffset At = new(2026, 3, 4, 14, 32, 0, TimeSpan.Zero);

    private static IReadOnlyList<CalculatorSession> Sample()
    {
        var groceries = CalculatorSession.Create("a", "Groceries", At)
            .WithResult(new CalculationEntry("2+3", "5", At), 5m)
            .WithResult(new CalculationEntry("5*4", "20", At), 20m);

        var rent = CalculatorSession.Create("b", "Rent", At)
            .WithResult(new CalculationEntry("1000/3", "333.33", At), 333.33m);

        return [groceries, rent];
    }

    [Fact]
    public void Text_lists_every_calculator_and_reads_oldest_first()
    {
        var text = HistoryExport.Render(Sample(), HistoryFormat.Text, At);

        Assert.Contains("Groceries", text, StringComparison.Ordinal);
        Assert.Contains("Rent", text, StringComparison.Ordinal);
        Assert.Contains("2+3 = 5", text, StringComparison.Ordinal);

        // A tape reads in the order the sums were done.
        Assert.True(
            text.IndexOf("2+3", StringComparison.Ordinal) < text.IndexOf("5*4", StringComparison.Ordinal));
    }

    [Fact]
    public void Csv_has_a_header_and_one_row_per_calculation()
    {
        var lines = HistoryExport.Render(Sample(), HistoryFormat.Csv, At)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Calculator,Expression,Result,Timestamp", lines[0]);
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void Csv_quotes_a_field_that_contains_a_comma()
    {
        var session = CalculatorSession.Create("a", "Split, three ways", At)
            .WithResult(new CalculationEntry("1,234+1", "1,235", At), 1235m);

        var csv = HistoryExport.Render([session], HistoryFormat.Csv, At);

        Assert.Contains("\"Split, three ways\"", csv, StringComparison.Ordinal);
        Assert.Contains("\"1,234+1\"", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_parses_and_keeps_the_structure()
    {
        var json = HistoryExport.Render(Sample(), HistoryFormat.Json, At);

        using var document = JsonDocument.Parse(json);
        var calculators = document.RootElement.GetProperty("calculators");

        Assert.Equal("MultiCalc", document.RootElement.GetProperty("app").GetString());
        Assert.Equal(2, calculators.GetArrayLength());
        Assert.Equal("Groceries", calculators[0].GetProperty("name").GetString());
        Assert.Equal(2, calculators[0].GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public void Nothing_worked_out_yet_counts_as_empty()
    {
        Assert.True(HistoryExport.IsEmpty([CalculatorSession.Create("a", "Calculator 1", At)]));
        Assert.False(HistoryExport.IsEmpty(Sample()));
    }

    [Theory]
    [InlineData(HistoryFormat.Text, "txt")]
    [InlineData(HistoryFormat.Csv, "csv")]
    [InlineData(HistoryFormat.Json, "json")]
    public void The_file_name_carries_the_right_extension(HistoryFormat format, string extension)
    {
        var name = HistoryExport.BuildFileName(format, At);

        Assert.StartsWith("multicalc-history-", name, StringComparison.Ordinal);
        Assert.EndsWith("." + extension, name, StringComparison.Ordinal);
    }
}
