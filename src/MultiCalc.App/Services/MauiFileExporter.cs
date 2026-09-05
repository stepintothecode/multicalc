using System.Text;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;

namespace MultiCalc.App.Services;

/// <summary>
/// Writes a file through the platform's own save dialog, which on Android is the Storage
/// Access Framework. The app never needs a storage permission this way, because the person
/// picks the destination themselves.
/// </summary>
public sealed class MauiFileExporter : IFileExporter
{
    private readonly IFileSaver saver;
    private readonly ILogger<MauiFileExporter> logger;

    /// <summary>Creates the exporter.</summary>
    public MauiFileExporter(IFileSaver saver, ILogger<MauiFileExporter> logger)
    {
        this.saver = saver;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileExportResult> SaveAsync(
        string suggestedFileName,
        string contents,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

        try
        {
            var result = await saver
                .SaveAsync(suggestedFileName, stream, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccessful)
            {
                return FileExportResult.Success(result.FilePath);
            }

            logger.LogInformation(result.Exception, "History export did not complete");

            return FileExportResult.Cancelled;
        }
        catch (OperationCanceledException)
        {
            return FileExportResult.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not save the history file");
            return FileExportResult.Error("Could not save the file");
        }
    }
}
