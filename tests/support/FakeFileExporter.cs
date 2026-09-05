using MultiCalc.Abstractions;

namespace MultiCalc.TestSupport;

/// <summary>Captures what would have been written, instead of opening a save dialog.</summary>
public sealed class FakeFileExporter : IFileExporter
{
    private readonly FileExportResult result;

    public FakeFileExporter(FileExportResult? result = null) =>
        this.result = result ?? FileExportResult.Success("/fake/path");

    public string? LastFileName { get; private set; }

    public string? LastContents { get; private set; }

    public Task<FileExportResult> SaveAsync(
        string suggestedFileName,
        string contents,
        CancellationToken cancellationToken = default)
    {
        LastFileName = suggestedFileName;
        LastContents = contents;

        return Task.FromResult(result);
    }
}
