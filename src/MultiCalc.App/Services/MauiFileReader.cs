using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;

namespace MultiCalc.App.Services;

/// <summary>
/// Reads a file the person picks. Going through the platform picker means the app never
/// needs a storage permission: it only ever sees the one file that was handed to it.
/// </summary>
public sealed class MauiFileReader : IFileReader
{
    private const long MaxBytes = 8 * 1024 * 1024;

    private readonly ILogger<MauiFileReader> logger;

    /// <summary>Creates the reader.</summary>
    public MauiFileReader(ILogger<MauiFileReader> logger) => this.logger = logger;

    /// <inheritdoc />
    public async Task<FileReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick a MultiCalc history file",
            }).ConfigureAwait(false);

            if (picked is null)
            {
                return FileReadResult.Cancelled;
            }

            await using var stream = await picked.OpenReadAsync().ConfigureAwait(false);

            if (stream.CanSeek && stream.Length > MaxBytes)
            {
                return FileReadResult.Error("That file is too large to be a history export");
            }

            using var text = new StreamReader(stream);
            var contents = await text.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            return FileReadResult.Success(contents, picked.FileName);
        }
        catch (OperationCanceledException)
        {
            return FileReadResult.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the picked file");
            return FileReadResult.Error("Could not read that file");
        }
    }
}
