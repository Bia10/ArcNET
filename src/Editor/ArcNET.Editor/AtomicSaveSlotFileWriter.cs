namespace ArcNET.Editor;

internal static class AtomicSaveSlotFileWriter
{
    public static void Write(
        string gsiPath,
        byte[] gsiBytes,
        string tfaiPath,
        byte[] tfaiBytes,
        string tfafPath,
        byte[] tfafBytes
    )
    {
        var gsiTemp = gsiPath + ".tmp";
        var tfaiTemp = tfaiPath + ".tmp";
        var tfafTemp = tfafPath + ".tmp";
        try
        {
            File.WriteAllBytes(gsiTemp, gsiBytes);
            File.WriteAllBytes(tfaiTemp, tfaiBytes);
            File.WriteAllBytes(tfafTemp, tfafBytes);
            File.Move(gsiTemp, gsiPath, overwrite: true);
            File.Move(tfaiTemp, tfaiPath, overwrite: true);
            File.Move(tfafTemp, tfafPath, overwrite: true);
        }
        catch
        {
            TryDeleteSilently(gsiTemp);
            TryDeleteSilently(tfaiTemp);
            TryDeleteSilently(tfafTemp);
            throw;
        }
    }

    public static async Task WriteAsync(
        string gsiPath,
        byte[] gsiBytes,
        string tfaiPath,
        byte[] tfaiBytes,
        string tfafPath,
        byte[] tfafBytes,
        CancellationToken cancellationToken
    )
    {
        var gsiTemp = gsiPath + ".tmp";
        var tfaiTemp = tfaiPath + ".tmp";
        var tfafTemp = tfafPath + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(gsiTemp, gsiBytes, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tfaiTemp, tfaiBytes, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tfafTemp, tfafBytes, cancellationToken).ConfigureAwait(false);
            File.Move(gsiTemp, gsiPath, overwrite: true);
            File.Move(tfaiTemp, tfaiPath, overwrite: true);
            File.Move(tfafTemp, tfafPath, overwrite: true);
        }
        catch
        {
            TryDeleteSilently(gsiTemp);
            TryDeleteSilently(tfaiTemp);
            TryDeleteSilently(tfafTemp);
            throw;
        }
    }

    private static void TryDeleteSilently(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch { }
    }
}
