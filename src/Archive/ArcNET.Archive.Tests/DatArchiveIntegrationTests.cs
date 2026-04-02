using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using ArcNET.Archive;

namespace ArcNET.Archive.Tests;

/// <summary>
/// Integration tests for <see cref="DatArchive"/>, <see cref="DatExtractor"/>, and
/// <see cref="DatPacker"/> using fully synthetic in-memory DAT fixtures written to a
/// temporary directory.
/// </summary>
public class DatArchiveIntegrationTests
{
    // ── DAT builder helper ───────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal valid Arcanum DAT archive in memory.
    /// Layout:  [file-data...] [directory-table] [footer: dirOffset(4) + archiveSize(4)]
    /// Directory entry: nameLen(4) + nameBytes + uncompressedSize(4) + compressedSize(4) + offset(4)
    /// </summary>
    private static byte[] BuildDat(IReadOnlyDictionary<string, byte[]> entries)
    {
        var out2 = new ArrayBufferWriter<byte>(4096);

        // 1. Write all file data sequentially and record offsets
        var infos = new List<(string Name, int Offset, int Size)>(entries.Count);
        foreach (var (name, data) in entries)
        {
            var offset = out2.WrittenCount;
            var span = out2.GetSpan(data.Length);
            data.CopyTo(span);
            out2.Advance(data.Length);
            infos.Add((name, offset, data.Length));
        }

        // 2. Write directory table
        var dirOffset = out2.WrittenCount;
        foreach (var (name, offset, size) in infos)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            WriteInt32(out2, nameBytes.Length);
            var nameSpan = out2.GetSpan(nameBytes.Length);
            nameBytes.CopyTo(nameSpan);
            out2.Advance(nameBytes.Length);
            WriteInt32(out2, size); // uncompressedSize
            WriteInt32(out2, 0); // compressedSize = 0 (stored, not compressed)
            WriteInt32(out2, offset);
        }

        // 3. Write footer
        WriteInt32(out2, dirOffset);
        WriteInt32(out2, out2.WrittenCount + 4); // archiveSize (incl. final 4 bytes)

        return out2.WrittenMemory.ToArray();
    }

    private static void WriteInt32(ArrayBufferWriter<byte> buf, int value)
    {
        var span = buf.GetSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        buf.Advance(4);
    }

    private static string WriteTempDat(IReadOnlyDictionary<string, byte[]> entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"arcnet_test_{Guid.NewGuid():N}.dat");
        File.WriteAllBytes(path, BuildDat(entries));
        return path;
    }

    // ── test entries ─────────────────────────────────────────────────────────

    private static readonly Dictionary<string, byte[]> s_entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["art\\tiles\\tile0.art"] = [0x01, 0x02, 0x03, 0x04],
        ["mes\\combat.mes"] = Encoding.UTF8.GetBytes("{100}{}{Attack}"),
        ["EMPTY.sec"] = [],
    };

    // ── open / enumerate ─────────────────────────────────────────────────────

    [Test]
    public async Task Open_ValidDat_ReturnsExpectedEntryCount()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            await Assert.That(archive.Entries.Count).IsEqualTo(s_entries.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FindEntry_KnownPath_ReturnsEntry()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            var entry = archive.FindEntry("mes\\combat.mes");
            await Assert.That(entry).IsNotNull();
            await Assert.That(entry!.UncompressedSize).IsEqualTo(s_entries["mes\\combat.mes"].Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FindEntry_CaseInsensitive_ReturnsEntry()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            var entry = archive.FindEntry("MES\\COMBAT.MES");
            await Assert.That(entry).IsNotNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FindEntry_UnknownPath_ReturnsNull()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            var entry = archive.FindEntry("nonexistent\\file.dat");
            await Assert.That(entry).IsNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── read content ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetEntryData_ReturnsCorrectBytes()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            var data = archive.GetEntryData("art\\tiles\\tile0.art");
            await Assert.That(data.ToArray().SequenceEqual(s_entries["art\\tiles\\tile0.art"])).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task OpenEntry_StreamContainsCorrectBytes()
    {
        var path = WriteTempDat(s_entries);
        try
        {
            using var archive = DatArchive.Open(path);
            using var stream = archive.OpenEntry("mes\\combat.mes");
            var buf = new byte[s_entries["mes\\combat.mes"].Length];
            _ = stream.Read(buf, 0, buf.Length);
            await Assert.That(buf.SequenceEqual(s_entries["mes\\combat.mes"])).IsTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── extract ───────────────────────────────────────────────────────────────

    [Test]
    public async Task ExtractAll_CreatesExpectedFiles()
    {
        var datPath = WriteTempDat(s_entries);
        var outDir = Path.Combine(Path.GetTempPath(), $"arcnet_extract_{Guid.NewGuid():N}");
        try
        {
            using var archive = DatArchive.Open(datPath);
            await DatExtractor.ExtractAllAsync(archive, outDir);

            foreach (var (name, data) in s_entries)
            {
                var relPath = name.Replace('\\', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(outDir, relPath);
                await Assert.That(File.Exists(filePath)).IsTrue();
                await Assert.That(File.ReadAllBytes(filePath).SequenceEqual(data)).IsTrue();
            }
        }
        finally
        {
            File.Delete(datPath);
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }

    // ── pack round-trip ───────────────────────────────────────────────────────

    [Test]
    public async Task Pack_ThenOpen_RoundTripsEntryData()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), $"arcnet_src_{Guid.NewGuid():N}");
        var datPath = Path.Combine(Path.GetTempPath(), $"arcnet_repacked_{Guid.NewGuid():N}.dat");
        try
        {
            // Create source files
            Directory.CreateDirectory(sourceDir);
            foreach (var (name, data) in s_entries)
            {
                var filePath = Path.Combine(sourceDir, name.Replace('\\', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllBytes(filePath, data);
            }

            // Pack
            await DatPacker.PackAsync(sourceDir, datPath);

            // Open and verify
            using var archive = DatArchive.Open(datPath);
            await Assert.That(archive.Entries.Count).IsEqualTo(s_entries.Count);

            foreach (var (name, expected) in s_entries)
            {
                var normalised = name.Replace('/', '\\');
                var retrieved = archive.GetEntryData(normalised).ToArray();
                await Assert.That(retrieved.SequenceEqual(expected)).IsTrue();
            }
        }
        finally
        {
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, recursive: true);
            if (File.Exists(datPath))
                File.Delete(datPath);
        }
    }
}
