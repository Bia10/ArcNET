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
        sb.AppendLine($"  GameTimeDays     : {info.GameTimeDays}");
        sb.AppendLine($"  GameTimeMs       : {info.GameTimeMs}");
        sb.AppendLine($"  LeaderPortraitId : {info.LeaderPortraitId}");
        sb.AppendLine($"  LeaderLevel      : {info.LeaderLevel}");
        sb.AppendLine($"  LeaderTile       : ({info.LeaderTileX}, {info.LeaderTileY})");
        sb.AppendLine($"  StoryState       : {info.StoryState}");
        return sb.ToString();
    }

    public static void Dump(SaveInfo info, TextWriter writer) => writer.Write(Dump(info));
}
