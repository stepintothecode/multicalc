using System.Globalization;
using System.Text;
using System.Text.Json;
using MultiCalc.Domain.Sessions;

namespace MultiCalc.Domain.History;

/// <summary>
/// Renders history to a file's worth of text. Pure: it decides what the file says,
/// and something else decides where it goes.
/// </summary>
public static class HistoryExport
{
    private static readonly JsonWriterOptions JsonOptions = new() { Indented = true };

    /// <summary>The extension that goes with a format, without the dot.</summary>
    public static string ExtensionFor(HistoryFormat format) => format switch
    {
        HistoryFormat.Csv => "csv",
        HistoryFormat.Json => "json",
        _ => "txt",
    };

    /// <summary>A file name stamped with the local date and time, safe on every platform.</summary>
    public static string BuildFileName(HistoryFormat format, DateTimeOffset at) =>
        $"multicalc-history-{at.LocalDateTime:yyyyMMdd-HHmm}.{ExtensionFor(format)}";

    /// <summary>Renders every calculator's history in the chosen format.</summary>
    public static string Render(
        IReadOnlyList<CalculatorSession> sessions,
        HistoryFormat format,
        DateTimeOffset at) => format switch
        {
            HistoryFormat.Csv => RenderCsv(sessions),
            HistoryFormat.Json => RenderJson(sessions, at),
            _ => RenderText(sessions, at),
        };

    /// <summary>True when there is nothing worth writing to a file.</summary>
    public static bool IsEmpty(IReadOnlyList<CalculatorSession> sessions) =>
        sessions.All(s => s.History.Count == 0);

    private static string RenderText(IReadOnlyList<CalculatorSession> sessions, DateTimeOffset at)
    {
        var output = new StringBuilder();

        output.Append("MultiCalc history").Append('\n');
        output.Append("Exported ").Append(at.LocalDateTime.ToString("f", CultureInfo.CurrentCulture)).Append('\n');

        foreach (var session in sessions.Where(s => s.History.Count > 0))
        {
            output.Append('\n').Append(session.Name).Append('\n');
            output.Append(new string('-', session.Name.Length)).Append('\n');

            // History is newest first. A tape reads better oldest first.
            foreach (var entry in session.History.Reverse())
            {
                output
                    .Append(entry.At.LocalDateTime.ToString("HH:mm", CultureInfo.CurrentCulture))
                    .Append("  ")
                    .Append(entry.Expression)
                    .Append(" = ")
                    .Append(entry.Result)
                    .Append('\n');
            }
        }

        return output.ToString();
    }

    private static string RenderCsv(IReadOnlyList<CalculatorSession> sessions)
    {
        var output = new StringBuilder();
        output.Append("Calculator,Expression,Result,Timestamp\n");

        foreach (var session in sessions)
        {
            foreach (var entry in session.History.Reverse())
            {
                output
                    .Append(Escape(session.Name)).Append(',')
                    .Append(Escape(entry.Expression)).Append(',')
                    .Append(Escape(entry.Result)).Append(',')
                    .Append(Escape(entry.At.ToString("o", CultureInfo.InvariantCulture)))
                    .Append('\n');
            }
        }

        return output.ToString();
    }

    /// <summary>
    /// Written with <see cref="Utf8JsonWriter"/> rather than by serialising an object graph,
    /// so the Release build's trimmer cannot strip the metadata this depends on.
    /// </summary>
    private static string RenderJson(IReadOnlyList<CalculatorSession> sessions, DateTimeOffset at)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, JsonOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("app", "MultiCalc");
            writer.WriteString("exportedAt", at);
            writer.WriteStartArray("calculators");

            foreach (var session in sessions)
            {
                writer.WriteStartObject();
                writer.WriteString("name", session.Name);

                // The colour and the scientific setting travel with the calculator, so an
                // export restores what the person actually had rather than a list of grey
                // strangers that all forgot their keypad.
                writer.WriteNumber("hue", session.Tint.Hue);
                writer.WriteBoolean("scientific", session.Scientific);
                writer.WriteString("createdAt", session.CreatedAt);
                writer.WriteStartArray("entries");

                foreach (var entry in session.History.Reverse())
                {
                    writer.WriteStartObject();
                    writer.WriteString("expression", entry.Expression);
                    writer.WriteString("result", entry.Result);
                    writer.WriteString("at", entry.At);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Quotes a CSV field only when it needs it.</summary>
    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
