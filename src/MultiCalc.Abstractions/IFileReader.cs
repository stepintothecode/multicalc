namespace MultiCalc.Abstractions;

/// <summary>What came back from asking someone to pick a file.</summary>
/// <param name="Contents">The file's text, or null when nothing was read.</param>
/// <param name="FileName">What the file was called, when the platform says.</param>
/// <param name="Failure">Why it could not be read. Null when it was, or when the person cancelled.</param>
public sealed record FileReadResult(string? Contents, string? FileName = null, string? Failure = null)
{
    /// <summary>True when there is text to work with.</summary>
    public bool HasContents => Contents is not null;

    /// <summary>The person backed out of the picker. Not an error, so nothing to report.</summary>
    /// <remarks>Named argument on purpose: a bare null also matches the record's copy constructor.</remarks>
    public static FileReadResult Cancelled { get; } = new(Contents: null);

    /// <summary>The file was read.</summary>
    public static FileReadResult Success(string contents, string? fileName) => new(contents, fileName);

    /// <summary>The read was attempted and failed.</summary>
    public static FileReadResult Error(string failure) => new(null, null, failure);
}

/// <summary>Asks the platform for a file and hands back its text.</summary>
public interface IFileReader
{
    /// <summary>
    /// Opens the platform's file picker and reads what is chosen.
    /// Cancelling is a normal outcome, not a failure.
    /// </summary>
    Task<FileReadResult> ReadAsync(CancellationToken cancellationToken = default);
}
