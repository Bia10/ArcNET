using System.Buffers.Binary;

namespace ArcNET.Formats;

public sealed partial record CharacterMdyRecord
{
    public static CharacterMdyRecord Parse(ReadOnlySpan<byte> span, out int consumed) =>
        CharacterMdyRecordParser.Parse(span, out consumed);
}
