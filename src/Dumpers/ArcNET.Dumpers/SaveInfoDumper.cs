using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="SaveInfo"/> instance.
/// </summary>
public static class SaveInfoDumper
{
    public static string Dump(SaveInfo info)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== SAVE GAME INFO ===");
        vsb.AppendLine($"  Save name    : {info.DisplayName}");
        vsb.AppendLine($"  Campaign     : {info.ModuleName}");
        vsb.AppendLine(
            $"  Leader       : {info.LeaderName}  (level {info.LeaderLevel}, portrait {info.LeaderPortraitId})"
        );
        vsb.AppendLine($"  Location     : map {info.MapId}, tile ({info.LeaderTileX}, {info.LeaderTileY})");

        // Decode in-game time as Days + time-of-day
        var totalMs = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var hours = (int)(totalMs / 3_600_000L % 24);
        var minutes = (int)(totalMs / 60_000L % 60);
        var seconds = (int)(totalMs / 1_000L % 60);
        vsb.AppendLine($"  In-game time : Day {info.GameTimeDays + 1}, {hours:D2}:{minutes:D2}:{seconds:D2}");

        if (info.StoryState != 0)
            vsb.AppendLine($"  Story state  : {info.StoryState}  (non-standard; typically 0)");

        return vsb.ToString();
    }

    public static void Dump(SaveInfo info, TextWriter writer) => writer.Write(Dump(info));
}
