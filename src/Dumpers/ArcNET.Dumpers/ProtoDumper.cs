using ArcNET.Formats;
using Bia.ValueBuffers;

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
        Span<char> buf = stackalloc char[1024];
        var vsb = new ValueStringBuilder(buf);
        var asMob = new MobData { Header = proto.Header, Properties = proto.Properties };
        MobDumper.DumpHeader(ref vsb, proto.Header, "=== PROTO HEADER ===");
        MobDumper.DumpProperties(ref vsb, asMob);
        return vsb.ToString();
    }

    public static void Dump(ProtoData proto, TextWriter writer) => writer.Write(Dump(proto));
}
