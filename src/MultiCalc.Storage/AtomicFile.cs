namespace MultiCalc.Storage;

/// <summary>
/// Writes a file in a way that survives the app being killed mid save.
/// Android stops processes without warning, and a half written settings file is worse
/// than no settings file.
/// </summary>
internal static class AtomicFile
{
    public static async Task WriteAsync(string path, string contents, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = path + ".tmp";

        await File.WriteAllTextAsync(temporary, contents, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }
}
