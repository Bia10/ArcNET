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
        vsb.Append("  Save name    : ");
        vsb.AppendLine(info.DisplayName);
        vsb.Append("  Campaign     : ");
        vsb.AppendLine(info.ModuleName);
        vsb.Append("  Leader       : ");
        vsb.Append(info.LeaderName);
        vsb.Append("  (level ");
        vsb.Append(info.LeaderLevel);
        vsb.Append(", portrait ");
        vsb.Append(info.LeaderPortraitId);
        vsb.AppendLine(")");
        vsb.Append("  Location     : map ");
        vsb.Append(info.MapId);
        vsb.Append(", tile (");
        vsb.Append(info.LeaderTileX);
        vsb.Append(", ");
        vsb.Append(info.LeaderTileY);
        vsb.AppendLine(")");

        // Decode in-game time as Days + time-of-day
        var totalMs = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var hours = (int)(totalMs / 3_600_000L % 24);
        var minutes = (int)(totalMs / 60_000L % 60);
        var seconds = (int)(totalMs / 1_000L % 60);
        vsb.Append("  In-game time : Day ");
        vsb.Append(info.GameTimeDays + 1);
        vsb.Append(", ");
        vsb.Append(hours, "D2");
        vsb.Append(':');
        vsb.Append(minutes, "D2");
        vsb.Append(':');
        vsb.Append(seconds, "D2");
        vsb.AppendLine();

        if (info.StoryState != 0)
        {
            vsb.Append("  Story state  : ");
            vsb.Append(info.StoryState);
            vsb.AppendLine("  (non-standard; typically 0)");
        }

        return vsb.ToString();
    }

    public static void Dump(SaveInfo info, TextWriter writer) => writer.Write(Dump(info));
}
