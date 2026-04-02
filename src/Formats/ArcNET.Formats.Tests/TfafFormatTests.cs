using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="TfafFormat"/>.</summary>
public sealed class TfafFormatTests
{
    private static SaveIndex BuildIndex(IReadOnlyList<TfaiEntry> root) => new() { Root = root };

    [Test]
    public async Task TotalPayloadSize_EmptyIndex_Zero()
    {
        var index = BuildIndex([]);
        await Assert.That(TfafFormat.TotalPayloadSize(index)).IsEqualTo(0);
    }

    [Test]
    public async Task TotalPayloadSize_SingleFile_MatchesSize()
    {
        var index = BuildIndex([new TfaiFileEntry { Name = "data.bin", Size = 128 }]);
        await Assert.That(TfafFormat.TotalPayloadSize(index)).IsEqualTo(128);
    }

    [Test]
    public async Task TotalPayloadSize_NestedFiles_SummedCorrectly()
    {
        var index = BuildIndex([
            new TfaiDirectoryEntry
            {
                Name = "maps",
                Children =
                [
                    new TfaiFileEntry { Name = "map.jmp", Size = 64 },
                    new TfaiFileEntry { Name = "map.prp", Size = 24 },
                ],
            },
            new TfaiFileEntry { Name = "save.dat", Size = 32 },
        ]);
        await Assert.That(TfafFormat.TotalPayloadSize(index)).IsEqualTo(120);
    }

    [Test]
    public async Task Pack_SingleFile_ContentsMatchPayload()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var index = BuildIndex([new TfaiFileEntry { Name = "f.bin", Size = 5 }]);
        var payloads = new Dictionary<string, byte[]> { ["f.bin"] = payload };

        var blob = TfafFormat.Pack(index, payloads);

        await Assert.That(blob.SequenceEqual(payload)).IsTrue();
    }

    [Test]
    public async Task Pack_ThenExtractAll_RoundTripsAllFiles()
    {
        var aData = new byte[] { 10, 20, 30 };
        var bData = new byte[] { 40, 50 };

        var index = BuildIndex([
            new TfaiDirectoryEntry
            {
                Name = "dir",
                Children =
                [
                    new TfaiFileEntry { Name = "a.bin", Size = aData.Length },
                    new TfaiFileEntry { Name = "b.bin", Size = bData.Length },
                ],
            },
        ]);

        var payloads = new Dictionary<string, byte[]> { ["dir/a.bin"] = aData, ["dir/b.bin"] = bData };

        var blob = TfafFormat.Pack(index, payloads);
        var extracted = TfafFormat.ExtractAll(index, blob.AsMemory());

        await Assert.That(extracted.Count).IsEqualTo(2);
        await Assert.That(extracted["dir/a.bin"].SequenceEqual(aData)).IsTrue();
        await Assert.That(extracted["dir/b.bin"].SequenceEqual(bData)).IsTrue();
    }

    [Test]
    public async Task Pack_MultipleFiles_DepthFirstOrder()
    {
        // Files should be packed in depth-first order: a, then b
        var aData = new byte[] { 0xAA, 0xBB };
        var bData = new byte[] { 0xCC, 0xDD, 0xEE };

        var index = BuildIndex([
            new TfaiFileEntry { Name = "a.bin", Size = aData.Length },
            new TfaiFileEntry { Name = "b.bin", Size = bData.Length },
        ]);

        var payloads = new Dictionary<string, byte[]> { ["a.bin"] = aData, ["b.bin"] = bData };

        var blob = TfafFormat.Pack(index, payloads);

        await Assert.That(blob.Length).IsEqualTo(aData.Length + bData.Length);
        await Assert.That(blob[0]).IsEqualTo((byte)0xAA);
        await Assert.That(blob[1]).IsEqualTo((byte)0xBB);
        await Assert.That(blob[2]).IsEqualTo((byte)0xCC);
    }

    [Test]
    public async Task Pack_MissingPayload_ThrowsKeyNotFound()
    {
        var index = BuildIndex([new TfaiFileEntry { Name = "missing.bin", Size = 4 }]);
        var payloads = new Dictionary<string, byte[]>();

        await Assert.That(() => TfafFormat.Pack(index, payloads)).Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task Pack_WrongPayloadSize_ThrowsArgumentException()
    {
        var index = BuildIndex([new TfaiFileEntry { Name = "f.bin", Size = 10 }]);
        var payloads = new Dictionary<string, byte[]> { ["f.bin"] = new byte[5] };

        await Assert.That(() => TfafFormat.Pack(index, payloads)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Extract_SingleFile_ReturnsCorrectBytes()
    {
        var data = new byte[] { 7, 8, 9 };
        var index = BuildIndex([new TfaiFileEntry { Name = "x.bin", Size = data.Length }]);
        var blob = TfafFormat.Pack(index, new Dictionary<string, byte[]> { ["x.bin"] = data });

        var result = TfafFormat.Extract(index, blob.AsMemory(), "x.bin");

        await Assert.That(result.SequenceEqual(data)).IsTrue();
    }

    [Test]
    public async Task Extract_NonExistentPath_ThrowsKeyNotFound()
    {
        var index = BuildIndex([new TfaiFileEntry { Name = "real.bin", Size = 1 }]);
        var blob = TfafFormat.Pack(index, new Dictionary<string, byte[]> { ["real.bin"] = [0] });

        await Assert.That(() => TfafFormat.Extract(index, blob.AsMemory(), "ghost.bin")).Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task ExtractAll_EmptyIndex_ReturnsEmptyDictionary()
    {
        var index = BuildIndex([]);
        var extracted = TfafFormat.ExtractAll(index, ReadOnlyMemory<byte>.Empty);
        await Assert.That(extracted.Count).IsEqualTo(0);
    }
}
