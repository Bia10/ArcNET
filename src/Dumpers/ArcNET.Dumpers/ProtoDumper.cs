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
        var asMob = new MobData { Header = proto.Header, Properties = proto.Properties };
        MobDumper.DumpHeader(sb, proto.Header, "=== PROTO HEADER ===");
        MobDumper.DumpProperties(sb, asMob);
        return sb.ToString();
    }

    public static void Dump(ProtoData proto, TextWriter writer) => writer.Write(Dump(proto));
}
