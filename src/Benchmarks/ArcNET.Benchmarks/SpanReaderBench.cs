using ArcNET.Core;
using BenchmarkDotNet.Attributes;

namespace ArcNET.Benchmarks;

/// <summary>Benchmarks for <see cref="SpanReader"/> primitive reads.</summary>
public class SpanReaderBench
{
    private byte[] _data = [];

    /// <summary>Setup benchmark data.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[32];
        Random.Shared.NextBytes(_data);
    }

    /// <summary>Baseline: read four Int32 values.</summary>
    [Benchmark(Baseline = true)]
    public int ReadFourInt32()
    {
        var reader = new SpanReader(_data);
        return reader.ReadInt32() + reader.ReadInt32() + reader.ReadInt32() + reader.ReadInt32();
    }
}
