using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="ProtoData"/> instance.
/// ProtoData is structurally identical to MobData (same header + properties model),
/// so this delegates the heavy lifting to <see cref="MobDumper"/>.
/// </summary>
public static class ProtoDumper
{
    public static string Dump(ProtoData proto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== PROTO HEADER ===");
        sb.AppendLine($"  Version      : 0x{proto.Header.Version:X2}");
        sb.AppendLine($"  ProtoId      : {proto.Header.ProtoId}  [IsProto={proto.Header.ProtoId.IsProto}]");
        sb.AppendLine($"  ObjectId     : {proto.Header.ObjectId}");
        sb.AppendLine($"  ObjectType   : {proto.Header.GameObjectType} ({(int)proto.Header.GameObjectType})");
        sb.AppendLine($"  IsPrototype  : {proto.Header.IsPrototype}");
        sb.AppendLine($"  BitmapLength : {proto.Header.Bitmap.Length} bits");
        var setBits = Enumerable.Range(0, proto.Header.Bitmap.Length).Where(i => proto.Header.Bitmap[i]).ToList();
        sb.AppendLine($"  Set bits     : [{string.Join(", ", setBits)}]");
        sb.AppendLine();

        // Reuse MobDumper for property dumping by wrapping in MobData
        var asMob = new MobData { Header = proto.Header, Properties = proto.Properties };
        sb.Append(MobDumper.Dump(asMob).Split("=== PROPERTIES ===", 2)[1].Insert(0, "=== PROPERTIES ==="));
        return sb.ToString();
    }

    public static void Dump(ProtoData proto, TextWriter writer) => writer.Write(Dump(proto));
}
