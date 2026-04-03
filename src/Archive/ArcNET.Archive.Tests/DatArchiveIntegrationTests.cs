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
    /// Builds a minimal valid Arcanum DAT archive in memory (DAT format, not DAT1).
    /// Layout (baseOffset=0 so entry offsets are absolute):
    ///   [data blobs...]
    ///   [entry_table_size (4): absolute position of entries_count]
    ///   [entries_count (4)]
    ///   [per entry: nameLen(4) + name+NUL(nameLen) + skip(4) + flags(4) + uncompSize(4) + compSize(4) + offset(4)]
    ///   [footer: magic(4)='DAT ' + nameTableSize(4)=0 + entryTableOffset(4)]
    /// </summary>
    private static byte[] BuildDat(IReadOnlyDictionary<string, byte[]> entries)
    {
        var buf = new ArrayBufferWriter<byte>(4096);

        // 1. Write all file data blobs and record absolute offsets
        var infos = new List<(string Name, int Offset, int Size)>(entries.Count);
        foreach (var (name, data) in entries)
        {
            var offset = buf.WrittenCount;
            var span = buf.GetSpan(data.Length);
            data.CopyTo(span);
            buf.Advance(data.Length);
            infos.Add((name, offset, data.Length));
        }

        // 2. entry_table_size = absolute position of entries_count (which is right after this field)
        var tableSizePos = buf.WrittenCount;
        var entriesCountPos = tableSizePos + 4;
        WriteUInt32(buf, (uint)entriesCountPos);

        // 3. entries_count
        WriteUInt32(buf, (uint)infos.Count);

        // 4. Entry records
        foreach (var (name, offset, size) in infos)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            var nameLen = nameBytes.Length + 1; // include null terminator
            WriteUInt32(buf, (uint)nameLen);
            var nameSpan = buf.GetSpan(nameBytes.Length);
            nameBytes.CopyTo(nameSpan);
            buf.Advance(nameBytes.Length);
            buf.GetSpan(1)[0] = 0; // null terminator (1 byte)
            buf.Advance(1);
            WriteUInt32(buf, 0u); // unknown skip field
            WriteUInt32(buf, 0x001u); // flags: Plain
            WriteUInt32(buf, (uint)size); // uncompressedSize
            WriteUInt32(buf, (uint)size); // compressedSize (= uncompressedSize for plain)
            WriteUInt32(buf, (uint)offset); // absolute file offset
        }

        // 5. Footer (12 bytes): magic + nameTableSize + entryTableOffset
        //    entryTableOffset = fileLength - 4 - tableSizePos  (ensures baseOffset = 0)
        var footerStart = buf.WrittenCount;
        var fileLength = footerStart + 12;
        var entryTableOffset = (uint)(fileLength - 4 - tableSizePos);
        WriteUInt32(buf, 0x44415420u); // 'DAT ' magic (FourCC LE)
        WriteUInt32(buf, 0u); // nameTableSize (unused by reader)
        WriteUInt32(buf, entryTableOffset);

        return buf.WrittenMemory.ToArray();
    }

    private static void WriteUInt32(ArrayBufferWriter<byte> buf, uint value)
    {
        var span = buf.GetSpan(4);
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
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
