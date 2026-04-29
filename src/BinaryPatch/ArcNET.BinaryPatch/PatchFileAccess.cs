using ArcNET.Archive;

namespace ArcNET.BinaryPatch;

internal static class PatchFileAccess
{
    private const string BackupExtension = ".bak";

    public static string ResolvePath(string gameDir, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(gameDir, normalized));
        var gameRoot = Path.GetFullPath(gameDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Patch target '{relativePath}' resolves to '{fullPath}' which is outside the game directory '{gameDir}'."
            );

        return fullPath;
    }

    public static string GetBackupPath(string path) => path + BackupExtension;

    public static (byte[]? Data, string? Error) TryReadOriginalBytes(IBinaryPatch patch, string path, string gameDir)
    {
        if (File.Exists(path))
        {
            try
            {
                return (File.ReadAllBytes(path), null);
            }
            catch (Exception ex)
            {
                return (null, $"Read failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (patch.Target.SourceDatPath is not null)
            return ReadFromDat(patch, gameDir);

        return (null, $"File not found: {path}");
    }

    public static string? WritePatchedBytes(string path, byte[] patched, bool ensureDirectory)
    {
        var tempPath = path + ".tmp";
        try
        {
            if (ensureDirectory)
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.WriteAllBytes(tempPath, patched);
            File.Move(tempPath, path, overwrite: true);
            return null;
        }
        catch (Exception ex)
        {
            TryDeleteSilently(tempPath);
            return $"Write failed: {ex.GetType().Name}: {ex.Message}";
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

    private static (byte[]? Data, string? Error) ReadFromDat(IBinaryPatch patch, string gameDir)
    {
        var datPath = ResolvePath(gameDir, patch.Target.SourceDatPath!);
        if (!File.Exists(datPath))
            return (null, $"DAT archive not found: {datPath}");

        try
        {
            using var dat = DatArchive.Open(datPath);
            var entry = dat.FindEntry(patch.Target.DatEntryPath!);
            if (entry is null)
                return (null, $"Entry '{patch.Target.DatEntryPath}' not found in '{Path.GetFileName(datPath)}'.");

            return (dat.ReadEntry(entry), null);
        }
        catch (Exception ex)
        {
            return (null, $"DAT read failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
