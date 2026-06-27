using ArcNET.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ArcNET.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class DiagnosticsBytePatternBench
{
    private BytePattern _pattern = null!;
    private byte?[] _legacyPattern = [];
    private byte[] _haystack = [];

    [Params(64 * 1024)]
    public int HaystackLength { get; set; }

    [Params(97, 8)]
    public int MatchStride { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pattern = BytePattern.Parse("8B ?? FF 10");
        _legacyPattern = _pattern.Bytes;
        _haystack = new byte[HaystackLength];

        for (var index = 0; index < _haystack.Length; index++)
            _haystack[index] = (byte)((index * 31) & 0x7F);

        for (var start = 0; start + _pattern.Length <= _haystack.Length; start += MatchStride)
        {
            _haystack[start] = 0x8B;
            _haystack[start + 1] = (byte)(start & 0xFF);
            _haystack[start + 2] = 0xFF;
            _haystack[start + 3] = 0x10;
        }
    }

    [Benchmark(Baseline = true)]
    public int[] FindMatches_Legacy()
    {
        if (_haystack.Length < _legacyPattern.Length)
            return [];

        List<int> matches = [];
        for (var start = 0; start <= _haystack.Length - _legacyPattern.Length; start++)
        {
            if (MatchesAtLegacy(start))
                matches.Add(start);
        }

        return [.. matches];
    }

    [Benchmark]
    public int[] FindMatches_Anchored() => _pattern.FindMatches(_haystack);

    private bool MatchesAtLegacy(int start)
    {
        for (var index = 0; index < _legacyPattern.Length; index++)
        {
            var expected = _legacyPattern[index];
            if (expected.HasValue && _haystack[start + index] != expected.Value)
                return false;
        }

        return true;
    }
}
