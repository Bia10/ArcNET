using System.Buffers;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;
using BenchmarkDotNet.Attributes;

namespace ArcNET.Benchmarks;

/// <summary>Parse-throughput benchmarks for <see cref="SectorFormat"/>.</summary>
[MemoryDiagnoser]
public class SectorFormatBench
{
    private byte[] _sectorBytes = [];

    /// <summary>Builds a minimal synthetic sector for benchmarking.</summary>
    [GlobalSetup]
    public void Setup()
    {
        // Build a minimal valid sector: 0 lights, 4096 tiles (all zero), roof absent, placeholder 0xAA0001, 0 tile scripts.
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);

        writer.WriteInt32(0); // light count
        for (var i = 0; i < 4096; i++)
            writer.WriteUInt32(0u); // tiles
        writer.WriteInt32(1); // roof list absent
        writer.WriteInt32(0xAA0001); // placeholder — includes tile scripts
        writer.WriteInt32(0); // tile script count

        _sectorBytes = buf.WrittenSpan.ToArray();
    }

    /// <summary>Parse a minimal sector from a byte array.</summary>
    [Benchmark(Baseline = true)]
    public Sector ParseFromMemory()
    {
        var memory = _sectorBytes.AsMemory();
        return SectorFormat.ParseMemory(memory);
    }

    /// <summary>Round-trip: parse then write back to a byte array.</summary>
    [Benchmark]
    public int WriteToArray()
    {
        var sector = SectorFormat.ParseMemory(_sectorBytes);
        return SectorFormat.WriteToArray(in sector).Length;
    }
}
