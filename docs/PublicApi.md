# ArcNET Public API

## ArcNET.Core

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Archive")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Benchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Core.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Formats")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameData")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameObjects")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Patch")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Core
{
    public static class ArcanumInstallation
    {
        public const int UapProtoIdOffset = 20;
        public static ArcNET.Core.ArcanumInstallationType Detect(string gameDir) { }
        public static int ToVanillaProtoId(int protoId, ArcNET.Core.ArcanumInstallationType installation) { }
    }
    public enum ArcanumInstallationType : byte
    {
        Vanilla = 0,
        UniversalArcanumPatcher = 1,
    }
    public static class EnumLookup<TEnum>
        where TEnum :  struct, System.Enum
    {
        public static readonly System.Collections.Frozen.FrozenDictionary<string, TEnum> ByName;
        public static readonly System.Collections.Frozen.FrozenDictionary<TEnum, string> ToName;
        public static string GetName(TEnum value) { }
        public static bool TryGetByName(string name, out TEnum value) { }
    }
    public interface IBinarySerializable<TSelf, TReader>
        where TSelf : ArcNET.Core.IBinarySerializable<TSelf, TReader>
    {
        void Write(ref ArcNET.Core.SpanWriter writer);
        TSelf Read(ref TReader reader);
    }
    public delegate T ReadElement<T>(ref ArcNET.Core.SpanReader reader);
    public ref struct SpanReader
    {
        public SpanReader(System.ReadOnlySpan<byte> data) { }
        public int Position { get; }
        public int Remaining { get; }
        public int PeekInt32At(int offset) { }
        public byte ReadByte() { }
        public System.ReadOnlySpan<byte> ReadBytes(int count) { }
        public double ReadDouble() { }
        public short ReadInt16() { }
        public int ReadInt32() { }
        public long ReadInt64() { }
        public float ReadSingle() { }
        public ushort ReadUInt16() { }
        public uint ReadUInt32() { }
        public ulong ReadUInt64() { }
        public void Skip(int count) { }
        public ArcNET.Core.SpanReader Slice(int length) { }
        public bool TryPeek(out byte value) { }
    }
    public static class SpanReaderExtensions
    {
        public static T[] ReadArray<T>(ref this ArcNET.Core.SpanReader reader, int count, ArcNET.Core.ReadElement<T> readOne) { }
        public static ArcNET.Core.Primitives.ArtId ReadArtId(ref this ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Core.Primitives.GameObjectGuid ReadGameObjectGuid(ref this ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Core.Primitives.Location ReadLocation(ref this ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Core.Primitives.PrefixedString ReadPrefixedString(ref this ArcNET.Core.SpanReader reader) { }
    }
    public ref struct SpanWriter
    {
        public SpanWriter(System.Buffers.IBufferWriter<byte> output) { }
        public void WriteByte(byte v) { }
        public void WriteBytes(System.ReadOnlySpan<byte> data) { }
        public void WriteDouble(double v) { }
        public void WriteInt16(short v) { }
        public void WriteInt32(int v) { }
        public void WriteInt64(long v) { }
        public void WriteSingle(float v) { }
        public void WriteUInt16(ushort v) { }
        public void WriteUInt32(uint v) { }
        public void WriteUInt64(ulong v) { }
    }
    public static class SpanWriterExtensions
    {
        public static void WriteArray<T>(ref this ArcNET.Core.SpanWriter writer, System.Collections.Generic.IReadOnlyList<T> items, ArcNET.Core.WriteElement<T> writeOne) { }
        public static void WriteArtId(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.ArtId value) { }
        public static void WriteGameObjectGuid(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.GameObjectGuid value) { }
        public static void WriteLocation(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.Location value) { }
        public static void WritePrefixedString(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.PrefixedString value) { }
    }
    public static class StackAllocPolicy { }
    public delegate void WriteElement<T>(ref ArcNET.Core.SpanWriter writer, T item);
}
namespace ArcNET.Core.Primitives
{
    public readonly struct ArtId : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.ArtId, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.ArtId>, System.IFormattable, System.ISpanFormattable, System.IUtf8SpanFormattable
    {
        public ArtId(uint Value) { }
        public uint Value { get; init; }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<byte> utf8Dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.ArtId Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public readonly struct Color : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.Color, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.Color>, System.IFormattable, System.ISpanFormattable
    {
        public Color(byte R, byte G, byte B) { }
        public byte B { get; init; }
        public byte G { get; init; }
        public byte R { get; init; }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.Color Read(ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Core.Primitives.Color ReadRgba(ref ArcNET.Core.SpanReader reader) { }
    }
    public readonly struct GameObjectGuid : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.GameObjectGuid, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.GameObjectGuid>, System.IFormattable, System.ISpanFormattable
    {
        public GameObjectGuid(short OidType, short Padding2, int Padding4, System.Guid Id) { }
        public System.Guid Id { get; init; }
        public bool IsProto { get; }
        public short OidType { get; init; }
        public short Padding2 { get; init; }
        public int Padding4 { get; init; }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.GameObjectGuid Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public readonly struct Location : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.Location, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.Location>, System.IFormattable, System.ISpanFormattable
    {
        public Location(short X, short Y) { }
        public short X { get; init; }
        public short Y { get; init; }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.Location Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public readonly struct PrefixedString : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.PrefixedString, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.PrefixedString>
    {
        public PrefixedString(string Value) { }
        public string Value { get; init; }
        public override string ToString() { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.PrefixedString Read(ref ArcNET.Core.SpanReader reader) { }
    }
}```

## ArcNET.Archive

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.App")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Archive.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Archive
{
    public sealed class ArchiveEntry
    {
        public ArchiveEntry() { }
        public required int CompressedSize { get; init; }
        public required ArcNET.Archive.DatEntryFlags Flags { get; init; }
        public bool IsCompressed { get; }
        public bool IsDirectory { get; }
        public required int Offset { get; init; }
        public required string Path { get; init; }
        public required int UncompressedSize { get; init; }
    }
    public sealed class DatArchive : System.IDisposable
    {
        public System.Collections.Generic.IReadOnlyCollection<ArcNET.Archive.ArchiveEntry> Entries { get; }
        public void Dispose() { }
        public ArcNET.Archive.ArchiveEntry? FindEntry(string virtualPath) { }
        public System.ReadOnlyMemory<byte> GetEntryData(string name) { }
        public System.IO.Stream OpenEntry(string name) { }
        public byte[] ReadEntry(ArcNET.Archive.ArchiveEntry entry) { }
        public static ArcNET.Archive.DatArchive Open(string path) { }
    }
    [System.Flags]
    public enum DatEntryFlags : ushort
    {
        None = 0,
        Plain = 1,
        Compressed = 2,
        InArchive = 256,
        Overridden = 512,
        Directory = 1024,
        Ignored = 2048,
    }
    public static class DatExtractor
    {
        public static System.Threading.Tasks.Task ExtractAllAsync(ArcNET.Archive.DatArchive archive, string outputDir, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static System.Threading.Tasks.Task ExtractEntryAsync(ArcNET.Archive.DatArchive archive, string entryName, string outputDir, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public static class DatPacker
    {
        public static System.Threading.Tasks.Task PackAsync(string inputDir, string outputPath, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
}```

## ArcNET.Formats

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Formats.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameData")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Formats
{
    public sealed class ArtFile
    {
        public ArtFile() { }
        public required uint ActionFrame { get; init; }
        public required uint[] DataSizes { get; init; }
        public int EffectiveRotationCount { get; }
        public required ArcNET.Formats.ArtFlags Flags { get; init; }
        public required uint FrameCount { get; init; }
        public required uint FrameRate { get; init; }
        public required ArcNET.Formats.ArtFrame[][] Frames { get; init; }
        public required int[] PaletteIds { get; init; }
        public required ArcNET.Formats.ArtPaletteEntry[]?[] Palettes { get; init; }
        public required uint[] PaletteData1 { get; init; }
        public required uint[] PaletteData2 { get; init; }
    }
    [System.Flags]
    public enum ArtFlags : uint
    {
        None = 0u,
        Static = 1u,
        Critter = 2u,
        Font = 4u,
    }
    public sealed class ArtFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.ArtFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ArtFile>
    {
        public ArtFormat() { }
        public static ArcNET.Formats.ArtFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ArtFile ParseFile(string path) { }
        public static ArcNET.Formats.ArtFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.ArtFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.ArtFile value) { }
        public static void WriteToFile(in ArcNET.Formats.ArtFile value, string path) { }
    }
    public sealed class ArtFrame
    {
        public ArtFrame() { }
        public required ArcNET.Formats.ArtFrameHeader Header { get; init; }
        public required byte[] Pixels { get; init; }
    }
    public readonly struct ArtFrameHeader : System.IEquatable<ArcNET.Formats.ArtFrameHeader>
    {
        public ArtFrameHeader(uint Width, uint Height, uint DataSize, int CenterX, int CenterY, int DeltaX, int DeltaY) { }
        public int CenterX { get; init; }
        public int CenterY { get; init; }
        public uint DataSize { get; init; }
        public int DeltaX { get; init; }
        public int DeltaY { get; init; }
        public uint Height { get; init; }
        public uint Width { get; init; }
    }
    public readonly struct ArtPaletteEntry : System.IEquatable<ArcNET.Formats.ArtPaletteEntry>
    {
        public ArtPaletteEntry(byte Blue, byte Green, byte Red) { }
        public byte Blue { get; init; }
        public byte Green { get; init; }
        public byte Red { get; init; }
    }
    public sealed class DialogEntry
    {
        public DialogEntry() { }
        public required string Actions { get; init; }
        public required string Conditions { get; init; }
        public required string GenderField { get; init; }
        public required int Iq { get; init; }
        public required int Num { get; init; }
        public required int ResponseVal { get; init; }
        public required string Text { get; init; }
    }
    public sealed class DialogFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.DlgFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.DlgFile>
    {
        public DialogFormat() { }
        public static ArcNET.Formats.DlgFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.DlgFile ParseFile(string path) { }
        public static ArcNET.Formats.DlgFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.DlgFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.DlgFile value) { }
        public static void WriteToFile(in ArcNET.Formats.DlgFile value, string path) { }
    }
    public sealed class DlgFile
    {
        public DlgFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.DialogEntry> Entries { get; init; }
    }
    public readonly struct FacWalkEntry : System.IEquatable<ArcNET.Formats.FacWalkEntry>
    {
        public FacWalkEntry(uint X, uint Y, bool Walkable) { }
        public bool Walkable { get; init; }
        public uint X { get; init; }
        public uint Y { get; init; }
    }
    public sealed class FacWalkFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.FacadeWalk>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.FacadeWalk>
    {
        public FacWalkFormat() { }
        public static ArcNET.Formats.FacadeWalk Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.FacadeWalk ParseFile(string path) { }
        public static ArcNET.Formats.FacadeWalk ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.FacadeWalk value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.FacadeWalk value) { }
        public static void WriteToFile(in ArcNET.Formats.FacadeWalk value, string path) { }
    }
    public readonly struct FacWalkHeader : System.IEquatable<ArcNET.Formats.FacWalkHeader>
    {
        public FacWalkHeader(uint Terrain, uint Outdoor, uint Flippable, uint Width, uint Height) { }
        public uint Flippable { get; init; }
        public uint Height { get; init; }
        public uint Outdoor { get; init; }
        public uint Terrain { get; init; }
        public uint Width { get; init; }
    }
    public sealed class FacadeWalk
    {
        public FacadeWalk() { }
        public required ArcNET.Formats.FacWalkEntry[] Entries { get; init; }
        public required ArcNET.Formats.FacWalkHeader Header { get; init; }
    }
    public enum FileFormat : byte
    {
        Unknown = 0,
        Sector = 1,
        Proto = 2,
        Message = 3,
        Mob = 4,
        Art = 5,
        Jmp = 6,
        Script = 7,
        Dialog = 8,
        Terrain = 9,
        MapProperties = 10,
        FacadeWalk = 11,
        DataArchive = 12,
        SaveInfo = 13,
        SaveIndex = 14,
        SaveData = 15,
    }
    public static class FileFormatExtensions
    {
        public static ArcNET.Formats.FileFormat FromExtension(string extension) { }
        public static ArcNET.Formats.FileFormat FromPath(string path) { }
    }
    public interface IFormatReader<T>
    {
        T Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader);
        T ParseFile(string path);
        T ParseMemory(System.ReadOnlyMemory<byte> memory);
    }
    public interface IFormatWriter<T>
    {
        void Write(in T value, ref ArcNET.Core.SpanWriter writer);
        byte[] WriteToArray(in T value);
        void WriteToFile(in T value, string path);
    }
    public sealed class JmpFile
    {
        public JmpFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.JumpEntry> Jumps { get; init; }
    }
    public sealed class JmpFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.JmpFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.JmpFile>
    {
        public JmpFormat() { }
        public static ArcNET.Formats.JmpFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.JmpFile ParseFile(string path) { }
        public static ArcNET.Formats.JmpFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.JmpFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.JmpFile value) { }
        public static void WriteToFile(in ArcNET.Formats.JmpFile value, string path) { }
    }
    public sealed class JumpEntry
    {
        public JumpEntry() { }
        public int DestX { get; }
        public int DestY { get; }
        public required long DestinationLoc { get; init; }
        public required int DestinationMapId { get; init; }
        public required uint Flags { get; init; }
        public required long SourceLoc { get; init; }
        public int SourceX { get; }
        public int SourceY { get; }
    }
    public sealed class MapProperties
    {
        public MapProperties() { }
        public required int ArtId { get; init; }
        public required ulong LimitX { get; init; }
        public required ulong LimitY { get; init; }
        public required int Unused { get; init; }
    }
    public sealed class MapPropertiesFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.MapProperties>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MapProperties>
    {
        public MapPropertiesFormat() { }
        public static ArcNET.Formats.MapProperties Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MapProperties ParseFile(string path) { }
        public static ArcNET.Formats.MapProperties ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.MapProperties value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MapProperties value) { }
        public static void WriteToFile(in ArcNET.Formats.MapProperties value, string path) { }
    }
    public sealed class MesFile
    {
        public MesFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry> Entries { get; init; }
    }
    public readonly struct MessageEntry : System.IEquatable<ArcNET.Formats.MessageEntry>
    {
        public MessageEntry(int index, string text) { }
        public MessageEntry(int Index, string? SoundId, string Text) { }
        public int Index { get; init; }
        public string? SoundId { get; init; }
        public string Text { get; init; }
    }
    public sealed class MessageFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.MesFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MesFile>
    {
        public MessageFormat() { }
        public static ArcNET.Formats.MesFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MesFile ParseFile(string path) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry> ParseLines(System.Collections.Generic.IEnumerable<string> lines) { }
        public static ArcNET.Formats.MesFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static string Serialize(System.Collections.Generic.IEnumerable<ArcNET.Formats.MessageEntry> entries) { }
        public static void Write(in ArcNET.Formats.MesFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MesFile value) { }
        public static void WriteToFile(in ArcNET.Formats.MesFile value, string path) { }
    }
    public sealed class MobData
    {
        public MobData() { }
        public required ArcNET.GameObjects.GameObjectHeader Header { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> Properties { get; init; }
    }
    public sealed class MobFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.MobData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MobData>
    {
        public MobFormat() { }
        public static ArcNET.Formats.MobData Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MobData ParseFile(string path) { }
        public static ArcNET.Formats.MobData ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.MobData value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MobData value) { }
        public static void WriteToFile(in ArcNET.Formats.MobData value, string path) { }
    }
    public sealed class ObjectProperty
    {
        public ObjectProperty() { }
        public required ArcNET.GameObjects.ObjectField Field { get; init; }
        public required byte[] RawBytes { get; init; }
    }
    public static class ObjectPropertyExtensions
    {
        public const int ObjectIdWireSize = 24;
        public static float GetFloat(this ArcNET.Formats.ObjectProperty property) { }
        public static int GetInt32(this ArcNET.Formats.ObjectProperty property) { }
        public static int[] GetInt32Array(this ArcNET.Formats.ObjectProperty property) { }
        public static long GetInt64(this ArcNET.Formats.ObjectProperty property) { }
        public static long[] GetInt64Array(this ArcNET.Formats.ObjectProperty property) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "X",
                "Y"})]
        public static System.ValueTuple<int, int> GetLocation(this ArcNET.Formats.ObjectProperty property) { }
        public static System.Guid[] GetObjectIdArray(this ArcNET.Formats.ObjectProperty property) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "OidType",
                "ProtoOrData1",
                "Id"})]
        public static System.ValueTuple<short, int, System.Guid>[] GetObjectIdArrayFull(this ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Formats.ObjectPropertyScript[] GetScriptArray(this ArcNET.Formats.ObjectProperty property) { }
        public static string GetString(this ArcNET.Formats.ObjectProperty property) { }
        public static uint[] GetUInt32Array(this ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Formats.ObjectProperty WithEmptyObjectIdArray(this ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Formats.ObjectProperty WithFloat(this ArcNET.Formats.ObjectProperty property, float value) { }
        public static ArcNET.Formats.ObjectProperty WithInt32(this ArcNET.Formats.ObjectProperty property, int value) { }
        public static ArcNET.Formats.ObjectProperty WithInt32Array(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<int> values) { }
        public static ArcNET.Formats.ObjectProperty WithInt64(this ArcNET.Formats.ObjectProperty property, long value) { }
        public static ArcNET.Formats.ObjectProperty WithInt64Array(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<long> values) { }
        public static ArcNET.Formats.ObjectProperty WithLocation(this ArcNET.Formats.ObjectProperty property, int x, int y) { }
        public static ArcNET.Formats.ObjectProperty WithObjectIdArray(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<System.Guid> ids) { }
        public static ArcNET.Formats.ObjectProperty WithScriptArray(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<ArcNET.Formats.ObjectPropertyScript> scripts) { }
        public static ArcNET.Formats.ObjectProperty WithString(this ArcNET.Formats.ObjectProperty property, string value) { }
        public static ArcNET.Formats.ObjectProperty WithUInt32Array(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<uint> values) { }
    }
    public readonly struct ObjectPropertyScript : System.IEquatable<ArcNET.Formats.ObjectPropertyScript>
    {
        public ObjectPropertyScript(uint Flags, uint Counters, int ScriptId) { }
        public uint Counters { get; init; }
        public uint Flags { get; init; }
        public int ScriptId { get; init; }
    }
    public sealed class ProtoData
    {
        public ProtoData() { }
        public required ArcNET.GameObjects.GameObjectHeader Header { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> Properties { get; init; }
    }
    public sealed class ProtoFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.ProtoData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ProtoData>
    {
        public ProtoFormat() { }
        public static ArcNET.Formats.ProtoData Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ProtoData ParseFile(string path) { }
        public static ArcNET.Formats.ProtoData ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.ProtoData value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.ProtoData value) { }
        public static void WriteToFile(in ArcNET.Formats.ProtoData value, string path) { }
    }
    public sealed class SaveIndex
    {
        public SaveIndex() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TfaiEntry> Root { get; init; }
    }
    public sealed class SaveIndexFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.SaveIndex>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.SaveIndex>
    {
        public SaveIndexFormat() { }
        public static ArcNET.Formats.SaveIndex Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.SaveIndex ParseFile(string path) { }
        public static ArcNET.Formats.SaveIndex ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.SaveIndex value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.SaveIndex value) { }
        public static void WriteToFile(in ArcNET.Formats.SaveIndex value, string path) { }
    }
    public sealed class SaveInfo
    {
        public SaveInfo() { }
        public required string DisplayName { get; init; }
        public required int GameTimeDays { get; init; }
        public required int GameTimeMs { get; init; }
        public required int LeaderLevel { get; init; }
        public required string LeaderName { get; init; }
        public required int LeaderPortraitId { get; init; }
        public required int LeaderTileX { get; init; }
        public required int LeaderTileY { get; init; }
        public required int MapId { get; init; }
        public required string ModuleName { get; init; }
        public required int StoryState { get; init; }
    }
    public sealed class SaveInfoFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.SaveInfo>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.SaveInfo>
    {
        public SaveInfoFormat() { }
        public static ArcNET.Formats.SaveInfo Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.SaveInfo ParseFile(string path) { }
        public static ArcNET.Formats.SaveInfo ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.SaveInfo value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.SaveInfo value) { }
        public static void WriteToFile(in ArcNET.Formats.SaveInfo value, string path) { }
    }
    public sealed class ScrFile
    {
        public ScrFile() { }
        public required string Description { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ScriptConditionData> Entries { get; init; }
        public required ArcNET.Formats.ScriptFlags Flags { get; init; }
        public required uint HeaderCounters { get; init; }
        public required uint HeaderFlags { get; init; }
    }
    public readonly struct ScriptActionData : System.IEquatable<ArcNET.Formats.ScriptActionData>
    {
        public ScriptActionData(int Type, byte[] OpTypes, int[] OpValues) { }
        public byte[] OpTypes { get; init; }
        public int[] OpValues { get; init; }
        public int Type { get; init; }
    }
    public enum ScriptActionType : byte
    {
        DoNothing = 0,
        ReturnAndSkipDefault = 1,
        ReturnAndRunDefault = 2,
        Goto = 3,
        Dialog = 4,
        RemoveThisScript = 5,
        ChangeThisScriptToScript = 6,
        CallScript = 7,
        SetLocalFlag = 8,
        ClearLocalFlag = 9,
        AssignNum = 10,
        Add = 11,
        Subtract = 12,
        Multiply = 13,
        Divide = 14,
        AssignObj = 15,
        SetPcQuestState = 16,
        SetQuestGlobalState = 17,
        LoopFor = 18,
        LoopEnd = 19,
        LoopBreak = 20,
        CritterFollow = 21,
        CritterDisband = 22,
        FloatLine = 23,
        PrintLine = 24,
        AddBlessing = 25,
        RemoveBlessing = 26,
        AddCurse = 27,
        RemoveCurse = 28,
        GetReaction = 29,
        SetReaction = 30,
        AdjustReaction = 31,
        GetArmor = 32,
        GetStat = 33,
        GetObjectType = 34,
        AdjustGold = 35,
        Attack = 36,
        Random = 37,
        GetSocialClass = 38,
        GetOrigin = 39,
        TransformAttacheeIntoBasicPrototype = 40,
        TransferItem = 41,
        GetStoryState = 42,
        SetStoryState = 43,
        Teleport = 44,
        SetDayStandpoint = 45,
        SetNightStandpoint = 46,
        GetSkill = 47,
        CastSpell = 48,
        MarkMapLocation = 49,
        SetRumor = 50,
        QuellRumor = 51,
        CreateObject = 52,
        SetLockState = 53,
        CallScriptIn = 54,
        CallScriptAt = 55,
        ToggleState = 56,
        ToggleInvulnerability = 57,
        Kill = 58,
        ChangeArtNum = 59,
        Damage = 60,
        CastSpellOn = 61,
        ActionPerformAnimation = 62,
        GiveQuestXp = 63,
        WrittenUiStartBook = 64,
        WrittenUiStartImage = 65,
        CreateItem = 66,
        ActionWaitForLeader = 67,
        Destroy = 68,
        ActionWalkTo = 69,
        GetWeaponType = 70,
        DistanceBetween = 71,
        AddReputation = 72,
        RemoveReputation = 73,
        ActionRunTo = 74,
        HealHp = 75,
        HealFatigue = 76,
        AddEffect = 77,
        RemoveEffect = 78,
        ActionUseItem = 79,
        GetMagictechAdjustment = 80,
        CallScriptEx = 81,
        PlaySound = 82,
        PlaySoundOn = 83,
        GetArea = 84,
        QueueNewspaper = 85,
        FloatNewspaperHeadline = 86,
        PlaySoundScheme = 87,
        ToggleOpenClosed = 88,
        GetFaction = 89,
        GetScrollDistance = 90,
        GetMagictechAdjustmentEx = 91,
        Rename = 92,
        ActionBecomeProne = 93,
        SetWrittenStart = 94,
        GetLocation = 95,
        GetDaySinceStartup = 96,
        GetCurrentHour = 97,
        GetCurrentMinute = 98,
        ChangeScript = 99,
        SetGlobalFlag = 100,
        ClearGlobalFlag = 101,
        FadeAndTeleport = 102,
        Fade = 103,
        PlaySpellEyeCandy = 104,
        GetHoursSinceStartup = 105,
        ToggleSectorBlocked = 106,
        GetHitPoints = 107,
        GetFatiguePoints = 108,
        ActionStopAttacking = 109,
        ToggleMonsterGenerator = 110,
        GetArmorCoverage = 111,
        GiveSpellMasteryInCollege = 112,
        UnfogTownmap = 113,
        StartWrittenUi = 114,
        ActionTryToSteal100Coins = 115,
        StopSpellEyeCandy = 116,
        GrantOneFatePoint = 117,
        CastFreeSpell = 118,
        SetPcQuestUnbotched = 119,
        PlayScriptEyeCandy = 120,
        ActionCastUnresistableSpell = 121,
        ActionCastFreeUnresistableSpell = 122,
        TouchArt = 123,
        StopScriptEyeCandy = 124,
        RemoveScriptCall = 125,
        DestroyItemNamed = 126,
        ToggleItemInventoryDisplay = 127,
        HealPoison = 128,
        StartSchematicUi = 129,
        StopSpell = 130,
        QueueSlide = 131,
        EndGameAndPlaySlides = 132,
        SetRotation = 133,
        SetFaction = 134,
        DrainCharges = 135,
        CastUnresistableSpell = 136,
        AdjustStat = 137,
        ApplyUnresistableDamage = 138,
        SetAutolevelScheme = 139,
        SetDayStandpointEx = 140,
        SetNightStandpointEx = 141,
    }
    public enum ScriptAttachmentPoint : byte
    {
        Examine = 0,
        Use = 1,
        Destroy = 2,
        Unlock = 3,
        Get = 4,
        Drop = 5,
        Throw = 6,
        Hit = 7,
        Miss = 8,
        Dialog = 9,
        FirstHeartbeat = 10,
        CatchingThiefPc = 11,
        Dying = 12,
        EnterCombat = 13,
        ExitCombat = 14,
        StartCombat = 15,
        EndCombat = 16,
        BuyObject = 17,
        Resurrect = 18,
        Heartbeat = 19,
        LeaderKilling = 20,
        InsertItem = 21,
        WillKos = 22,
        TakingDamage = 23,
        WieldOn = 24,
        WieldOff = 25,
        CritterHits = 26,
        NewSector = 27,
        RemoveItem = 28,
        LeaderSleeping = 29,
        Bust = 30,
        DialogOverride = 31,
        Transfer = 32,
        CaughtThief = 33,
        CriticalHit = 34,
        CriticalMiss = 35,
    }
    public readonly struct ScriptConditionData : System.IEquatable<ArcNET.Formats.ScriptConditionData>
    {
        public ScriptConditionData(int Type, byte[] OpTypes, int[] OpValues, ArcNET.Formats.ScriptActionData Action, ArcNET.Formats.ScriptActionData Else) { }
        public ArcNET.Formats.ScriptActionData Action { get; init; }
        public ArcNET.Formats.ScriptActionData Else { get; init; }
        public byte[] OpTypes { get; init; }
        public int[] OpValues { get; init; }
        public int Type { get; init; }
    }
    public enum ScriptConditionType : byte
    {
        True = 0,
        Daytime = 1,
        HasGold = 2,
        LocalFlag = 3,
        Eq = 4,
        Le = 5,
        PcQuestState = 6,
        GlobalQuestState = 7,
        ObjHasBless = 8,
        ObjHasCurse = 9,
        ObjMetPcBefore = 10,
        ObjHasBadAssociates = 11,
        ObjIsPolymorphed = 12,
        ObjIsShrunk = 13,
        ObjHasBodySpell = 14,
        ObjIsInvisible = 15,
        ObjHasMirrorImage = 16,
        ObjHasItemNamed = 17,
        ObjFollowingPc = 18,
        ObjIsMonsterOfType = 19,
        ObjIsNamed = 20,
        ObjIsWieldingItem = 21,
        ObjIsDead = 22,
        ObjHasMaxFollowers = 23,
        ObjCanOpenContainer = 24,
        ObjHasSurrendered = 25,
        ObjIsInDialog = 26,
        ObjIsSwitchedOff = 27,
        ObjCanSeeObj = 28,
        ObjCanHearObj = 29,
        ObjIsInvulnerable = 30,
        ObjIsInCombat = 31,
        ObjIsAtLocation = 32,
        ObjHasReputation = 33,
        ObjWithinRange = 34,
        ObjIsInfluencedBySpell = 35,
        ObjIsOpen = 36,
        ObjIsAnimal = 37,
        ObjIsUndead = 38,
        ObjJilted = 39,
        RumorKnown = 40,
        RumorQuelled = 41,
        ObjIsBusted = 42,
        GlobalFlag = 43,
        CanOpenPortal = 44,
        SectorIsBlocked = 45,
        MonstergenDisabled = 46,
        Identified = 47,
        KnowsSpell = 48,
        MasteredSpellCollege = 49,
        ItemsAreBeingRewielded = 50,
        Prowling = 51,
        WaitingForLeader = 52,
    }
    [System.Flags]
    public enum ScriptFlags : ushort
    {
        None = 0,
        NonmagicalTrap = 1,
        MagicalTrap = 2,
        AutoRemoving = 4,
        DeathSpeech = 8,
        SurrenderSpeech = 16,
        RadiusTwo = 32,
        RadiusThree = 64,
        RadiusFive = 128,
        TeleportTrigger = 256,
    }
    public enum ScriptFocusObject : byte
    {
        Triggerer = 0,
        Attachee = 1,
        EveryFollower = 2,
        AnyFollower = 3,
        EveryoneInParty = 4,
        AnyoneInParty = 5,
        EveryoneInTeam = 6,
        AnyoneInTeam = 7,
        EveryoneInVicinity = 8,
        AnyoneInVicinity = 9,
        CurrentLoopedObject = 10,
        LocalObject = 11,
        ExtraObject = 12,
        EveryoneInGroup = 13,
        AnyoneInGroup = 14,
        EverySceneryInVicinity = 15,
        AnySceneryInVicinity = 16,
        EveryContainerInVicinity = 17,
        AnyContainerInVicinity = 18,
        EveryPortalInVicinity = 19,
        AnyPortalInVicinity = 20,
        Player = 21,
        EveryItemInVicinity = 22,
        AnyItemInVicinity = 23,
    }
    public sealed class ScriptFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.ScrFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ScrFile>
    {
        public ScriptFormat() { }
        public static ArcNET.Formats.ScrFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ScrFile ParseFile(string path) { }
        public static ArcNET.Formats.ScrFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.ScrFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.ScrFile value) { }
        public static void WriteToFile(in ArcNET.Formats.ScrFile value, string path) { }
    }
    public enum ScriptValueType : byte
    {
        Counter = 0,
        GlobalVar = 1,
        LocalVar = 2,
        Number = 3,
        GlobalFlag = 4,
        PcVar = 5,
        PcFlag = 6,
    }
    public sealed class Sector
    {
        public Sector() { }
        public required int AptitudeAdjustment { get; init; }
        public required uint[] BlockMask { get; init; }
        public required bool HasRoofs { get; init; }
        public required int LightSchemeIdx { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.SectorLight> Lights { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> Objects { get; init; }
        public required uint[]? Roofs { get; init; }
        public ArcNET.GameObjects.GameObjectScript? SectorScript { get; init; }
        public required ArcNET.Formats.SectorSoundList SoundList { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TileScript> TileScripts { get; init; }
        public required uint[] Tiles { get; init; }
        public required int TownmapInfo { get; init; }
        public static uint GetSectorLoc(int x, int y) { }
    }
    public sealed class SectorFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.Sector>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.Sector>
    {
        public SectorFormat() { }
        public static ArcNET.Formats.Sector Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.Sector ParseFile(string path) { }
        public static ArcNET.Formats.Sector ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.Sector value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.Sector value) { }
        public static void WriteToFile(in ArcNET.Formats.Sector value, string path) { }
    }
    public sealed class SectorLight
    {
        public SectorLight() { }
        public required uint ArtId { get; init; }
        public required byte B { get; init; }
        public required ArcNET.Formats.SectorLightFlags Flags { get; init; }
        public required byte G { get; init; }
        public required long ObjHandle { get; init; }
        public required int OffsetX { get; init; }
        public required int OffsetY { get; init; }
        public required int Padding2C { get; init; }
        public required int Palette { get; init; }
        public required byte R { get; init; }
        public required long TileLoc { get; init; }
        public int TileX { get; }
        public int TileY { get; }
        public required uint TintColor { get; init; }
    }
    [System.Flags]
    public enum SectorLightFlags : uint
    {
        None = 0u,
        Off = 1u,
        Dark = 2u,
        Animating = 4u,
        Indoor = 8u,
        Outdoor = 16u,
    }
    public sealed class SectorSoundList
    {
        public SectorSoundList() { }
        public required int AmbientSchemeIdx { get; init; }
        public required uint Flags { get; init; }
        public required int MusicSchemeIdx { get; init; }
        public static ArcNET.Formats.SectorSoundList Default { get; }
    }
    public sealed class TerrainData
    {
        public TerrainData() { }
        public required ArcNET.Formats.TerrainType BaseTerrainType { get; init; }
        public required bool Compressed { get; init; }
        public required long Height { get; init; }
        public required ushort[] Tiles { get; init; }
        public required float Version { get; init; }
        public required long Width { get; init; }
    }
    public sealed class TerrainFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.TerrainData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.TerrainData>
    {
        public TerrainFormat() { }
        public static ArcNET.Formats.TerrainData Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.TerrainData ParseFile(string path) { }
        public static ArcNET.Formats.TerrainData ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.TerrainData value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.TerrainData value) { }
        public static void WriteToFile(in ArcNET.Formats.TerrainData value, string path) { }
    }
    public enum TerrainType : byte
    {
        Grasslands = 0,
        Desert = 1,
        Swamp = 2,
        Forest = 3,
        Mountain = 4,
        Tundra = 5,
        Wasteland = 6,
        DeepForest = 7,
        Urban = 8,
        Cavern = 9,
        Underground = 10,
        Void = 11,
        ScorchedEarth = 12,
        Plains = 13,
        ShallowWater = 14,
        DeepWater = 15,
        Farmland = 16,
        Beach = 17,
        Jungle = 18,
    }
    public readonly struct TextDataEntry : System.IEquatable<ArcNET.Formats.TextDataEntry>
    {
        public TextDataEntry(string Key, string Value) { }
        public string Key { get; init; }
        public string Value { get; init; }
    }
    public sealed class TextDataFile
    {
        public TextDataFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TextDataEntry> Entries { get; init; }
    }
    public sealed class TextDataFormat : ArcNET.Formats.IFormatReader<ArcNET.Formats.TextDataFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.TextDataFile>
    {
        public TextDataFormat() { }
        public static ArcNET.Formats.TextDataFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.TextDataFile ParseFile(string path) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TextDataEntry> ParseLines(System.Collections.Generic.IEnumerable<string> lines) { }
        public static ArcNET.Formats.TextDataFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.TextDataFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.TextDataFile value) { }
        public static void WriteToFile(in ArcNET.Formats.TextDataFile value, string path) { }
    }
    public static class TfafFormat
    {
        public static byte[] Extract(ArcNET.Formats.SaveIndex index, System.ReadOnlyMemory<byte> tfafData, string virtualPath) { }
        public static System.Collections.Generic.IReadOnlyDictionary<string, byte[]> ExtractAll(ArcNET.Formats.SaveIndex index, System.ReadOnlyMemory<byte> tfafData) { }
        public static byte[] Pack(ArcNET.Formats.SaveIndex index, System.Collections.Generic.IReadOnlyDictionary<string, byte[]> payloads) { }
        public static int TotalPayloadSize(ArcNET.Formats.SaveIndex index) { }
    }
    public sealed class TfaiDirectoryEntry : ArcNET.Formats.TfaiEntry
    {
        public TfaiDirectoryEntry() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TfaiEntry> Children { get; init; }
    }
    public abstract class TfaiEntry
    {
        protected TfaiEntry() { }
        public required string Name { get; init; }
    }
    public enum TfaiEntryType : byte
    {
        File = 0,
        Directory = 1,
        EndOfDirectory = 2,
        EndOfFile = 3,
    }
    public sealed class TfaiFileEntry : ArcNET.Formats.TfaiEntry
    {
        public TfaiFileEntry() { }
        public required int Size { get; init; }
    }
    public sealed class TileScript
    {
        public TileScript() { }
        public required uint NodeFlags { get; init; }
        public required uint ScriptCounters { get; init; }
        public required uint ScriptFlags { get; init; }
        public required int ScriptNum { get; init; }
        public required uint TileId { get; init; }
    }
}```

## ArcNET.GameObjects

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Formats")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameData")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameObjects.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.GameObjects.Classes
{
    public enum BasicStatType : byte
    {
        Gender = 0,
        Race = 1,
        Strength = 2,
        Dexterity = 3,
        Constitution = 4,
        Beauty = 5,
        Intelligence = 6,
        Willpower = 7,
        Charisma = 8,
        Perception = 9,
        TechPoints = 10,
        MagickPoints = 11,
    }
    public enum DamageType : byte
    {
        Normal = 0,
        Fatigue = 1,
        Poison = 2,
        Electrical = 3,
        Fire = 4,
    }
    public class Entity
    {
        public Entity() { }
        public int AIPacket { get; set; }
        public int Alignment { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "ArtNumber",
                "Palette"})]
        public System.ValueTuple<int, int> ArtNumberAndPalette { get; set; }
        public int AutoLevelScheme { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Stat",
                "Value"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.BasicStatType, int>> BasicStats { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFBlitFlags> BlitFlags { get; set; }
        public int Category { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFCritterFlags> CritterFlags { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFCritterFlags2> CritterFlags2 { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Type",
                "Min",
                "Max"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.DamageType, int, int>> Damages { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Id",
                "Text"})]
        public System.ValueTuple<int, string> Description { get; set; }
        public int Faction { get; set; }
        public int Fatigue { get; set; }
        public int HitChart { get; set; }
        public int HitPoints { get; set; }
        public int InternalName { get; set; }
        public int InventorySource { get; set; }
        public int Level { get; set; }
        public int Material { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFNpcFlags> NpcFlags { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFFlags> ObjectFlags { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Type",
                "Value"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.ResistanceType, int>> Resistances { get; set; }
        public int Scale { get; set; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "A",
                "B",
                "C",
                "D",
                "E",
                "F"})]
        public System.Collections.Generic.List<System.ValueTuple<int, int, int, int, int, int>> Scripts { get; set; }
        public int SoundBank { get; set; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFSpellFlags> SpellFlags { get; set; }
        public System.Collections.Generic.List<string> Spells { get; set; }
    }
    public sealed class InventorySource
    {
        public InventorySource() { }
        public System.Collections.Generic.List<ArcNET.GameObjects.Classes.InventorySourceEntry> Entries { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public sealed class InventorySourceBuy
    {
        public InventorySourceBuy() { }
        public System.Collections.Generic.List<ArcNET.GameObjects.Classes.InventorySourceBuyEntry> Entries { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public sealed class InventorySourceBuyEntry : System.IEquatable<ArcNET.GameObjects.Classes.InventorySourceBuyEntry>
    {
        public InventorySourceBuyEntry(int PrototypeId) { }
        public int PrototypeId { get; init; }
    }
    public sealed class InventorySourceEntry : System.IEquatable<ArcNET.GameObjects.Classes.InventorySourceEntry>
    {
        public InventorySourceEntry(int PrototypeId, double DropChance) { }
        public double DropChance { get; init; }
        public int PrototypeId { get; init; }
    }
    public sealed class Monster : ArcNET.GameObjects.Classes.Entity
    {
        public Monster() { }
    }
    public enum ResistanceType : byte
    {
        Damage = 0,
        Fire = 1,
        Electrical = 2,
        Poison = 3,
        Magic = 4,
    }
    public sealed class Unique : ArcNET.GameObjects.Classes.Entity
    {
        public Unique() { }
    }
}
namespace ArcNET.GameObjects
{
    public sealed class GameObject : ArcNET.GameObjects.IGameObject
    {
        public GameObject() { }
        public required ArcNET.GameObjects.Types.ObjectCommon Common { get; init; }
        public required ArcNET.GameObjects.GameObjectHeader Header { get; init; }
        public bool IsPrototype { get; }
        public ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; }
        public ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; }
        public ArcNET.GameObjects.ObjectType Type { get; }
        public static ArcNET.GameObjects.GameObject Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public sealed class GameObjectHeader
    {
        public GameObjectHeader() { }
        public required System.Collections.BitArray Bitmap { get; init; }
        public required ArcNET.GameObjects.ObjectType GameObjectType { get; init; }
        public bool IsPrototype { get; }
        public required ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; init; }
        public short PropCollectionItems { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; init; }
        public required int Version { get; init; }
    }
    public sealed class GameObjectScript : ArcNET.Core.IBinarySerializable<ArcNET.GameObjects.GameObjectScript, ArcNET.Core.SpanReader>
    {
        public GameObjectScript() { }
        public required byte[] Counters { get; init; }
        public required int Flags { get; init; }
        public bool IsEmpty { get; }
        public required int ScriptId { get; init; }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.GameObjects.GameObjectScript Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public sealed class GameObjectStore
    {
        public GameObjectStore() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> Headers { get; }
        public void Add(ArcNET.GameObjects.GameObjectHeader header) { }
        public void Clear() { }
    }
    public interface IGameObject
    {
        ArcNET.GameObjects.Types.ObjectCommon Common { get; }
        ArcNET.GameObjects.GameObjectHeader Header { get; }
        bool IsPrototype { get; }
        ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; }
        ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; }
        ArcNET.GameObjects.ObjectType Type { get; }
    }
    [System.Flags]
    public enum ObjFArmorFlags : uint
    {
        None = 0u,
        SizeSmall = 1u,
        SizeMedium = 2u,
        SizeLarge = 4u,
        MaleOnly = 8u,
        FemaleOnly = 16u,
    }
    [System.Flags]
    public enum ObjFBlitFlags : uint
    {
        None = 0u,
        BlendAdd = 1u,
    }
    [System.Flags]
    public enum ObjFContainerFlags : uint
    {
        None = 0u,
        Locked = 1u,
        Jammed = 2u,
        MagicallyHeld = 4u,
        NeverLocked = 8u,
        AlwaysLocked = 16u,
        LockedDay = 32u,
        LockedNight = 64u,
        Busted = 128u,
        Sticky = 256u,
        InvenSpawnOnce = 512u,
        InvenSpawnIndependent = 1024u,
    }
    [System.Flags]
    public enum ObjFCritterFlags : uint
    {
        None = 0u,
        IsConcealed = 1u,
        MovingSilently = 2u,
        Undead = 4u,
        Animal = 8u,
        Fleeing = 16u,
        Stunned = 32u,
        Paralyzed = 64u,
        Blinded = 128u,
        CrippledArmsOne = 256u,
        CrippledArmsBoth = 512u,
        CrippledLegsBoth = 1024u,
        Unused = 2048u,
        Sleeping = 4096u,
        Mute = 8192u,
        Surrendered = 16384u,
        Monster = 32768u,
        SpellFlee = 65536u,
        Encounter = 131072u,
        CombatModeActive = 262144u,
        LightSmall = 524288u,
        LightMedium = 1048576u,
        LightLarge = 2097152u,
        LightXLarge = 4194304u,
        Unrevivifiable = 8388608u,
        Unressurectable = 16777216u,
        Demon = 33554432u,
        FatigueImmune = 67108864u,
        NoFlee = 134217728u,
        NonLethalCombat = 268435456u,
        Mechanical = 536870912u,
        AnimalEnshroud = 1073741824u,
        FatigueLimiting = 2147483648u,
    }
    [System.Flags]
    public enum ObjFCritterFlags2 : uint
    {
        None = 0u,
        ItemStolen = 1u,
        AutoAnimates = 2u,
        UsingBoomerang = 4u,
        FatigueDraining = 8u,
        SlowParty = 16u,
        CombatToggleFx = 32u,
        NoDecay = 64u,
        NoPickpocket = 128u,
        NoBloodSplotches = 256u,
        NighInvulnerable = 512u,
        Elemental = 1024u,
        DarkSight = 2048u,
        NoSlip = 4096u,
        NoDisintegrate = 8192u,
        Reaction0 = 16384u,
        Reaction1 = 32768u,
        Reaction2 = 65536u,
        Reaction3 = 131072u,
        Reaction4 = 262144u,
        Reaction5 = 524288u,
        Reaction6 = 1048576u,
        TargetLock = 2097152u,
        PermaPolymorph = 4194304u,
        SafeOff = 8388608u,
        CheckReactionBad = 16777216u,
        CheckAlignGood = 33554432u,
        CheckAlignBad = 67108864u,
    }
    [System.Flags]
    public enum ObjFFlags : uint
    {
        None = 0u,
        Destroyed = 1u,
        Off = 2u,
        Flat = 4u,
        Text = 8u,
        SeeThrough = 16u,
        ShootThrough = 32u,
        Translucent = 64u,
        Shrunk = 128u,
        DontDraw = 256u,
        Invisible = 512u,
        NoBlock = 1024u,
        ClickThrough = 2048u,
        Inventory = 4096u,
        Dynamic = 8192u,
        ProvidesCover = 16384u,
        HasOverlays = 32768u,
        HasUnderlays = 65536u,
        Wading = 131072u,
        WaterWalking = 262144u,
        Stoned = 524288u,
        DontLight = 1048576u,
        TextFloater = 2097152u,
        Invulnerable = 4194304u,
        Extinct = 8388608u,
        TrapPc = 16777216u,
        TrapSpotted = 33554432u,
        DisallowWading = 67108864u,
        MultiplayerLock = 134217728u,
        Frozen = 268435456u,
        AnimatedDead = 536870912u,
        Teleported = 1073741824u,
    }
    [System.Flags]
    public enum ObjFItemFlags : uint
    {
        None = 0u,
        Identified = 1u,
        WontSell = 2u,
        IsMagical = 4u,
        TransferLight = 8u,
        NoDisplay = 16u,
        NoDrop = 32u,
        Hexed = 64u,
        CanUseBox = 128u,
        NeedsTarget = 256u,
        LightSmall = 512u,
        LightMedium = 1024u,
        LightLarge = 2048u,
        LightXLarge = 4096u,
        Persistent = 8192u,
        MtTriggered = 16384u,
        Stolen = 32768u,
        UseIsThrow = 65536u,
        NoDecay = 131072u,
        Uber = 262144u,
        NoNpcPickup = 524288u,
        NoRangedUse = 1048576u,
        ValidAiAction = 2097152u,
        MpInserted = 4194304u,
    }
    [System.Flags]
    public enum ObjFNpcFlags : uint
    {
        None = 0u,
        Fighting = 1u,
        WaypointsDay = 2u,
        WaypointsNight = 4u,
        AiWaitHere = 8u,
        AiSpreadOut = 16u,
        Jilted = 32u,
        CheckWield = 64u,
        CheckWeapon = 128u,
        Kos = 256u,
        WaypointsBed = 512u,
        ForcedFollower = 1024u,
        KosOverride = 2048u,
        Wanders = 4096u,
        WandersInDark = 8192u,
        Fence = 16384u,
        Familiar = 32768u,
        CheckLeader = 65536u,
        Aloof = 131072u,
        CastHighest = 262144u,
        Generator = 524288u,
        Generated = 1048576u,
        GeneratorRate1 = 2097152u,
        GeneratorRate2 = 4194304u,
        GeneratorRate3 = 8388608u,
        DemaintainSpells = 16777216u,
        LookForWeapon = 33554432u,
        LookForArmor = 67108864u,
        LookForAmmo = 134217728u,
        BackingOff = 268435456u,
        NoAttack = 536870912u,
    }
    [System.Flags]
    public enum ObjFPortalFlags : uint
    {
        None = 0u,
        Locked = 1u,
        Jammed = 2u,
        MagicallyHeld = 4u,
        NeverLocked = 8u,
        AlwaysLocked = 16u,
        LockedDay = 32u,
        LockedNight = 64u,
        Busted = 128u,
        Sticky = 256u,
    }
    [System.Flags]
    public enum ObjFSceneryFlags : uint
    {
        None = 0u,
        NoAutoAnimate = 1u,
        Busted = 2u,
        Nocturnal = 4u,
        MarksTownmap = 8u,
        IsFire = 16u,
        Respawnable = 32u,
        SoundSmall = 64u,
        SoundMedium = 128u,
        SoundExtraLarge = 256u,
        UnderAll = 512u,
        Respawning = 1024u,
    }
    [System.Flags]
    public enum ObjFSpellFlags : uint
    {
        None = 0u,
        Invisible = 1u,
        Floating = 2u,
        BodyOfAir = 4u,
        BodyOfEarth = 8u,
        BodyOfFire = 16u,
        BodyOfWater = 32u,
        DetectingMagic = 64u,
        DetectingAlignment = 128u,
        DetectingTraps = 256u,
        DetectingInvisible = 512u,
        Shielded = 1024u,
        AntiMagicShell = 2048u,
        BondsOfMagic = 4096u,
        FullReflection = 8192u,
        Summoned = 16384u,
        Illusion = 32768u,
        Stoned = 65536u,
        Polymorphed = 131072u,
        Mirrored = 262144u,
        Shrunk = 524288u,
        Passwalled = 1048576u,
        WaterWalking = 2097152u,
        MagneticInversion = 4194304u,
        Charmed = 8388608u,
        Entangled = 16777216u,
        SpokenWithDead = 33554432u,
        TempusFugit = 67108864u,
        MindControlled = 134217728u,
        Drunk = 268435456u,
        Enshrouded = 536870912u,
        Familiar = 1073741824u,
        HardenedHands = 2147483648u,
    }
    [System.Flags]
    public enum ObjFWeaponFlags : uint
    {
        None = 0u,
        Loud = 1u,
        Silent = 2u,
        TwoHanded = 4u,
        HandCountFixed = 8u,
        Throwable = 16u,
        TransProjectile = 32u,
        Boomerangs = 64u,
        IgnoreResistance = 128u,
        DamageArmor = 256u,
        DefaultThrows = 512u,
    }
    public enum ObjectField : byte
    {
        ObjFCurrentAid = 0,
        ObjFLocation = 1,
        ObjFOffsetX = 2,
        ObjFOffsetY = 3,
        ObjFShadow = 4,
        ObjFOverlayFore = 5,
        ObjFOverlayBack = 6,
        ObjFUnderlay = 7,
        ObjFBlitFlags = 8,
        ObjFBlitColor = 9,
        ObjFBlitAlpha = 10,
        ObjFBlitScale = 11,
        ObjFLightFlags = 12,
        ObjFLightAid = 13,
        ObjFLightColor = 14,
        ObjFOverlayLightFlags = 15,
        ObjFOverlayLightAid = 16,
        ObjFOverlayLightColor = 17,
        ObjFFlags = 18,
        ObjFSpellFlags = 19,
        ObjFBlockingMask = 20,
        ObjFName = 21,
        ObjFDescription = 22,
        ObjFAid = 23,
        ObjFDestroyedAid = 24,
        ObjFAc = 25,
        ObjFHpPts = 26,
        ObjFHpAdj = 27,
        ObjFHpDamage = 28,
        ObjFMaterial = 29,
        ObjFResistanceIdx = 30,
        ObjFScriptsIdx = 31,
        ObjFSoundEffect = 32,
        ObjFCategory = 33,
        ObjFPadIas1 = 34,
        ObjFPadI64As1 = 35,
        ObjFSpeedRun = 36,
        ObjFSpeedWalk = 37,
        ObjFPadFloat1 = 38,
        ObjFRadius = 39,
        ObjFHeight = 40,
        ObjFWallFlags = 64,
        ObjFWallPadI1 = 65,
        ObjFWallPadI2 = 66,
        ObjFWallPadIas1 = 67,
        ObjFWallPadI64As1 = 68,
        ObjFPortalFlags = 64,
        ObjFPortalLockDifficulty = 65,
        ObjFPortalKeyId = 66,
        ObjFPortalNotifyNpc = 67,
        ObjFPortalPadI1 = 68,
        ObjFPortalPadI2 = 69,
        ObjFPortalPadIas1 = 70,
        ObjFPortalPadI64As1 = 71,
        ObjFContainerFlags = 64,
        ObjFContainerLockDifficulty = 65,
        ObjFContainerKeyId = 66,
        ObjFContainerInventoryNum = 67,
        ObjFContainerInventoryListIdx = 68,
        ObjFContainerInventorySource = 69,
        ObjFContainerNotifyNpc = 70,
        ObjFContainerPadI1 = 71,
        ObjFContainerPadI2 = 72,
        ObjFContainerPadIas1 = 73,
        ObjFContainerPadI64As1 = 74,
        ObjFSceneryFlags = 64,
        ObjFSceneryWhosInMe = 65,
        ObjFSceneryRespawnDelay = 66,
        ObjFSceneryPadI2 = 67,
        ObjFSceneryPadIas1 = 68,
        ObjFSceneryPadI64As1 = 69,
        ObjFProjectileFlagsCombat = 64,
        ObjFProjectileFlagsCombatDamage = 65,
        ObjFProjectileHitLoc = 66,
        ObjFProjectileParentWeapon = 67,
        ObjFProjectilePadI1 = 68,
        ObjFProjectilePadI2 = 69,
        ObjFProjectilePadIas1 = 70,
        ObjFProjectilePadI64As1 = 71,
        ObjFTrapFlags = 64,
        ObjFTrapDifficulty = 65,
        ObjFTrapPadI2 = 66,
        ObjFTrapPadIas1 = 67,
        ObjFTrapPadI64As1 = 68,
        ObjFItemFlags = 64,
        ObjFItemParent = 65,
        ObjFItemWeight = 66,
        ObjFItemMagicWeightAdj = 67,
        ObjFItemWorth = 68,
        ObjFItemManaStore = 69,
        ObjFItemInvAid = 70,
        ObjFItemInvLocation = 71,
        ObjFItemUseAidFragment = 72,
        ObjFItemMagicTechComplexity = 73,
        ObjFItemDiscipline = 74,
        ObjFItemDescriptionUnknown = 75,
        ObjFItemDescriptionEffects = 76,
        ObjFItemSpell1 = 77,
        ObjFItemSpell2 = 78,
        ObjFItemSpell3 = 79,
        ObjFItemSpell4 = 80,
        ObjFItemSpell5 = 81,
        ObjFItemSpellManaStore = 82,
        ObjFItemAiAction = 83,
        ObjFItemPadI1 = 84,
        ObjFItemPadIas1 = 85,
        ObjFItemPadI64As1 = 86,
        ObjFWeaponFlags = 96,
        ObjFWeaponPaperDollAid = 97,
        ObjFWeaponBonusToHit = 98,
        ObjFWeaponMagicHitAdj = 99,
        ObjFWeaponDamageLowerIdx = 100,
        ObjFWeaponDamageUpperIdx = 101,
        ObjFWeaponMagicDamageAdjIdx = 102,
        ObjFWeaponSpeedFactor = 103,
        ObjFWeaponMagicSpeedAdj = 104,
        ObjFWeaponRange = 105,
        ObjFWeaponMagicRangeAdj = 106,
        ObjFWeaponMinStrength = 107,
        ObjFWeaponMagicMinStrengthAdj = 108,
        ObjFWeaponAmmoType = 109,
        ObjFWeaponAmmoConsumption = 110,
        ObjFWeaponMissileAid = 111,
        ObjFWeaponVisualEffectAid = 112,
        ObjFWeaponCritHitChart = 113,
        ObjFWeaponMagicCritHitChance = 114,
        ObjFWeaponMagicCritHitEffect = 115,
        ObjFWeaponCritMissChart = 116,
        ObjFWeaponMagicCritMissChance = 117,
        ObjFWeaponMagicCritMissEffect = 118,
        ObjFWeaponPadI1 = 119,
        ObjFWeaponPadI2 = 120,
        ObjFWeaponPadIas1 = 121,
        ObjFWeaponPadI64As1 = 122,
        ObjFAmmoFlags = 96,
        ObjFAmmoQuantity = 97,
        ObjFAmmoType = 98,
        ObjFAmmoPadI1 = 99,
        ObjFAmmoPadI2 = 100,
        ObjFAmmoPadIas1 = 101,
        ObjFAmmoPadI64As1 = 102,
        ObjFArmorFlags = 96,
        ObjFArmorPaperDollAid = 97,
        ObjFArmorAcAdj = 98,
        ObjFArmorMagicAcAdj = 99,
        ObjFArmorResistanceAdjIdx = 100,
        ObjFArmorMagicResistanceAdjIdx = 101,
        ObjFArmorSilentMoveAdj = 102,
        ObjFArmorMagicSilentMoveAdj = 103,
        ObjFArmorUnarmedBonusDamage = 104,
        ObjFArmorPadI2 = 105,
        ObjFArmorPadIas1 = 106,
        ObjFArmorPadI64As1 = 107,
        ObjFGoldFlags = 96,
        ObjFGoldQuantity = 97,
        ObjFGoldPadI1 = 98,
        ObjFGoldPadI2 = 99,
        ObjFGoldPadIas1 = 100,
        ObjFGoldPadI64As1 = 101,
        ObjFFoodFlags = 96,
        ObjFFoodPadI1 = 97,
        ObjFFoodPadI2 = 98,
        ObjFFoodPadIas1 = 99,
        ObjFFoodPadI64As1 = 100,
        ObjFScrollFlags = 96,
        ObjFScrollPadI1 = 97,
        ObjFScrollPadI2 = 98,
        ObjFScrollPadIas1 = 99,
        ObjFScrollPadI64As1 = 100,
        ObjFKeyKeyId = 96,
        ObjFKeyPadI1 = 97,
        ObjFKeyPadI2 = 98,
        ObjFKeyPadIas1 = 99,
        ObjFKeyPadI64As1 = 100,
        ObjFKeyRingFlags = 96,
        ObjFKeyRingListIdx = 97,
        ObjFKeyRingPadI1 = 98,
        ObjFKeyRingPadI2 = 99,
        ObjFKeyRingPadIas1 = 100,
        ObjFKeyRingPadI64As1 = 101,
        ObjFWrittenFlags = 96,
        ObjFWrittenSubtype = 97,
        ObjFWrittenTextStartLine = 98,
        ObjFWrittenTextEndLine = 99,
        ObjFWrittenPadI1 = 100,
        ObjFWrittenPadI2 = 101,
        ObjFWrittenPadIas1 = 102,
        ObjFWrittenPadI64As1 = 103,
        ObjFGenericFlags = 96,
        ObjFGenericUsageBonus = 97,
        ObjFGenericUsageCountRemaining = 98,
        ObjFGenericPadIas1 = 99,
        ObjFGenericPadI64As1 = 100,
        ObjFCritterFlags = 64,
        ObjFCritterFlags2 = 65,
        ObjFCritterStatBaseIdx = 66,
        ObjFCritterBasicSkillIdx = 67,
        ObjFCritterTechSkillIdx = 68,
        ObjFCritterSpellTechIdx = 69,
        ObjFCritterFatiguePts = 70,
        ObjFCritterFatigueAdj = 71,
        ObjFCritterFatigueDamage = 72,
        ObjFCritterCritHitChart = 73,
        ObjFCritterEffectsIdx = 74,
        ObjFCritterEffectCauseIdx = 75,
        ObjFCritterFleeingFrom = 76,
        ObjFCritterPortrait = 77,
        ObjFCritterGold = 78,
        ObjFCritterArrows = 79,
        ObjFCritterBullets = 80,
        ObjFCritterPowerCells = 81,
        ObjFCritterFuel = 82,
        ObjFCritterInventoryNum = 83,
        ObjFCritterInventoryListIdx = 84,
        ObjFCritterInventorySource = 85,
        ObjFCritterDescriptionUnknown = 86,
        ObjFCritterFollowerIdx = 87,
        ObjFCritterTeleportDest = 88,
        ObjFCritterTeleportMap = 89,
        ObjFCritterDeathTime = 90,
        ObjFCritterAutoLevelScheme = 91,
        ObjFCritterPadI1 = 92,
        ObjFCritterPadI2 = 93,
        ObjFCritterPadI3 = 94,
        ObjFCritterPadIas1 = 95,
        ObjFCritterPadI64As1 = 96,
        ObjFPcFlags = 128,
        ObjFPcFlagsFate = 129,
        ObjFPcReputationIdx = 130,
        ObjFPcReputationTsIdx = 131,
        ObjFPcBackground = 132,
        ObjFPcBackgroundText = 133,
        ObjFPcQuestIdx = 134,
        ObjFPcBlessingIdx = 135,
        ObjFPcBlessingTsIdx = 136,
        ObjFPcCurseIdx = 137,
        ObjFPcCurseTsIdx = 138,
        ObjFPcPartyId = 139,
        ObjFPcRumorIdx = 140,
        ObjFPcPadIas2 = 141,
        ObjFPcSchematicsFoundIdx = 142,
        ObjFPcLogbookEgoIdx = 143,
        ObjFPcFogMask = 144,
        ObjFPcPlayerName = 145,
        ObjFPcBankMoney = 146,
        ObjFPcGlobalFlags = 147,
        ObjFPcGlobalVariables = 148,
        ObjFPcPadI1 = 149,
        ObjFPcPadI2 = 150,
        ObjFPcPadIas1 = 151,
        ObjFPcPadI64As1 = 152,
        ObjFNpcFlags = 128,
        ObjFNpcLeader = 129,
        ObjFNpcAiData = 130,
        ObjFNpcCombatFocus = 131,
        ObjFNpcWhoHitMeLast = 132,
        ObjFNpcExperienceWorth = 133,
        ObjFNpcExperiencePool = 134,
        ObjFNpcWaypointsIdx = 135,
        ObjFNpcWaypointCurrent = 136,
        ObjFNpcStandpointDay = 137,
        ObjFNpcStandpointNight = 138,
        ObjFNpcOrigin = 139,
        ObjFNpcFaction = 140,
        ObjFNpcRetailPriceMultiplier = 141,
        ObjFNpcSubstituteInventory = 142,
        ObjFNpcReactionBase = 143,
        ObjFNpcSocialClass = 144,
        ObjFNpcReactionPcIdx = 145,
        ObjFNpcReactionLevelIdx = 146,
        ObjFNpcReactionTimeIdx = 147,
        ObjFNpcWait = 148,
        ObjFNpcGeneratorData = 149,
        ObjFNpcPadI1 = 150,
        ObjFNpcDamageIdx = 151,
        ObjFNpcShitListIdx = 152,
    }
    public enum ObjectType : byte
    {
        Wall = 0,
        Portal = 1,
        Container = 2,
        Scenery = 3,
        Projectile = 4,
        Weapon = 5,
        Ammo = 6,
        Armor = 7,
        Gold = 8,
        Food = 9,
        Scroll = 10,
        Key = 11,
        KeyRing = 12,
        Written = 13,
        Generic = 14,
        Pc = 15,
        Npc = 16,
        Trap = 17,
    }
}
namespace ArcNET.GameObjects.Types
{
    public sealed class ObjectAmmo : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectAmmo() { }
        public int AmmoFlags { get; set; }
        public int AmmoPadI1 { get; set; }
        public int AmmoPadI2 { get; set; }
        public long AmmoPadI64As1 { get; set; }
        public int AmmoPadIas1 { get; set; }
        public int AmmoQuantity { get; set; }
        public int AmmoType { get; set; }
    }
    public sealed class ObjectArmor : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectArmor() { }
        public int ArmorAcAdj { get; set; }
        public int ArmorFlags { get; set; }
        public int ArmorMagicAcAdj { get; set; }
        public int[] ArmorMagicResistanceAdj { get; set; }
        public int ArmorMagicSilentMoveAdj { get; set; }
        public int ArmorPadI2 { get; set; }
        public long ArmorPadI64As1 { get; set; }
        public int ArmorPadIas1 { get; set; }
        public int ArmorPaperDollAid { get; set; }
        public int[] ArmorResistanceAdj { get; set; }
        public int ArmorSilentMoveAdj { get; set; }
        public int ArmorUnarmedBonusDamage { get; set; }
    }
    public class ObjectCommon
    {
        public ObjectCommon() { }
        public int Ac { get; set; }
        public ArcNET.Core.Primitives.ArtId Aid { get; set; }
        public int BlitAlpha { get; set; }
        public ArcNET.Core.Primitives.Color BlitColor { get; set; }
        public int BlitFlags { get; set; }
        public int BlitScale { get; set; }
        public int BlockingMask { get; set; }
        public int Category { get; set; }
        public ArcNET.Core.Primitives.ArtId CurrentAid { get; set; }
        public int Description { get; set; }
        public ArcNET.Core.Primitives.ArtId DestroyedAid { get; set; }
        public int Flags { get; set; }
        public int HpAdj { get; set; }
        public int HpDamage { get; set; }
        public int HpPts { get; set; }
        public ArcNET.Core.Primitives.ArtId LightAid { get; set; }
        public ArcNET.Core.Primitives.Color LightColor { get; set; }
        public int LightFlags { get; set; }
        public ArcNET.Core.Primitives.Location? Location { get; set; }
        public int Material { get; set; }
        public int Name { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public int[] OverlayBack { get; set; }
        public int[] OverlayFore { get; set; }
        public int[] OverlayLightAid { get; set; }
        public int OverlayLightColor { get; set; }
        public int OverlayLightFlags { get; set; }
        public long PadI64As1 { get; set; }
        public int PadIas1 { get; set; }
        public int[] ResistanceIdx { get; set; }
        public ArcNET.GameObjects.GameObjectScript[] ScriptsIdx { get; set; }
        public ArcNET.Core.Primitives.ArtId Shadow { get; set; }
        public int SoundEffect { get; set; }
        public int SpellFlags { get; set; }
        public int[] Underlay { get; set; }
        protected void ReadCommonFields(ref ArcNET.Core.SpanReader reader, System.Collections.BitArray bitmap, bool isPrototype) { }
        protected void WriteCommonFields(ref ArcNET.Core.SpanWriter writer, System.Collections.BitArray bitmap, bool isPrototype) { }
    }
    public sealed class ObjectContainer : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectContainer() { }
        public int ContainerFlags { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid[] ContainerInventoryList { get; set; }
        public int ContainerInventoryNum { get; set; }
        public int ContainerInventorySource { get; set; }
        public int ContainerKeyId { get; set; }
        public int ContainerLockDifficulty { get; set; }
        public int ContainerNotifyNpc { get; set; }
        public int ContainerPadI1 { get; set; }
        public int ContainerPadI2 { get; set; }
        public long ContainerPadI64As1 { get; set; }
        public int ContainerPadIas1 { get; set; }
    }
    public class ObjectCritter : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectCritter() { }
        public ArcNET.Core.Primitives.GameObjectGuid CritterArrows { get; set; }
        public int CritterAutoLevelScheme { get; set; }
        public int[] CritterBasicSkill { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterBullets { get; set; }
        public int CritterCritHitChart { get; set; }
        public int CritterDeathTime { get; set; }
        public int CritterDescriptionUnknown { get; set; }
        public int[] CritterEffectCause { get; set; }
        public int[] CritterEffects { get; set; }
        public int CritterFatigueAdj { get; set; }
        public int CritterFatigueDamage { get; set; }
        public int CritterFatiguePts { get; set; }
        public int CritterFlags { get; set; }
        public int CritterFlags2 { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterFleeingFrom { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterFollowers { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterFuel { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterGold { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterInventoryList { get; set; }
        public int CritterInventoryNum { get; set; }
        public int CritterInventorySource { get; set; }
        public int CritterPadI1 { get; set; }
        public int CritterPadI2 { get; set; }
        public int CritterPadI3 { get; set; }
        public long CritterPadI64As1 { get; set; }
        public int CritterPadIas1 { get; set; }
        public int CritterPortrait { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterPowerCells { get; set; }
        public int[] CritterSpellTech { get; set; }
        public int[] CritterStatBase { get; set; }
        public int[] CritterTechSkill { get; set; }
        public ArcNET.Core.Primitives.Location CritterTeleportDest { get; set; }
        public int CritterTeleportMap { get; set; }
        protected void ReadCritterFields(ref ArcNET.Core.SpanReader reader, System.Collections.BitArray bitmap, bool isPrototype) { }
        protected void WriteCritterFields(ref ArcNET.Core.SpanWriter writer, System.Collections.BitArray bitmap, bool isPrototype) { }
    }
    public sealed class ObjectFood : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectFood() { }
        public int FoodFlags { get; set; }
        public int FoodPadI1 { get; set; }
        public int FoodPadI2 { get; set; }
        public long FoodPadI64As1 { get; set; }
        public int FoodPadIas1 { get; set; }
    }
    public sealed class ObjectGeneric : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectGeneric() { }
        public int GenericFlags { get; set; }
        public long GenericPadI64As1 { get; set; }
        public int GenericPadIas1 { get; set; }
        public int GenericUsageBonus { get; set; }
        public int GenericUsageCountRemaining { get; set; }
    }
    public sealed class ObjectGold : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectGold() { }
        public int GoldFlags { get; set; }
        public int GoldPadI1 { get; set; }
        public int GoldPadI2 { get; set; }
        public long GoldPadI64As1 { get; set; }
        public int GoldPadIas1 { get; set; }
        public int GoldQuantity { get; set; }
    }
    public class ObjectItem : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectItem() { }
        public int ItemAiAction { get; set; }
        public int ItemDescriptionEffects { get; set; }
        public int ItemDescriptionUnknown { get; set; }
        public int ItemDiscipline { get; set; }
        public int ItemFlags { get; set; }
        public int ItemInvAid { get; set; }
        public int ItemInvLocation { get; set; }
        public int ItemMagicTechComplexity { get; set; }
        public int ItemMagicWeightAdj { get; set; }
        public int ItemManaStore { get; set; }
        public int ItemPadI1 { get; set; }
        public long ItemPadI64As1 { get; set; }
        public int ItemPadIas1 { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid ItemParent { get; set; }
        public int ItemSpell1 { get; set; }
        public int ItemSpell2 { get; set; }
        public int ItemSpell3 { get; set; }
        public int ItemSpell4 { get; set; }
        public int ItemSpell5 { get; set; }
        public int ItemSpellManaStore { get; set; }
        public int ItemUseAidFragment { get; set; }
        public int ItemWeight { get; set; }
        public int ItemWorth { get; set; }
        protected void ReadItemFields(ref ArcNET.Core.SpanReader reader, System.Collections.BitArray bitmap, bool isPrototype) { }
        protected void WriteItemFields(ref ArcNET.Core.SpanWriter writer, System.Collections.BitArray bitmap, bool isPrototype) { }
    }
    public sealed class ObjectKey : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectKey() { }
        public int KeyKeyId { get; set; }
        public int KeyPadI1 { get; set; }
        public int KeyPadI2 { get; set; }
        public long KeyPadI64As1 { get; set; }
        public int KeyPadIas1 { get; set; }
    }
    public sealed class ObjectKeyRing : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectKeyRing() { }
        public int KeyRingFlags { get; set; }
        public int[] KeyRingList { get; set; }
        public int KeyRingPadI1 { get; set; }
        public int KeyRingPadI2 { get; set; }
        public long KeyRingPadI64As1 { get; set; }
        public int KeyRingPadIas1 { get; set; }
    }
    public sealed class ObjectNpc : ArcNET.GameObjects.Types.ObjectCritter
    {
        public ObjectNpc() { }
        public int NpcAiData { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid NpcCombatFocus { get; set; }
        public int[] NpcDamage { get; set; }
        public int NpcExperiencePool { get; set; }
        public int NpcExperienceWorth { get; set; }
        public int NpcFaction { get; set; }
        public int NpcFlags { get; set; }
        public int NpcGeneratorData { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid NpcLeader { get; set; }
        public int NpcOrigin { get; set; }
        public int NpcPadI1 { get; set; }
        public int NpcReactionBase { get; set; }
        public int[] NpcReactionLevel { get; set; }
        public int[] NpcReactionPc { get; set; }
        public int[] NpcReactionTime { get; set; }
        public int NpcRetailPriceMultiplier { get; set; }
        public int[] NpcShitList { get; set; }
        public int NpcSocialClass { get; set; }
        public ArcNET.Core.Primitives.Location NpcStandpointDay { get; set; }
        public ArcNET.Core.Primitives.Location NpcStandpointNight { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid NpcSubstituteInventory { get; set; }
        public int NpcWait { get; set; }
        public int NpcWaypointCurrent { get; set; }
        public ArcNET.Core.Primitives.Location[] NpcWaypoints { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid NpcWhoHitMeLast { get; set; }
    }
    public sealed class ObjectPc : ArcNET.GameObjects.Types.ObjectCritter
    {
        public ObjectPc() { }
        public int PcBackground { get; set; }
        public int PcBackgroundText { get; set; }
        public int PcBankMoney { get; set; }
        public int[] PcBlessing { get; set; }
        public int[] PcBlessingTs { get; set; }
        public int[] PcCurse { get; set; }
        public int[] PcCurseTs { get; set; }
        public int PcFlags { get; set; }
        public int PcFlagsFate { get; set; }
        public int PcFogMask { get; set; }
        public int[] PcGlobalFlags { get; set; }
        public int[] PcGlobalVariables { get; set; }
        public int[] PcLogbookEgo { get; set; }
        public int PcPadI1 { get; set; }
        public int PcPadI2 { get; set; }
        public long PcPadI64As1 { get; set; }
        public int PcPadIas1 { get; set; }
        public int PcPadIas2 { get; set; }
        public int PcPartyId { get; set; }
        public ArcNET.Core.Primitives.PrefixedString PcPlayerName { get; set; }
        public int[] PcQuest { get; set; }
        public int[] PcReputation { get; set; }
        public int[] PcReputationTs { get; set; }
        public int[] PcRumor { get; set; }
        public int[] PcSchematicsFound { get; set; }
    }
    public sealed class ObjectPortal : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectPortal() { }
        public int PortalFlags { get; set; }
        public int PortalKeyId { get; set; }
        public int PortalLockDifficulty { get; set; }
        public int PortalNotifyNpc { get; set; }
        public int PortalPadI1 { get; set; }
        public int PortalPadI2 { get; set; }
        public long PortalPadI64As1 { get; set; }
        public int PortalPadIas1 { get; set; }
    }
    public sealed class ObjectProjectile : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectProjectile() { }
        public int ProjectileFlagsCombat { get; set; }
        public int ProjectileFlagsCombatDamage { get; set; }
        public ArcNET.Core.Primitives.Location ProjectileHitLoc { get; set; }
        public int ProjectilePadI1 { get; set; }
        public int ProjectilePadI2 { get; set; }
        public long ProjectilePadI64As1 { get; set; }
        public int ProjectilePadIas1 { get; set; }
        public int ProjectileParentWeapon { get; set; }
    }
    public sealed class ObjectScenery : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectScenery() { }
        public int SceneryFlags { get; set; }
        public int SceneryPadI2 { get; set; }
        public long SceneryPadI64As1 { get; set; }
        public int SceneryPadIas1 { get; set; }
        public int SceneryRespawnDelay { get; set; }
        public ArcNET.Core.Primitives.GameObjectGuid SceneryWhosInMe { get; set; }
    }
    public sealed class ObjectScroll : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectScroll() { }
        public int ScrollFlags { get; set; }
        public int ScrollPadI1 { get; set; }
        public int ScrollPadI2 { get; set; }
        public long ScrollPadI64As1 { get; set; }
        public int ScrollPadIas1 { get; set; }
    }
    public sealed class ObjectTrap : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectTrap() { }
        public int TrapDifficulty { get; set; }
        public int TrapFlags { get; set; }
        public int TrapPadI2 { get; set; }
        public long TrapPadI64As1 { get; set; }
        public int TrapPadIas1 { get; set; }
    }
    public sealed class ObjectUnknown : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectUnknown() { }
    }
    public sealed class ObjectWall : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectWall() { }
        public int WallFlags { get; set; }
        public int WallPadI1 { get; set; }
        public int WallPadI2 { get; set; }
        public long WallPadI64As1 { get; set; }
        public int WallPadIas1 { get; set; }
    }
    public sealed class ObjectWeapon : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectWeapon() { }
        public int WeaponAmmoConsumption { get; set; }
        public int WeaponAmmoType { get; set; }
        public int WeaponBonusToHit { get; set; }
        public int WeaponCritHitChart { get; set; }
        public int WeaponCritMissChart { get; set; }
        public int[] WeaponDamageLower { get; set; }
        public int[] WeaponDamageUpper { get; set; }
        public int WeaponFlags { get; set; }
        public int WeaponMagicCritHitChance { get; set; }
        public int WeaponMagicCritHitEffect { get; set; }
        public int WeaponMagicCritMissChance { get; set; }
        public int WeaponMagicCritMissEffect { get; set; }
        public int[] WeaponMagicDamageAdj { get; set; }
        public int WeaponMagicHitAdj { get; set; }
        public int WeaponMagicMinStrengthAdj { get; set; }
        public int WeaponMagicRangeAdj { get; set; }
        public int WeaponMagicSpeedAdj { get; set; }
        public int WeaponMinStrength { get; set; }
        public int WeaponMissileAid { get; set; }
        public int WeaponPadI1 { get; set; }
        public int WeaponPadI2 { get; set; }
        public long WeaponPadI64As1 { get; set; }
        public int WeaponPadIas1 { get; set; }
        public int WeaponPaperDollAid { get; set; }
        public int WeaponRange { get; set; }
        public int WeaponSpeedFactor { get; set; }
        public int WeaponVisualEffectAid { get; set; }
    }
    public sealed class ObjectWritten : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectWritten() { }
        public int WrittenFlags { get; set; }
        public int WrittenPadI1 { get; set; }
        public int WrittenPadI2 { get; set; }
        public long WrittenPadI64As1 { get; set; }
        public int WrittenPadIas1 { get; set; }
        public int WrittenSubtype { get; set; }
        public int WrittenTextEndLine { get; set; }
        public int WrittenTextStartLine { get; set; }
    }
}```

