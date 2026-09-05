namespace MultiCalc.Abstractions;

/// <summary>Hands a block of text to the platform's own "save file" flow.</summary>
public interface IFileExporter
{
    /// <summary>
    /// Prompts for a location and writes <paramref name="contents"/> there.
    /// Cancelling is a normal outcome, not a failure.
    /// </summary>
    Task<FileExportResult> SaveAsync(
        string suggestedFileName,
        string contents,
        CancellationToken cancellationToken = default);
}
