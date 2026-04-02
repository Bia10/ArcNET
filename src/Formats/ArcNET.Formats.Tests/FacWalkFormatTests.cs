using System.Buffers;
using System.Text;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="FacWalkFormat"/>.</summary>
public sealed class FacWalkFormatTests
{
    private static byte[] BuildBytes(Action<SpanWriter> fill)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        fill(w);
        return buf.WrittenSpan.ToArray();
    }

    private static byte[] BuildMinimalFacWalk(uint entryCount = 0)
    {
        return BuildBytes(w =>
        {
            // 14-byte marker
            w.WriteBytes(Encoding.ASCII.GetBytes("FacWalk V101  "));
            w.WriteUInt32(1); // terrain
            w.WriteUInt32(1); // outdoor
            w.WriteUInt32(0); // flippable
            w.WriteUInt32(4); // width
            w.WriteUInt32(3); // height
            w.WriteUInt32(entryCount);

            for (var i = 0u; i < entryCount; i++)
            {
                w.WriteUInt32(i); // x
                w.WriteUInt32(i + 1); // y
                w.WriteUInt32(1u); // walkable
            }
        });
    }

    [Test]
    public async Task Parse_HeaderFields_CorrectValues()
    {
        var bytes = BuildMinimalFacWalk();
        var facwalk = FacWalkFormat.ParseMemory(bytes);

        await Assert.That(facwalk.Header.Terrain).IsEqualTo(1u);
        await Assert.That(facwalk.Header.Outdoor).IsEqualTo(1u);
        await Assert.That(facwalk.Header.Flippable).IsEqualTo(0u);
        await Assert.That(facwalk.Header.Width).IsEqualTo(4u);
        await Assert.That(facwalk.Header.Height).IsEqualTo(3u);
    }

    [Test]
    public async Task Parse_Empty_ZeroEntries()
    {
        var bytes = BuildMinimalFacWalk(0);
        var facwalk = FacWalkFormat.ParseMemory(bytes);
        await Assert.That(facwalk.Entries.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_ThreeEntries_Walkable()
    {
        var bytes = BuildMinimalFacWalk(3);
        var facwalk = FacWalkFormat.ParseMemory(bytes);

        await Assert.That(facwalk.Entries.Length).IsEqualTo(3);
        await Assert.That(facwalk.Entries[0].X).IsEqualTo(0u);
        await Assert.That(facwalk.Entries[0].Y).IsEqualTo(1u);
        await Assert.That(facwalk.Entries[0].Walkable).IsTrue();
    }

    [Test]
    public async Task Parse_WalkableZero_IsFalse()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteBytes(Encoding.ASCII.GetBytes("FacWalk V101  "));
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1);
            w.WriteUInt32(1);
            w.WriteUInt32(1); // one entry
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0); // not walkable
        });

        var facwalk = FacWalkFormat.ParseMemory(bytes);
        await Assert.That(facwalk.Entries[0].Walkable).IsFalse();
    }

    [Test]
    public async Task Parse_BadMarker_Throws()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteBytes(Encoding.ASCII.GetBytes("NotFacWalk    "));
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1);
            w.WriteUInt32(1);
            w.WriteUInt32(0);
        });

        Assert.Throws<InvalidDataException>(() => FacWalkFormat.ParseMemory(bytes));
    }

    [Test]
    public async Task RoundTrip_EntriesPreserved()
    {
        var bytes = BuildMinimalFacWalk(5);
        var original = FacWalkFormat.ParseMemory(bytes);
        var rewritten = FacWalkFormat.WriteToArray(in original);
        var back = FacWalkFormat.ParseMemory(rewritten);

        await Assert.That(back.Header.Terrain).IsEqualTo(original.Header.Terrain);
        await Assert.That(back.Header.Width).IsEqualTo(original.Header.Width);
        await Assert.That(back.Entries.Length).IsEqualTo(original.Entries.Length);

        for (var i = 0; i < original.Entries.Length; i++)
        {
            await Assert.That(back.Entries[i].X).IsEqualTo(original.Entries[i].X);
            await Assert.That(back.Entries[i].Y).IsEqualTo(original.Entries[i].Y);
            await Assert.That(back.Entries[i].Walkable).IsEqualTo(original.Entries[i].Walkable);
        }
    }
}