## ArcNET.GameData

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.App")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameData.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.GameData
{
    public sealed class GameDataExportDto : System.IEquatable<ArcNET.GameData.GameDataExportDto>
    {
        public GameDataExportDto(System.Collections.Generic.IReadOnlyList<ArcNET.GameData.GameObjectHeaderDto> Objects, System.Collections.Generic.IReadOnlyList<ArcNET.GameData.MessageEntryDto> Messages, System.Collections.Generic.IReadOnlyList<ArcNET.GameData.SectorDto> Sectors, System.Collections.Generic.IReadOnlyList<ArcNET.GameData.ProtoDto> Protos, System.Collections.Generic.IReadOnlyList<ArcNET.GameData.MobDto> Mobs) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.MessageEntryDto> Messages { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.MobDto> Mobs { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.GameObjectHeaderDto> Objects { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.ProtoDto> Protos { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.SectorDto> Sectors { get; init; }
    }
    public static class GameDataExporter
    {
        public static string ExportToJson(ArcNET.GameData.GameDataStore store) { }
        public static System.Threading.Tasks.Task ExportToJsonFileAsync(ArcNET.GameData.GameDataStore store, string outputPath, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public static class GameDataLoader
    {
        public static System.Collections.Generic.IReadOnlyDictionary<ArcNET.Formats.FileFormat, System.Collections.Generic.IReadOnlyList<string>> DiscoverFiles(string dirPath) { }
        public static System.Threading.Tasks.Task<ArcNET.GameData.GameDataStore> LoadFromDirectoryAsync(string dirPath, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default) { }
        public static System.Threading.Tasks.Task<ArcNET.GameData.GameDataStore> LoadFromMemoryAsync(System.Collections.Generic.IReadOnlyDictionary<string, System.ReadOnlyMemory<byte>> files, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default) { }
        public static System.Collections.Generic.IReadOnlyDictionary<int, string> LoadMessages(string dirPath) { }
    }
    public sealed class GameDataSaver
    {
        public GameDataSaver() { }
        public static void SaveMessagesToFile(ArcNET.GameData.GameDataStore store, string outputPath) { }
        public static byte[] SaveMessagesToMemory(ArcNET.GameData.GameDataStore store) { }
        public static void SaveMobsToDirectory(ArcNET.GameData.GameDataStore store, string outputDir) { }
        public static void SaveProtosToDirectory(ArcNET.GameData.GameDataStore store, string outputDir) { }
        public static void SaveSectorsToDirectory(ArcNET.GameData.GameDataStore store, string outputDir) { }
        public static System.Threading.Tasks.Task SaveToDirectoryAsync(ArcNET.GameData.GameDataStore store, string outputDir, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default) { }
        public static System.Collections.Generic.IReadOnlyDictionary<string, byte[]> SaveToMemory(ArcNET.GameData.GameDataStore store) { }
    }
    public sealed class GameDataStore
    {
        public GameDataStore() { }
        public System.Collections.Generic.IReadOnlySet<ArcNET.Core.Primitives.GameObjectGuid> DirtyObjects { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry> Messages { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry>> MessagesBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> Mobs { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData>> MobsBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> Objects { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ProtoData> Protos { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ProtoData>> ProtosBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Sector> Sectors { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Sector>> SectorsBySource { get; }
        public event System.EventHandler<ArcNET.Core.Primitives.GameObjectGuid>? ObjectChanged;
        public void AddMessage(ArcNET.Formats.MessageEntry entry) { }
        public void AddMob(ArcNET.Formats.MobData mob) { }
        public void AddObject(ArcNET.GameObjects.GameObjectHeader header) { }
        public void AddProto(ArcNET.Formats.ProtoData proto) { }
        public void AddSector(ArcNET.Formats.Sector sector) { }
        public void Clear() { }
        public void ClearDirty() { }
        public ArcNET.GameObjects.GameObjectHeader? FindByGuid(in ArcNET.Core.Primitives.GameObjectGuid id) { }
        public void MarkDirty(in ArcNET.Core.Primitives.GameObjectGuid id) { }
    }
    public sealed class GameObjectHeaderDto : System.IEquatable<ArcNET.GameData.GameObjectHeaderDto>
    {
        public GameObjectHeaderDto(int Version, string Type, string ObjectId, string ProtoId, bool IsPrototype) { }
        public bool IsPrototype { get; init; }
        public string ObjectId { get; init; }
        public string ProtoId { get; init; }
        public string Type { get; init; }
        public int Version { get; init; }
    }
    public sealed class MessageEntryDto : System.IEquatable<ArcNET.GameData.MessageEntryDto>
    {
        public MessageEntryDto(int Index, string? SoundId, string Text) { }
        public int Index { get; init; }
        public string? SoundId { get; init; }
        public string Text { get; init; }
    }
    public sealed class MobDto : System.IEquatable<ArcNET.GameData.MobDto>
    {
        public MobDto(int Version, string Type, string ObjectId, string ProtoId, int PropertyCount) { }
        public string ObjectId { get; init; }
        public int PropertyCount { get; init; }
        public string ProtoId { get; init; }
        public string Type { get; init; }
        public int Version { get; init; }
    }
    public sealed class ProtoDto : System.IEquatable<ArcNET.GameData.ProtoDto>
    {
        public ProtoDto(int Version, string Type, string ObjectId, string ProtoId, int PropertyCount) { }
        public string ObjectId { get; init; }
        public int PropertyCount { get; init; }
        public string ProtoId { get; init; }
        public string Type { get; init; }
        public int Version { get; init; }
    }
    public sealed class SectorDto : System.IEquatable<ArcNET.GameData.SectorDto>
    {
        public SectorDto(int LightCount, int TileCount, bool HasRoofs, int TileScriptCount, int ObjectCount) { }
        public bool HasRoofs { get; init; }
        public int LightCount { get; init; }
        public int ObjectCount { get; init; }
        public int TileCount { get; init; }
        public int TileScriptCount { get; init; }
    }
}```

## ArcNET.Patch

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.App")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Patch.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Patch
{
    public sealed class GitHubRelease
    {
        public GitHubRelease() { }
        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string Body { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; init; }
    }
    public static class GitHubReleaseClient
    {
        public static System.Threading.Tasks.Task DownloadFileAsync(string url, string destinationPath, System.Threading.CancellationToken cancellationToken = default) { }
        public static System.Threading.Tasks.Task<ArcNET.Patch.GitHubRelease?> GetLatestHighResPatchReleaseAsync(System.Threading.CancellationToken cancellationToken = default) { }
    }
    public sealed class HighResConfig
    {
        public HighResConfig() { }
        public int BitDepth { get; init; }
        public int Borders { get; init; }
        public int BroadcastLimit { get; init; }
        public int DDrawWrapper { get; init; }
        public int DialogFont { get; init; }
        public int DoubleBuffer { get; init; }
        public int DxWrapper { get; init; }
        public int Height { get; init; }
        public int Intro { get; init; }
        public int Language { get; init; }
        public int LogbookFont { get; init; }
        public int Logos { get; init; }
        public int MainMenuArt { get; init; }
        public int MenuPosition { get; init; }
        public int PreloadLimit { get; init; }
        public int Renderer { get; init; }
        public int ScrollDist { get; init; }
        public int ScrollFPS { get; init; }
        public int ShowFPS { get; init; }
        public int Width { get; init; }
        public int Windowed { get; init; }
        public void WriteFile(string iniPath) { }
        public static ArcNET.Patch.HighResConfig ParseFile(string iniPath) { }
    }
    public static class PatchInstaller
    {
        public static System.Threading.Tasks.Task InstallAsync(string gameDir, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public static class PatchUninstaller
    {
        public static bool IsPatchInstalled(string gameDir) { }
        public static System.Threading.Tasks.Task UninstallAsync(string gameDir, System.Threading.CancellationToken cancellationToken = default) { }
    }
}```

## ArcNET.BinaryPatch

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.App")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.BinaryPatch.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.BinaryPatch
{
    public sealed class BinaryPatchSet
    {
        public BinaryPatchSet() { }
        public required string Name { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.BinaryPatch.IBinaryPatch> Patches { get; init; }
        public required string Version { get; init; }
    }
    public static class BinaryPatcher
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.BinaryPatch.PatchResult> Apply(ArcNET.BinaryPatch.BinaryPatchSet patchSet, string gameDir, ArcNET.BinaryPatch.PatchOptions? options = null) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.BinaryPatch.PatchResult> Revert(ArcNET.BinaryPatch.BinaryPatchSet patchSet, string gameDir) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.BinaryPatch.PatchVerifyResult> Verify(ArcNET.BinaryPatch.BinaryPatchSet patchSet, string gameDir) { }
    }
    public interface IBinaryPatch
    {
        string Description { get; }
        string Id { get; }
        string PatchSummary { get; }
        ArcNET.BinaryPatch.PatchTarget Target { get; }
        byte[] Apply(System.ReadOnlyMemory<byte> original);
        bool NeedsApply(System.ReadOnlyMemory<byte> original);
    }
    public static class PatchDiscovery
    {
        public static string DefaultPatchesDir { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.BinaryPatch.BinaryPatchSet> LoadAll(string? patchesDir = null, System.Action<string, System.Exception>? onError = null) { }
    }
    public sealed class PatchOptions : System.IEquatable<ArcNET.BinaryPatch.PatchOptions>
    {
        public static readonly ArcNET.BinaryPatch.PatchOptions Default;
        public PatchOptions() { }
        public bool CreateBackup { get; init; }
        public bool DryRun { get; init; }
    }
    public sealed class PatchResult : System.IEquatable<ArcNET.BinaryPatch.PatchResult>
    {
        public PatchResult(string PatchId, ArcNET.BinaryPatch.PatchStatus Status, string? BackupPath, string? Reason) { }
        public string? BackupPath { get; init; }
        public string PatchId { get; init; }
        public string? Reason { get; init; }
        public ArcNET.BinaryPatch.PatchStatus Status { get; init; }
    }
    public enum PatchStatus : byte
    {
        Applied = 0,
        AlreadyApplied = 1,
        Skipped = 2,
        Failed = 3,
    }
    public sealed class PatchTarget : System.IEquatable<ArcNET.BinaryPatch.PatchTarget>
    {
        public PatchTarget(string RelativePath, ArcNET.BinaryPatch.PatchTargetFormat Format) { }
        public string? DatEntryPath { get; init; }
        public ArcNET.BinaryPatch.PatchTargetFormat Format { get; init; }
        public string RelativePath { get; init; }
        public string? SourceDatPath { get; init; }
    }
    public enum PatchTargetFormat : byte
    {
        Proto = 0,
        Mob = 1,
        Raw = 2,
    }
    public sealed class PatchVerifyResult : System.IEquatable<ArcNET.BinaryPatch.PatchVerifyResult>
    {
        public PatchVerifyResult(string PatchId, bool NeedsApply, bool FileExists, string? Reason) { }
        public bool FileExists { get; init; }
        public bool NeedsApply { get; init; }
        public string PatchId { get; init; }
        public string? Reason { get; init; }
    }
}
namespace ArcNET.BinaryPatch.Json
{
    public static class JsonPatchLoader
    {
        public static ArcNET.BinaryPatch.BinaryPatchSet Load(string jsonText) { }
        public static ArcNET.BinaryPatch.BinaryPatchSet LoadEmbedded(System.Reflection.Assembly assembly, string resourceName) { }
        public static ArcNET.BinaryPatch.BinaryPatchSet LoadFile(string path) { }
    }
}
namespace ArcNET.BinaryPatch.Patches
{
    public sealed class MobFieldPatch : ArcNET.BinaryPatch.IBinaryPatch
    {
        public string Description { get; }
        public string Id { get; }
        public string PatchSummary { get; }
        public ArcNET.BinaryPatch.PatchTarget Target { get; }
        public byte[] Apply(System.ReadOnlyMemory<byte> original) { }
        public bool NeedsApply(System.ReadOnlyMemory<byte> original) { }
        public static ArcNET.BinaryPatch.Patches.MobFieldPatch Custom(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, System.Func<ArcNET.Formats.ObjectProperty, bool>? needsApplyPredicate, System.Func<ArcNET.Formats.ObjectProperty, ArcNET.Formats.ObjectProperty> transform) { }
        public static ArcNET.BinaryPatch.Patches.MobFieldPatch SetInt32(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, int expectedValue, int newValue) { }
    }
    public sealed class ProtoFieldPatch : ArcNET.BinaryPatch.IBinaryPatch
    {
        public string Description { get; }
        public string Id { get; }
        public string PatchSummary { get; }
        public ArcNET.BinaryPatch.PatchTarget Target { get; }
        public byte[] Apply(System.ReadOnlyMemory<byte> original) { }
        public bool NeedsApply(System.ReadOnlyMemory<byte> original) { }
        public static ArcNET.BinaryPatch.Patches.ProtoFieldPatch Custom(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, System.Func<ArcNET.Formats.ObjectProperty, bool>? needsApplyPredicate, System.Func<ArcNET.Formats.ObjectProperty, ArcNET.Formats.ObjectProperty> transform) { }
        public static ArcNET.BinaryPatch.Patches.ProtoFieldPatch SetInt32(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, int expectedValue, int newValue) { }
    }
    public sealed class RawBinaryPatch : ArcNET.BinaryPatch.IBinaryPatch
    {
        public string Description { get; }
        public string Id { get; }
        public string PatchSummary { get; }
        public ArcNET.BinaryPatch.PatchTarget Target { get; }
        public byte[] Apply(System.ReadOnlyMemory<byte> original) { }
        public bool NeedsApply(System.ReadOnlyMemory<byte> original) { }
        public static ArcNET.BinaryPatch.Patches.RawBinaryPatch AtOffset(string id, string description, string relativePath, int offset, byte[] expectedBytes, byte[] newBytes) { }
    }
}
namespace ArcNET.BinaryPatch.State
{
    public sealed class PatchState
    {
        public PatchState() { }
        public System.Collections.Generic.List<ArcNET.BinaryPatch.State.PatchStateEntry> Applied { get; set; }
    }
    public sealed class PatchStateEntry
    {
        public PatchStateEntry() { }
        public System.DateTimeOffset AppliedAt { get; set; }
        public string PatchSetName { get; set; }
        public string PatchSetVersion { get; set; }
    }
    public static class PatchStateStore
    {
        public static bool IsRecorded(string gameDir, ArcNET.BinaryPatch.BinaryPatchSet patchSet) { }
        public static ArcNET.BinaryPatch.State.PatchState Load(string gameDir) { }
        public static ArcNET.BinaryPatch.State.PatchState RecordApply(string gameDir, ArcNET.BinaryPatch.BinaryPatchSet patchSet) { }
        public static ArcNET.BinaryPatch.State.PatchState RecordRevert(string gameDir, ArcNET.BinaryPatch.BinaryPatchSet patchSet) { }
    }
}```

## ArcNET.Dumpers

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Dumpers
{
    public static class ArtDumper
    {
        public static string Dump(ArcNET.Formats.ArtFile art) { }
        public static void Dump(ArcNET.Formats.ArtFile art, System.IO.TextWriter writer) { }
    }
    public static class DialogDumper
    {
        public static string Dump(ArcNET.Formats.DlgFile dlg) { }
        public static void Dump(ArcNET.Formats.DlgFile dlg, System.IO.TextWriter writer) { }
    }
    public static class FacWalkDumper
    {
        public static string Dump(ArcNET.Formats.FacadeWalk fac) { }
        public static void Dump(ArcNET.Formats.FacadeWalk fac, System.IO.TextWriter writer) { }
    }
    public static class ItemDumper
    {
        public static string DumpContainerItems(ArcNET.Archive.DatArchive archive, string containerMobPath, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup) { }
        public static string DumpContainerItems(ArcNET.Formats.MobData container, ArcNET.Archive.DatArchive archiveForItems, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup) { }
        public static void DumpContainerItems(ArcNET.Archive.DatArchive archive, string containerMobPath, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, System.IO.TextWriter writer) { }
        public static void DumpContainerItems(ArcNET.Formats.MobData container, ArcNET.Archive.DatArchive archiveForItems, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, System.IO.TextWriter writer) { }
        public static string DumpItem(ArcNET.Formats.MobData mob, int protoId, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation) { }
        public static void DumpItem(ArcNET.Formats.MobData mob, int protoId, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation, System.IO.TextWriter writer) { }
        public static string? DumpProtoById(string gameDir, int protoId, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation) { }
        public static string? DumpProtoByName(string gameDir, string itemName, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation) { }
        public static System.Collections.Generic.Dictionary<int, string> LoadProtoNameLookup(string gameDir) { }
        public static string ResolveItemName(ArcNET.Formats.MobData mob, int protoId, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation) { }
    }
    public static class JmpDumper
    {
        public static string Dump(ArcNET.Formats.JmpFile jmp) { }
        public static void Dump(ArcNET.Formats.JmpFile jmp, System.IO.TextWriter writer) { }
    }
    public static class MapPropertiesDumper
    {
        public static string Dump(ArcNET.Formats.MapProperties props) { }
        public static void Dump(ArcNET.Formats.MapProperties props, System.IO.TextWriter writer) { }
    }
    public static class MessageDumper
    {
        public static string Dump(ArcNET.Formats.MesFile mes) { }
        public static void Dump(ArcNET.Formats.MesFile mes, System.IO.TextWriter writer) { }
    }
    public static class MobDumper
    {
        public static string Dump(ArcNET.Formats.MobData mob) { }
        public static void Dump(ArcNET.Formats.MobData mob, System.IO.TextWriter writer) { }
    }
    public static class ProtoDumper
    {
        public static string Dump(ArcNET.Formats.ProtoData proto) { }
        public static void Dump(ArcNET.Formats.ProtoData proto, System.IO.TextWriter writer) { }
    }
    public static class SaveIndexDumper
    {
        public static string Dump(ArcNET.Formats.SaveIndex index) { }
        public static void Dump(ArcNET.Formats.SaveIndex index, System.IO.TextWriter writer) { }
    }
    public static class SaveInfoDumper
    {
        public static string Dump(ArcNET.Formats.SaveInfo info) { }
        public static void Dump(ArcNET.Formats.SaveInfo info, System.IO.TextWriter writer) { }
    }
    public static class ScriptDumper
    {
        public static string Dump(ArcNET.Formats.ScrFile scr) { }
        public static void Dump(ArcNET.Formats.ScrFile scr, System.IO.TextWriter writer) { }
    }
    public static class SectorDumper
    {
        public static string Dump(ArcNET.Formats.Sector sector) { }
        public static void Dump(ArcNET.Formats.Sector sector, System.IO.TextWriter writer) { }
    }
    public static class TerrainDumper
    {
        public static string Dump(ArcNET.Formats.TerrainData terrain) { }
        public static void Dump(ArcNET.Formats.TerrainData terrain, System.IO.TextWriter writer) { }
    }
    public static class TextDataDumper
    {
        public static string Dump(ArcNET.Formats.TextDataFile file) { }
        public static void Dump(ArcNET.Formats.TextDataFile file, System.IO.TextWriter writer) { }
    }
}```
