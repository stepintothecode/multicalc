namespace MultiCalc.Abstractions;

/// <summary>What came back from asking the platform to write a file.</summary>
/// <param name="Saved">True when the file reached disk.</param>
/// <param name="Location">Where it landed, when the platform tells us.</param>
/// <param name="Failure">Why it did not save. Null when it did, or when the person cancelled.</param>
public sealed record FileExportResult(bool Saved, string? Location = null, string? Failure = null)
{
    /// <summary>The person backed out of the save dialog. Not an error, so nothing to report.</summary>
    public static FileExportResult Cancelled { get; } = new(false);

    /// <summary>The file was written.</summary>
    public static FileExportResult Success(string? location) => new(true, location);

    /// <summary>The save was attempted and failed.</summary>
    public static FileExportResult Error(string failure) => new(false, null, failure);
}
