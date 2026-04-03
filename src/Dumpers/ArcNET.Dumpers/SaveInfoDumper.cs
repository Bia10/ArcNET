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
        sb.AppendLine("=== SAVE GAME INFO ===");
        sb.AppendLine($"  Save name    : {info.DisplayName}");
        sb.AppendLine($"  Campaign     : {info.ModuleName}");
        sb.AppendLine(
            $"  Leader       : {info.LeaderName}  (level {info.LeaderLevel}, portrait {info.LeaderPortraitId})"
        );
        sb.AppendLine($"  Location     : map {info.MapId}, tile ({info.LeaderTileX}, {info.LeaderTileY})");

        // Decode in-game time as Days + time-of-day
        var totalMs = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var hours = (int)(totalMs / 3_600_000L % 24);
        var minutes = (int)(totalMs / 60_000L % 60);
        var seconds = (int)(totalMs / 1_000L % 60);
        sb.AppendLine($"  In-game time : Day {info.GameTimeDays + 1}, {hours:D2}:{minutes:D2}:{seconds:D2}");

        if (info.StoryState != 0)
            sb.AppendLine($"  Story state  : {info.StoryState}  (non-standard; typically 0)");

        return sb.ToString();
    }

    public static void Dump(SaveInfo info, TextWriter writer) => writer.Write(Dump(info));
}
