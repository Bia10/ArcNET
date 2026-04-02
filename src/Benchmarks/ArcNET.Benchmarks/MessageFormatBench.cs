using System.Text;
using ArcNET.Formats;
using BenchmarkDotNet.Attributes;

namespace ArcNET.Benchmarks;

/// <summary>Parse-throughput benchmarks for <see cref="MessageFormat"/>.</summary>
[MemoryDiagnoser]
public class MessageFormatBench
{
    private string[] _lines = [];
    private byte[] _bytes = [];

    /// <summary>Builds synthetic benchmark data: 1000 valid MES entries.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lines = new string[1000];
        for (var i = 0; i < _lines.Length; i++)
            _lines[i] = $"{{{i}}}{{Message text number {i} with some content}}";

        _bytes = Encoding.UTF8.GetBytes(string.Join('\n', _lines));
    }

    /// <summary>Parse 1000 entries from pre-split string lines.</summary>
    [Benchmark(Baseline = true)]
    public int ParseFromLines() => MessageFormat.Parse(_lines).Count;

    /// <summary>Parse 1000 entries from a UTF-8 byte buffer via <see cref="MessageFormat.ParseMemory"/>.</summary>
    [Benchmark]
    public int ParseFromMemory() => MessageFormat.ParseMemory(_bytes).Count;

    /// <summary>Round-trip: parse then write back to a byte array.</summary>
    [Benchmark]
    public int WriteToArray()
    {
        var entries = MessageFormat.Parse(_lines);
        return MessageFormat.WriteToArray(entries).Length;
    }
}
