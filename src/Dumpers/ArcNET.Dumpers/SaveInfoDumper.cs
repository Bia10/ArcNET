using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="SaveInfo"/> instance.
/// </summary>
public static class SaveInfoDumper
{
    public static string Dump(SaveInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SAVE INFO ===");
        sb.AppendLine($"  DisplayName      : {info.DisplayName}");
        sb.AppendLine($"  ModuleName       : {info.ModuleName}");
        sb.AppendLine($"  LeaderName       : {info.LeaderName}");
        sb.AppendLine($"  MapId            : {info.MapId}");

        // Format in-game time as Days + time-of-day
        var totalMs = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var hours = (int)(totalMs / 3_600_000L % 24);
        var minutes = (int)(totalMs / 60_000L % 60);
        var seconds = (int)(totalMs / 1_000L % 60);
        sb.AppendLine(
            $"  GameTime         : Day {info.GameTimeDays + 1}, {hours:D2}:{minutes:D2}:{seconds:D2}  ({info.GameTimeDays}d {info.GameTimeMs}ms)"
        );

        sb.AppendLine($"  LeaderPortraitId : {info.LeaderPortraitId}");
        sb.AppendLine($"  LeaderLevel      : {info.LeaderLevel}");
        sb.AppendLine($"  LeaderTile       : ({info.LeaderTileX}, {info.LeaderTileY})");
        sb.AppendLine($"  StoryState       : {info.StoryState}");
        return sb.ToString();
    }

    public static void Dump(SaveInfo info, TextWriter writer) => writer.Write(Dump(info));
}
