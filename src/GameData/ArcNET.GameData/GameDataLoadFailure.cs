using ArcNET.Formats;

namespace ArcNET.GameData;

public sealed class GameDataLoadFailure(string sourcePath, FileFormat format, string reason)
{
    public string SourcePath { get; } = sourcePath;

    public FileFormat Format { get; } = format;

    public string Reason { get; } = reason;
}
