using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>Hands back canned file contents instead of opening a picker.</summary>
public sealed class FakeFileReader : IFileReader
{
    private readonly FileReadResult result;

    public FakeFileReader(FileReadResult? result = null) =>
        this.result = result ?? FileReadResult.Cancelled;

    /// <summary>Creates a reader that returns this text as the picked file.</summary>
    public static FakeFileReader Returning(string contents) =>
        new(FileReadResult.Success(contents, "history.json"));

    /// <summary>How many times a file was asked for.</summary>
    public int ReadCount { get; private set; }

    public Task<FileReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult(result);
    }
}
