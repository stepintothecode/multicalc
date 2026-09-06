using System.Text.Json;
using MultiCalc.Domain.Sessions;

namespace MultiCalc.Domain.History;

/// <summary>One calculator read back out of an exported file.</summary>
/// <param name="Name">What the calculator was called when it was exported.</param>
/// <param name="Entries">Its calculations, newest first.</param>
/// <param name="Tint">The colour it was tagged with, or null if the file predates colours.</param>
/// <param name="Scientific">Whether it had the scientific keys showing.</param>
public sealed record ImportedCalculator(
    string Name,
    IReadOnlyList<CalculationEntry> Entries,
    CalculatorTint? Tint = null,
    bool Scientific = false);

/// <summary>What came of reading an export file.</summary>
/// <param name="Calculators">What was found. Empty when it failed.</param>
/// <param name="Failure">Why it failed, or null when it did not.</param>
public sealed record ImportOutcome(IReadOnlyList<ImportedCalculator> Calculators, string? Failure = null)
{
    /// <summary>True when the file was read.</summary>
    public bool IsSuccess => Failure is null;

    /// <summary>How many calculations were found across every calculator.</summary>
    public int EntryCount => Calculators.Sum(c => c.Entries.Count);

    /// <summary>A failed read, with something to show the person.</summary>
    public static ImportOutcome Error(string failure) => new([], failure);
}

/// <summary>
/// Reads back a file written by <see cref="HistoryExport"/>.
/// <para>
/// Only the JSON export can be imported. Text and CSV are for reading and for
/// spreadsheets; neither round trips cleanly, and half reading one would invent history
/// that never happened.
/// </para>
/// </summary>
public static class HistoryImport
{
    /// <summary>Parses an exported file. Never throws: a bad file comes back as a failure.</summary>
    public static ImportOutcome Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ImportOutcome.Error("That file is empty");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("calculators", out var calculators)
                || calculators.ValueKind != JsonValueKind.Array)
            {
                return ImportOutcome.Error("That is not a MultiCalc history file");
            }

            var imported = new List<ImportedCalculator>();

            foreach (var calculator in calculators.EnumerateArray())
            {
                var name = ReadString(calculator, "name") ?? "Imported";
                var entries = ReadEntries(calculator);

                if (entries.Count > 0)
                {
                    imported.Add(new ImportedCalculator(
                        name,
                        entries,
                        ReadTint(calculator),
                        ReadBool(calculator, "scientific")));
                }
            }

            return imported.Count == 0
                ? ImportOutcome.Error("There is no history in that file")
                : new ImportOutcome(imported);
        }
        catch (JsonException)
        {
            return ImportOutcome.Error("That file could not be read as JSON");
        }
    }

    private static List<CalculationEntry> ReadEntries(JsonElement calculator)
    {
        var entries = new List<CalculationEntry>();

        if (!calculator.TryGetProperty("entries", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (var element in array.EnumerateArray())
        {
            var expression = ReadString(element, "expression");
            var result = ReadString(element, "result");

            // An entry missing either half is not a calculation. Skipped rather than
            // guessed at, so nothing invented ends up in someone's tape.
            if (expression is null || result is null)
            {
                continue;
            }

            entries.Add(new CalculationEntry(expression, result, ReadDate(element, "at")));
        }

        return entries;
    }

    private static CalculatorTint? ReadTint(JsonElement calculator) =>
        calculator.TryGetProperty("hue", out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var hue)
            ? CalculatorTint.FromHue(hue)
            : null;

    private static bool ReadBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset ReadDate(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.TryGetDateTimeOffset(out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
}
