namespace MultiCalc.Domain.History;

/// <summary>The shapes history can be exported in.</summary>
public enum HistoryFormat
{
    /// <summary>Readable text, for keeping or pasting into a message.</summary>
    Text,

    /// <summary>One row per calculation, for a spreadsheet.</summary>
    Csv,

    /// <summary>Structured, for anything that has to read it back.</summary>
    Json,
}
