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
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Diagnostics")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Formats")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameData")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.GameObjects")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Probe")]
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
        public System.ReadOnlySpan<byte> RemainingSpan { get; }
        public int PeekInt32At(int offset) { }
        public System.ReadOnlySpan<byte> PeekSpan(int count) { }
        public uint PeekUInt32At(int offset) { }
        public byte ReadByte() { }
        public System.ReadOnlySpan<byte> ReadBytes(int count) { }
        public double ReadDouble() { }
        public short ReadInt16() { }
        public int ReadInt32() { }
        public void ReadInt32Array(System.Span<int> dest) { }
        public long ReadInt64() { }
        public float ReadSingle() { }
        public ushort ReadUInt16() { }
        public void ReadUInt16Array(System.Span<ushort> dest) { }
        public uint ReadUInt32() { }
        public void ReadUInt32Array(System.Span<uint> dest) { }
        public ulong ReadUInt64() { }
        public void ReadUnmanaged<T>(System.Span<T> dest)
            where T :  unmanaged { }
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
        public void WriteBytes([System.Runtime.CompilerServices.ScopedRef] System.ReadOnlySpan<byte> data) { }
        public void WriteDouble(double v) { }
        public void WriteInt16(short v) { }
        public void WriteInt32(int v) { }
        public void WriteInt64(long v) { }
        public void WriteSingle(float v) { }
        public void WriteUInt16(ushort v) { }
        public void WriteUInt32(uint v) { }
        public void WriteUInt64(ulong v) { }
        public void WriteUnmanaged<T>(System.ReadOnlySpan<T> data)
            where T :  unmanaged { }
    }
    public static class SpanWriterExtensions
    {
        public static void WriteArray<T>(ref this ArcNET.Core.SpanWriter writer, System.Collections.Generic.IReadOnlyList<T> items, ArcNET.Core.WriteElement<T> writeOne) { }
        public static void WriteArtId(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.ArtId value) { }
        public static void WriteGameObjectGuid(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.GameObjectGuid value) { }
        public static void WriteLocation(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.Location value) { }
        public static void WritePrefixedString(ref this ArcNET.Core.SpanWriter writer, in ArcNET.Core.Primitives.PrefixedString value) { }
    }
    public static class StackAllocPolicy
    {
        public const int MaxStackAllocBytes = 256;
    }
    public static class VirtualPath
    {
        public static string Normalize(string path) { }
    }
    public delegate void WriteElement<T>(ref ArcNET.Core.SpanWriter writer, T item);
}
namespace ArcNET.Core.Primitives
{
    public readonly struct ArtId : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.ArtId, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.ArtId>, System.IFormattable, System.ISpanFormattable, System.IUtf8SpanFormattable
    {
        public ArtId(uint Value) { }
        public int ArtNum { get; }
        public int FacadeNumber { get; }
        public int FrameIndex { get; }
        public bool IsEyeCandyTranslucent { get; }
        public bool IsRoofFaded { get; }
        public bool IsRoofFill { get; }
        public bool IsRoofMirrored { get; }
        public int PaletteIndex { get; }
        public int RoofPieceIndex { get; }
        public int TileType { get; }
        public ArcNET.Core.Primitives.ArtId.TypeCode Type { get; }
        public uint Value { get; init; }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<byte> utf8Dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public ArcNET.Core.Primitives.ArtId WithFrameIndex(int frameIndex) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.ArtId Read(ref ArcNET.Core.SpanReader reader) { }
        public enum TypeCode : uint
        {
            Tile = 0u,
            None = 0u,
            Wall = 1u,
            Critter = 2u,
            Portal = 3u,
            Scenery = 4u,
            Interface = 5u,
            Item = 6u,
            Container = 7u,
            Misc = 8u,
            Light = 9u,
            Roof = 10u,
            Facade = 11u,
            Monster = 12u,
            UniqueNpc = 13u,
            EyeCandy = 14u,
        }
    }
    public readonly struct Color : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.Color, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.Color>, System.IFormattable, System.ISpanFormattable
    {
        public Color(byte R, byte G, byte B) { }
        public byte B { get; init; }
        public byte G { get; init; }
        public byte R { get; init; }
        public int ToPackedRgb() { }
        public override string ToString() { }
        public string ToString(string? format, System.IFormatProvider? provider) { }
        public bool TryFormat(System.Span<char> dest, out int written, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { }
        public void Write(ref ArcNET.Core.SpanWriter writer) { }
        public static ArcNET.Core.Primitives.Color FromPackedRgb(int packedColor) { }
        public static ArcNET.Core.Primitives.Color Read(ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Core.Primitives.Color ReadRgba(ref ArcNET.Core.SpanReader reader) { }
    }
    public readonly struct GameObjectGuid : ArcNET.Core.IBinarySerializable<ArcNET.Core.Primitives.GameObjectGuid, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.Core.Primitives.GameObjectGuid>, System.IFormattable, System.ISpanFormattable
    {
        public const short OidTypeA = 1;
        public const short OidTypeBlocked = -1;
        public const short OidTypeGuid = 2;
        public const short OidTypeHandle = -2;
        public const short OidTypeNull = 0;
        public const short OidTypeP = 3;
        public GameObjectGuid(short OidType, short Padding2, int Padding4, System.Guid Id) { }
        public System.Guid Id { get; init; }
        public bool IsProto { get; }
        public short OidType { get; init; }
        public short Padding2 { get; init; }
        public int Padding4 { get; init; }
        public int? GetProtoNumber() { }
        public string ToLabel() { }
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
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Editor")]
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
        public bool IsMetadataOnly { get; init; }
        public required uint[] PaletteData1 { get; init; }
        public required uint[] PaletteData2 { get; init; }
        public required int[] PaletteIds { get; init; }
        public required ArcNET.Formats.ArtPaletteEntry[]?[] Palettes { get; init; }
    }
    [System.Flags]
    public enum ArtFlags : uint
    {
        None = 0u,
        Static = 1u,
        Critter = 2u,
        Font = 4u,
    }
    public sealed class ArtFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.ArtFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.ArtFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.ArtFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ArtFile>
    {
        public ArtFormat() { }
        public static ArcNET.Formats.ArtFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ArtFile ParseFile(string path) { }
        public static ArcNET.Formats.ArtFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static ArcNET.Formats.ArtFile ParseMetadata([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ArtFile ParseMetadataMemory(System.ReadOnlyMemory<byte> memory) { }
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
        public ArtPaletteEntry(byte Blue, byte Green, byte Red, byte Alpha = 255) { }
        public byte Alpha { get; init; }
        public byte Blue { get; init; }
        public byte Green { get; init; }
        public byte Red { get; init; }
    }
    public static class BlockMaskExtensions
    {
        public static int CountBlocked(this System.ReadOnlySpan<uint> blockMask) { }
        public static int CountBlocked(this uint[] blockMask) { }
        public static bool IsBlocked(this System.ReadOnlySpan<uint> blockMask, int tileIndex) { }
        public static bool IsBlocked(this uint[] blockMask, int tileIndex) { }
        public static bool IsBlocked(this System.ReadOnlySpan<uint> blockMask, int tileX, int tileY) { }
        public static bool IsBlocked(this uint[] blockMask, int tileX, int tileY) { }
        public static void SetBlocked(this System.Span<uint> blockMask, int tileIndex, bool blocked) { }
        public static void SetBlocked(this uint[] blockMask, int tileIndex, bool blocked) { }
        public static void SetBlocked(this System.Span<uint> blockMask, int tileX, int tileY, bool blocked) { }
        public static void SetBlocked(this uint[] blockMask, int tileX, int tileY, bool blocked) { }
    }
    public sealed class CharacterMdyRecord : System.IEquatable<ArcNET.Formats.CharacterMdyRecord>
    {
        public CharacterMdyRecord() { }
        public int Arrows { get; }
        public required int[] BasicSkills { get; init; }
        public int BlessingProtoElementCount { get; init; }
        public int[]? BlessingRaw { get; }
        public byte[]? BlessingTsRaw { get; }
        public int Bullets { get; }
        public int CurseProtoElementCount { get; init; }
        public int[]? CurseRaw { get; }
        public byte[]? CurseTsRaw { get; }
        public int[]? EffectCauses { get; }
        public int[]? Effects { get; }
        public int FatigueDamage { get; }
        public int[]? FatigueDamageRaw { get; }
        public int Gold { get; }
        public required bool HasCompleteData { get; init; }
        public int HpDamage { get; }
        public int[]? HpDamageRaw { get; }
        public int MaxFollowers { get; }
        public string? Name { get; }
        public int PortraitIndex { get; }
        public int[]? PositionAiRaw { get; }
        public int PowerCells { get; }
        public int[]? QuestActiveIds { get; }
        public int[]? QuestBitsetRaw { get; }
        public int QuestCount { get; init; }
        public byte[]? QuestDataRaw { get; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "ProtoId",
                "Context",
                "Timestamp",
                "State"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<int, int, int, int>>? QuestEntries { get; }
        public required byte[] RawBytes { get; init; }
        public int[]? ReputationFactionSlots { get; }
        public int[]? ReputationRaw { get; }
        public int RumorsCount { get; init; }
        public byte[]? RumorsRaw { get; }
        public int SchematicsElementCount { get; init; }
        public int[]? SchematicsRaw { get; }
        public required int[] SpellTech { get; init; }
        public required int[] Stats { get; init; }
        public required int[] TechSkills { get; init; }
        public int TotalKills { get; }
        public ArcNET.Formats.CharacterMdyRecord WithArrows(int arrows) { }
        public ArcNET.Formats.CharacterMdyRecord WithBasicSkills(int[] basicSkills) { }
        public ArcNET.Formats.CharacterMdyRecord WithBlessingRaw(System.ReadOnlySpan<int> blessingProtoIds) { }
        public ArcNET.Formats.CharacterMdyRecord WithBullets(int bullets) { }
        public ArcNET.Formats.CharacterMdyRecord WithCurseRaw(System.ReadOnlySpan<int> curseProtoIds) { }
        public ArcNET.Formats.CharacterMdyRecord WithFatigueDamage(int[] values) { }
        public ArcNET.Formats.CharacterMdyRecord WithFatigueDamageValue(int damage) { }
        public ArcNET.Formats.CharacterMdyRecord WithGold(int gold) { }
        public ArcNET.Formats.CharacterMdyRecord WithHpDamage(int[] values) { }
        public ArcNET.Formats.CharacterMdyRecord WithHpDamageValue(int damage) { }
        public ArcNET.Formats.CharacterMdyRecord WithMaxFollowers(int maxFollowers) { }
        public ArcNET.Formats.CharacterMdyRecord WithName(string? newName) { }
        public ArcNET.Formats.CharacterMdyRecord WithPortraitIndex(int portraitIndex) { }
        public ArcNET.Formats.CharacterMdyRecord WithPositionAi(int[] values) { }
        public ArcNET.Formats.CharacterMdyRecord WithPowerCells(int powerCells) { }
        public ArcNET.Formats.CharacterMdyRecord WithQuestDataRaw(System.ReadOnlySpan<byte> newData) { }
        public ArcNET.Formats.CharacterMdyRecord WithQuestStateRaw(System.ReadOnlySpan<byte> newData, System.ReadOnlySpan<int> newBitset) { }
        public ArcNET.Formats.CharacterMdyRecord WithReputationRaw(System.ReadOnlySpan<int> values) { }
        public ArcNET.Formats.CharacterMdyRecord WithRumorsRaw(System.ReadOnlySpan<byte> newData) { }
        public ArcNET.Formats.CharacterMdyRecord WithSchematicsRaw(System.ReadOnlySpan<int> schematicProtoIds) { }
        public ArcNET.Formats.CharacterMdyRecord WithSpellTech(int[] spellTech) { }
        public ArcNET.Formats.CharacterMdyRecord WithStats(int[] stats) { }
        public ArcNET.Formats.CharacterMdyRecord WithTechSkills(int[] techSkills) { }
        public ArcNET.Formats.CharacterMdyRecord WithTotalKills(int totalKills) { }
        public static ArcNET.Formats.CharacterMdyRecord Parse(System.ReadOnlySpan<byte> span, out int consumed) { }
    }
    public static class CharacterMdyRecordBuilder
    {
        public static ArcNET.Formats.CharacterMdyRecord Create(int[] stats, int[] basicSkills, int[] techSkills, int[] spellTech, int gold = 0, string name = "Hero", int portraitIndex = 0, int maxFollowers = 5) { }
    }
    public sealed class Data2SavFile
    {
        public Data2SavFile() { }
        public int Header0 { get; }
        public int Header1 { get; }
        public int IdPairTableEndInt { get; }
        public required int IdPairTableStartInt { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Data2SavIdPairEntry> IdPairs { get; init; }
        public int PrefixIntCount { get; }
        public required byte[] RawBytes { get; init; }
        public int SuffixIntCount { get; }
        public int TotalInts { get; }
        public int TrailingBytes { get; }
        public void CopyPrefixInts(int startIndex, System.Span<int> destination) { }
        public void CopySuffixInts(int startIndex, System.Span<int> destination) { }
        public int GetPrefixInt(int index) { }
        public int GetSuffixInt(int index) { }
        public ArcNET.Formats.Data2SavFile.Builder ToBuilder() { }
        public bool TryGetIdPairValue(int id, out int value) { }
        public ArcNET.Formats.Data2SavFile WithIdPairValue(int id, int value) { }
        public ArcNET.Formats.Data2SavFile WithPrefixInt(int index, int value) { }
        public ArcNET.Formats.Data2SavFile WithPrefixInts(int startIndex, System.ReadOnlySpan<int> values) { }
        public ArcNET.Formats.Data2SavFile WithSuffixInt(int index, int value) { }
        public ArcNET.Formats.Data2SavFile WithSuffixInts(int startIndex, System.ReadOnlySpan<int> values) { }
        public sealed class Builder
        {
            public Builder(ArcNET.Formats.Data2SavFile from) { }
            public int Header0 { get; }
            public int Header1 { get; }
            public int IdPairTableEndInt { get; }
            public int IdPairTableStartInt { get; }
            public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Data2SavIdPairEntry> IdPairs { get; }
            public int PrefixIntCount { get; }
            public int SuffixIntCount { get; }
            public int TotalInts { get; }
            public int TrailingBytes { get; }
            public ArcNET.Formats.Data2SavFile Build() { }
            public void CopyPrefixInts(int startIndex, System.Span<int> destination) { }
            public void CopySuffixInts(int startIndex, System.Span<int> destination) { }
            public int GetPrefixInt(int index) { }
            public int GetSuffixInt(int index) { }
            public bool TryGetIdPairValue(int id, out int value) { }
            public ArcNET.Formats.Data2SavFile.Builder WithIdPairValue(int id, int value) { }
            public ArcNET.Formats.Data2SavFile.Builder WithPrefixInt(int index, int value) { }
            public ArcNET.Formats.Data2SavFile.Builder WithPrefixInts(int startIndex, System.ReadOnlySpan<int> values) { }
            public ArcNET.Formats.Data2SavFile.Builder WithSuffixInt(int index, int value) { }
            public ArcNET.Formats.Data2SavFile.Builder WithSuffixInts(int startIndex, System.ReadOnlySpan<int> values) { }
        }
    }
    public sealed class Data2SavFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.Data2SavFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.Data2SavFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.Data2SavFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.Data2SavFile>
    {
        public Data2SavFormat() { }
        public static ArcNET.Formats.Data2SavFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.Data2SavFile ParseFile(string path) { }
        public static ArcNET.Formats.Data2SavFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.Data2SavFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.Data2SavFile value) { }
        public static void WriteToFile(in ArcNET.Formats.Data2SavFile value, string path) { }
    }
    public readonly struct Data2SavIdPairEntry : System.IEquatable<ArcNET.Formats.Data2SavIdPairEntry>
    {
        public Data2SavIdPairEntry(int Id, int Value) { }
        public int Id { get; init; }
        public int Value { get; init; }
    }
    public sealed class DataSavFile
    {
        public DataSavFile() { }
        public int Header0 { get; }
        public int Header1 { get; }
        public int QuadRowCount { get; }
        public required byte[] RawBytes { get; init; }
        public int RemainderIntCount { get; }
        public int TotalInts { get; }
        public int TrailingBytes { get; }
        public void CopyQuadRows(int startRowIndex, System.Span<ArcNET.Formats.DataSavQuadRow> destination) { }
        public void CopyRemainderInts(int startIndex, System.Span<int> destination) { }
        public ArcNET.Formats.DataSavQuadRow GetQuadRow(int rowIndex) { }
        public int GetRemainderInt(int index) { }
        public ArcNET.Formats.DataSavFile.Builder ToBuilder() { }
        public ArcNET.Formats.DataSavFile WithHeader(int header0, int header1) { }
        public ArcNET.Formats.DataSavFile WithQuadRow(int rowIndex, ArcNET.Formats.DataSavQuadRow row) { }
        public ArcNET.Formats.DataSavFile WithQuadRows(int startRowIndex, System.ReadOnlySpan<ArcNET.Formats.DataSavQuadRow> rows) { }
        public ArcNET.Formats.DataSavFile WithRemainderInt(int index, int value) { }
        public ArcNET.Formats.DataSavFile WithRemainderInts(int startIndex, System.ReadOnlySpan<int> values) { }
        public sealed class Builder
        {
            public Builder(ArcNET.Formats.DataSavFile from) { }
            public int Header0 { get; }
            public int Header1 { get; }
            public int QuadRowCount { get; }
            public int RemainderIntCount { get; }
            public int TotalInts { get; }
            public int TrailingBytes { get; }
            public ArcNET.Formats.DataSavFile Build() { }
            public void CopyQuadRows(int startRowIndex, System.Span<ArcNET.Formats.DataSavQuadRow> destination) { }
            public void CopyRemainderInts(int startIndex, System.Span<int> destination) { }
            public ArcNET.Formats.DataSavQuadRow GetQuadRow(int rowIndex) { }
            public int GetRemainderInt(int index) { }
            public ArcNET.Formats.DataSavFile.Builder WithHeader(int header0, int header1) { }
            public ArcNET.Formats.DataSavFile.Builder WithQuadRow(int rowIndex, ArcNET.Formats.DataSavQuadRow row) { }
            public ArcNET.Formats.DataSavFile.Builder WithQuadRows(int startRowIndex, System.ReadOnlySpan<ArcNET.Formats.DataSavQuadRow> rows) { }
            public ArcNET.Formats.DataSavFile.Builder WithRemainderInt(int index, int value) { }
            public ArcNET.Formats.DataSavFile.Builder WithRemainderInts(int startIndex, System.ReadOnlySpan<int> values) { }
        }
    }
    public sealed class DataSavFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.DataSavFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.DataSavFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.DataSavFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.DataSavFile>
    {
        public DataSavFormat() { }
        public static ArcNET.Formats.DataSavFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.DataSavFile ParseFile(string path) { }
        public static ArcNET.Formats.DataSavFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.DataSavFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.DataSavFile value) { }
        public static void WriteToFile(in ArcNET.Formats.DataSavFile value, string path) { }
    }
    public readonly struct DataSavQuadRow : System.IEquatable<ArcNET.Formats.DataSavQuadRow>
    {
        public DataSavQuadRow(int A, int B, int C, int D) { }
        public int A { get; init; }
        public int B { get; init; }
        public int C { get; init; }
        public int D { get; init; }
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
    public sealed class DialogFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.DlgFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.DlgFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.DlgFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.DlgFile>
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
    public sealed class FacWalkFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.FacadeWalk>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.FacadeWalk>, ArcNET.Formats.IFormatReader<ArcNET.Formats.FacadeWalk>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.FacadeWalk>
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
        TownMapFog = 16,
    }
    public static class FileFormatExtensions
    {
        public static ArcNET.Formats.FileFormat FromExtension(string extension) { }
        public static ArcNET.Formats.FileFormat FromPath(string path) { }
    }
    public static class FormatIo
    {
        public static T ParseFile<TFormat, T>(string path)
            where TFormat : ArcNET.Formats.IFormatFileReader<T> { }
        public static T ParseMemory<TFormat, T>(System.ReadOnlyMemory<byte> memory)
            where TFormat : ArcNET.Formats.IFormatReader<T> { }
        public static byte[] WriteToArray<TFormat, T>(in T value)
            where TFormat : ArcNET.Formats.IFormatWriter<T> { }
        public static void WriteToFile<TFormat, T>(in T value, string path)
            where TFormat : ArcNET.Formats.IFormatFileWriter<T> { }
    }
    public interface IFormatFileReader<T> : ArcNET.Formats.IFormatReader<T>
    {
        T ParseFile(string path);
    }
    public interface IFormatFileWriter<T> : ArcNET.Formats.IFormatWriter<T>
    {
        void WriteToFile(in T value, string path);
    }
    public interface IFormatReader<T>
    {
        T Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader);
        T ParseMemory(System.ReadOnlyMemory<byte> memory);
    }
    public interface IFormatWriter<T>
    {
        void Write(in T value, ref ArcNET.Core.SpanWriter writer);
        byte[] WriteToArray(in T value);
    }
    public sealed class JmpFile
    {
        public JmpFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.JumpEntry> Jumps { get; init; }
    }
    public sealed class JmpFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.JmpFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.JmpFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.JmpFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.JmpFile>
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
    public sealed class MapPropertiesFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.MapProperties>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.MapProperties>, ArcNET.Formats.IFormatReader<ArcNET.Formats.MapProperties>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MapProperties>
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
    public sealed class MessageFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.MesFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.MesFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.MesFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MesFile>
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
    public static class MobDataExtensions
    {
        public static ArcNET.Formats.ObjectProperty? GetProperty(this ArcNET.Formats.MobData mob, ArcNET.GameObjects.ObjectField field) { }
        public static ArcNET.Formats.ObjectProperty? GetProperty(this ArcNET.Formats.ProtoData proto, ArcNET.GameObjects.ObjectField field) { }
        public static ArcNET.Formats.MobData RebuildHeader(this ArcNET.Formats.MobData mob) { }
        public static ArcNET.Formats.ProtoData RebuildHeader(this ArcNET.Formats.ProtoData proto) { }
        public static ArcNET.GameObjects.GameObject ToGameObject(this ArcNET.Formats.MobData data) { }
        public static ArcNET.Formats.MobData ToMobData(this ArcNET.GameObjects.GameObject obj) { }
        public static ArcNET.Formats.MobData WithProperty(this ArcNET.Formats.MobData mob, ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Formats.ProtoData WithProperty(this ArcNET.Formats.ProtoData proto, ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Formats.MobData WithoutProperty(this ArcNET.Formats.MobData mob, ArcNET.GameObjects.ObjectField field) { }
        public static ArcNET.Formats.ProtoData WithoutProperty(this ArcNET.Formats.ProtoData proto, ArcNET.GameObjects.ObjectField field) { }
    }
    public sealed class MobFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.MobData>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.MobData>, ArcNET.Formats.IFormatReader<ArcNET.Formats.MobData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MobData>
    {
        public MobFormat() { }
        public static ArcNET.Formats.MobData Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MobData ParseFile(string path) { }
        public static ArcNET.Formats.MobData ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.MobData value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MobData value) { }
        public static void WriteToFile(in ArcNET.Formats.MobData value, string path) { }
    }
    public static class MobGoldResolver
    {
        public static System.Collections.Generic.IReadOnlyList<System.Guid> GetContainerInventoryObjectIds(ArcNET.Formats.MobData mob) { }
        public static ArcNET.Core.Primitives.GameObjectGuid? GetCritterGoldHandle(ArcNET.Formats.MobData mob) { }
        public static int? GetGoldQuantity(ArcNET.Formats.MobData mob) { }
        public static int? ResolveContainerGoldQuantity(ArcNET.Formats.MobData container, System.Func<ArcNET.Core.Primitives.GameObjectGuid, ArcNET.Formats.MobData?> mobResolver) { }
        public static int? ResolveCritterGoldQuantity(ArcNET.Formats.MobData critter, System.Func<ArcNET.Core.Primitives.GameObjectGuid, ArcNET.Formats.MobData?> mobResolver) { }
        public static int? ResolveGoldQuantity(ArcNET.Core.Primitives.GameObjectGuid goldHandle, System.Func<ArcNET.Core.Primitives.GameObjectGuid, ArcNET.Formats.MobData?> mobResolver) { }
    }
    public sealed class MobileMdFile
    {
        public MobileMdFile() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobileMdRecord> Records { get; init; }
    }
    public sealed class MobileMdFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.MobileMdFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.MobileMdFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.MobileMdFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MobileMdFile>
    {
        public MobileMdFormat() { }
        public static ArcNET.Formats.MobileMdFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MobileMdFile ParseFile(string path) { }
        public static ArcNET.Formats.MobileMdFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.MobileMdFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MobileMdFile value) { }
        public static void WriteToFile(in ArcNET.Formats.MobileMdFile value, string path) { }
    }
    public sealed class MobileMdRecord
    {
        public MobileMdRecord() { }
        public ArcNET.Formats.MobData? Data { get; init; }
        public bool IsCompact { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid MapObjectId { get; init; }
        public string? ParseNote { get; init; }
        public required byte[] RawMobBytes { get; init; }
        public byte[]? TailBytes { get; init; }
        public required int Version { get; init; }
    }
    public sealed class MobileMdyFile
    {
        public MobileMdyFile() { }
        public System.Collections.Generic.IEnumerable<ArcNET.Formats.CharacterMdyRecord> Characters { get; }
        public System.Collections.Generic.IEnumerable<ArcNET.Formats.MobData> Mobs { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobileMdyRecord> Records { get; init; }
    }
    public sealed class MobileMdyFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.MobileMdyFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.MobileMdyFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.MobileMdyFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.MobileMdyFile>
    {
        public MobileMdyFormat() { }
        public static ArcNET.Formats.MobileMdyFile Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.MobileMdyFile ParseFile(string path) { }
        public static ArcNET.Formats.MobileMdyFile ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.MobileMdyFile value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.MobileMdyFile value) { }
        public static void WriteToFile(in ArcNET.Formats.MobileMdyFile value, string path) { }
    }
    public sealed class MobileMdyRecord
    {
        public ArcNET.Formats.CharacterMdyRecord? Character { get; }
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, "Mob")]
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "Character")]
        [get: System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, "Mob")]
        [get: System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "Character")]
        public bool IsCharacter { get; }
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, "Character")]
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "Mob")]
        [get: System.Diagnostics.CodeAnalysis.MemberNotNullWhen(false, "Character")]
        [get: System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "Mob")]
        public bool IsMob { get; }
        public ArcNET.Formats.MobData? Mob { get; }
        public static ArcNET.Formats.MobileMdyRecord FromCharacter(ArcNET.Formats.CharacterMdyRecord character) { }
        public static ArcNET.Formats.MobileMdyRecord FromMob(ArcNET.Formats.MobData mob) { }
    }
    public sealed class ObjectProperty
    {
        public ObjectProperty() { }
        public required ArcNET.GameObjects.ObjectField Field { get; init; }
        public string? ParseNote { get; init; }
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
        public static ArcNET.Core.Primitives.GameObjectGuid GetObjectId(this ArcNET.Formats.ObjectProperty property) { }
        public static System.Guid[] GetObjectIdArray(this ArcNET.Formats.ObjectProperty property) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "OidType",
                "ProtoOrData1",
                "Id"})]
        public static System.ValueTuple<short, int, System.Guid>[] GetObjectIdArrayFull(this ArcNET.Formats.ObjectProperty property) { }
        public static ArcNET.Core.Primitives.Color GetPackedRgbColor(this ArcNET.Formats.ObjectProperty property) { }
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
        public static ArcNET.Formats.ObjectProperty WithObjectId(this ArcNET.Formats.ObjectProperty property, ArcNET.Core.Primitives.GameObjectGuid value) { }
        public static ArcNET.Formats.ObjectProperty WithObjectIdArray(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<System.Guid> ids) { }
        public static ArcNET.Formats.ObjectProperty WithObjectIdArrayFull(this ArcNET.Formats.ObjectProperty property, [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "OidType",
                "ProtoOrData1",
                "Id"})] System.ReadOnlySpan<System.ValueTuple<short, int, System.Guid>> ids) { }
        public static ArcNET.Formats.ObjectProperty WithPackedRgbColor(this ArcNET.Formats.ObjectProperty property, ArcNET.Core.Primitives.Color value) { }
        public static ArcNET.Formats.ObjectProperty WithScriptArray(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<ArcNET.Formats.ObjectPropertyScript> scripts) { }
        public static ArcNET.Formats.ObjectProperty WithString(this ArcNET.Formats.ObjectProperty property, string value) { }
        public static ArcNET.Formats.ObjectProperty WithUInt32Array(this ArcNET.Formats.ObjectProperty property, System.ReadOnlySpan<uint> values) { }
    }
    public static class ObjectPropertyFactory
    {
        public static ArcNET.Formats.ObjectProperty ForEmptyObjectIdArray(ArcNET.GameObjects.ObjectField field) { }
        public static ArcNET.Formats.ObjectProperty ForFloat(ArcNET.GameObjects.ObjectField field, float value) { }
        public static ArcNET.Formats.ObjectProperty ForInt32(ArcNET.GameObjects.ObjectField field, int value) { }
        public static ArcNET.Formats.ObjectProperty ForInt32Array(ArcNET.GameObjects.ObjectField field, System.ReadOnlySpan<int> values) { }
        public static ArcNET.Formats.ObjectProperty ForInt64(ArcNET.GameObjects.ObjectField field, long value) { }
        public static ArcNET.Formats.ObjectProperty ForInt64Array(ArcNET.GameObjects.ObjectField field, System.ReadOnlySpan<long> values) { }
        public static ArcNET.Formats.ObjectProperty ForLocation(ArcNET.GameObjects.ObjectField field, int tileX, int tileY) { }
        public static ArcNET.Formats.ObjectProperty ForObjectId(ArcNET.GameObjects.ObjectField field, ArcNET.Core.Primitives.GameObjectGuid value) { }
        public static ArcNET.Formats.ObjectProperty ForObjectIdArray(ArcNET.GameObjects.ObjectField field, System.ReadOnlySpan<System.Guid> ids) { }
        public static ArcNET.Formats.ObjectProperty ForPackedRgbColor(ArcNET.GameObjects.ObjectField field, ArcNET.Core.Primitives.Color value) { }
        public static ArcNET.Formats.ObjectProperty ForString(ArcNET.GameObjects.ObjectField field, string value) { }
    }
    public readonly struct ObjectPropertyScript : System.IEquatable<ArcNET.Formats.ObjectPropertyScript>
    {
        public ObjectPropertyScript(uint Flags, uint Counters, int ScriptId) { }
        public uint Counters { get; init; }
        public uint Flags { get; init; }
        public int ScriptId { get; init; }
    }
    [System.Runtime.CompilerServices.InlineArray(8)]
    public struct OpTypeBuffer { }
    [System.Runtime.CompilerServices.InlineArray(8)]
    public struct OpValueBuffer { }
    public sealed class ProtoData
    {
        public ProtoData() { }
        public required ArcNET.GameObjects.GameObjectHeader Header { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> Properties { get; init; }
    }
    public sealed class ProtoFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.ProtoData>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.ProtoData>, ArcNET.Formats.IFormatReader<ArcNET.Formats.ProtoData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ProtoData>
    {
        public ProtoFormat() { }
        public static ArcNET.Formats.ProtoData Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.ProtoData ParseFile(string path) { }
        public static ArcNET.Formats.ProtoData ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.ProtoData value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.ProtoData value) { }
        public static void WriteToFile(in ArcNET.Formats.ProtoData value, string path) { }
    }
    public enum SaveEngineVersion
    {
        Vanilla = 8,
        ArcanumCE = 119,
    }
    public sealed class SaveGame
    {
        public SaveGame() { }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "VirtualPath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, ArcNET.Formats.Data2SavFile>> Data2SavFiles { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "VirtualPath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, ArcNET.Formats.DataSavFile>> DataSavFiles { get; init; }
        public ArcNET.Formats.SaveEngineVersion EngineVersion { get; init; }
        public required ArcNET.Formats.SaveInfo Info { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.SaveMapState> Maps { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "VirtualPath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, byte[]>> MessageFiles { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "VirtualPath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, byte[]>> RawFiles { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "VirtualPath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, ArcNET.Formats.TownMapFog>> TownMapFogs { get; init; }
    }
    public static class SaveGameBuilder
    {
        public static ArcNET.Formats.SaveGame CreateNew(ArcNET.Formats.SaveInfo info, ArcNET.Formats.SaveMapState map) { }
        public static ArcNET.Formats.SaveGame CreateNew(ArcNET.Formats.SaveInfo info, string mapPath, ArcNET.Formats.CharacterMdyRecord pc) { }
    }
    public static class SaveGameReader
    {
        public static ArcNET.Formats.SaveGame Load(string tfaiPath) { }
        public static ArcNET.Formats.SaveGame Load(string tfaiPath, string tfafPath) { }
        public static ArcNET.Formats.SaveGame Load(string tfaiPath, string tfafPath, string gsiPath) { }
        public static ArcNET.Formats.SaveGame ParseMemory(System.ReadOnlyMemory<byte> tfaiData, System.ReadOnlyMemory<byte> tfafData, System.ReadOnlyMemory<byte> gsiData) { }
    }
    public static class SaveGameWriter
    {
        public static void Save(ArcNET.Formats.SaveGame save, string tfaiPath) { }
        public static void Save(ArcNET.Formats.SaveGame save, string tfaiPath, string tfafPath) { }
        public static void Save(ArcNET.Formats.SaveGame save, string tfaiPath, string tfafPath, string gsiPath) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Tfai",
                "Tfaf",
                "Gsi"})]
        public static System.ValueTuple<byte[], byte[], byte[]> SaveToMemory(ArcNET.Formats.SaveGame save) { }
    }
    public sealed class SaveIndex
    {
        public SaveIndex() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TfaiEntry> Root { get; init; }
    }
    public sealed class SaveIndexFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.SaveIndex>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.SaveIndex>, ArcNET.Formats.IFormatReader<ArcNET.Formats.SaveIndex>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.SaveIndex>
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
        public int Version { get; init; }
        public ArcNET.Formats.SaveInfo With(string? moduleName = null, string? leaderName = null, string? displayName = null, int? mapId = default, int? gameTimeDays = default, int? gameTimeMs = default, int? leaderPortraitId = default, int? leaderLevel = default, int? leaderTileX = default, int? leaderTileY = default, int? storyState = default) { }
    }
    public sealed class SaveInfoFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.SaveInfo>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.SaveInfo>, ArcNET.Formats.IFormatReader<ArcNET.Formats.SaveInfo>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.SaveInfo>
    {
        public SaveInfoFormat() { }
        public static ArcNET.Formats.SaveInfo Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.SaveInfo ParseFile(string path) { }
        public static ArcNET.Formats.SaveInfo ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.SaveInfo value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.SaveInfo value) { }
        public static void WriteToFile(in ArcNET.Formats.SaveInfo value, string path) { }
    }
    public sealed class SaveMapState
    {
        public SaveMapState() { }
        public ArcNET.Formats.MobileMdyFile? DynamicObjects { get; init; }
        public ArcNET.Formats.JmpFile? JumpPoints { get; init; }
        public required string MapPath { get; init; }
        public ArcNET.Formats.MapProperties? Properties { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string?[]?[] {
                "FileName",
                "Data"})]
        public required System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, ArcNET.Formats.Sector>> Sectors { get; init; }
        public ArcNET.Formats.MobileMdFile? StaticDiffs { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string?[]?[] {
                "FileName",
                "Data"})]
        public required System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, ArcNET.Formats.MobData>> StaticObjects { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string?[]?[] {
                "RelativePath",
                "Data"})]
        public System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, byte[]>> UnknownFiles { get; init; }
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
        public ScriptActionData(int Type, ArcNET.Formats.OpTypeBuffer OpTypes, ArcNET.Formats.OpValueBuffer OpValues) { }
        public ArcNET.Formats.ScriptActionType ActionType { get; }
        public ArcNET.Formats.OpTypeBuffer OpTypes { get; init; }
        public ArcNET.Formats.OpValueBuffer OpValues { get; init; }
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
        public ScriptConditionData(int Type, ArcNET.Formats.OpTypeBuffer OpTypes, ArcNET.Formats.OpValueBuffer OpValues, ArcNET.Formats.ScriptActionData Action, ArcNET.Formats.ScriptActionData Else) { }
        public ArcNET.Formats.ScriptActionData Action { get; init; }
        public ArcNET.Formats.ScriptConditionType ConditionType { get; }
        public ArcNET.Formats.ScriptActionData Else { get; init; }
        public ArcNET.Formats.OpTypeBuffer OpTypes { get; init; }
        public ArcNET.Formats.OpValueBuffer OpValues { get; init; }
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
        ollowingPc = 18,
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
    public sealed class ScriptFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.ScrFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.ScrFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.ScrFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.ScrFile>
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
    public sealed class SectorFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.Sector>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.Sector>, ArcNET.Formats.IFormatReader<ArcNET.Formats.Sector>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.Sector>
    {
        public SectorFormat() { }
        public static ArcNET.Formats.Sector Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.Sector ParseFile(string path) { }
        public static ArcNET.Formats.Sector ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.Sector value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.Sector value) { }
        public static void WriteToFile(in ArcNET.Formats.Sector value, string path) { }
    }
    public readonly struct SectorLight : System.IEquatable<ArcNET.Formats.SectorLight>
    {
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
    public readonly struct SectorSoundList : System.IEquatable<ArcNET.Formats.SectorSoundList>
    {
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
    public sealed class TerrainFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.TerrainData>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.TerrainData>, ArcNET.Formats.IFormatReader<ArcNET.Formats.TerrainData>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.TerrainData>
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
    public sealed class TextDataFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.TextDataFile>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.TextDataFile>, ArcNET.Formats.IFormatReader<ArcNET.Formats.TextDataFile>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.TextDataFile>
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
    public readonly struct TileScript : System.IEquatable<ArcNET.Formats.TileScript>
    {
        public required uint NodeFlags { get; init; }
        public required uint ScriptCounters { get; init; }
        public required uint ScriptFlags { get; init; }
        public required int ScriptNum { get; init; }
        public required uint TileId { get; init; }
    }
    public sealed class TownMapFog
    {
        public TownMapFog() { }
        public double CoveragePercent { get; }
        public required byte[] RawBytes { get; init; }
        public int RevealedTiles { get; }
        public int TotalTiles { get; }
    }
    public sealed class TownMapFogFormat : ArcNET.Formats.IFormatFileReader<ArcNET.Formats.TownMapFog>, ArcNET.Formats.IFormatFileWriter<ArcNET.Formats.TownMapFog>, ArcNET.Formats.IFormatReader<ArcNET.Formats.TownMapFog>, ArcNET.Formats.IFormatWriter<ArcNET.Formats.TownMapFog>
    {
        public TownMapFogFormat() { }
        public static ArcNET.Formats.TownMapFog Parse([System.Runtime.CompilerServices.ScopedRef] ref ArcNET.Core.SpanReader reader) { }
        public static ArcNET.Formats.TownMapFog ParseFile(string path) { }
        public static ArcNET.Formats.TownMapFog ParseMemory(System.ReadOnlyMemory<byte> memory) { }
        public static void Write(in ArcNET.Formats.TownMapFog value, ref ArcNET.Core.SpanWriter writer) { }
        public static byte[] WriteToArray(in ArcNET.Formats.TownMapFog value) { }
        public static void WriteToFile(in ArcNET.Formats.TownMapFog value, string path) { }
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
namespace ArcNET.GameObjects
{
    [System.Flags]
    public enum ArmorFlags : uint
    {
        None = 0u,
        SizeSmall = 1u,
        SizeMedium = 2u,
        SizeLarge = 4u,
        MaleOnly = 8u,
        FemaleOnly = 16u,
    }
    public static class BitArrayObjectExtensions
    {
        public static bool HasField(this byte[] bitmap, ArcNET.GameObjects.ObjectField field) { }
        public static void SetField(this byte[] bitmap, ArcNET.GameObjects.ObjectField field, bool value) { }
    }
    [System.Flags]
    public enum BlitFlags : uint
    {
        None = 0u,
        BlendAdd = 16u,
        BlendSub = 32u,
        BlendMul = 64u,
        BlendAlphaConst = 256u,
        BlendAlphaSrc = 512u,
        BlendColorConst = 8192u,
    }
    [System.Flags]
    public enum ContainerFlags : uint
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
    public enum CritterFlags : uint
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
    public enum CritterFlags2 : uint
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
    public sealed class GameObject : ArcNET.GameObjects.IGameObject
    {
        public GameObject() { }
        public required ArcNET.GameObjects.Types.ObjectCommon Common { get; init; }
        public required ArcNET.GameObjects.GameObjectHeader Header { get; init; }
        public bool IsPrototype { get; }
        public ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; }
        public ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; }
        public ArcNET.GameObjects.ObjectType Type { get; }
        public byte[] WriteToArray() { }
        public static ArcNET.GameObjects.GameObject Read(ref ArcNET.Core.SpanReader reader) { }
    }
    public sealed class GameObjectHeader
    {
        public GameObjectHeader() { }
        public required byte[] Bitmap { get; init; }
        public required ArcNET.GameObjects.ObjectType GameObjectType { get; init; }
        public bool IsOriginalVersion { get; }
        public bool IsPrototype { get; }
        public required ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; init; }
        public short PropCollectionItems { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; init; }
        public required int Version { get; init; }
    }
    public readonly struct GameObjectScript : ArcNET.Core.IBinarySerializable<ArcNET.GameObjects.GameObjectScript, ArcNET.Core.SpanReader>, System.IEquatable<ArcNET.GameObjects.GameObjectScript>
    {
        public GameObjectScript(uint Counters, int Flags, int ScriptId) { }
        public uint Counters { get; init; }
        public int Flags { get; init; }
        public bool IsEmpty { get; }
        public int ScriptId { get; init; }
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
    public enum ItemFlags : uint
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
    public enum NpcFlags : uint
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
    public enum ObjectField : byte
    {
        CurrentAid = 0,
        Location = 1,
        OffsetX = 2,
        OffsetY = 3,
        Shadow = 4,
        OverlayFore = 5,
        OverlayBack = 6,
        Underlay = 7,
        BlitFlags = 8,
        BlitColor = 9,
        BlitAlpha = 10,
        BlitScale = 11,
        LightFlags = 12,
        LightAid = 13,
        LightColor = 14,
        OverlayLightFlags = 15,
        OverlayLightAid = 16,
        OverlayLightColor = 17,
        ObjectFlags = 18,
        SpellFlags = 19,
        BlockingMask = 20,
        Name = 21,
        Description = 22,
        Aid = 23,
        DestroyedAid = 24,
        Ac = 25,
        HpPts = 26,
        HpAdj = 27,
        HpDamage = 28,
        Material = 29,
        ResistanceIdx = 30,
        ScriptsIdx = 31,
        SoundEffect = 32,
        Category = 33,
        PadIas1 = 34,
        PadI64As1 = 35,
        SpeedRun = 36,
        SpeedWalk = 37,
        PadFloat1 = 38,
        Radius = 39,
        Height = 40,
        Conditions = 41,
        ConditionArg0 = 42,
        PermanentMods = 43,
        Initiative = 44,
        Dispatcher = 45,
        Subinitiative = 46,
        SecretdoorFlags = 47,
        SecretdoorEffectName = 48,
        SecretdoorDc = 49,
        PadI7 = 50,
        PadI8 = 51,
        PadI9 = 52,
        PadI0 = 53,
        OffsetZ = 54,
        RotationPitch = 55,
        PadF3 = 56,
        PadF4 = 57,
        PadF5 = 58,
        PadF6 = 59,
        PadF7 = 60,
        PadF8 = 61,
        PadF9 = 62,
        PadF0 = 63,
        WallFlags = 64,
        WallPadI1 = 65,
        WallPadI2 = 66,
        WallPadIas1 = 67,
        WallPadI64As1 = 68,
        PortalFlags = 64,
        PortalLockDifficulty = 65,
        PortalKeyId = 66,
        PortalNotifyNpc = 67,
        PortalPadI1 = 68,
        PortalPadI2 = 69,
        PortalPadIas1 = 70,
        PortalPadI64As1 = 71,
        ContainerFlags = 64,
        ContainerLockDifficulty = 65,
        ContainerKeyId = 66,
        ContainerInventoryNum = 67,
        ContainerInventoryListIdx = 68,
        ContainerInventorySource = 69,
        ContainerNotifyNpc = 70,
        ContainerPadI1 = 71,
        ContainerPadI2 = 72,
        ContainerPadIas1 = 73,
        ContainerPadI64As1 = 74,
        SceneryFlags = 64,
        SceneryWhosInMe = 65,
        SceneryRespawnDelay = 66,
        SceneryPadI2 = 67,
        SceneryPadIas1 = 68,
        SceneryPadI64As1 = 69,
        ProjectileFlagsCombat = 64,
        ProjectileFlagsCombatDamage = 65,
        ProjectileHitLoc = 66,
        ProjectileParentWeapon = 67,
        ProjectilePadI1 = 68,
        ProjectilePadI2 = 69,
        ProjectilePadIas1 = 70,
        ProjectilePadI64As1 = 71,
        TrapFlags = 64,
        TrapDifficulty = 65,
        TrapPadI2 = 66,
        TrapPadIas1 = 67,
        TrapPadI64As1 = 68,
        ItemFlags = 64,
        ItemParent = 65,
        ItemWeight = 66,
        ItemMagicWeightAdj = 67,
        ItemWorth = 68,
        ItemManaStore = 69,
        ItemInvAid = 70,
        ItemInvLocation = 71,
        ItemUseAidFragment = 72,
        ItemMagicTechComplexity = 73,
        ItemDiscipline = 74,
        ItemDescriptionUnknown = 75,
        ItemDescriptionEffects = 76,
        ItemSpell1 = 77,
        ItemSpell2 = 78,
        ItemSpell3 = 79,
        ItemSpell4 = 80,
        ItemSpell5 = 81,
        ItemSpellManaStore = 82,
        ItemAiAction = 83,
        ItemPadI1 = 84,
        ItemPadIas1 = 85,
        ItemPadI64As1 = 86,
        WeaponFlags = 96,
        WeaponPaperDollAid = 97,
        WeaponBonusToHit = 98,
        WeaponMagicHitAdj = 99,
        WeaponDamageLowerIdx = 100,
        WeaponDamageUpperIdx = 101,
        WeaponMagicDamageAdjIdx = 102,
        WeaponSpeedFactor = 103,
        WeaponMagicSpeedAdj = 104,
        WeaponRange = 105,
        WeaponMagicRangeAdj = 106,
        WeaponMinStrength = 107,
        WeaponMagicMinStrengthAdj = 108,
        WeaponAmmoType = 109,
        WeaponAmmoConsumption = 110,
        WeaponMissileAid = 111,
        WeaponVisualEffectAid = 112,
        WeaponCritHitChart = 113,
        WeaponMagicCritHitChance = 114,
        WeaponMagicCritHitEffect = 115,
        WeaponCritMissChart = 116,
        WeaponMagicCritMissChance = 117,
        WeaponMagicCritMissEffect = 118,
        WeaponPadI1 = 119,
        WeaponPadI2 = 120,
        WeaponPadIas1 = 121,
        WeaponPadI64As1 = 122,
        AmmoFlags = 96,
        AmmoQuantity = 97,
        AmmoType = 98,
        AmmoPadI1 = 99,
        AmmoPadI2 = 100,
        AmmoPadIas1 = 101,
        AmmoPadI64As1 = 102,
        ArmorFlags = 96,
        ArmorPaperDollAid = 97,
        ArmorAcAdj = 98,
        ArmorMagicAcAdj = 99,
        ArmorResistanceAdjIdx = 100,
        ArmorMagicResistanceAdjIdx = 101,
        ArmorSilentMoveAdj = 102,
        ArmorMagicSilentMoveAdj = 103,
        ArmorUnarmedBonusDamage = 104,
        ArmorPadI2 = 105,
        ArmorPadIas1 = 106,
        ArmorPadI64As1 = 107,
        GoldFlags = 96,
        GoldQuantity = 97,
        GoldPadI1 = 98,
        GoldPadI2 = 99,
        GoldPadIas1 = 100,
        GoldPadI64As1 = 101,
        FoodFlags = 96,
        FoodPadI1 = 97,
        FoodPadI2 = 98,
        FoodPadIas1 = 99,
        FoodPadI64As1 = 100,
        ScrollFlags = 96,
        ScrollPadI1 = 97,
        ScrollPadI2 = 98,
        ScrollPadIas1 = 99,
        ScrollPadI64As1 = 100,
        KeyKeyId = 96,
        KeyPadI1 = 97,
        KeyPadI2 = 98,
        KeyPadIas1 = 99,
        KeyPadI64As1 = 100,
        KeyRingFlags = 96,
        KeyRingListIdx = 97,
        KeyRingPadI1 = 98,
        KeyRingPadI2 = 99,
        KeyRingPadIas1 = 100,
        KeyRingPadI64As1 = 101,
        WrittenFlags = 96,
        WrittenSubtype = 97,
        WrittenTextStartLine = 98,
        WrittenTextEndLine = 99,
        WrittenPadI1 = 100,
        WrittenPadI2 = 101,
        WrittenPadIas1 = 102,
        WrittenPadI64As1 = 103,
        GenericFlags = 96,
        GenericUsageBonus = 97,
        GenericUsageCountRemaining = 98,
        GenericPadIas1 = 99,
        GenericPadI64As1 = 100,
        CritterFlags = 64,
        CritterFlags2 = 65,
        CritterStatBaseIdx = 66,
        CritterBasicSkillIdx = 67,
        CritterTechSkillIdx = 68,
        CritterSpellTechIdx = 69,
        CritterFatiguePts = 70,
        CritterFatigueAdj = 71,
        CritterFatigueDamage = 72,
        CritterCritHitChart = 73,
        CritterEffectsIdx = 74,
        CritterEffectCauseIdx = 75,
        CritterFleeingFrom = 76,
        CritterPortrait = 77,
        CritterGold = 78,
        CritterArrows = 79,
        CritterBullets = 80,
        CritterPowerCells = 81,
        CritterFuel = 82,
        CritterInventoryNum = 83,
        CritterInventoryListIdx = 84,
        CritterInventorySource = 85,
        CritterDescriptionUnknown = 86,
        CritterFollowerIdx = 87,
        CritterTeleportDest = 88,
        CritterTeleportMap = 89,
        CritterDeathTime = 90,
        CritterAutoLevelScheme = 91,
        CritterPadI1 = 92,
        CritterPadI2 = 93,
        CritterPadI3 = 94,
        CritterPadIas1 = 95,
        CritterPadI64As1 = 96,
        PcFlags = 128,
        PcFlagsFate = 129,
        PcReputationIdx = 130,
        PcReputationTsIdx = 131,
        PcBackground = 132,
        PcBackgroundText = 133,
        PcQuestIdx = 134,
        PcBlessingIdx = 135,
        PcBlessingTsIdx = 136,
        PcCurseIdx = 137,
        PcCurseTsIdx = 138,
        PcPartyId = 139,
        PcRumorIdx = 140,
        PcPadIas2 = 141,
        PcSchematicsFoundIdx = 142,
        PcLogbookEgoIdx = 143,
        PcFogMask = 144,
        PcPlayerName = 145,
        PcBankMoney = 146,
        PcGlobalFlags = 147,
        PcGlobalVariables = 148,
        PcPadI1 = 149,
        PcPadI2 = 150,
        PcPadIas1 = 151,
        PcPadI64As1 = 152,
        NpcFlags = 128,
        NpcLeader = 129,
        NpcAiData = 130,
        NpcCombatFocus = 131,
        NpcWhoHitMeLast = 132,
        NpcExperienceWorth = 133,
        NpcExperiencePool = 134,
        NpcWaypointsIdx = 135,
        NpcWaypointCurrent = 136,
        NpcStandpointDay = 137,
        NpcStandpointNight = 138,
        NpcOrigin = 139,
        NpcFaction = 140,
        NpcRetailPriceMultiplier = 141,
        NpcSubstituteInventory = 142,
        NpcReactionBase = 143,
        NpcSocialClass = 144,
        NpcReactionPcIdx = 145,
        NpcReactionLevelIdx = 146,
        NpcReactionTimeIdx = 147,
        NpcWait = 148,
        NpcGeneratorData = 149,
        NpcPadI1 = 150,
        NpcDamageIdx = 151,
        NpcHostileListIdx = 152,
    }
    public static class ObjectFieldBitmapSize
    {
        public static int For(ArcNET.GameObjects.ObjectType type) { }
    }
    [System.Flags]
    public enum ObjectFlags : uint
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
    [System.Flags]
    public enum PortalFlags : uint
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
    public enum SceneryFlags : uint
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
    public enum SpellFlags : uint
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
    public enum WeaponFlags : uint
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
}
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
        public int AIPacket { get; init; }
        public int Alignment { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "ArtNumber",
                "Palette"})]
        public System.ValueTuple<int, int> ArtNumberAndPalette { get; init; }
        public int AutoLevelScheme { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Stat",
                "Value"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.BasicStatType, int>> BasicStats { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.BlitFlags> BlitFlags { get; init; }
        public int Category { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.CritterFlags> CritterFlags { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.CritterFlags2> CritterFlags2 { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Type",
                "Min",
                "Max"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.DamageType, int, int>> Damages { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Id",
                "Text"})]
        public System.ValueTuple<int, string> Description { get; init; }
        public int Faction { get; init; }
        public int Fatigue { get; init; }
        public int HitChart { get; init; }
        public int HitPoints { get; init; }
        public int InternalName { get; init; }
        public int InventorySource { get; init; }
        public int Level { get; init; }
        public int Material { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.NpcFlags> NpcFlags { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjectFlags> ObjectFlags { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Type",
                "Value"})]
        public System.Collections.Generic.List<System.ValueTuple<ArcNET.GameObjects.Classes.ResistanceType, int>> Resistances { get; init; }
        public int Scale { get; init; }
        [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "A",
                "B",
                "C",
                "D",
                "E",
                "F"})]
        public System.Collections.Generic.List<System.ValueTuple<int, int, int, int, int, int>> Scripts { get; init; }
        public int SoundBank { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.SpellFlags> SpellFlags { get; init; }
        public System.Collections.Generic.List<string> Spells { get; init; }
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
namespace ArcNET.GameObjects.Types
{
    public sealed class ObjectAmmo : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectAmmo() { }
        public int AmmoFlags { get; }
        public int Quantity { get; }
        public int Type { get; }
    }
    public sealed class ObjectArmor : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectArmor() { }
        public int AcAdj { get; }
        public ArcNET.GameObjects.ArmorFlags ArmorFlags { get; }
        public int MagicAcAdj { get; }
        public int[] MagicResistanceAdj { get; }
        public int MagicSilentMoveAdj { get; }
        public int PaperDollAid { get; }
        public int[] ResistanceAdj { get; }
        public int SilentMoveAdj { get; }
        public int UnarmedBonusDamage { get; }
    }
    public abstract class ObjectCommon
    {
        protected ObjectCommon() { }
        public int Ac { get; }
        public ArcNET.Core.Primitives.ArtId Aid { get; }
        public int BlitAlpha { get; }
        public ArcNET.Core.Primitives.Color BlitColor { get; }
        public int BlitFlags { get; }
        public int BlitScale { get; }
        public int BlockingMask { get; }
        public int Category { get; }
        public ArcNET.Core.Primitives.ArtId CurrentAid { get; }
        public int Description { get; }
        public ArcNET.Core.Primitives.ArtId DestroyedAid { get; }
        public int HpAdj { get; }
        public int HpDamage { get; }
        public int HpPts { get; }
        public ArcNET.Core.Primitives.ArtId LightAid { get; }
        public ArcNET.Core.Primitives.Color LightColor { get; }
        public int LightFlags { get; }
        public ArcNET.Core.Primitives.Location? Location { get; }
        public int Material { get; }
        public int Name { get; }
        public ArcNET.GameObjects.ObjectFlags ObjectFlags { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public int[] OverlayBack { get; }
        public int[] OverlayFore { get; }
        public int[] OverlayLightAid { get; }
        public int[] OverlayLightColor { get; }
        public int[] OverlayLightFlags { get; }
        public int[] ResistanceIdx { get; }
        public ArcNET.GameObjects.GameObjectScript[] ScriptsIdx { get; }
        public ArcNET.Core.Primitives.ArtId Shadow { get; }
        public int SoundEffect { get; }
        public ArcNET.GameObjects.SpellFlags SpellFlags { get; }
        public int[] Underlay { get; }
        protected void ReadCommonFields(ref ArcNET.Core.SpanReader reader, byte[] bitmap, bool isPrototype) { }
        protected void WriteCommonFields(ref ArcNET.Core.SpanWriter writer, byte[] bitmap, bool isPrototype) { }
    }
    public sealed class ObjectContainer : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectContainer() { }
        public ArcNET.GameObjects.ContainerFlags ContainerFlags { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] InventoryList { get; }
        public int InventorySource { get; }
        public int KeyId { get; }
        public int LockDifficulty { get; }
        public int NotifyNpc { get; }
    }
    public class ObjectCritter : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectCritter() { }
        public ArcNET.Core.Primitives.GameObjectGuid CritterArrows { get; }
        public int CritterAutoLevelScheme { get; }
        public int[] CritterBasicSkill { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterBullets { get; }
        public int CritterCritHitChart { get; }
        public long CritterDeathTime { get; }
        public int CritterDescriptionUnknown { get; }
        public int[] CritterEffectCause { get; }
        public int[] CritterEffects { get; }
        public int CritterFatigueAdj { get; }
        public int CritterFatigueDamage { get; }
        public int CritterFatiguePts { get; }
        public ArcNET.GameObjects.CritterFlags CritterFlags { get; }
        public ArcNET.GameObjects.CritterFlags2 CritterFlags2 { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterFleeingFrom { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterFollowers { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterFuel { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterGold { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterInventoryList { get; }
        public int CritterInventorySource { get; }
        public int CritterPortrait { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterPowerCells { get; }
        public int[] CritterSpellTech { get; }
        public int[] CritterStatBase { get; }
        public int[] CritterTechSkill { get; }
        public ArcNET.Core.Primitives.Location CritterTeleportDest { get; }
        public int CritterTeleportMap { get; }
        protected void ReadCritterFields(ref ArcNET.Core.SpanReader reader, byte[] bitmap, bool isPrototype) { }
        protected void WriteCritterFields(ref ArcNET.Core.SpanWriter writer, byte[] bitmap, bool isPrototype) { }
    }
    public sealed class ObjectFood : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectFood() { }
        public int FoodFlags { get; }
    }
    public sealed class ObjectGeneric : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectGeneric() { }
        public int GenericFlags { get; }
        public int UsageBonus { get; }
        public int UsageCountRemaining { get; }
    }
    public sealed class ObjectGold : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectGold() { }
        public int GoldFlags { get; }
        public int Quantity { get; }
    }
    public class ObjectItem : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectItem() { }
        public int ItemAiAction { get; }
        public int ItemDescriptionEffects { get; }
        public int ItemDescriptionUnknown { get; }
        public int ItemDiscipline { get; }
        public ArcNET.GameObjects.ItemFlags ItemFlags { get; }
        public int ItemInvAid { get; }
        public int ItemInvLocation { get; }
        public int ItemMagicTechComplexity { get; }
        public int ItemMagicWeightAdj { get; }
        public int ItemManaStore { get; }
        public ArcNET.Core.Primitives.GameObjectGuid ItemParent { get; }
        public int ItemSpell1 { get; }
        public int ItemSpell2 { get; }
        public int ItemSpell3 { get; }
        public int ItemSpell4 { get; }
        public int ItemSpell5 { get; }
        public int ItemSpellManaStore { get; }
        public int ItemUseAidFragment { get; }
        public int ItemWeight { get; }
        public int ItemWorth { get; }
        protected void ReadItemFields(ref ArcNET.Core.SpanReader reader, byte[] bitmap, bool isPrototype) { }
        protected void WriteItemFields(ref ArcNET.Core.SpanWriter writer, byte[] bitmap, bool isPrototype) { }
    }
    public sealed class ObjectKey : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectKey() { }
        public int KeyId { get; }
    }
    public sealed class ObjectKeyRing : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectKeyRing() { }
        public int KeyRingFlags { get; }
        public int[] List { get; }
    }
    public sealed class ObjectNpc : ArcNET.GameObjects.Types.ObjectCritter
    {
        public ObjectNpc() { }
        public int AiData { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CombatFocus { get; }
        public int[] Damage { get; }
        public int ExperiencePool { get; }
        public int ExperienceWorth { get; }
        public int Faction { get; }
        public int GeneratorData { get; }
        public int[] HostileList { get; }
        public ArcNET.Core.Primitives.GameObjectGuid Leader { get; }
        public ArcNET.GameObjects.NpcFlags NpcFlags { get; }
        public int Origin { get; }
        public int ReactionBase { get; }
        public int[] ReactionLevel { get; }
        public int[] ReactionPc { get; }
        public int[] ReactionTime { get; }
        public int RetailPriceMultiplier { get; }
        public int SocialClass { get; }
        public ArcNET.Core.Primitives.Location StandpointDay { get; }
        public ArcNET.Core.Primitives.Location StandpointNight { get; }
        public ArcNET.Core.Primitives.GameObjectGuid SubstituteInventory { get; }
        public int Wait { get; }
        public int WaypointCurrent { get; }
        public ArcNET.Core.Primitives.Location[] Waypoints { get; }
        public ArcNET.Core.Primitives.GameObjectGuid WhoHitMeLast { get; }
    }
    public sealed class ObjectPc : ArcNET.GameObjects.Types.ObjectCritter
    {
        public ObjectPc() { }
        public int Background { get; }
        public int BackgroundText { get; }
        public int BankMoney { get; }
        public int[] Blessing { get; }
        public int[] BlessingTs { get; }
        public int[] Curse { get; }
        public int[] CurseTs { get; }
        public int FateFlags { get; }
        public int FogMask { get; }
        public int[] GlobalFlags { get; }
        public int[] GlobalVariables { get; }
        public int[] LogbookEgo { get; }
        public int PartyId { get; }
        public int PcFlags { get; }
        public ArcNET.Core.Primitives.PrefixedString PlayerName { get; }
        public int[] Quest { get; }
        public int[] Reputation { get; }
        public int[] ReputationTs { get; }
        public int[] Rumor { get; }
        public int[] SchematicsFound { get; }
    }
    public sealed class ObjectPortal : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectPortal() { }
        public int KeyId { get; }
        public int LockDifficulty { get; }
        public int NotifyNpc { get; }
        public ArcNET.GameObjects.PortalFlags PortalFlags { get; }
    }
    public sealed class ObjectProjectile : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectProjectile() { }
        public int CombatDamageFlags { get; }
        public int CombatFlags { get; }
        public ArcNET.Core.Primitives.Location HitLoc { get; }
        public int ParentWeapon { get; }
    }
    public sealed class ObjectScenery : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectScenery() { }
        public int RespawnDelay { get; }
        public ArcNET.GameObjects.SceneryFlags SceneryFlags { get; }
        public ArcNET.Core.Primitives.GameObjectGuid WhosInMe { get; }
    }
    public sealed class ObjectScroll : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectScroll() { }
        public int ScrollFlags { get; }
    }
    public sealed class ObjectTrap : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectTrap() { }
        public int Difficulty { get; }
        public int TrapFlags { get; }
    }
    public sealed class ObjectWall : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectWall() { }
        public int WallFlags { get; }
    }
    public sealed class ObjectWeapon : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectWeapon() { }
        public int AmmoConsumption { get; }
        public int AmmoType { get; }
        public int BonusToHit { get; }
        public int CritHitChart { get; }
        public int CritMissChart { get; }
        public int[] DamageLower { get; }
        public int[] DamageUpper { get; }
        public int MagicCritHitChance { get; }
        public int MagicCritHitEffect { get; }
        public int MagicCritMissChance { get; }
        public int MagicCritMissEffect { get; }
        public int[] MagicDamageAdj { get; }
        public int MagicHitAdj { get; }
        public int MagicMinStrengthAdj { get; }
        public int MagicRangeAdj { get; }
        public int MagicSpeedAdj { get; }
        public int MinStrength { get; }
        public int MissileAid { get; }
        public int PaperDollAid { get; }
        public int Range { get; }
        public int SpeedFactor { get; }
        public int VisualEffectAid { get; }
        public ArcNET.GameObjects.WeaponFlags WeaponFlags { get; }
    }
    public sealed class ObjectWritten : ArcNET.GameObjects.Types.ObjectItem
    {
        public ObjectWritten() { }
        public int Subtype { get; }
        public int TextEndLine { get; }
        public int TextStartLine { get; }
        public int WrittenFlags { get; }
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
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Editor")]
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
    public sealed class GameDataLoadEntry
    {
        public GameDataLoadEntry(ArcNET.Formats.FileFormat format, string sourcePath, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<System.ReadOnlyMemory<byte>>> loadContentAsync, long estimatedContentLength = 0) { }
        public long EstimatedContentLength { get; }
        public ArcNET.Formats.FileFormat Format { get; }
        public System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<System.ReadOnlyMemory<byte>>> LoadContentAsync { get; }
        public string SourcePath { get; }
        public static ArcNET.GameData.GameDataLoadEntry FromFile(ArcNET.Formats.FileFormat format, string sourcePath, string filePath) { }
        public static ArcNET.GameData.GameDataLoadEntry FromMemory(ArcNET.Formats.FileFormat format, string sourcePath, System.ReadOnlyMemory<byte> memory) { }
    }
    public sealed class GameDataLoadFailure
    {
        public GameDataLoadFailure(string sourcePath, ArcNET.Formats.FileFormat format, string reason) { }
        public ArcNET.Formats.FileFormat Format { get; }
        public string Reason { get; }
        public string SourcePath { get; }
    }
    public sealed class GameDataLoadOptions : System.IEquatable<ArcNET.GameData.GameDataLoadOptions>
    {
        public GameDataLoadOptions() { }
        public bool LoadArtMetadata { get; init; }
        public static ArcNET.GameData.GameDataLoadOptions Default { get; }
    }
    public sealed class GameDataLoadProgress
    {
        public GameDataLoadProgress(string activity, float progress, int? completedEntries = default, int? totalEntries = default) { }
        public string Activity { get; }
        public int? CompletedEntries { get; }
        public float Progress { get; }
        public int? TotalEntries { get; }
    }
    public sealed class GameDataLoadResult
    {
        public GameDataLoadResult(ArcNET.GameData.GameDataStore store, System.Collections.Generic.IReadOnlyList<ArcNET.GameData.GameDataLoadFailure> failures) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameData.GameDataLoadFailure> Failures { get; }
        public ArcNET.GameData.GameDataStore Store { get; }
    }
    public sealed class GameDataLoadStageTiming : System.IEquatable<ArcNET.GameData.GameDataLoadStageTiming>
    {
        public GameDataLoadStageTiming() { }
        public required long ElapsedMs { get; init; }
        public int? ItemCount { get; init; }
        public required string StageName { get; init; }
        public string? UnitLabel { get; init; }
    }
    public static class GameDataLoader
    {
        public static System.Collections.Generic.IReadOnlyDictionary<ArcNET.Formats.FileFormat, System.Collections.Generic.IReadOnlyList<string>> DiscoverFiles(string dirPath) { }
        public static System.Threading.Tasks.Task<ArcNET.GameData.GameDataStore> LoadFromDirectoryAsync(string dirPath, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default, System.IProgress<ArcNET.GameData.GameDataLoadProgress>? loadProgress = null, System.IProgress<ArcNET.GameData.GameDataLoadStageTiming>? stageProgress = null, ArcNET.GameData.GameDataLoadOptions? options = null) { }
        public static System.Threading.Tasks.Task<ArcNET.GameData.GameDataLoadResult> LoadFromEntriesAsync(System.Collections.Generic.IReadOnlyList<ArcNET.GameData.GameDataLoadEntry> entries, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default, System.IProgress<ArcNET.GameData.GameDataLoadProgress>? loadProgress = null, System.IProgress<ArcNET.GameData.GameDataLoadStageTiming>? stageProgress = null, ArcNET.GameData.GameDataLoadOptions? options = null) { }
        public static System.Threading.Tasks.Task<ArcNET.GameData.GameDataStore> LoadFromMemoryAsync(System.Collections.Generic.IReadOnlyDictionary<string, System.ReadOnlyMemory<byte>> files, System.IProgress<float>? progress = null, System.Threading.CancellationToken ct = default, System.IProgress<ArcNET.GameData.GameDataLoadProgress>? loadProgress = null, System.IProgress<ArcNET.GameData.GameDataLoadStageTiming>? stageProgress = null, ArcNET.GameData.GameDataLoadOptions? options = null) { }
        public static System.Collections.Generic.IReadOnlyDictionary<int, string> LoadMessages(string dirPath) { }
    }
    public static class GameDataSaver
    {
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
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ArtFile> Arts { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ArtFile>> ArtsBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.DlgFile> Dialogs { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.DlgFile>> DialogsBySource { get; }
        public System.Collections.Generic.IReadOnlySet<ArcNET.Core.Primitives.GameObjectGuid> DirtyObjects { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.FacadeWalk> FacadeWalks { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.FacadeWalk>> FacadeWalksBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.JmpFile> JumpFiles { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.JmpFile>> JumpFilesBySource { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MapProperties>> MapPropertiesBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MapProperties> MapPropertiesList { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry> Messages { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry>> MessagesBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> Mobs { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData>> MobsBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> Objects { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ProtoData> Protos { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ProtoData>> ProtosBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ScrFile> Scripts { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ScrFile>> ScriptsBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Sector> Sectors { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.Sector>> SectorsBySource { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TerrainData> Terrains { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.TerrainData>> TerrainsBySource { get; }
        public event System.EventHandler<ArcNET.Core.Primitives.GameObjectGuid>? ObjectChanged;
        public void AddArt(ArcNET.Formats.ArtFile art) { }
        public void AddDialog(ArcNET.Formats.DlgFile dialog) { }
        public void AddFacadeWalk(ArcNET.Formats.FacadeWalk facadeWalk) { }
        public void AddJumpFile(ArcNET.Formats.JmpFile jumpFile) { }
        public void AddMapProperties(ArcNET.Formats.MapProperties properties) { }
        public void AddMessage(ArcNET.Formats.MessageEntry entry) { }
        public void AddMob(ArcNET.Formats.MobData mob) { }
        public void AddObject(ArcNET.GameObjects.GameObjectHeader header) { }
        public void AddProto(ArcNET.Formats.ProtoData proto) { }
        public void AddScript(ArcNET.Formats.ScrFile script) { }
        public void AddSector(ArcNET.Formats.Sector sector) { }
        public void AddTerrain(ArcNET.Formats.TerrainData terrain) { }
        public void Clear() { }
        public void ClearDirty() { }
        public ArcNET.GameObjects.GameObjectHeader? FindByGuid(in ArcNET.Core.Primitives.GameObjectGuid id) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> FindByProtoId(in ArcNET.Core.Primitives.GameObjectGuid protoId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> FindByType(ArcNET.GameObjects.ObjectType type) { }
        public void MarkDirty(in ArcNET.Core.Primitives.GameObjectGuid id) { }
        public static ArcNET.GameData.GameDataStore Overlay(ArcNET.GameData.GameDataStore baseStore, ArcNET.GameData.GameDataStore overlayStore) { }
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

## ArcNET.Diagnostics

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Diagnostics
{
    public sealed class ActionPointsMutationSnapshot : System.IEquatable<ArcNET.Diagnostics.ActionPointsMutationSnapshot>
    {
        public ActionPointsMutationSnapshot(System.DateTimeOffset GeneratedAtUtc, int ProcessId, string ProcessName, string ModulePath, string Address, int Before, int After) { }
        public string Address { get; init; }
        public int After { get; init; }
        public int Before { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public string ModulePath { get; init; }
        public int ProcessId { get; init; }
        public string ProcessName { get; init; }
    }
    public sealed class AttachedSessionSnapshot : System.IEquatable<ArcNET.Diagnostics.AttachedSessionSnapshot>
    {
        public AttachedSessionSnapshot(System.DateTimeOffset GeneratedAtUtc, ArcNET.Diagnostics.SessionOrigin Origin, string DisplayName, string Summary, string Detail, string ProcessName, int ProcessId, bool HasExited, ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, ArcNET.Diagnostics.LaunchPreviewSnapshot? LaunchPreview, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public string Detail { get; init; }
        public string DisplayName { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool HasExited { get; init; }
        public ArcNET.Diagnostics.LaunchPreviewSnapshot? LaunchPreview { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public ArcNET.Diagnostics.SessionOrigin Origin { get; init; }
        public int ProcessId { get; init; }
        public string ProcessName { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
        public string Summary { get; init; }
    }
    public sealed class AuditRequest : System.IEquatable<ArcNET.Diagnostics.AuditRequest>
    {
        public AuditRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, bool IncludeDispatcher, bool IncludeFunctions, bool IncludeHooks, System.Collections.Generic.IReadOnlyList<string> HookSelectors, System.TimeSpan HookDuration, bool IncludeWatchPass, bool IncludeInterceptPass, int StackCaptureDwordCount, bool StopOnFailure) { }
        public System.TimeSpan HookDuration { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> HookSelectors { get; init; }
        public bool IncludeDispatcher { get; init; }
        public bool IncludeFunctions { get; init; }
        public bool IncludeHooks { get; init; }
        public bool IncludeInterceptPass { get; init; }
        public bool IncludeWatchPass { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public int StackCaptureDwordCount { get; init; }
        public bool StopOnFailure { get; init; }
    }
    public sealed class AuditSnapshot : System.IEquatable<ArcNET.Diagnostics.AuditSnapshot>
    {
        public AuditSnapshot(System.DateTimeOffset GeneratedAtUtc, ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, ArcNET.Diagnostics.DispatcherAuditSnapshot? Dispatcher, ArcNET.Diagnostics.FunctionAuditSnapshot? Functions, ArcNET.Diagnostics.HookAuditSnapshot? Hooks, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.DispatcherAuditSnapshot? Dispatcher { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint { get; init; }
        public ArcNET.Diagnostics.FunctionAuditSnapshot? Functions { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public ArcNET.Diagnostics.HookAuditSnapshot? Hooks { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public sealed class BackgroundLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.BackgroundLogbookPageSnapshot>
    {
        public BackgroundLogbookPageSnapshot(int BackgroundId, int BackgroundTextId, string? Name, string? Body, string? CatalogName, string? CatalogBody, ArcNET.Diagnostics.NativeReadSnapshot BackgroundRead, ArcNET.Diagnostics.NativeReadSnapshot BackgroundTextRead) { }
        public int BackgroundId { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot BackgroundRead { get; init; }
        public int BackgroundTextId { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot BackgroundTextRead { get; init; }
        public string? Body { get; init; }
        public string? CatalogBody { get; init; }
        public string? CatalogName { get; init; }
        public string? Name { get; init; }
    }
    public readonly struct BlessingCurseLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.BlessingCurseLogbookEntrySnapshot>
    {
        public BlessingCurseLogbookEntrySnapshot(string Kind, int Id, ArcNET.Diagnostics.GameDateTimeSnapshot DateTime, string Name) { }
        public ArcNET.Diagnostics.GameDateTimeSnapshot DateTime { get; init; }
        public int Id { get; init; }
        public string Kind { get; init; }
        public string Name { get; init; }
    }
    public sealed class BlessingCurseLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.BlessingCurseLogbookPageSnapshot>
    {
        public BlessingCurseLogbookPageSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.BlessingCurseLogbookEntrySnapshot> Entries, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.NativeReadSnapshot> NativeReads) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.BlessingCurseLogbookEntrySnapshot> Entries { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.NativeReadSnapshot> NativeReads { get; init; }
    }
    public sealed class CeSourceAuditAreaSummary : System.IEquatable<ArcNET.Diagnostics.CeSourceAuditAreaSummary>
    {
        public CeSourceAuditAreaSummary(string Area, int FunctionCount, int CoveredCount, int MissingCount, int UniqueSymbolCount) { }
        public string Area { get; init; }
        public int CoveredCount { get; init; }
        public int FunctionCount { get; init; }
        public int MissingCount { get; init; }
        public int UniqueSymbolCount { get; init; }
    }
    public sealed class CeSourceAuditRequest : System.IEquatable<ArcNET.Diagnostics.CeSourceAuditRequest>
    {
        public CeSourceAuditRequest(string? SourceRoot, string? Filter, string? Area, int Limit = 200, bool MissingOnly = false, bool CoveredOnly = false) { }
        public string? Area { get; init; }
        public bool CoveredOnly { get; init; }
        public string? Filter { get; init; }
        public int Limit { get; init; }
        public bool MissingOnly { get; init; }
        public string? SourceRoot { get; init; }
    }
    public static class CeSourceAuditService
    {
        public static ArcNET.Diagnostics.CeSourceAuditSnapshot Create(ArcNET.Diagnostics.CeSourceAuditRequest request, ArcNET.Diagnostics.ModuleSymbolCatalog? symbolCatalog = null) { }
    }
    public sealed class CeSourceAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceAuditSnapshot>
    {
        public CeSourceAuditSnapshot(System.DateTimeOffset GeneratedAtUtc, string SourceRoot, bool AutoDetectedSourceRoot, string? Filter, string? Area, int Limit, bool MissingOnly, bool CoveredOnly, ArcNET.Diagnostics.CeSourceAuditSymbolCatalogSnapshot? SymbolCatalog, ArcNET.Diagnostics.CeSourceAuditSummarySnapshot Summary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CeSourceAuditAreaSummary> Areas, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CeSourceFunctionSnapshot> Functions) { }
        public string? Area { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CeSourceAuditAreaSummary> Areas { get; init; }
        public bool AutoDetectedSourceRoot { get; init; }
        public bool CoveredOnly { get; init; }
        public string? Filter { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CeSourceFunctionSnapshot> Functions { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public int Limit { get; init; }
        public bool MissingOnly { get; init; }
        public string SourceRoot { get; init; }
        public ArcNET.Diagnostics.CeSourceAuditSummarySnapshot Summary { get; init; }
        public ArcNET.Diagnostics.CeSourceAuditSymbolCatalogSnapshot? SymbolCatalog { get; init; }
    }
    public sealed class CeSourceAuditSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceAuditSummarySnapshot>
    {
        public CeSourceAuditSummarySnapshot(int FunctionCount, int UniqueNameCount, int DuplicateNameCount, int SelectionCount, int WatchHookCoverageCount, int DebuggerFunctionCoverageCount, int SignatureCoverageCount, int AnyCatalogCoverageCount, int MissingCoverageCount, int UniqueSymbolMatchCount, int AmbiguousSymbolMatchCount) { }
        public int AmbiguousSymbolMatchCount { get; init; }
        public int AnyCatalogCoverageCount { get; init; }
        public int DebuggerFunctionCoverageCount { get; init; }
        public int DuplicateNameCount { get; init; }
        public int FunctionCount { get; init; }
        public int MissingCoverageCount { get; init; }
        public int SelectionCount { get; init; }
        public int SignatureCoverageCount { get; init; }
        public int UniqueNameCount { get; init; }
        public int UniqueSymbolMatchCount { get; init; }
        public int WatchHookCoverageCount { get; init; }
    }
    public sealed class CeSourceAuditSymbolCatalogSnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceAuditSymbolCatalogSnapshot>
    {
        public CeSourceAuditSymbolCatalogSnapshot(string ModulePath, string ModuleFileName, int FunctionCount, int UniqueNameCount, int DuplicateNameCount) { }
        public int DuplicateNameCount { get; init; }
        public int FunctionCount { get; init; }
        public string ModuleFileName { get; init; }
        public string ModulePath { get; init; }
        public int UniqueNameCount { get; init; }
    }
    public sealed class CeSourceCoverageSnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceCoverageSnapshot>
    {
        public CeSourceCoverageSnapshot(bool WatchHookCoverage, bool DebuggerFunctionCoverage, bool SignatureCoverage, bool AnyCatalogCoverage) { }
        public bool AnyCatalogCoverage { get; init; }
        public bool DebuggerFunctionCoverage { get; init; }
        public bool SignatureCoverage { get; init; }
        public bool WatchHookCoverage { get; init; }
    }
    public sealed class CeSourceFunctionSnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceFunctionSnapshot>
    {
        public CeSourceFunctionSnapshot(string Name, string RelativePath, int LineNumber, string Area, bool IsStatic, string Signature, ArcNET.Diagnostics.CeSourceCoverageSnapshot Coverage, ArcNET.Diagnostics.CeSourceSymbolCoverageSnapshot Symbol) { }
        public string Area { get; init; }
        public ArcNET.Diagnostics.CeSourceCoverageSnapshot Coverage { get; init; }
        public bool IsStatic { get; init; }
        public int LineNumber { get; init; }
        public string Name { get; init; }
        public string RelativePath { get; init; }
        public string Signature { get; init; }
        public ArcNET.Diagnostics.CeSourceSymbolCoverageSnapshot Symbol { get; init; }
    }
    public sealed class CeSourceSymbolCoverageSnapshot : System.IEquatable<ArcNET.Diagnostics.CeSourceSymbolCoverageSnapshot>
    {
        public CeSourceSymbolCoverageSnapshot(bool UniqueSymbolMatch, int MatchCount, System.Collections.Generic.IReadOnlyList<string> SampleSites) { }
        public int MatchCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> SampleSites { get; init; }
        public bool UniqueSymbolMatch { get; init; }
    }
    public sealed class CrashDumpAutoConfigurationSnapshot : System.IEquatable<ArcNET.Diagnostics.CrashDumpAutoConfigurationSnapshot>
    {
        public CrashDumpAutoConfigurationSnapshot(bool IsEnabled, string ProcessExecutableName, string Scope, string RegistryPath, string? DumpFolder, ArcNET.Diagnostics.CrashDumpKind? DumpKind, int? DumpCount) { }
        public int? DumpCount { get; init; }
        public string? DumpFolder { get; init; }
        public ArcNET.Diagnostics.CrashDumpKind? DumpKind { get; init; }
        public bool IsEnabled { get; init; }
        public string ProcessExecutableName { get; init; }
        public string RegistryPath { get; init; }
        public string Scope { get; init; }
    }
    public enum CrashDumpKind
    {
        Mini = 0,
        Full = 1,
    }
    public sealed class CrashDumpWriteSnapshot : System.IEquatable<ArcNET.Diagnostics.CrashDumpWriteSnapshot>
    {
        public CrashDumpWriteSnapshot(System.DateTimeOffset GeneratedAtUtc, int ProcessId, string ProcessName, string ModulePath, string ModuleBase, string OutputPath, ArcNET.Diagnostics.CrashDumpKind DumpKind) { }
        public ArcNET.Diagnostics.CrashDumpKind DumpKind { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public string ModuleBase { get; init; }
        public string ModulePath { get; init; }
        public string OutputPath { get; init; }
        public int ProcessId { get; init; }
        public string ProcessName { get; init; }
    }
    public sealed class DashboardRequest : System.IEquatable<ArcNET.Diagnostics.DashboardRequest>
    {
        public DashboardRequest(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols, System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames) { }
        public bool HasModuleSymbols { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class DashboardService
    {
        public static ArcNET.Diagnostics.DashboardSnapshot Create(ArcNET.Diagnostics.DashboardRequest request) { }
    }
    public sealed class DashboardSnapshot : System.IEquatable<ArcNET.Diagnostics.DashboardSnapshot>
    {
        public DashboardSnapshot(System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames, ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.Contracts.ProbeProfile> RecommendedProbeProfiles, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PanelDescriptor> RecommendedPanels, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PanelDescriptor> RecommendedPanels { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.Contracts.ProbeProfile> RecommendedProbeProfiles { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames { get; init; }
    }
    public static class DiagnosticsCapabilityPolicy
    {
        public static ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Create(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot profile, bool hasModuleSymbols = false) { }
    }
    public sealed class DispatcherAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.DispatcherAuditSnapshot>
    {
        public DispatcherAuditSnapshot(bool Success, string? Mode, string? Site, string? Error) { }
        public string? Error { get; init; }
        public string? Mode { get; init; }
        public string? Site { get; init; }
        public bool Success { get; init; }
    }
    public readonly struct DispatcherCandidateDefinition : System.IEquatable<ArcNET.Diagnostics.DispatcherCandidateDefinition>
    {
        public DispatcherCandidateDefinition(string Key, int Rva, string Site) { }
        public string Key { get; init; }
        public int Rva { get; init; }
        public string Site { get; init; }
    }
    public sealed class EnvironmentRequest : System.IEquatable<ArcNET.Diagnostics.EnvironmentRequest>
    {
        public EnvironmentRequest(System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames, string? InstallPath, ArcNET.Patch.ArcanumExecutableKind LaunchExecutableKind, bool LaunchWindowed) { }
        public string? InstallPath { get; init; }
        public ArcNET.Patch.ArcanumExecutableKind LaunchExecutableKind { get; init; }
        public bool LaunchWindowed { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames { get; init; }
    }
    public sealed class EnvironmentSnapshot : System.IEquatable<ArcNET.Diagnostics.EnvironmentSnapshot>
    {
        public EnvironmentSnapshot(System.DateTimeOffset GeneratedAtUtc, System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ProcessCandidateSnapshot> ProcessCandidates, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.LiveRuntimeSnapshot> LiveRuntimes, bool CanAttachSingleRuntime, string AttachSummary, ArcNET.Diagnostics.LaunchPreviewSnapshot? LaunchPreview, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public string AttachSummary { get; init; }
        public bool CanAttachSingleRuntime { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public ArcNET.Diagnostics.LaunchPreviewSnapshot? LaunchPreview { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.LiveRuntimeSnapshot> LiveRuntimes { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ProcessCandidateSnapshot> ProcessCandidates { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames { get; init; }
    }
    public sealed class FunctionAuditResultSnapshot : System.IEquatable<ArcNET.Diagnostics.FunctionAuditResultSnapshot>
    {
        public FunctionAuditResultSnapshot(string Key, bool Success, string? Site, string? Resolution, string? Summary, string? Error) { }
        public string? Error { get; init; }
        public string Key { get; init; }
        public string? Resolution { get; init; }
        public string? Site { get; init; }
        public bool Success { get; init; }
        public string? Summary { get; init; }
    }
    public sealed class FunctionAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.FunctionAuditSnapshot>
    {
        public FunctionAuditSnapshot(int TotalFunctions, int ResolvedFunctions, int FailedFunctions, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionAuditResultSnapshot> Results) { }
        public int FailedFunctions { get; init; }
        public int ResolvedFunctions { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionAuditResultSnapshot> Results { get; init; }
        public int TotalFunctions { get; init; }
    }
    public sealed class FunctionBrowserRequest : System.IEquatable<ArcNET.Diagnostics.FunctionBrowserRequest>
    {
        public FunctionBrowserRequest(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols) { }
        public bool HasModuleSymbols { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class FunctionBrowserService
    {
        public static ArcNET.Diagnostics.FunctionBrowserSnapshot Create(ArcNET.Diagnostics.FunctionBrowserRequest request) { }
    }
    public sealed class FunctionBrowserSnapshot : System.IEquatable<ArcNET.Diagnostics.FunctionBrowserSnapshot>
    {
        public FunctionBrowserSnapshot(ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.DispatcherCandidateDefinition> DispatcherCandidates, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionDefinition> Functions, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.DispatcherCandidateDefinition> DispatcherCandidates { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionDefinition> Functions { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
    }
    public sealed class FunctionCallArgumentSnapshot : System.IEquatable<ArcNET.Diagnostics.FunctionCallArgumentSnapshot>
    {
        public FunctionCallArgumentSnapshot(int Index, string ValueText, string SourceText) { }
        public int Index { get; init; }
        public string SourceText { get; init; }
        public string ValueText { get; init; }
    }
    public sealed class FunctionCallExecutionResult : System.IEquatable<ArcNET.Diagnostics.FunctionCallExecutionResult>
    {
        public FunctionCallExecutionResult(string DispatcherMode, string DispatcherSite, string TargetAddressText, uint ResultEax, uint ResultEdx, string CompletionState) { }
        public string CompletionState { get; init; }
        public string DispatcherMode { get; init; }
        public string DispatcherSite { get; init; }
        public uint ResultEax { get; init; }
        public uint ResultEdx { get; init; }
        public string TargetAddressText { get; init; }
    }
    public sealed class FunctionCallRequest : System.IEquatable<ArcNET.Diagnostics.FunctionCallRequest>
    {
        public FunctionCallRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string TargetText, string StackArgumentsText, string EcxValueText, string EdxValueText, bool UseSuggestedCleanup, ArcNET.Diagnostics.StackCleanupMode OverrideCleanupMode, string TimeoutMillisecondsText) { }
        public string EcxValueText { get; init; }
        public string EdxValueText { get; init; }
        public ArcNET.Diagnostics.StackCleanupMode OverrideCleanupMode { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public string StackArgumentsText { get; init; }
        public string TargetText { get; init; }
        public string TimeoutMillisecondsText { get; init; }
        public bool UseSuggestedCleanup { get; init; }
    }
    public sealed class FunctionCallSnapshot : System.IEquatable<ArcNET.Diagnostics.FunctionCallSnapshot>
    {
        public FunctionCallSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string TargetKey, string TargetSite, string CleanupModeText, string DispatcherText, string TargetAddressText, string ResultEaxText, string ResultEdxText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionCallArgumentSnapshot> Arguments) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionCallArgumentSnapshot> Arguments { get; init; }
        public string CleanupModeText { get; init; }
        public string DispatcherText { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public string ResultEaxText { get; init; }
        public string ResultEdxText { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetAddressText { get; init; }
        public string TargetKey { get; init; }
        public string TargetSite { get; init; }
    }
    public static class FunctionCatalog
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.DispatcherCandidateDefinition> DispatcherCandidates { get; }
        public static System.Collections.Generic.IReadOnlyList<string> KnownFunctionKeys { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.FunctionDefinition> KnownFunctions { get; }
        public static ArcNET.Diagnostics.FunctionDefinition GetDefinition(string token) { }
        public static bool HasKnownFunction(string token) { }
        public static bool TryGetDefinition(string token, out ArcNET.Diagnostics.FunctionDefinition definition) { }
    }
    public readonly struct FunctionDefinition : System.IEquatable<ArcNET.Diagnostics.FunctionDefinition>
    {
        public FunctionDefinition(string Key, int Rva, string Site, string Summary, ArcNET.Diagnostics.StackCleanupMode SuggestedCleanup, string? Example) { }
        public string? Example { get; init; }
        public string Key { get; init; }
        public int Rva { get; init; }
        public string Site { get; init; }
        public ArcNET.Diagnostics.StackCleanupMode SuggestedCleanup { get; init; }
        public string Summary { get; init; }
    }
    public readonly struct GameDateTimeSnapshot : System.IEquatable<ArcNET.Diagnostics.GameDateTimeSnapshot>
    {
        public GameDateTimeSnapshot(uint Days, uint Milliseconds) { }
        public uint Days { get; init; }
        public uint Milliseconds { get; init; }
        public long SortKey { get; }
    }
    public static class GuidedActionCatalog
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.GuidedActionDescriptor> Actions { get; }
        public static ArcNET.Diagnostics.GuidedActionDescriptor GetDescriptor(string key) { }
        public static bool TryGetDescriptor(string key, out ArcNET.Diagnostics.GuidedActionDescriptor descriptor) { }
    }
    public sealed class GuidedActionDescriptor : System.IEquatable<ArcNET.Diagnostics.GuidedActionDescriptor>
    {
        public GuidedActionDescriptor(string Key, string DisplayName, string Summary, string FunctionKey) { }
        public string DisplayName { get; init; }
        public string FunctionKey { get; init; }
        public string Key { get; init; }
        public string Summary { get; init; }
    }
    public sealed class GuidedActionRequest : System.IEquatable<ArcNET.Diagnostics.GuidedActionRequest>
    {
        public GuidedActionRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string ActionKey, string TravelerToken, string TileXText, string TileYText, string MapIdText, string FlagsText, string TimeoutMillisecondsText) { }
        public string ActionKey { get; init; }
        public string FlagsText { get; init; }
        public string MapIdText { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public string TileXText { get; init; }
        public string TileYText { get; init; }
        public string TimeoutMillisecondsText { get; init; }
        public string TravelerToken { get; init; }
    }
    public sealed class GuidedActionSnapshot : System.IEquatable<ArcNET.Diagnostics.GuidedActionSnapshot>
    {
        public GuidedActionSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string ActionKey, string ActionDisplayName, string FunctionKey, string FunctionSite, string DispatcherText, string ExecutionDetailText, string ResultText) { }
        public string ActionDisplayName { get; init; }
        public string ActionKey { get; init; }
        public string DispatcherText { get; init; }
        public string ExecutionDetailText { get; init; }
        public string FunctionKey { get; init; }
        public string FunctionSite { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public string ResultText { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
    }
    public sealed class HookAuditResultSnapshot : System.IEquatable<ArcNET.Diagnostics.HookAuditResultSnapshot>
    {
        public HookAuditResultSnapshot(string Key, string Area, ArcNET.Diagnostics.HookBindAuditSnapshot Bind, ArcNET.Diagnostics.HookPassAuditSnapshot? Watch, ArcNET.Diagnostics.HookPassAuditSnapshot? Intercept) { }
        public string Area { get; init; }
        public ArcNET.Diagnostics.HookBindAuditSnapshot Bind { get; init; }
        public ArcNET.Diagnostics.HookPassAuditSnapshot? Intercept { get; init; }
        public string Key { get; init; }
        public ArcNET.Diagnostics.HookPassAuditSnapshot? Watch { get; init; }
    }
    public sealed class HookAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.HookAuditSnapshot>
    {
        public HookAuditSnapshot(
                    System.Collections.Generic.IReadOnlyList<string> Selectors,
                    int DurationMilliseconds,
                    bool IncludeWatch,
                    bool IncludeIntercept,
                    int StackCaptureDwordCount,
                    int AuditedHookCount,
                    int BoundHookCount,
                    int BindFailureCount,
                    int WatchSuccessCount,
                    int WatchFailureCount,
                    int InterceptSuccessCount,
                    int InterceptFailureCount,
                    int WatchObservedEventCount,
                    int InterceptObservedEventCount,
                    int WatchDroppedEventHookCount,
                    int InterceptDroppedEventHookCount,
                    bool ProcessExited,
                    string? AbortedAtHook,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.HookAuditResultSnapshot> Hooks) { }
        public string? AbortedAtHook { get; init; }
        public int AuditedHookCount { get; init; }
        public int BindFailureCount { get; init; }
        public int BoundHookCount { get; init; }
        public int DurationMilliseconds { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.HookAuditResultSnapshot> Hooks { get; init; }
        public bool IncludeIntercept { get; init; }
        public bool IncludeWatch { get; init; }
        public int InterceptDroppedEventHookCount { get; init; }
        public int InterceptFailureCount { get; init; }
        public int InterceptObservedEventCount { get; init; }
        public int InterceptSuccessCount { get; init; }
        public bool ProcessExited { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Selectors { get; init; }
        public int StackCaptureDwordCount { get; init; }
        public int WatchDroppedEventHookCount { get; init; }
        public int WatchFailureCount { get; init; }
        public int WatchObservedEventCount { get; init; }
        public int WatchSuccessCount { get; init; }
    }
    public sealed class HookBindAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.HookBindAuditSnapshot>
    {
        public HookBindAuditSnapshot(bool Success, string Site, string? Error) { }
        public string? Error { get; init; }
        public string Site { get; init; }
        public bool Success { get; init; }
    }
    public sealed class HookPassAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.HookPassAuditSnapshot>
    {
        public HookPassAuditSnapshot(bool Success, bool ObservedEvents, int EventCount, int DroppedEvents, int InconsistentRecords, int ContentionDrops, string? FirstCallerSite, string? Error) { }
        public int ContentionDrops { get; init; }
        public int DroppedEvents { get; init; }
        public string? Error { get; init; }
        public int EventCount { get; init; }
        public string? FirstCallerSite { get; init; }
        public int InconsistentRecords { get; init; }
        public bool ObservedEvents { get; init; }
        public bool Success { get; init; }
    }
    public readonly struct InjuryLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.InjuryLogbookEntrySnapshot>
    {
        public InjuryLogbookEntrySnapshot(int SlotIndex, int DescriptionId, string SourceName, int InjuryType, string InjuryTypeName, bool Active, string StateText, string SummaryText) { }
        public bool Active { get; init; }
        public int DescriptionId { get; init; }
        public int InjuryType { get; init; }
        public string InjuryTypeName { get; init; }
        public int SlotIndex { get; init; }
        public string SourceName { get; init; }
        public string StateText { get; init; }
        public string SummaryText { get; init; }
    }
    public sealed class InterceptArgumentOverrideRequest : System.IEquatable<ArcNET.Diagnostics.InterceptArgumentOverrideRequest>
    {
        public InterceptArgumentOverrideRequest(int Index, uint Value) { }
        public int Index { get; init; }
        public uint Value { get; init; }
    }
    public sealed class InterceptDereferenceRequest : System.IEquatable<ArcNET.Diagnostics.InterceptDereferenceRequest>
    {
        public InterceptDereferenceRequest(string Source, ArcNET.Diagnostics.InterceptDereferenceSourceKind SourceKind, int Index, int ByteCount) { }
        public int ByteCount { get; init; }
        public int Index { get; init; }
        public string Source { get; init; }
        public ArcNET.Diagnostics.InterceptDereferenceSourceKind SourceKind { get; init; }
    }
    public sealed class InterceptDereferenceSnapshot : System.IEquatable<ArcNET.Diagnostics.InterceptDereferenceSnapshot>
    {
        public InterceptDereferenceSnapshot(string Source, string AddressText, int RequestedByteCount, int ReadByteCount, string Hex, string Ascii, System.Collections.Generic.IReadOnlyList<string> UInt32Preview, string? Error) { }
        public string AddressText { get; init; }
        public string Ascii { get; init; }
        public string? Error { get; init; }
        public string Hex { get; init; }
        public int ReadByteCount { get; init; }
        public int RequestedByteCount { get; init; }
        public string Source { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> UInt32Preview { get; init; }
    }
    public enum InterceptDereferenceSourceKind
    {
        Eax = 0,
        Ecx = 1,
        Edx = 2,
        Ebx = 3,
        Esi = 4,
        Edi = 5,
        Ebp = 6,
        OriginalEsp = 7,
        StackIndex = 8,
    }
    public sealed class InterceptEventSnapshot : System.IEquatable<ArcNET.Diagnostics.InterceptEventSnapshot>
    {
        public InterceptEventSnapshot(System.DateTimeOffset TimestampUtc, uint Sequence, string CallerSite, string ReturnAddressText, string CallerRvaText, string EflagsText, ArcNET.Diagnostics.InterceptRegistersSnapshot Registers, System.Collections.Generic.IReadOnlyList<string> StackDwords, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptPotentialHandleSnapshot> PotentialHandles, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptDereferenceSnapshot> Dereferences) { }
        public string CallerRvaText { get; init; }
        public string CallerSite { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptDereferenceSnapshot> Dereferences { get; init; }
        public string EflagsText { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptPotentialHandleSnapshot> PotentialHandles { get; init; }
        public ArcNET.Diagnostics.InterceptRegistersSnapshot Registers { get; init; }
        public string ReturnAddressText { get; init; }
        public uint Sequence { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> StackDwords { get; init; }
        public System.DateTimeOffset TimestampUtc { get; init; }
    }
    public sealed class InterceptMutationRequest : System.IEquatable<ArcNET.Diagnostics.InterceptMutationRequest>
    {
        public InterceptMutationRequest(bool SkipOriginal, int CleanupBytes, uint? ReturnEax, uint? ReturnEdx, ArcNET.Diagnostics.InterceptRegisterOverrideRequest Registers, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptArgumentOverrideRequest> ArgumentOverrides) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptArgumentOverrideRequest> ArgumentOverrides { get; init; }
        public int CleanupBytes { get; init; }
        public ArcNET.Diagnostics.InterceptRegisterOverrideRequest Registers { get; init; }
        public uint? ReturnEax { get; init; }
        public uint? ReturnEdx { get; init; }
        public bool SkipOriginal { get; init; }
    }
    public sealed class InterceptPotentialHandleSnapshot : System.IEquatable<ArcNET.Diagnostics.InterceptPotentialHandleSnapshot>
    {
        public InterceptPotentialHandleSnapshot(int StackIndex, string HandleText) { }
        public string HandleText { get; init; }
        public int StackIndex { get; init; }
    }
    public sealed class InterceptRegisterOverrideRequest : System.IEquatable<ArcNET.Diagnostics.InterceptRegisterOverrideRequest>
    {
        public InterceptRegisterOverrideRequest(uint? Edi, uint? Esi, uint? Ebp, uint? Ebx, uint? Edx, uint? Ecx, uint? Eax) { }
        public uint? Eax { get; init; }
        public uint? Ebp { get; init; }
        public uint? Ebx { get; init; }
        public uint? Ecx { get; init; }
        public uint? Edi { get; init; }
        public uint? Edx { get; init; }
        public uint? Esi { get; init; }
    }
    public sealed class InterceptRegistersSnapshot : System.IEquatable<ArcNET.Diagnostics.InterceptRegistersSnapshot>
    {
        public InterceptRegistersSnapshot(string Edi, string Esi, string Ebp, string OriginalEsp, string Ebx, string Edx, string Ecx, string Eax) { }
        public string Eax { get; init; }
        public string Ebp { get; init; }
        public string Ebx { get; init; }
        public string Ecx { get; init; }
        public string Edi { get; init; }
        public string Edx { get; init; }
        public string Esi { get; init; }
        public string OriginalEsp { get; init; }
    }
    public sealed class InterceptSnapshot : System.IEquatable<ArcNET.Diagnostics.InterceptSnapshot>
    {
        public InterceptSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsRunning, string Status, string Summary, string TargetKey, string TargetSite, string TargetSummary, string TargetResolution, string ExecutionModeText, int StackCaptureDwordCount, int TotalEvents, int TotalDroppedEvents, int TotalContentionDrops, int TotalWarnings, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptEventSnapshot> Events) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptEventSnapshot> Events { get; init; }
        public string ExecutionModeText { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsRunning { get; init; }
        public int StackCaptureDwordCount { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetKey { get; init; }
        public string TargetResolution { get; init; }
        public string TargetSite { get; init; }
        public string TargetSummary { get; init; }
        public int TotalContentionDrops { get; init; }
        public int TotalDroppedEvents { get; init; }
        public int TotalEvents { get; init; }
        public int TotalWarnings { get; init; }
    }
    public sealed class InterceptStartRequest : System.IEquatable<ArcNET.Diagnostics.InterceptStartRequest>
    {
        public InterceptStartRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, ArcNET.Diagnostics.InterceptTarget Target, int StackCaptureDwordCount, ArcNET.Diagnostics.InterceptMutationRequest Mutation, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptDereferenceRequest> Dereferences) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InterceptDereferenceRequest> Dereferences { get; init; }
        public ArcNET.Diagnostics.InterceptMutationRequest Mutation { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public int StackCaptureDwordCount { get; init; }
        public ArcNET.Diagnostics.InterceptTarget Target { get; init; }
    }
    public sealed class InterceptTarget : System.IEquatable<ArcNET.Diagnostics.InterceptTarget>
    {
        public InterceptTarget(string Key, uint Address, uint? Rva, string Site, string Summary, string Resolution) { }
        public uint Address { get; init; }
        public string Key { get; init; }
        public string Resolution { get; init; }
        public uint? Rva { get; init; }
        public string Site { get; init; }
        public string Summary { get; init; }
    }
    public readonly struct KeyringLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.KeyringLogbookEntrySnapshot>
    {
        public KeyringLogbookEntrySnapshot(int Index, int KeyId, string Name) { }
        public int Index { get; init; }
        public int KeyId { get; init; }
        public string Name { get; init; }
    }
    public sealed class KeyringLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.KeyringLogbookPageSnapshot>
    {
        public KeyringLogbookPageSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.KeyringLogbookEntrySnapshot> Entries) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.KeyringLogbookEntrySnapshot> Entries { get; init; }
    }
    public readonly struct KillLogbookSummaryEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.KillLogbookSummaryEntrySnapshot>
    {
        public KillLogbookSummaryEntrySnapshot(string Key, string Label, int DescriptionId, string? Name, int Value) { }
        public int DescriptionId { get; init; }
        public string Key { get; init; }
        public string Label { get; init; }
        public string? Name { get; init; }
        public int Value { get; init; }
    }
    public sealed class KillsAndInjuriesLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.KillsAndInjuriesLogbookPageSnapshot>
    {
        public KillsAndInjuriesLogbookPageSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.KillLogbookSummaryEntrySnapshot> Summary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InjuryLogbookEntrySnapshot> Injuries, ArcNET.Diagnostics.NativeReadSnapshot NativeRead) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.InjuryLogbookEntrySnapshot> Injuries { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot NativeRead { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.KillLogbookSummaryEntrySnapshot> Summary { get; init; }
    }
    public sealed class LaunchPreviewSnapshot : System.IEquatable<ArcNET.Diagnostics.LaunchPreviewSnapshot>
    {
        public LaunchPreviewSnapshot(bool CanLaunch, string Summary, string? Error, ArcNET.Patch.ArcanumExecutableKind? ExecutableKind, string? ExecutablePath, string? WorkingDirectory, System.Collections.Generic.IReadOnlyList<string> Arguments, System.Collections.Generic.IReadOnlyList<string> EnvironmentVariables) { }
        public System.Collections.Generic.IReadOnlyList<string> Arguments { get; init; }
        public bool CanLaunch { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> EnvironmentVariables { get; init; }
        public string? Error { get; init; }
        public ArcNET.Patch.ArcanumExecutableKind? ExecutableKind { get; init; }
        public string? ExecutablePath { get; init; }
        public string Summary { get; init; }
        public string? WorkingDirectory { get; init; }
    }
    public sealed class LaunchSessionRequest : System.IEquatable<ArcNET.Diagnostics.LaunchSessionRequest>
    {
        public LaunchSessionRequest(string InstallPath, ArcNET.Patch.ArcanumExecutableKind ExecutableKind, bool LaunchWindowed, System.TimeSpan? AttachTimeout = default) { }
        public System.TimeSpan? AttachTimeout { get; init; }
        public ArcNET.Patch.ArcanumExecutableKind ExecutableKind { get; init; }
        public string InstallPath { get; init; }
        public bool LaunchWindowed { get; init; }
    }
    public sealed class LiveRuntimeSnapshot : System.IEquatable<ArcNET.Diagnostics.LiveRuntimeSnapshot>
    {
        public LiveRuntimeSnapshot(string ScenarioKey, string DisplayName, string Summary, string ProcessName, int ProcessId, ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities) { }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public string DisplayName { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint { get; init; }
        public int ProcessId { get; init; }
        public string ProcessName { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
        public string ScenarioKey { get; init; }
        public string Summary { get; init; }
    }
    public enum LogbookPage
    {
        All = 0,
        RumorsAndNotes = 1,
        Quests = 2,
        Reputations = 3,
        BlessingsAndCurses = 4,
        KillsAndInjuries = 5,
        Background = 6,
        KeyringContents = 7,
    }
    public sealed class LogbookPayload : System.IEquatable<ArcNET.Diagnostics.LogbookPayload>
    {
        public LogbookPayload(ArcNET.Diagnostics.RumorLogbookPageSnapshot? RumorsAndNotes, ArcNET.Diagnostics.QuestLogbookPageSnapshot? Quests, ArcNET.Diagnostics.ReputationLogbookPageSnapshot? Reputations, ArcNET.Diagnostics.BlessingCurseLogbookPageSnapshot? BlessingsAndCurses, ArcNET.Diagnostics.KillsAndInjuriesLogbookPageSnapshot? KillsAndInjuries, ArcNET.Diagnostics.BackgroundLogbookPageSnapshot? Background, ArcNET.Diagnostics.KeyringLogbookPageSnapshot? KeyringContents) { }
        public ArcNET.Diagnostics.BackgroundLogbookPageSnapshot? Background { get; init; }
        public ArcNET.Diagnostics.BlessingCurseLogbookPageSnapshot? BlessingsAndCurses { get; init; }
        public ArcNET.Diagnostics.KeyringLogbookPageSnapshot? KeyringContents { get; init; }
        public ArcNET.Diagnostics.KillsAndInjuriesLogbookPageSnapshot? KillsAndInjuries { get; init; }
        public ArcNET.Diagnostics.QuestLogbookPageSnapshot? Quests { get; init; }
        public ArcNET.Diagnostics.ReputationLogbookPageSnapshot? Reputations { get; init; }
        public ArcNET.Diagnostics.RumorLogbookPageSnapshot? RumorsAndNotes { get; init; }
    }
    public sealed class LogbookReadResult : System.IEquatable<ArcNET.Diagnostics.LogbookReadResult>
    {
        public LogbookReadResult(ArcNET.Diagnostics.LogbookPayload Data, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.LogbookPayload Data { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
    }
    public sealed class LogbookRequest : System.IEquatable<ArcNET.Diagnostics.LogbookRequest>
    {
        public LogbookRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string HandleToken, string PageToken = "all") { }
        public string HandleToken { get; init; }
        public string PageToken { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class LogbookSnapshot : System.IEquatable<ArcNET.Diagnostics.LogbookSnapshot>
    {
        public LogbookSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string RequestedPageToken, ArcNET.Diagnostics.LogbookPage? Page, string TargetHandleText, string TargetText, ArcNET.Diagnostics.LogbookPayload Data, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.LogbookPayload Data { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public ArcNET.Diagnostics.LogbookPage? Page { get; init; }
        public string RequestedPageToken { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetHandleText { get; init; }
        public string TargetText { get; init; }
    }
    public sealed class ModuleSymbolEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.ModuleSymbolEntrySnapshot>
    {
        public ModuleSymbolEntrySnapshot(string Name, string Site, uint Rva, string RvaText, string? Address, uint Size, string SizeText, int DuplicateNameCount) { }
        public string? Address { get; init; }
        public int DuplicateNameCount { get; init; }
        public string Name { get; init; }
        public uint Rva { get; init; }
        public string RvaText { get; init; }
        public string Site { get; init; }
        public uint Size { get; init; }
        public string SizeText { get; init; }
    }
    public sealed class ModuleSymbolQueryRequest : System.IEquatable<ArcNET.Diagnostics.ModuleSymbolQueryRequest>
    {
        public ModuleSymbolQueryRequest(string? Filter, int Limit = 100, bool DuplicatesOnly = false) { }
        public bool DuplicatesOnly { get; init; }
        public string? Filter { get; init; }
        public int Limit { get; init; }
    }
    public sealed class ModuleSymbolQuerySnapshot : System.IEquatable<ArcNET.Diagnostics.ModuleSymbolQuerySnapshot>
    {
        public ModuleSymbolQuerySnapshot(System.DateTimeOffset GeneratedAtUtc, string ModulePath, string ModuleFileName, string? ModuleBase, ArcNET.Diagnostics.Contracts.RuntimeFingerprint? Fingerprint, string? Filter, int Limit, bool DuplicatesOnly, int FunctionCount, int UniqueNameCount, int DuplicateNameCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ModuleSymbolEntrySnapshot> Symbols) { }
        public int DuplicateNameCount { get; init; }
        public bool DuplicatesOnly { get; init; }
        public string? Filter { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeFingerprint? Fingerprint { get; init; }
        public int FunctionCount { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public int Limit { get; init; }
        public string? ModuleBase { get; init; }
        public string ModuleFileName { get; init; }
        public string ModulePath { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ModuleSymbolEntrySnapshot> Symbols { get; init; }
        public int UniqueNameCount { get; init; }
    }
    public sealed class NativeReadSnapshot : System.IEquatable<ArcNET.Diagnostics.NativeReadSnapshot>
    {
        public NativeReadSnapshot(string FunctionKey, string FunctionSite, string FunctionSummary, string DispatcherMode, string DispatcherSite, string CompletionState, int Int32Value, string ResultEaxText, string ResultEdxText) { }
        public string CompletionState { get; init; }
        public string DispatcherMode { get; init; }
        public string DispatcherSite { get; init; }
        public string FunctionKey { get; init; }
        public string FunctionSite { get; init; }
        public string FunctionSummary { get; init; }
        public int Int32Value { get; init; }
        public string ResultEaxText { get; init; }
        public string ResultEdxText { get; init; }
    }
    public sealed class ObjectExplorerRequest : System.IEquatable<ArcNET.Diagnostics.ObjectExplorerRequest>
    {
        public ObjectExplorerRequest(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols) { }
        public bool HasModuleSymbols { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class ObjectExplorerService
    {
        public static ArcNET.Diagnostics.ObjectExplorerSnapshot Create(ArcNET.Diagnostics.ObjectExplorerRequest request) { }
    }
    public sealed class ObjectExplorerSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectExplorerSnapshot>
    {
        public ObjectExplorerSnapshot(ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldGroupDescriptor> RecommendedGroups, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldGroupDescriptor> AllGroups, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldGroupDescriptor> AllGroups { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldGroupDescriptor> RecommendedGroups { get; init; }
    }
    public static class ObjectFieldCatalog
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldDescriptor> Fields { get; }
        public static string ArrayElementName(int fieldId, int index) { }
        public static string BasicSkillName(int index) { }
        public static string CollectionName(int fieldId) { }
        public static string DisplayName(int fieldId) { }
        public static bool IsNoiseField(int fieldId) { }
        public static string RawName(int fieldId) { }
        public static string ResistanceName(int index) { }
        public static string SpellCollegeName(int index) { }
        public static string TechSkillName(int index) { }
        public static string TrainingName(int training) { }
        public static bool TryGetFieldId(string rawName, out int fieldId) { }
    }
    public readonly struct ObjectFieldDescriptor : System.IEquatable<ArcNET.Diagnostics.ObjectFieldDescriptor>
    {
        public ObjectFieldDescriptor(int FieldId, string RawName, string DisplayName, string CollectionName, bool IsNoise) { }
        public string CollectionName { get; init; }
        public string DisplayName { get; init; }
        public int FieldId { get; init; }
        public bool IsNoise { get; init; }
        public string RawName { get; init; }
    }
    public sealed class ObjectFieldGroupDescriptor : System.IEquatable<ArcNET.Diagnostics.ObjectFieldGroupDescriptor>
    {
        public ObjectFieldGroupDescriptor(string Key, string DisplayName, string Description, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldDescriptor> Fields, int NoiseFieldCount) { }
        public string Description { get; init; }
        public string DisplayName { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldDescriptor> Fields { get; init; }
        public string Key { get; init; }
        public int NoiseFieldCount { get; init; }
    }
    public sealed class ObjectProbeDetailSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectProbeDetailSnapshot>
    {
        public ObjectProbeDetailSnapshot(string Label, string Value, string ResolutionSource) { }
        public string Label { get; init; }
        public string ResolutionSource { get; init; }
        public string Value { get; init; }
    }
    public sealed class ObjectProbeObjectSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectProbeObjectSnapshot>
    {
        public ObjectProbeObjectSnapshot(string HandleHex, string ResolutionSource, string ObjectTypeText, string ObjectIdText, string PrototypeText, string PrototypeHandleText, string AddressText, string StatusText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeSectionSnapshot> Sections, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeDetailSnapshot> Details) { }
        public string AddressText { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeDetailSnapshot> Details { get; init; }
        public string HandleHex { get; init; }
        public string ObjectIdText { get; init; }
        public string ObjectTypeText { get; init; }
        public string PrototypeHandleText { get; init; }
        public string PrototypeText { get; init; }
        public string ResolutionSource { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeSectionSnapshot> Sections { get; init; }
        public string StatusText { get; init; }
    }
    public sealed class ObjectProbeRequest : System.IEquatable<ArcNET.Diagnostics.ObjectProbeRequest>
    {
        public ObjectProbeRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, System.Collections.Generic.IReadOnlyList<string> HandleTexts, string SourceLabel, int MaxObjects = 4) { }
        public System.Collections.Generic.IReadOnlyList<string> HandleTexts { get; init; }
        public int MaxObjects { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public string SourceLabel { get; init; }
    }
    public sealed class ObjectProbeSectionSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectProbeSectionSnapshot>
    {
        public ObjectProbeSectionSnapshot(string Key, string Title, string SourceText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeDetailSnapshot> Details) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeDetailSnapshot> Details { get; init; }
        public string Key { get; init; }
        public string SourceText { get; init; }
        public string Title { get; init; }
    }
    public sealed class ObjectProbeSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectProbeSnapshot>
    {
        public ObjectProbeSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string SourceLabel, System.Collections.Generic.IReadOnlyList<string> RequestedHandles, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeObjectSnapshot> Objects) { }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectProbeObjectSnapshot> Objects { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedHandles { get; init; }
        public string SourceLabel { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
    }
    public static class ObjectValueFormatter
    {
        public static string FormatArrayInt32(int fieldId, int index, int value) { }
        public static string FormatArrayUInt32(int fieldId, int index, uint value) { }
        public static string FormatFieldInt32(int fieldId, int value) { }
        public static string FormatSkillTraining(int training) { }
    }
    public sealed class PanelDescriptor : System.IEquatable<ArcNET.Diagnostics.PanelDescriptor>
    {
        public PanelDescriptor(string Key, string DisplayName, string Description) { }
        public string Description { get; init; }
        public string DisplayName { get; init; }
        public string Key { get; init; }
    }
    public static class ProbeCatalog
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.Contracts.ProbeProfile> Profiles { get; }
        public static string[] ExpandSelectors(System.Collections.Generic.IEnumerable<string> tokens) { }
        public static bool TryGetProfile(string key, out ArcNET.Diagnostics.Contracts.ProbeProfile profile) { }
    }
    public sealed class ProcessCandidateSnapshot : System.IEquatable<ArcNET.Diagnostics.ProcessCandidateSnapshot>
    {
        public ProcessCandidateSnapshot(string ProcessName, string DisplayName, bool IsRunning, int RunningInstanceCount, string StatusText) { }
        public string DisplayName { get; init; }
        public bool IsRunning { get; init; }
        public string ProcessName { get; init; }
        public int RunningInstanceCount { get; init; }
        public string StatusText { get; init; }
    }
    public sealed class PrototypePaletteEntry : System.IEquatable<ArcNET.Diagnostics.PrototypePaletteEntry>
    {
        public PrototypePaletteEntry(int ProtoNumber, string ObjectType, string AssetPath, string? DisplayName, string? Description, string? PaletteGroup, string? ArtAssetPath) { }
        public string? ArtAssetPath { get; init; }
        public string AssetPath { get; init; }
        public string? Description { get; init; }
        public string? DisplayName { get; init; }
        public string ObjectType { get; init; }
        public string? PaletteGroup { get; init; }
        public int ProtoNumber { get; init; }
    }
    public sealed class PrototypeResolutionRequest : System.IEquatable<ArcNET.Diagnostics.PrototypeResolutionRequest>
    {
        public PrototypeResolutionRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string PrototypeText) { }
        public string PrototypeText { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class PrototypeResolutionSnapshot : System.IEquatable<ArcNET.Diagnostics.PrototypeResolutionSnapshot>
    {
        public PrototypeResolutionSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string Token, int? ProtoNumber, string? DisplayName, string? AssetPath, ulong? Handle, string HandleText, string ResolutionSource, ArcNET.Diagnostics.ResolvedObjectSnapshot? ResolvedObject, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public string? AssetPath { get; init; }
        public string? DisplayName { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public ulong? Handle { get; init; }
        public string HandleText { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public int? ProtoNumber { get; init; }
        public string ResolutionSource { get; init; }
        public ArcNET.Diagnostics.ResolvedObjectSnapshot? ResolvedObject { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string Token { get; init; }
    }
    public readonly struct QuestLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.QuestLogbookEntrySnapshot>
    {
        public QuestLogbookEntrySnapshot(int QuestId, ArcNET.Diagnostics.GameDateTimeSnapshot DateTime, int State, string StateName, string Label, string? Description, string? NormalDescription, string? DumbDescription) { }
        public ArcNET.Diagnostics.GameDateTimeSnapshot DateTime { get; init; }
        public string? Description { get; init; }
        public string? DumbDescription { get; init; }
        public string Label { get; init; }
        public string? NormalDescription { get; init; }
        public int QuestId { get; init; }
        public int State { get; init; }
        public string StateName { get; init; }
    }
    public sealed class QuestLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.QuestLogbookPageSnapshot>
    {
        public QuestLogbookPageSnapshot(int Intelligence, bool UsesDumbText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.QuestLogbookEntrySnapshot> Entries, ArcNET.Diagnostics.NativeReadSnapshot NativeRead) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.QuestLogbookEntrySnapshot> Entries { get; init; }
        public int Intelligence { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot NativeRead { get; init; }
        public bool UsesDumbText { get; init; }
    }
    public sealed class ReadRequest : System.IEquatable<ArcNET.Diagnostics.ReadRequest>
    {
        public ReadRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string AdapterKey, System.Collections.Generic.IReadOnlyList<string> Arguments) { }
        public string AdapterKey { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Arguments { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class ReadSnapshot : System.IEquatable<ArcNET.Diagnostics.ReadSnapshot>
    {
        public ReadSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string AdapterKey, System.Collections.Generic.IReadOnlyList<string> RequestedArguments, string? TargetHandleText, string? TargetText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReadValueSnapshot> Values, ArcNET.Diagnostics.NativeReadSnapshot? NativeRead, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public string AdapterKey { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot? NativeRead { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedArguments { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string? TargetHandleText { get; init; }
        public string? TargetText { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReadValueSnapshot> Values { get; init; }
    }
    public readonly struct ReadValueSnapshot : System.IEquatable<ArcNET.Diagnostics.ReadValueSnapshot>
    {
        public ReadValueSnapshot(string Key, string Label, string ValueText) { }
        public string Key { get; init; }
        public string Label { get; init; }
        public string ValueText { get; init; }
    }
    public readonly struct ReputationLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.ReputationLogbookEntrySnapshot>
    {
        public ReputationLogbookEntrySnapshot(int ReputationId, ArcNET.Diagnostics.GameDateTimeSnapshot DateTime, string Name) { }
        public ArcNET.Diagnostics.GameDateTimeSnapshot DateTime { get; init; }
        public string Name { get; init; }
        public int ReputationId { get; init; }
    }
    public sealed class ReputationLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.ReputationLogbookPageSnapshot>
    {
        public ReputationLogbookPageSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReputationLogbookEntrySnapshot> Entries, ArcNET.Diagnostics.NativeReadSnapshot NativeRead) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReputationLogbookEntrySnapshot> Entries { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot NativeRead { get; init; }
    }
    public sealed class ResolvedObjectSnapshot : System.IEquatable<ArcNET.Diagnostics.ResolvedObjectSnapshot>
    {
        public ResolvedObjectSnapshot(string HandleText, string DisplayValue, string? Name, string? ObjectType, int? ProtoNumber, string ResolutionSource, ArcNET.Diagnostics.Contracts.LiveObjectIdentity RuntimeIdentity) { }
        public string DisplayValue { get; init; }
        public string HandleText { get; init; }
        public string? Name { get; init; }
        public string? ObjectType { get; init; }
        public int? ProtoNumber { get; init; }
        public string ResolutionSource { get; init; }
        public ArcNET.Diagnostics.Contracts.LiveObjectIdentity RuntimeIdentity { get; init; }
    }
    public readonly struct RumorLogbookEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.RumorLogbookEntrySnapshot>
    {
        public RumorLogbookEntrySnapshot(int RumorId, ArcNET.Diagnostics.GameDateTimeSnapshot DateTime, bool Quelled, string? Text, string? NormalText, string? DumbText) { }
        public ArcNET.Diagnostics.GameDateTimeSnapshot DateTime { get; init; }
        public string? DumbText { get; init; }
        public string? NormalText { get; init; }
        public bool Quelled { get; init; }
        public int RumorId { get; init; }
        public string? Text { get; init; }
    }
    public sealed class RumorLogbookPageSnapshot : System.IEquatable<ArcNET.Diagnostics.RumorLogbookPageSnapshot>
    {
        public RumorLogbookPageSnapshot(int Intelligence, bool UsesDumbText, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RumorLogbookEntrySnapshot> Entries, ArcNET.Diagnostics.NativeReadSnapshot NativeRead) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RumorLogbookEntrySnapshot> Entries { get; init; }
        public int Intelligence { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot NativeRead { get; init; }
        public bool UsesDumbText { get; init; }
    }
    public static class RuntimeProfileMatcher
    {
        public static ArcNET.Diagnostics.Contracts.RuntimeKind ClassifyRuntimeKind(string moduleFileName, string processName) { }
        public static ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot Match(ArcNET.Diagnostics.Contracts.RuntimeFingerprint fingerprint, string? moduleSha256, string? hashError = null) { }
        public static bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error) { }
    }
    public static class RuntimeSemanticCatalog
    {
        public static string AttachmentPointName(int attachmentPoint) { }
        public static string FormatHandle(ulong handle) { }
        public static string InventoryLocationContext(int inventoryLocation) { }
        public static string InventoryLocationName(int inventoryLocation) { }
        public static bool LooksLikeObjectHandle(ulong handle) { }
        public static string StatName(int stat) { }
    }
    public sealed class RuntimeStatusSnapshot : System.IEquatable<ArcNET.Diagnostics.RuntimeStatusSnapshot>
    {
        public RuntimeStatusSnapshot(System.DateTimeOffset GeneratedAtUtc, string DisplayName, string ModulePath, string ModuleBase, ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, uint? CurrentCharacterSheetId, int? ActionPoints, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public int? ActionPoints { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public uint? CurrentCharacterSheetId { get; init; }
        public string DisplayName { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeFingerprint Fingerprint { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public string ModuleBase { get; init; }
        public string ModulePath { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class RuntimeWatchCatalog
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> AllHooks { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchProfileDescriptor> Profiles { get; }
        public static object DescribeCatalog() { }
        public static ArcNET.Diagnostics.RuntimeWatchHookDefinition GetDefinition(ArcNET.Diagnostics.RuntimeWatchHookId id) { }
        public static bool NeedsNameCatalog(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> hooks) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> ResolveSelectors(System.Collections.Generic.IEnumerable<string> selectors) { }
        public static bool UsesHighVolumeHooks(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> hooks) { }
    }
    public readonly struct RuntimeWatchHookDefinition : System.IEquatable<ArcNET.Diagnostics.RuntimeWatchHookDefinition>
    {
        public RuntimeWatchHookDefinition(ArcNET.Diagnostics.RuntimeWatchHookId Id, string Key, string EventName, int Rva, string Site, string Area, string Description) { }
        public string Area { get; init; }
        public string Description { get; init; }
        public string EventName { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchHookId Id { get; init; }
        public string Key { get; init; }
        public int Rva { get; init; }
        public string Site { get; init; }
    }
    public enum RuntimeWatchHookId
    {
        LevelRecalc = 1,
        UpdateFollowerLevel = 2,
        StatBaseSet = 3,
        BackgroundEducateFollowers = 4,
        UiShowInvenLoot = 5,
        ItemInsert = 6,
        ItemEquipped = 7,
        ItemForceRemove = 8,
        ItemUnequipped = 9,
        ObjectDestroy = 10,
        ObjectScriptExecute = 11,
        UiStartDialog = 12,
        ReactionAdj = 13,
        CritterKill = 14,
        ScriptGlobalVarSet = 15,
        ScriptGlobalFlagSet = 16,
        ScriptPcVarSet = 17,
        ScriptPcFlagSet = 18,
        ScriptLocalFlagSet = 19,
        ScriptLocalCounterSet = 20,
        ScriptStoryStateSet = 21,
        QuestStateSet = 22,
        QuestGlobalStateSet = 23,
        TimeEventAddDelay = 24,
        TimeEventNotifyPcTeleported = 25,
        MapOpenInGame = 26,
        CombatTurnBasedWhosTurnSet = 27,
        ObjectCreate = 28,
        ObjFieldInt32Set = 29,
        ObjFieldInt64Set = 30,
        ObjFieldHandleSet = 31,
        UiSpellAdd = 32,
        UiSpellMaintainAdd = 33,
        UiSpellMaintainEnd = 34,
        TeleportDo = 35,
        ObjArrayFieldInt32Set = 36,
        ObjArrayFieldUInt32Set = 37,
        ObjArrayFieldInt64Set = 38,
        ObjArrayFieldObjSet = 39,
        ObjArrayFieldLengthSet = 40,
        EffectAdd = 41,
        EffectRemoveOneTyped = 42,
        EffectRemoveAllTyped = 43,
        EffectRemoveOneCausedBy = 44,
        EffectRemoveAllCausedBy = 45,
        SpellAdd = 46,
        SpellRemove = 47,
        ObjArrayFieldScriptSet = 48,
        ObjArrayFieldPcQuestSet = 49,
        SpellCollegeLevelSet = 50,
        EffectRemoveInternal = 51,
        GamelibInvalidateRect = 52,
        GamelibDraw = 53,
        GamelibDrawGame = 54,
        LightDraw = 55,
        TileDraw = 56,
        ObjectHoverDraw = 57,
        ObjectDraw = 58,
        RoofDraw = 59,
        TextBubbleDraw = 60,
        TextFloaterDraw = 61,
        TextConversationDraw = 62,
        TigWindowDisplay = 63,
        TigWindowComposeDirtyRect = 64,
        TigWindowBlitArt = 65,
        TigWindowCopyFromVBuffer = 66,
        TigWindowInvalidateRect = 67,
        TigVideoFlip = 68,
        CritterGiveXp = 69,
        BackgroundSet = 70,
        BackgroundClear = 71,
        ReputationAdd = 72,
        ReputationRemove = 73,
        RumorQstateSet = 74,
        RumorKnownSet = 75,
        TechLearnSchematic = 76,
        LogbookAddKill = 77,
        LogbookAddInjury = 78,
        AreaSetKnown = 79,
        AreaResetLastKnownArea = 80,
        BlessAdd = 81,
        BlessRemove = 82,
        CurseAdd = 83,
        CurseRemove = 84,
        WmapRndEncounterCheck = 85,
        WmapUiEncounterStart = 86,
        WmapLoadWorldmapInfo = 87,
    }
    public readonly struct RuntimeWatchProfileDescriptor : System.IEquatable<ArcNET.Diagnostics.RuntimeWatchProfileDescriptor>
    {
        public RuntimeWatchProfileDescriptor(string Key, string Description, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> Hooks) { }
        public string Description { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchHookDefinition> Hooks { get; init; }
        public string Key { get; init; }
    }
    public enum RuntimeWatchTimeEventParamKind
    {
        None = 0,
        Integer = 1,
        Object = 2,
        Location = 3,
        Float = 4,
    }
    public readonly struct RuntimeWatchTimeEventTypeDescriptor : System.IEquatable<ArcNET.Diagnostics.RuntimeWatchTimeEventTypeDescriptor>
    {
        public RuntimeWatchTimeEventTypeDescriptor(string Name, string TimeTypeName, bool Saveable, ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param0, ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param1, ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param2, ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param3) { }
        public string Name { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param0 { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param1 { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param2 { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind Param3 { get; init; }
        public bool Saveable { get; init; }
        public string TimeTypeName { get; init; }
        public ArcNET.Diagnostics.RuntimeWatchTimeEventParamKind ParamKind(int index) { }
    }
    public static class RuntimeWatchValueCatalog
    {
        public const int QuestBotchedModifier = 256;
        public static string ArtBlitFlagsText(uint flags) { }
        public static string ArtIdSummary(int artId) { }
        public static string ArtTypeName(int artType) { }
        public static string EffectCauseName(int cause) { }
        public static string FallbackEffectName(int effectId) { }
        public static string FormatGameDateTime(ulong rawValue) { }
        public static string FormatPackedLocation(ulong rawValue) { }
        public static bool IsNoiseTimeEventType(int type) { }
        public static string MagicTechActionName(int action) { }
        public static string MagicTechRunFlagsText(uint flags) { }
        public static int QuestBaseState(int rawState) { }
        public static bool QuestHasBotchedModifier(int rawState) { }
        public static string QuestPcStateName(int rawState) { }
        public static string QuestStateName(int state) { }
        public static string QuestStateVerb(int state) { }
        public static string ScriptFlagsText(uint flags) { }
        public static string ScriptLocalFlagName(int flag) { }
        public static string TeleportFlagsText(uint flags) { }
        public static ArcNET.Diagnostics.RuntimeWatchTimeEventTypeDescriptor TimeEventDescriptor(int type) { }
        public static string ViewTypeName(int viewType) { }
        public static string WindowFlagsText(uint flags) { }
    }
    public sealed class ScriptAttachmentPayload : System.IEquatable<ArcNET.Diagnostics.ScriptAttachmentPayload>
    {
        public ScriptAttachmentPayload(ArcNET.Diagnostics.ScriptAttachmentRecordSnapshot Script, ArcNET.Diagnostics.NativeReadSnapshot NativeRead) { }
        public ArcNET.Diagnostics.NativeReadSnapshot NativeRead { get; init; }
        public ArcNET.Diagnostics.ScriptAttachmentRecordSnapshot Script { get; init; }
    }
    public sealed class ScriptAttachmentRecordSnapshot : System.IEquatable<ArcNET.Diagnostics.ScriptAttachmentRecordSnapshot>
    {
        public ScriptAttachmentRecordSnapshot(int ScriptNumber, uint Flags, string FlagsText, uint CountersPacked, string CountersPackedText, System.Collections.Generic.IReadOnlyList<int> Counters, bool IsEmpty) { }
        public System.Collections.Generic.IReadOnlyList<int> Counters { get; init; }
        public uint CountersPacked { get; init; }
        public string CountersPackedText { get; init; }
        public uint Flags { get; init; }
        public string FlagsText { get; init; }
        public bool IsEmpty { get; init; }
        public int ScriptNumber { get; init; }
    }
    public sealed class ScriptAttachmentRequest : System.IEquatable<ArcNET.Diagnostics.ScriptAttachmentRequest>
    {
        public ScriptAttachmentRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string HandleToken, string AttachmentPointText) { }
        public string AttachmentPointText { get; init; }
        public string HandleToken { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class ScriptAttachmentSnapshot : System.IEquatable<ArcNET.Diagnostics.ScriptAttachmentSnapshot>
    {
        public ScriptAttachmentSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string RequestedAttachmentPointText, int? AttachmentPoint, string AttachmentPointName, string TargetHandleText, string TargetText, ArcNET.Diagnostics.ScriptAttachmentRecordSnapshot? Script, ArcNET.Diagnostics.NativeReadSnapshot? NativeRead, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public int? AttachmentPoint { get; init; }
        public string AttachmentPointName { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public ArcNET.Diagnostics.NativeReadSnapshot? NativeRead { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public string RequestedAttachmentPointText { get; init; }
        public ArcNET.Diagnostics.ScriptAttachmentRecordSnapshot? Script { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetHandleText { get; init; }
        public string TargetText { get; init; }
    }
    public enum SessionOrigin : byte
    {
        Attach = 0,
        Launch = 1,
    }
    public static class SheetCatalog
    {
        public static ArcNET.Diagnostics.SheetReference ResolveReference(string token) { }
    }
    public readonly struct SheetChangeSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetChangeSnapshot>
    {
        public SheetChangeSnapshot(string Category, string Name, int Before, int After, string? Detail) { }
        public int After { get; init; }
        public int Before { get; init; }
        public string Category { get; init; }
        public string? Detail { get; init; }
        public string Name { get; init; }
    }
    public sealed class SheetDataSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetDataSnapshot>
    {
        public SheetDataSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> PrimaryStats, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> Progression, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> DerivedStats, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> Resistances, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetSkillSnapshot> BasicSkills, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetSkillSnapshot> TechSkills, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> SpellColleges, ArcNET.Diagnostics.SheetScalarSnapshot SpellMastery, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> TechDisciplines) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetSkillSnapshot> BasicSkills { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> DerivedStats { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> PrimaryStats { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> Progression { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> Resistances { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> SpellColleges { get; init; }
        public ArcNET.Diagnostics.SheetScalarSnapshot SpellMastery { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetScalarSnapshot> TechDisciplines { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetSkillSnapshot> TechSkills { get; init; }
    }
    public sealed class SheetDiffRequest : System.IEquatable<ArcNET.Diagnostics.SheetDiffRequest>
    {
        public SheetDiffRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string HandleToken, int DelayMilliseconds) { }
        public int DelayMilliseconds { get; init; }
        public string HandleToken { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class SheetDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetDiffSnapshot>
    {
        public SheetDiffSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string TargetHandleText, string TargetText, int DelayMilliseconds, bool Changed, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetChangeSnapshot> Changes, ArcNET.Diagnostics.SheetDataSnapshot Before, ArcNET.Diagnostics.SheetDataSnapshot After, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.SheetDataSnapshot After { get; init; }
        public ArcNET.Diagnostics.SheetDataSnapshot Before { get; init; }
        public bool Changed { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SheetChangeSnapshot> Changes { get; init; }
        public int DelayMilliseconds { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetHandleText { get; init; }
        public string TargetText { get; init; }
    }
    public readonly struct SheetReference : System.IEquatable<ArcNET.Diagnostics.SheetReference>
    {
        public SheetReference(ArcNET.Diagnostics.SheetRoute Route, int Id, string DisplayName) { }
        public string DisplayName { get; init; }
        public int Id { get; init; }
        public ArcNET.Diagnostics.SheetRoute Route { get; init; }
    }
    public sealed class SheetRequest : System.IEquatable<ArcNET.Diagnostics.SheetRequest>
    {
        public SheetRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string HandleToken, string SheetLabel) { }
        public string HandleToken { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
        public string SheetLabel { get; init; }
    }
    public enum SheetRoute
    {
        Stat = 0,
        DerivedStat = 1,
        BasicSkill = 2,
        TechSkill = 3,
        SpellCollege = 4,
        TechDiscipline = 5,
        Resistance = 6,
    }
    public readonly struct SheetScalarSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetScalarSnapshot>
    {
        public SheetScalarSnapshot(int Id, string Name, int Value) { }
        public int Id { get; init; }
        public string Name { get; init; }
        public int Value { get; init; }
    }
    public sealed class SheetScanRequest : System.IEquatable<ArcNET.Diagnostics.SheetScanRequest>
    {
        public SheetScanRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, string HandleToken) { }
        public string HandleToken { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class SheetScanSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetScanSnapshot>
    {
        public SheetScanSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string TargetHandleText, string TargetText, ArcNET.Diagnostics.SheetDataSnapshot Data, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public ArcNET.Diagnostics.SheetDataSnapshot Data { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetHandleText { get; init; }
        public string TargetText { get; init; }
    }
    public readonly struct SheetSkillSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetSkillSnapshot>
    {
        public SheetSkillSnapshot(int Id, string Name, int Value, int Training, string TrainingName, int Encoded) { }
        public int Encoded { get; init; }
        public int Id { get; init; }
        public string Name { get; init; }
        public int Training { get; init; }
        public string TrainingName { get; init; }
        public int Value { get; init; }
    }
    public sealed class SheetSnapshot : System.IEquatable<ArcNET.Diagnostics.SheetSnapshot>
    {
        public SheetSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsAvailable, string Status, string Summary, string TargetHandleText, string TargetText, string SheetLabel, ArcNET.Diagnostics.SheetRoute Route, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReadValueSnapshot> Values, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsAvailable { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public ArcNET.Diagnostics.SheetRoute Route { get; init; }
        public string SheetLabel { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public string TargetHandleText { get; init; }
        public string TargetText { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ReadValueSnapshot> Values { get; init; }
    }
    public enum StackCleanupMode
    {
        Cdecl = 0,
        StdCall = 1,
    }
    public sealed class TimelinePresetDescriptor : System.IEquatable<ArcNET.Diagnostics.TimelinePresetDescriptor>
    {
        public TimelinePresetDescriptor(string Key, string DisplayName, string Description, System.Collections.Generic.IReadOnlyList<string> Selectors, System.Collections.Generic.IReadOnlyList<string> HookKeys, System.Collections.Generic.IReadOnlyList<string> Areas, bool UsesHighVolumeHooks) { }
        public System.Collections.Generic.IReadOnlyList<string> Areas { get; init; }
        public string Description { get; init; }
        public string DisplayName { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> HookKeys { get; init; }
        public string Key { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Selectors { get; init; }
        public bool UsesHighVolumeHooks { get; init; }
    }
    public sealed class TimelineRequest : System.IEquatable<ArcNET.Diagnostics.TimelineRequest>
    {
        public TimelineRequest(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols) { }
        public bool HasModuleSymbols { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class TimelineService
    {
        public static ArcNET.Diagnostics.TimelineSnapshot Create(ArcNET.Diagnostics.TimelineRequest request) { }
    }
    public sealed class TimelineSnapshot : System.IEquatable<ArcNET.Diagnostics.TimelineSnapshot>
    {
        public TimelineSnapshot(ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.TimelinePresetDescriptor> RecommendedPresets, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.TimelinePresetDescriptor> AvailableProbePresets, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchProfileDescriptor> AdvancedProfiles, System.Collections.Generic.IReadOnlyList<string> Notes) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.RuntimeWatchProfileDescriptor> AdvancedProfiles { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.TimelinePresetDescriptor> AvailableProbePresets { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeCapabilityReport Capabilities { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> Notes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.TimelinePresetDescriptor> RecommendedPresets { get; init; }
    }
    public sealed class WatchEventSnapshot : System.IEquatable<ArcNET.Diagnostics.WatchEventSnapshot>
    {
        public WatchEventSnapshot(uint Sequence, string HookKey, string EventName, string SemanticEvent, string Area, string Site, string CallerAddress, string CallerRva, string Signature, string Summary, string? SuggestedHandleHex, System.Collections.Generic.IReadOnlyList<string> CandidateHandles, string StackPreview) { }
        public string Area { get; init; }
        public string CallerAddress { get; init; }
        public string CallerRva { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> CandidateHandles { get; init; }
        public string EventName { get; init; }
        public string HookKey { get; init; }
        public string SemanticEvent { get; init; }
        public uint Sequence { get; init; }
        public string Signature { get; init; }
        public string Site { get; init; }
        public string StackPreview { get; init; }
        public string? SuggestedHandleHex { get; init; }
        public string Summary { get; init; }
    }
    public sealed class WatchSnapshot : System.IEquatable<ArcNET.Diagnostics.WatchSnapshot>
    {
        public WatchSnapshot(System.DateTimeOffset GeneratedAtUtc, bool IsRunning, string Status, string Summary, string PresetDisplayName, int TotalEvents, int TotalDroppedEvents, int TotalContentionDrops, int TotalWarnings, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.WatchEventSnapshot> Events) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.WatchEventSnapshot> Events { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool IsRunning { get; init; }
        public string PresetDisplayName { get; init; }
        public string Status { get; init; }
        public string Summary { get; init; }
        public int TotalContentionDrops { get; init; }
        public int TotalDroppedEvents { get; init; }
        public int TotalEvents { get; init; }
        public int TotalWarnings { get; init; }
    }
    public sealed class WatchStartRequest : System.IEquatable<ArcNET.Diagnostics.WatchStartRequest>
    {
        public WatchStartRequest(ArcNET.Diagnostics.AttachedSessionSnapshot Session, ArcNET.Diagnostics.TimelinePresetDescriptor Preset, int EventCapacity = 40) { }
        public int EventCapacity { get; init; }
        public ArcNET.Diagnostics.TimelinePresetDescriptor Preset { get; init; }
        public ArcNET.Diagnostics.AttachedSessionSnapshot Session { get; init; }
    }
    public sealed class WorkspacePanelWorkflowSnapshot : System.IEquatable<ArcNET.Diagnostics.WorkspacePanelWorkflowSnapshot>
    {
        public WorkspacePanelWorkflowSnapshot(string PanelKey, string PanelDisplayName, string PanelDescription, string ShellSurfaceText, string WorkflowTitle, string WorkflowSummary) { }
        public string PanelDescription { get; init; }
        public string PanelDisplayName { get; init; }
        public string PanelKey { get; init; }
        public string ShellSurfaceText { get; init; }
        public string WorkflowSummary { get; init; }
        public string WorkflowTitle { get; init; }
    }
    public sealed class WorkspaceRequest : System.IEquatable<ArcNET.Diagnostics.WorkspaceRequest>
    {
        public WorkspaceRequest(ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols, System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames) { }
        public bool HasModuleSymbols { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RequestedProcessNames { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
    }
    public static class WorkspaceService
    {
        public static ArcNET.Diagnostics.WorkspaceSnapshot Create(ArcNET.Diagnostics.WorkspaceRequest request) { }
        public static ArcNET.Diagnostics.WorkspaceSnapshot CreateForRuntime(ArcNET.Diagnostics.LiveRuntimeSnapshot runtime, bool hasModuleSymbols = false) { }
        public static ArcNET.Diagnostics.WorkspaceSnapshot CreateForSession(ArcNET.Diagnostics.AttachedSessionSnapshot session, bool hasModuleSymbols = false) { }
    }
    public sealed class WorkspaceSnapshot : System.IEquatable<ArcNET.Diagnostics.WorkspaceSnapshot>
    {
        public WorkspaceSnapshot(System.DateTimeOffset GeneratedAtUtc, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool HasModuleSymbols, ArcNET.Diagnostics.DashboardSnapshot Dashboard, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.WorkspacePanelWorkflowSnapshot> PanelWorkflows, ArcNET.Diagnostics.TimelineSnapshot Timeline, ArcNET.Diagnostics.FunctionBrowserSnapshot FunctionBrowser, ArcNET.Diagnostics.ObjectExplorerSnapshot ObjectExplorer) { }
        public ArcNET.Diagnostics.DashboardSnapshot Dashboard { get; init; }
        public ArcNET.Diagnostics.FunctionBrowserSnapshot FunctionBrowser { get; init; }
        public System.DateTimeOffset GeneratedAtUtc { get; init; }
        public bool HasModuleSymbols { get; init; }
        public ArcNET.Diagnostics.ObjectExplorerSnapshot ObjectExplorer { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.WorkspacePanelWorkflowSnapshot> PanelWorkflows { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
        public ArcNET.Diagnostics.TimelineSnapshot Timeline { get; init; }
    }
}
namespace ArcNET.Patch
{
    public enum ArcanumExecutableKind : byte
    {
        Auto = 0,
        Classic = 1,
        CommunityEdition = 2,
    }
    public sealed class ArcanumLaunchOptions
    {
        public ArcanumLaunchOptions() { }
        public System.Collections.Generic.IReadOnlyList<string> AdditionalArguments { get; init; }
        public ArcNET.Patch.ArcanumExecutableKind ExecutableKind { get; init; }
        public int? Height { get; init; }
        public ArcNET.Patch.SdlRenderDriver RenderDriver { get; init; }
        public int? Width { get; init; }
        public bool Windowed { get; init; }
    }
    public sealed class ArcanumLaunchPlan : System.IEquatable<ArcNET.Patch.ArcanumLaunchPlan>
    {
        public ArcanumLaunchPlan(ArcNET.Patch.ArcanumExecutableKind ExecutableKind, string ExecutablePath, string WorkingDirectory, System.Collections.Generic.IReadOnlyList<string> Arguments, System.Collections.Generic.IReadOnlyDictionary<string, string> EnvironmentVariables) { }
        public System.Collections.Generic.IReadOnlyList<string> Arguments { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }
        public ArcNET.Patch.ArcanumExecutableKind ExecutableKind { get; init; }
        public string ExecutablePath { get; init; }
        public string WorkingDirectory { get; init; }
    }
    public static class ArcanumLauncher
    {
        public const string ClassicExecutableName = "Arcanum.exe";
        public const string CommunityEditionExecutableName = "arcanum-ce.exe";
        public const string CommunityEditionPosixExecutableName = "arcanum-ce";
        public const string SdlRenderDriverEnvironmentVariable = "SDL_RENDER_DRIVER";
        public static ArcNET.Patch.ArcanumLaunchPlan CreatePlan(string gamePath, ArcNET.Patch.ArcanumLaunchOptions? options = null) { }
        public static System.Diagnostics.ProcessStartInfo CreateStartInfo(ArcNET.Patch.ArcanumLaunchPlan plan) { }
        public static System.Diagnostics.Process Launch(ArcNET.Patch.ArcanumLaunchPlan plan) { }
        public static System.Diagnostics.Process Launch(string gamePath, ArcNET.Patch.ArcanumLaunchOptions? options = null) { }
        public static string ResolveExecutablePath(string gamePath) { }
        public static string ResolveExecutablePath(string gamePath, ArcNET.Patch.ArcanumLaunchOptions? options) { }
    }
    public sealed class GitHubRelease
    {
        public GitHubRelease() { }
        [System.Text.Json.Serialization.JsonPropertyName("assets")]
        public System.Collections.Generic.IReadOnlyList<ArcNET.Patch.GitHubReleaseAsset> Assets { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string Body { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
        public string TagName { get; init; }
    }
    public sealed class GitHubReleaseAsset
    {
        public GitHubReleaseAsset() { }
        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("content_type")]
        public string ContentType { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; init; }
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
        public static void Uninstall(string gameDir) { }
    }
    public enum SdlRenderDriver : byte
    {
        Auto = 0,
        Software = 1,
        Direct3D = 2,
        Direct3D11 = 3,
        Direct3D12 = 4,
        OpenGL = 5,
        Vulkan = 6,
        Gpu = 7,
    }
    public static class SdlRenderDriverExtensions
    {
        public static string? ToHintValue(this ArcNET.Patch.SdlRenderDriver driver) { }
        public static bool TryParse(string text, out ArcNET.Patch.SdlRenderDriver driver) { }
    }
}```

## ArcNET.Editor

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/ArcNET/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ArcNET.Editor.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Editor
{
    public sealed class CharacterBuilder
    {
        public CharacterBuilder(ArcNET.Formats.MobData existing) { }
        public CharacterBuilder(ArcNET.GameObjects.ObjectType type, ArcNET.Core.Primitives.GameObjectGuid objectId, ArcNET.Core.Primitives.GameObjectGuid protoId) { }
        public ArcNET.Formats.MobData Build() { }
        public ArcNET.Editor.CharacterBuilder WithBankMoney(int amount) { }
        public ArcNET.Editor.CharacterBuilder WithBaseStats(System.ReadOnlySpan<int> stats) { }
        public ArcNET.Editor.CharacterBuilder WithBasicSkills(System.ReadOnlySpan<int> skills) { }
        public ArcNET.Editor.CharacterBuilder WithFatigue(int pts, int adj = 0) { }
        public ArcNET.Editor.CharacterBuilder WithFollowers(System.ReadOnlySpan<System.Guid> followerIds) { }
        [System.Obsolete("CritterGold is a handle, not a quantity. Use WithGoldHandle for MOB data or edit " +
            "the mobile.mdy character record.")]
        public ArcNET.Editor.CharacterBuilder WithGold(int amount) { }
        public ArcNET.Editor.CharacterBuilder WithGoldHandle(ArcNET.Core.Primitives.GameObjectGuid goldHandle) { }
        public ArcNET.Editor.CharacterBuilder WithHitPoints(int pts, int adj = 0) { }
        public ArcNET.Editor.CharacterBuilder WithHpDamage(int damage) { }
        public ArcNET.Editor.CharacterBuilder WithInventory(System.ReadOnlySpan<System.Guid> itemIds) { }
        public ArcNET.Editor.CharacterBuilder WithLocation(int tileX, int tileY) { }
        public ArcNET.Editor.CharacterBuilder WithPlayerName(string name) { }
        public ArcNET.Editor.CharacterBuilder WithPortrait(int portraitId) { }
        public ArcNET.Editor.CharacterBuilder WithProperty(ArcNET.Formats.ObjectProperty property) { }
        public ArcNET.Editor.CharacterBuilder WithSpellTech(System.ReadOnlySpan<int> ranks) { }
        public ArcNET.Editor.CharacterBuilder WithTechSkills(System.ReadOnlySpan<int> skills) { }
        public ArcNET.Editor.CharacterBuilder WithoutProperty(ArcNET.GameObjects.ObjectField field) { }
    }
    public sealed class CharacterRecord
    {
        public int AcAdjustment { get; }
        public int Age { get; }
        public int Alignment { get; }
        public int Arrows { get; }
        public int Beauty { get; }
        public int BlessingProtoElementCount { get; }
        public int[]? BlessingRaw { get; }
        public byte[]? BlessingTsRaw { get; }
        public int Bullets { get; }
        public int CarryWeight { get; }
        public int Charisma { get; }
        public int Constitution { get; }
        public int CurseProtoElementCount { get; }
        public int[]? CurseRaw { get; }
        public byte[]? CurseTsRaw { get; }
        public int DamageBonus { get; }
        public int Dexterity { get; }
        public int ExperiencePoints { get; }
        public int FatePoints { get; }
        public int FatigueDamage { get; }
        public int[]? FatigueDamageRaw { get; }
        public int Gender { get; }
        public int Gold { get; }
        public bool HasCompleteData { get; }
        public int HealRate { get; }
        public int HpDamage { get; }
        public int[]? HpDamageRaw { get; }
        public int Intelligence { get; }
        public int Level { get; }
        public int MagickPoints { get; }
        public int MagickTechAptitude { get; }
        public int MaxFollowers { get; }
        public string? Name { get; }
        public int Perception { get; }
        public int PoisonLevel { get; }
        public int PoisonRecovery { get; }
        public int PortraitIndex { get; }
        public int[]? PositionAiRaw { get; }
        public int PowerCells { get; }
        public int[]? QuestBitsetRaw { get; }
        public int QuestCount { get; }
        public byte[]? QuestDataRaw { get; }
        public int Race { get; }
        public int ReactionModifier { get; }
        public int[]? ReputationRaw { get; }
        public int RumorsCount { get; }
        public byte[]? RumorsRaw { get; }
        public int SchematicsElementCount { get; }
        public int[]? SchematicsRaw { get; }
        public int SkillBackstab { get; }
        public int SkillBow { get; }
        public int SkillDisarmTraps { get; }
        public int SkillDodge { get; }
        public int SkillFirearms { get; }
        public int SkillGambling { get; }
        public int SkillHaggle { get; }
        public int SkillHeal { get; }
        public int SkillMelee { get; }
        public int SkillPersuasion { get; }
        public int SkillPickLocks { get; }
        public int SkillPickPocket { get; }
        public int SkillProwling { get; }
        public int SkillRepair { get; }
        public int SkillSpotTrap { get; }
        public int SkillThrowing { get; }
        public int Speed { get; }
        public int SpellAir { get; }
        public int SpellConveyance { get; }
        public int SpellDivination { get; }
        public int SpellEarth { get; }
        public int SpellFire { get; }
        public int SpellForce { get; }
        public int SpellMastery { get; }
        public int SpellMental { get; }
        public int SpellMeta { get; }
        public int SpellMorph { get; }
        public int SpellNature { get; }
        public int SpellNecroBlack { get; }
        public int SpellNecroWhite { get; }
        public int SpellPhantasm { get; }
        public int SpellSummoning { get; }
        public int SpellTemporal { get; }
        public int SpellWater { get; }
        public int Strength { get; }
        public int TechChemistry { get; }
        public int TechElectric { get; }
        public int TechExplosives { get; }
        public int TechGun { get; }
        public int TechHerbology { get; }
        public int TechMechanical { get; }
        public int TechPoints { get; }
        public int TechSmithy { get; }
        public int TechTherapeutics { get; }
        public int TotalKills { get; }
        public int UnspentPoints { get; }
        public int Willpower { get; }
        public ArcNET.Formats.CharacterMdyRecord ApplyTo(ArcNET.Formats.CharacterMdyRecord original) { }
        public ArcNET.Editor.CharacterRecord.Builder ToBuilder() { }
        public static ArcNET.Editor.CharacterRecord From(ArcNET.Formats.CharacterMdyRecord rec) { }
        public sealed class Builder
        {
            public Builder() { }
            public Builder(ArcNET.Editor.CharacterRecord from) { }
            public ArcNET.Editor.CharacterRecord Build() { }
            public ArcNET.Editor.CharacterRecord.Builder WithAcAdjustment(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithAge(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithAlignment(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithArrows(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithBeauty(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithBlessingRaw(int[] bless) { }
            public ArcNET.Editor.CharacterRecord.Builder WithBlessingTsRaw(byte[] ts) { }
            public ArcNET.Editor.CharacterRecord.Builder WithBullets(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithCarryWeight(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithCharisma(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithConstitution(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithCurseRaw(int[] curse) { }
            public ArcNET.Editor.CharacterRecord.Builder WithCurseTsRaw(byte[] ts) { }
            public ArcNET.Editor.CharacterRecord.Builder WithDamageBonus(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithDexterity(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithExperiencePoints(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithFatePoints(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithFatigueDamage(int damage) { }
            public ArcNET.Editor.CharacterRecord.Builder WithFatigueDamageRaw(int[] v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithGender(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithGold(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithHealRate(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithHpDamage(int damage) { }
            public ArcNET.Editor.CharacterRecord.Builder WithHpDamageRaw(int[] v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithIntelligence(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithLevel(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithMagickPoints(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithMagickTechAptitude(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithMaxFollowers(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithName(string? v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPerception(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPoisonLevel(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPoisonRecovery(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPortraitIndex(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPositionAiRaw(int[] v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithPowerCells(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithQuestBitsetRaw(int[] bitset) { }
            public ArcNET.Editor.CharacterRecord.Builder WithQuestDataRaw(byte[] data) { }
            public ArcNET.Editor.CharacterRecord.Builder WithRace(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithReactionModifier(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithReputationRaw(int[] rep) { }
            public ArcNET.Editor.CharacterRecord.Builder WithRumorsRaw(byte[] rumors) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSchematicsRaw(int[] sch) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillBackstab(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillBow(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillDisarmTraps(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillDodge(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillFirearms(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillGambling(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillHaggle(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillHeal(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillMelee(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillPersuasion(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillPickLocks(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillPickPocket(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillProwling(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillRepair(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillSpotTrap(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSkillThrowing(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpeed(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellAir(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellConveyance(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellDivination(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellEarth(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellFire(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellForce(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellMastery(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellMental(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellMeta(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellMorph(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellNature(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellNecroBlack(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellNecroWhite(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellPhantasm(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellSummoning(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellTemporal(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithSpellWater(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithStrength(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechChemistry(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechElectric(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechExplosives(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechGun(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechHerbology(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechMechanical(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechPoints(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechSmithy(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTechTherapeutics(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithTotalKills(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithUnspentPoints(int v) { }
            public ArcNET.Editor.CharacterRecord.Builder WithWillpower(int v) { }
        }
    }
    public sealed class DialogBuilder
    {
        public DialogBuilder() { }
        public DialogBuilder(ArcNET.Formats.DlgFile existing) { }
        public ArcNET.Editor.DialogBuilder AddControlEntry(int num, string text, int responseTargetNumber = 0, string conditions = "", string actions = "") { }
        public ArcNET.Editor.DialogBuilder AddEntry(ArcNET.Formats.DialogEntry entry) { }
        public ArcNET.Editor.DialogBuilder AddNpcReply(int num, string text, int responseTargetNumber = 0, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Editor.DialogBuilder AddPcOption(int num, string text, int intelligenceRequirement, int responseTargetNumber = 0, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Formats.DlgFile Build() { }
        public ArcNET.Editor.DialogBuilder RemoveEntry(int num) { }
        public ArcNET.Editor.DialogBuilder SetResponseTarget(int num, int responseTargetNumber) { }
        public ArcNET.Editor.DialogBuilder UpdateEntry(int num, System.Func<ArcNET.Formats.DialogEntry, ArcNET.Formats.DialogEntry> update) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.DialogValidationIssue> Validate() { }
    }
    public sealed class DialogEditor
    {
        public DialogEditor(ArcNET.Formats.DlgFile dialog) { }
        public bool CanRedo { get; }
        public bool CanUndo { get; }
        public bool HasPendingChanges { get; }
        public ArcNET.Editor.DialogEditor AddControlEntry(int num, string text, int responseTargetNumber = 0, string conditions = "", string actions = "") { }
        public ArcNET.Editor.DialogEditor AddEntry(ArcNET.Formats.DialogEntry entry) { }
        public ArcNET.Editor.DialogEditor AddNpcReply(int num, string text, int responseTargetNumber = 0, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Editor.DialogEditor AddPcOption(int num, string text, int intelligenceRequirement, int responseTargetNumber = 0, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Formats.DlgFile CommitPendingChanges() { }
        public ArcNET.Editor.DialogEditor DiscardPendingChanges() { }
        public ArcNET.Editor.DialogEditor Edit(System.Action<ArcNET.Editor.DialogBuilder> update) { }
        public ArcNET.Formats.DlgFile GetCurrentDialog() { }
        public ArcNET.Formats.DlgFile? GetPendingDialog() { }
        public ArcNET.Editor.DialogEditor InsertControlEntryAfter(int sourceEntryNumber, int num, string text, string conditions = "", string actions = "") { }
        public ArcNET.Editor.DialogEditor InsertEntryAfter(int sourceEntryNumber, ArcNET.Formats.DialogEntry entry) { }
        public ArcNET.Editor.DialogEditor InsertNpcReplyAfter(int sourceEntryNumber, int num, string text, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Editor.DialogEditor InsertPcOptionAfter(int sourceEntryNumber, int num, string text, int intelligenceRequirement, string conditions = "", string actions = "", string genderField = "") { }
        public ArcNET.Editor.DialogEditor Redo() { }
        public ArcNET.Editor.DialogEditor RemoveEntry(int num) { }
        public ArcNET.Editor.DialogEditor SetResponseTarget(int num, int responseTargetNumber) { }
        public ArcNET.Editor.DialogEditor Undo() { }
        public ArcNET.Editor.DialogEditor UpdateEntry(int num, System.Func<ArcNET.Formats.DialogEntry, ArcNET.Formats.DialogEntry> update) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.DialogValidationIssue> Validate() { }
        public ArcNET.Editor.DialogEditor WithDialog(ArcNET.Formats.DlgFile dialog) { }
        public ArcNET.Editor.DialogEditor WithDialog(System.Func<ArcNET.Formats.DlgFile, ArcNET.Formats.DlgFile> update) { }
    }
    public enum DialogValidationCode
    {
        DuplicateEntryNumber = 0,
        NegativeIntelligenceRequirement = 1,
        MissingResponseTarget = 2,
    }
    public sealed class DialogValidationIssue : System.IEquatable<ArcNET.Editor.DialogValidationIssue>
    {
        public DialogValidationIssue() { }
        public required ArcNET.Editor.DialogValidationCode Code { get; init; }
        public int? EntryNumber { get; init; }
        public int? IntelligenceRequirement { get; init; }
        public required string Message { get; init; }
        public int? ResponseTargetNumber { get; init; }
        public required ArcNET.Editor.DialogValidationSeverity Severity { get; init; }
        public override string ToString() { }
    }
    public enum DialogValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
    public static class DialogValidator
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.DialogValidationIssue> Validate(ArcNET.Formats.DlgFile dialog) { }
    }
    public sealed class EditorArtDefinition
    {
        public EditorArtDefinition() { }
        public required uint ActionFrame { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required ArcNET.Formats.ArtFlags Flags { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required uint FrameRate { get; init; }
        public required int FramesPerRotation { get; init; }
        public bool IsAnimated { get; }
        public required int MaxFrameHeight { get; init; }
        public required int MaxFrameWidth { get; init; }
        public required int PaletteCount { get; init; }
        public required int RotationCount { get; init; }
    }
    public sealed class EditorArtPreview
    {
        public EditorArtPreview() { }
        public required uint ActionFrame { get; init; }
        public required ArcNET.Formats.ArtFlags Flags { get; init; }
        public System.TimeSpan FrameDuration { get; }
        public required uint FrameRate { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtPreviewFrame> Frames { get; init; }
        public required int FramesPerRotation { get; init; }
        public required int PaletteSlot { get; init; }
        public required ArcNET.Editor.EditorArtPreviewPixelFormat PixelFormat { get; init; }
        public required int RotationCount { get; init; }
    }
    public static class EditorArtPreviewBuilder
    {
        public static ArcNET.Editor.EditorArtPreview Build(ArcNET.Formats.ArtFile art, ArcNET.Editor.EditorArtPreviewOptions? options = null) { }
        public static ArcNET.Editor.EditorArtPreviewFrame BuildFrame(ArcNET.Formats.ArtFile art, int rotationIndex, int frameIndex, ArcNET.Editor.EditorArtPreviewOptions? options = null) { }
    }
    public sealed class EditorArtPreviewFrame
    {
        public EditorArtPreviewFrame() { }
        public required int FrameIndex { get; init; }
        public required ArcNET.Formats.ArtFrameHeader Header { get; init; }
        public int Height { get; }
        public required byte[] PixelData { get; init; }
        public required int RotationIndex { get; init; }
        public int Stride { get; }
        public int Width { get; }
    }
    public sealed class EditorArtPreviewOptions
    {
        public EditorArtPreviewOptions() { }
        public bool FlipVertically { get; init; }
        public bool IsLightMask { get; init; }
        public int PaletteSlot { get; init; }
        public ArcNET.Editor.EditorArtPreviewPixelFormat PixelFormat { get; init; }
    }
    public enum EditorArtPreviewPixelFormat
    {
        Rgba32 = 0,
        Bgra32 = 1,
    }
    public sealed class EditorArtReference
    {
        public EditorArtReference() { }
        public required uint ArtId { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int Count { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
    }
    public sealed class EditorArtResolver
    {
        public int BindingCount { get; }
        public void Bind(ArcNET.Core.Primitives.ArtId artId, string assetPath) { }
        public void BindRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ArcNET.Core.Primitives.ArtId, string>> bindings) { }
        public ArcNET.Formats.ArtFile? FindArt(ArcNET.Core.Primitives.ArtId artId) { }
        public string? FindAssetPath(ArcNET.Core.Primitives.ArtId artId) { }
    }
    public enum EditorArtResolverBindingStrategy
    {
        None = 0,
        Conservative = 1,
        ArcanumMessageTables = 2,
    }
    public sealed class EditorAssetCatalog
    {
        public int Count { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> Entries { get; }
        public static ArcNET.Editor.EditorAssetCatalog Empty { get; }
        public ArcNET.Editor.EditorAssetEntry? Find(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindByFormat(ArcNET.Formats.FileFormat format) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> Search(string text, ArcNET.Formats.FileFormat? format = default) { }
    }
    public sealed class EditorAssetDependencySummary
    {
        public EditorAssetDependencySummary() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtReference> ArtReferences { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public int? DefinedDialogId { get; init; }
        public int? DefinedProtoNumber { get; init; }
        public int? DefinedScriptId { get; init; }
        public bool HasDependencies { get; }
        public bool HasIncomingReferences { get; }
        public bool HasRelationships { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProtoReference> IncomingProtoReferences { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptReference> IncomingScriptReferences { get; init; }
        public string? MapName { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProtoReference> ProtoReferences { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptReference> ScriptReferences { get; init; }
    }
    public sealed class EditorAssetEntry
    {
        public EditorAssetEntry() { }
        public required string AssetPath { get; init; }
        public required ArcNET.Formats.FileFormat Format { get; init; }
        public required int ItemCount { get; init; }
        public string? SourceEntryPath { get; init; }
        public required ArcNET.Editor.EditorAssetSourceKind SourceKind { get; init; }
        public required string SourcePath { get; init; }
    }
    public sealed class EditorAssetIndex : ArcNET.Editor.IArtIndex, ArcNET.Editor.IAssetDependencyIndex, ArcNET.Editor.IDialogIndex, ArcNET.Editor.IFacadeWalkIndex, ArcNET.Editor.IJumpIndex, ArcNET.Editor.IMapIndex, ArcNET.Editor.IMapPropertiesIndex, ArcNET.Editor.IMessageIndex, ArcNET.Editor.IProtoIndex, ArcNET.Editor.ISchemeIndex, ArcNET.Editor.IScriptIndex, ArcNET.Editor.ITerrainIndex
    {
        public System.Collections.Generic.IReadOnlyList<string> MapNames { get; }
        public static ArcNET.Editor.EditorAssetIndex Empty { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindAmbientSchemeSectors(int ambientSchemeIndex) { }
        public ArcNET.Editor.EditorArtDefinition? FindArtDetail(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtReference> FindArtReferences(uint artId) { }
        public ArcNET.Editor.EditorAssetDependencySummary? FindAssetDependencySummary(string assetPath) { }
        public string? FindAssetMap(string assetPath) { }
        public ArcNET.Editor.EditorAssetEntry? FindDialogDefinition(int dialogId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindDialogDefinitions(int dialogId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogDefinition> FindDialogDetails(int dialogId) { }
        public ArcNET.Editor.EditorFacadeWalkDefinition? FindFacadeWalkDetail(string assetPath) { }
        public ArcNET.Editor.EditorJumpDefinition? FindJumpDetail(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindLightSchemeSectors(int lightSchemeIndex) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMapAssets(string mapName) { }
        public ArcNET.Editor.EditorMapProjection? FindMapProjection(string mapName) { }
        public ArcNET.Editor.EditorMapPropertiesDefinition? FindMapPropertiesDetail(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindMapSectors(string mapName) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMessageAssets(int messageIndex) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindMusicSchemeSectors(int musicSchemeIndex) { }
        public ArcNET.Editor.EditorAssetEntry? FindProtoDefinition(int protoNumber) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProtoReference> FindProtoReferences(int protoNumber) { }
        public ArcNET.Editor.EditorAssetEntry? FindScriptDefinition(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindScriptDefinitions(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptDefinition> FindScriptDetails(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptReference> FindScriptReferences(int scriptId) { }
        public ArcNET.Editor.EditorSectorSummary? FindSectorSummary(string assetPath) { }
        public ArcNET.Editor.EditorTerrainDefinition? FindTerrainDetail(string assetPath) { }
        public System.Collections.Generic.IReadOnlyCollection<uint> GetReferencedArtIds() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtDefinition> SearchArtDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogDefinition> SearchDialogDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorFacadeWalkDefinition> SearchFacadeWalkDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorJumpDefinition> SearchJumpDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<string> SearchMapNames(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapPropertiesDefinition> SearchMapPropertiesDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptDefinition> SearchScriptDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> SearchSectors(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainDefinition> SearchTerrainDetails(string text) { }
    }
    public enum EditorAssetSourceKind
    {
        LooseFile = 0,
        DatArchive = 1,
    }
    public sealed class EditorAudioAssetCatalog
    {
        public int Count { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAudioAssetEntry> Entries { get; }
        public static ArcNET.Editor.EditorAudioAssetCatalog Empty { get; }
        public ArcNET.Editor.EditorAudioAssetEntry? Find(string assetPath) { }
    }
    public sealed class EditorAudioAssetEntry
    {
        public EditorAudioAssetEntry() { }
        public required string AssetPath { get; init; }
        public required int ByteLength { get; init; }
        public string? SourceEntryPath { get; init; }
        public required ArcNET.Editor.EditorAssetSourceKind SourceKind { get; init; }
        public required string SourcePath { get; init; }
    }
    public sealed class EditorAudioDefinition
    {
        public EditorAudioDefinition() { }
        public required ArcNET.Editor.EditorAudioAssetEntry Asset { get; init; }
        public required int BitsPerSample { get; init; }
        public required int BlockAlign { get; init; }
        public required int ByteRate { get; init; }
        public required int ChannelCount { get; init; }
        public required System.TimeSpan Duration { get; init; }
        public required ArcNET.Editor.EditorAudioSampleEncoding Encoding { get; init; }
        public required int SampleByteLength { get; init; }
        public required long SampleFrameCount { get; init; }
        public required int SampleRate { get; init; }
    }
    public sealed class EditorAudioPreview
    {
        public EditorAudioPreview() { }
        public string? AssetPath { get; init; }
        public required int BitsPerSample { get; init; }
        public required int BlockAlign { get; init; }
        public required int ByteRate { get; init; }
        public required int ChannelCount { get; init; }
        public System.TimeSpan Duration { get; }
        public required ArcNET.Editor.EditorAudioSampleEncoding Encoding { get; init; }
        public required byte[] SampleData { get; init; }
        public required long SampleFrameCount { get; init; }
        public required int SampleRate { get; init; }
    }
    public static class EditorAudioPreviewBuilder
    {
        public static ArcNET.Editor.EditorAudioPreview BuildWave(System.ReadOnlyMemory<byte> waveData, string? assetPath = null) { }
    }
    public enum EditorAudioSampleEncoding
    {
        Pcm = 0,
        IeeeFloat = 1,
    }
    public enum EditorCapability
    {
        WorkspaceLoadContentDirectory = 0,
        WorkspaceLoadGameInstall = 1,
        WorkspaceLoadModule = 2,
        WorkspaceComposeSaveSlot = 3,
        AssetCatalog = 4,
        AssetDependencySummary = 5,
        WorkspaceValidation = 6,
        SessionStagedUndoRedo = 7,
        SessionAppliedHistory = 8,
        SessionPartialApplySaveDiscard = 9,
        ProjectPersistence = 10,
        ProjectRestore = 11,
        DialogEditing = 12,
        ScriptEditing = 13,
        SaveEditing = 14,
        TerrainPaletteBrowsing = 15,
        TerrainLayerEditing = 16,
        TrackedTerrainToolWorkflow = 17,
        ObjectPaletteBrowsing = 18,
        ObjectPlacement = 19,
        TrackedObjectPlacementWorkflow = 20,
        ObjectTransformEditing = 21,
        SectorLightEditing = 22,
        SectorTileScriptEditing = 23,
        MapPreview = 24,
        MapScenePreview = 25,
        SceneHitTesting = 26,
        ArtPreview = 27,
        AudioPreviewWave = 28,
        ObjectInspectorSummary = 29,
        ObjectInspectorFlags = 30,
        ObjectInspectorScriptAttachments = 31,
        ObjectInspectorCritterProgression = 32,
        ObjectInspectorLight = 33,
        ObjectInspectorGenerator = 34,
        ObjectInspectorBlending = 35,
    }
    public sealed class EditorCapabilitySummary
    {
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorCapability> AvailableCapabilities { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorCapability> SupportedCapabilities { get; }
        public bool IsAvailable(ArcNET.Editor.EditorCapability capability) { }
        public bool Supports(ArcNET.Editor.EditorCapability capability) { }
    }
    public sealed class EditorDialogDefinition
    {
        public EditorDialogDefinition() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int ControlEntryCount { get; init; }
        public required int DialogId { get; init; }
        public required int EntryCount { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public bool HasMissingResponseTargets { get; }
        public required System.Collections.Generic.IReadOnlyList<int> MissingResponseTargetNumbers { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogNode> Nodes { get; init; }
        public required int NpcEntryCount { get; init; }
        public required int PcOptionCount { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> RootEntryNumbers { get; init; }
        public required int TerminalEntryCount { get; init; }
        public required int TransitionCount { get; init; }
    }
    public sealed class EditorDialogNode
    {
        public EditorDialogNode() { }
        public required string Actions { get; init; }
        public required string Conditions { get; init; }
        public required int EntryNumber { get; init; }
        public required string GenderField { get; init; }
        public required bool HasMissingResponseTarget { get; init; }
        public bool HasTransition { get; }
        public required int IntelligenceRequirement { get; init; }
        public required bool IsRoot { get; init; }
        public bool IsTerminal { get; }
        public required ArcNET.Editor.EditorDialogNodeKind Kind { get; init; }
        public required int ResponseTargetNumber { get; init; }
        public required string Text { get; init; }
    }
    public enum EditorDialogNodeKind
    {
        NpcReply = 0,
        PcOption = 1,
        Control = 2,
    }
    public sealed class EditorFacadeWalkDefinition
    {
        public EditorFacadeWalkDefinition() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int EntryCount { get; init; }
        public required bool Flippable { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required uint Height { get; init; }
        public required bool Outdoor { get; init; }
        public required uint Terrain { get; init; }
        public required int WalkableEntryCount { get; init; }
        public required uint Width { get; init; }
    }
    public sealed class EditorJumpDefinition
    {
        public EditorJumpDefinition() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> DestinationMapIds { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required int JumpCount { get; init; }
    }
    public readonly struct EditorMapAmbientLightColors : System.IEquatable<ArcNET.Editor.EditorMapAmbientLightColors>
    {
        public EditorMapAmbientLightColors(ArcNET.Core.Primitives.Color Indoor, ArcNET.Core.Primitives.Color Outdoor) { }
        public ArcNET.Core.Primitives.Color Indoor { get; init; }
        public ArcNET.Core.Primitives.Color Outdoor { get; init; }
    }
    public sealed class EditorMapAmbientLightingState
    {
        public EditorMapAmbientLightingState() { }
        public int CurrentHour { get; init; }
        public ArcNET.Core.Primitives.Color FallbackIndoorColor { get; init; }
        public ArcNET.Core.Primitives.Color FallbackOutdoorColor { get; init; }
        public int MapDefaultLightSchemeIndex { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<int, ArcNET.Editor.EditorMapAmbientLightColors> SchemeColorsByIndex { get; init; }
        public ArcNET.Editor.EditorMapAmbientLightColors ResolveForSector(int lightSchemeIndex) { }
    }
    public static class EditorMapCameraMath
    {
        public static ArcNET.Editor.EditorMapTileBounds GetVisibleTileBounds(ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1) { }
        public static ArcNET.Editor.EditorMapSceneHit? HitTestScene(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportX, double viewportY) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneHit> HitTestSceneArea(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportStartX, double viewportStartY, double viewportEndX, double viewportEndY) { }
        public static ArcNET.Editor.EditorProjectMapSelectionState? HitTestSceneAreaSelection(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportStartX, double viewportStartY, double viewportEndX, double viewportEndY) { }
        public static ArcNET.Editor.EditorProjectMapSelectionState? HitTestSceneSelection(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportX, double viewportY) { }
        public static double ProjectTileX(ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double tileX) { }
        public static double ProjectTileY(ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double tileY) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneHit> ResolveSceneAreaSelection(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> ResolveSceneAreaSelectionBySector(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection) { }
        public static double UnprojectTileX(ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportX) { }
        public static double UnprojectTileY(ArcNET.Editor.EditorProjectMapCameraState camera, double viewportWidth, double viewportHeight, double pixelsPerTileAtZoom1, double viewportY) { }
    }
    public enum EditorMapCommittedRenderLayer
    {
        Ground = 0,
        GroundDecal = 1,
        Wall = 2,
        Scenery = 3,
        Mobile = 4,
        Roof = 5,
    }
    public static class EditorMapFacadePaintableSceneBuilder
    {
        public static ArcNET.Editor.EditorMapPaintableScene? Build(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorProjectMapSelectionState? selection, ArcNET.Editor.EditorTerrainPaletteEntry? terrainEntry, ArcNET.Formats.FacadeWalk? facadeWalk, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null) { }
    }
    public readonly struct EditorMapFloorRenderBounds : System.IEquatable<ArcNET.Editor.EditorMapFloorRenderBounds>
    {
        public EditorMapFloorRenderBounds(double MinLeft, double MinTop, double MaxRight, double MaxBottom) { }
        public bool IsValid { get; }
        public double MaxBottom { get; init; }
        public double MaxRight { get; init; }
        public double MinLeft { get; init; }
        public double MinTop { get; init; }
    }
    public static class EditorMapFloorRenderBuilder
    {
        public static ArcNET.Editor.EditorMapFloorRenderPreview Build(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorMapFloorRenderRequest? request = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static ArcNET.Editor.EditorMapFloorRenderPreview BuildDelta(ArcNET.Editor.EditorMapFloorRenderPreview existingPreview, ArcNET.Editor.EditorMapScenePreview scenePreview, string changedSectorAssetPath, ArcNET.Editor.EditorMapFloorRenderRequest? request = null, System.Threading.CancellationToken cancellationToken = default) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "CenterX",
                "CenterY"})]
        public static System.ValueTuple<int, int> GetLayoutSpriteCenter(ArcNET.Editor.EditorMapObjectPreview objectPreview, ArcNET.Editor.EditorMapObjectSpriteBounds spriteBounds) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "CenterX",
                "CenterY"})]
        public static System.ValueTuple<int, int> GetLayoutSpriteCenter(ArcNET.GameObjects.ObjectType objectType, ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapObjectSpriteBounds spriteBounds) { }
    }
    public sealed class EditorMapFloorRenderPreview
    {
        public EditorMapFloorRenderPreview() { }
        public ArcNET.Editor.EditorMapAmbientLightingState? AmbientLighting { get; init; }
        public required double HeightPixels { get; init; }
        public bool IncludeEditorObjectStateTint { get; init; }
        public bool IncludeEmptyTerrainTiles { get; init; }
        public bool IncludeFloorLightTint { get; init; }
        public bool IncludeTerrainBlockedTileOverlays { get; init; }
        public bool IncludeTerrainJumpPointOverlays { get; init; }
        public bool IncludeTerrainLightOverlays { get; init; }
        public bool IncludeTerrainRoofs { get; init; }
        public bool IncludeTerrainScriptOverlays { get; init; }
        public bool IsTerrainMaterializationPartial { get; init; }
        public int LightCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapLightRenderItem> Lights { get; init; }
        public required string MapName { get; init; }
        public System.Collections.Generic.IReadOnlySet<string> MaterializedTerrainSectorAssetPaths { get; init; }
        public int MaterializedTerrainSectorCount { get; init; }
        public int ObjectAuxiliaryCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems { get; init; }
        public int ObjectCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectRenderItem> Objects { get; init; }
        public int OverlayCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapTileOverlayRenderItem> Overlays { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderQueueItem> RenderQueue { get; init; }
        public int RenderQueueCount { get; }
        public int RoofCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRoofRenderItem> Roofs { get; init; }
        public long SceneRevision { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSectorRenderSlice> Slices { get; init; }
        public int TileCount { get; }
        public required double TileHeightPixels { get; init; }
        public required double TileWidthPixels { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapFloorTileRenderItem> Tiles { get; init; }
        public int TotalTerrainSectorCount { get; init; }
        public required ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSectorScenePreview> VirtualTerrainSectors { get; init; }
        public required double WidthPixels { get; init; }
        public System.Collections.Generic.IEnumerable<ArcNET.Editor.EditorMapRenderQueueItem> EnumerateVisibleRenderItems(ArcNET.Editor.EditorMapSceneViewportLayout viewport) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectRenderItem> GetObjectsAtTile(string? sectorAssetPath, ArcNET.Core.Primitives.Location tile) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSectorRenderSliceBounds> GetSectorBounds() { }
        public ArcNET.Editor.EditorMapSectorRenderSliceBounds GetVirtualTerrainSectorBounds(ArcNET.Editor.EditorMapSectorScenePreview sector) { }
        public bool IsTerrainSectorMaterialized(string sectorAssetPath) { }
        public bool TryGetObject(ArcNET.Core.Primitives.GameObjectGuid objectId, out ArcNET.Editor.EditorMapObjectRenderItem? item) { }
        public bool TryGetObjectDrawOrder(ArcNET.Core.Primitives.GameObjectGuid objectId, out int drawOrder) { }
        public bool TryGetTile(string sectorAssetPath, ArcNET.Core.Primitives.Location tile, out ArcNET.Editor.EditorMapFloorTileRenderItem? item) { }
        public bool TryGetVirtualTerrainSector(string sectorAssetPath, out ArcNET.Editor.EditorMapSectorScenePreview? sector) { }
    }
    public sealed class EditorMapFloorRenderRequest
    {
        public EditorMapFloorRenderRequest() { }
        public ArcNET.Editor.EditorMapAmbientLightingState? AmbientLighting { get; init; }
        public ArcNET.Editor.EditorArtResolver? ArtResolver { get; init; }
        public bool IncludeBlockedTileOverlays { get; init; }
        public bool IncludeEditorObjectStateTint { get; init; }
        public bool IncludeEmptyTiles { get; init; }
        public bool IncludeFloorLightTint { get; init; }
        public bool IncludeJumpPointOverlays { get; init; }
        public bool IncludeLightOverlays { get; init; }
        public bool IncludeObjects { get; init; }
        public bool IncludeRoofs { get; init; }
        public bool IncludeScriptOverlays { get; init; }
        public System.Collections.Generic.IReadOnlySet<string>? MaterializedTerrainSectorAssetPaths { get; init; }
        public ArcNET.Editor.EditorMapFloorRenderBounds? SceneBoundsOverride { get; init; }
        public double TileHeightPixels { get; init; }
        public double TileWidthPixels { get; init; }
        public ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public ArcNET.Editor.EditorMapFloorRenderRequest WithAmbientLighting(ArcNET.Editor.EditorMapAmbientLightingState? ambientLighting) { }
        public ArcNET.Editor.EditorMapFloorRenderRequest WithArtResolver(ArcNET.Editor.EditorArtResolver? artResolver) { }
        public ArcNET.Editor.EditorMapFloorRenderRequest WithMaterializedTerrainSectors(System.Collections.Generic.IReadOnlySet<string>? sectorAssetPaths, ArcNET.Editor.EditorMapFloorRenderBounds? sceneBoundsOverride) { }
        public ArcNET.Editor.EditorMapFloorRenderRequest WithPreviewState(ArcNET.Editor.EditorProjectMapPreviewState previewState) { }
        public static ArcNET.Editor.EditorMapFloorRenderRequest CreateWorldEditPreset(ArcNET.Editor.EditorMapSceneViewMode viewMode = 1) { }
    }
    public sealed class EditorMapFloorTileRenderItem
    {
        public EditorMapFloorTileRenderItem() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required double CenterX { get; init; }
        public required double CenterY { get; init; }
        public required int DrawOrder { get; init; }
        public required bool HasLight { get; init; }
        public required bool HasScript { get; init; }
        public required bool IsBlocked { get; init; }
        public ArcNET.Editor.EditorMapTileLightDiagnostics? LightDiagnostics { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public ArcNET.Core.Primitives.Location RoofCell { get; }
        public required string SectorAssetPath { get; init; }
        public uint? SuggestedTintColor { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public static class EditorMapFocusLocator
    {
        public static ArcNET.Editor.EditorMapFocusTarget? FindTarget(ArcNET.Editor.EditorWorkspace workspace, string query) { }
    }
    public sealed class EditorMapFocusTarget
    {
        public EditorMapFocusTarget() { }
        public required double CenterTileX { get; init; }
        public required double CenterTileY { get; init; }
        public string? FocusAssetPath { get; init; }
        public required string MapName { get; init; }
        public int MatchCount { get; init; }
        public ArcNET.Core.Primitives.GameObjectGuid? ObjectId { get; init; }
        public int? ProtoNumber { get; init; }
        public required string Query { get; init; }
        public required string SectorAssetPath { get; init; }
        public string? SourceAssetPath { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public sealed class EditorMapJumpPointPreview
    {
        public EditorMapJumpPointPreview() { }
        public required int DestinationMapId { get; init; }
        public required int DestinationTileX { get; init; }
        public required int DestinationTileY { get; init; }
        public required uint Flags { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required int TileIndex { get; init; }
        public required int TileX { get; init; }
        public required int TileY { get; init; }
    }
    public enum EditorMapLayerBrushMode
    {
        SetTileArt = 0,
        SetRoofArt = 1,
        SetBlocked = 2,
    }
    public sealed class EditorMapLayerBrushRequest
    {
        public EditorMapLayerBrushRequest() { }
        public uint ArtId { get; init; }
        public bool Blocked { get; init; }
        public required ArcNET.Editor.EditorMapLayerBrushMode Mode { get; init; }
        public static ArcNET.Editor.EditorMapLayerBrushRequest SetBlocked(bool blocked) { }
        public static ArcNET.Editor.EditorMapLayerBrushRequest SetRoofArt(uint artId) { }
        public static ArcNET.Editor.EditorMapLayerBrushRequest SetTileArt(uint artId) { }
    }
    public sealed class EditorMapLayerBrushResult
    {
        public EditorMapLayerBrushResult() { }
        public int ChangeCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> Changes { get; init; }
        public bool HasChanges { get; }
    }
    public sealed class EditorMapLightPreview
    {
        public EditorMapLightPreview() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required byte Blue { get; init; }
        public required ArcNET.Formats.SectorLightFlags Flags { get; init; }
        public required byte Green { get; init; }
        public required int OffsetX { get; init; }
        public required int OffsetY { get; init; }
        public required int Palette { get; init; }
        public required byte Red { get; init; }
        public required int TileX { get; init; }
        public required int TileY { get; init; }
        public required uint TintColor { get; init; }
    }
    public sealed class EditorMapLightRenderItem
    {
        public EditorMapLightRenderItem() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required int DrawOrder { get; init; }
        public required ArcNET.Formats.SectorLightFlags Flags { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required string SectorAssetPath { get; init; }
        public required double SuggestedOpacity { get; init; }
        public required uint SuggestedTintColor { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public readonly struct EditorMapObjectAlphaLerp : System.IEquatable<ArcNET.Editor.EditorMapObjectAlphaLerp>
    {
        public EditorMapObjectAlphaLerp(byte Left, byte Right) { }
        public byte Left { get; init; }
        public byte Right { get; init; }
    }
    public sealed class EditorMapObjectAuxiliaryRenderItem
    {
        public EditorMapObjectAuxiliaryRenderItem() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public ArcNET.Editor.EditorMapSpriteBlendMode BlendMode { get; init; }
        public required ArcNET.Editor.EditorMapCommittedRenderLayer CommittedRenderLayer { get; init; }
        public required int DrawOrder { get; init; }
        public bool IsRoofCovered { get; init; }
        public required bool IsShrunk { get; init; }
        public required ArcNET.Editor.EditorMapObjectAuxiliaryRenderLayer Layer { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ParentObjectId { get; init; }
        public required ArcNET.GameObjects.ObjectType ParentObjectType { get; init; }
        public required int RotationIndex { get; init; }
        public required int ScalePercent { get; init; }
        public required string SectorAssetPath { get; init; }
        public uint? SuggestedTintColor { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
        public bool UseLightMaskTint { get; init; }
    }
    public enum EditorMapObjectAuxiliaryRenderLayer
    {
        Underlay = 0,
        Shadow = 1,
        OverlayBack = 2,
        OverlayFore = 3,
    }
    public enum EditorMapObjectBrushMode
    {
        StampFromProto = 0,
        ReplaceWithProto = 1,
        Erase = 2,
        Rotate = 3,
        RotatePitch = 4,
        MoveByOffset = 5,
    }
    public sealed class EditorMapObjectBrushRequest
    {
        public EditorMapObjectBrushRequest() { }
        public int DeltaTileX { get; init; }
        public int DeltaTileY { get; init; }
        public required ArcNET.Editor.EditorMapObjectBrushMode Mode { get; init; }
        public int ProtoNumber { get; init; }
        public float Rotation { get; init; }
        public float RotationPitch { get; init; }
        public static ArcNET.Editor.EditorMapObjectBrushRequest Erase() { }
        public static ArcNET.Editor.EditorMapObjectBrushRequest MoveByOffset(int deltaTileX, int deltaTileY) { }
        public static ArcNET.Editor.EditorMapObjectBrushRequest ReplaceWithProto(int protoNumber) { }
        public static ArcNET.Editor.EditorMapObjectBrushRequest Rotate(float rotation) { }
        public static ArcNET.Editor.EditorMapObjectBrushRequest RotatePitch(float rotationPitch) { }
        public static ArcNET.Editor.EditorMapObjectBrushRequest StampFromProto(int protoNumber) { }
    }
    public sealed class EditorMapObjectBrushResult
    {
        public EditorMapObjectBrushResult() { }
        public int CreatedObjectCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> CreatedObjects { get; init; }
        public bool HasChanges { get; }
        public int RemovedObjectCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> RemovedObjectIds { get; init; }
        public int UpdatedObjectCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> UpdatedObjectIds { get; init; }
    }
    public sealed class EditorMapObjectColorArray : System.IEquatable<ArcNET.Editor.EditorMapObjectColorArray>
    {
        public EditorMapObjectColorArray(System.ReadOnlySpan<uint> colors) { }
        public int Count { get; }
        public uint this[int index] { get; }
        public System.ReadOnlySpan<uint> AsSpan() { }
        public bool Equals(ArcNET.Editor.EditorMapObjectColorArray? other) { }
        public override bool Equals(object? obj) { }
        public override int GetHashCode() { }
    }
    public sealed class EditorMapObjectOverlayLightPreview
    {
        public EditorMapObjectOverlayLightPreview() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public ArcNET.Core.Primitives.Color? Color { get; init; }
        public int Flags { get; init; }
    }
    public sealed class EditorMapObjectPaletteSummary
    {
        public EditorMapObjectPaletteSummary() { }
        public required System.Collections.Generic.IReadOnlyList<string> AvailableCategories { get; init; }
        public bool CanBrowse { get; }
        public string? Category { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> Entries { get; init; }
        public bool HasSelectedEntry { get; }
        public required string MapName { get; init; }
        public required string MapViewStateId { get; init; }
        public string? SearchText { get; init; }
        public ArcNET.Editor.EditorObjectPaletteEntry? SelectedEntry { get; init; }
        public required ArcNET.Editor.EditorProjectMapObjectPlacementToolState ToolState { get; init; }
    }
    public sealed class EditorMapObjectPlacementToolSummary
    {
        public EditorMapObjectPlacementToolSummary() { }
        public bool CanPreviewOrApply { get; }
        public ArcNET.Editor.EditorObjectPalettePlacementSet? EffectivePlacementSet { get; init; }
        public required string MapName { get; init; }
        public required string MapViewStateId { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> MissingProtoNumbers { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> ResolvedPaletteEntries { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementPreset? SelectedPreset { get; init; }
        public required ArcNET.Editor.EditorProjectMapObjectPlacementToolState ToolState { get; init; }
    }
    public sealed class EditorMapObjectPreview
    {
        public EditorMapObjectPreview() { }
        public int BlitAlpha { get; init; }
        public uint BlitColor { get; init; }
        public int BlitFlags { get; init; }
        public int BlitScale { get; init; }
        public float CollisionHeight { get; init; }
        public required ArcNET.Core.Primitives.ArtId CurrentArtId { get; init; }
        public ArcNET.GameObjects.ObjectFlags Flags { get; init; }
        public bool IsDead { get; init; }
        public bool IsFlat { get; }
        public bool IsShrunk { get; }
        public bool IsTileGridSnapped { get; }
        public bool IsUnderAllScenery { get; }
        public bool IsWading { get; }
        public ArcNET.Core.Primitives.ArtId LightAid { get; init; }
        public ArcNET.Core.Primitives.Color? LightColor { get; init; }
        public int LightFlags { get; init; }
        public ArcNET.Core.Primitives.Location? Location { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; init; }
        public required ArcNET.GameObjects.ObjectType ObjectType { get; init; }
        public int OffsetX { get; init; }
        public int OffsetY { get; init; }
        public float OffsetZ { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> OverlayBackArtIds { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> OverlayForeArtIds { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectOverlayLightPreview> OverlayLights { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; init; }
        public uint? ReactionColor { get; init; }
        public float Rotation { get; init; }
        public int RotationIndex { get; init; }
        public required float RotationPitch { get; init; }
        public ArcNET.GameObjects.SceneryFlags SceneryFlags { get; init; }
        public ArcNET.Core.Primitives.ArtId ShadowArtId { get; init; }
        public string? SourceAssetPath { get; init; }
        public int? SourceObjectIndex { get; init; }
        public ArcNET.Editor.EditorMapObjectSpriteBounds? SpriteBounds { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> UnderlayArtIds { get; init; }
        public int WallFlags { get; init; }
    }
    public sealed class EditorMapObjectRenderItem
    {
        public EditorMapObjectRenderItem() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public int BlitAlpha { get; init; }
        public uint BlitColor { get; init; }
        public int BlitFlags { get; init; }
        public int BlitScale { get; init; }
        public ArcNET.Editor.EditorMapCommittedRenderLayer? CommittedRenderLayer { get; init; }
        public required ArcNET.Core.Primitives.ArtId CurrentArtId { get; init; }
        public required int DrawOrder { get; init; }
        public ArcNET.GameObjects.ObjectFlags Flags { get; init; }
        public bool IsDead { get; init; }
        public bool IsIndoorTile { get; init; }
        public bool IsRoofCovered { get; init; }
        public bool IsShrunk { get; init; }
        public required bool IsTileGridSnapped { get; init; }
        public ArcNET.Core.Primitives.ArtId LightAid { get; init; }
        public ArcNET.Core.Primitives.Color? LightColor { get; init; }
        public int LightFlags { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ObjectId { get; init; }
        public required ArcNET.GameObjects.ObjectType ObjectType { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; init; }
        public required float Rotation { get; init; }
        public int RotationIndex { get; init; }
        public required float RotationPitch { get; init; }
        public int SameTileOrder { get; init; }
        public ArcNET.GameObjects.SceneryFlags SceneryFlags { get; init; }
        public required string SectorAssetPath { get; init; }
        public int? SourceObjectIndex { get; init; }
        public ArcNET.Editor.EditorMapObjectSpriteBounds? SpriteBounds { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
        public int WallFlags { get; init; }
    }
    public sealed class EditorMapObjectSelectionSummary
    {
        public EditorMapObjectSelectionSummary() { }
        public bool CanApplyTrackedEdit { get; }
        public bool HasResolvedObjects { get; }
        public required string MapName { get; init; }
        public required string MapViewStateId { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> MissingObjectIds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> SectorAssetPaths { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectPreview> SelectedObjects { get; init; }
        public required ArcNET.Editor.EditorProjectMapSelectionState Selection { get; init; }
    }
    public sealed class EditorMapObjectSpriteBounds
    {
        public EditorMapObjectSpriteBounds() { }
        public required int MaxFrameCenterX { get; init; }
        public required int MaxFrameCenterY { get; init; }
        public required int MaxFrameHeight { get; init; }
        public required int MaxFrameWidth { get; init; }
    }
    public sealed class EditorMapObjectTransformRequest
    {
        public EditorMapObjectTransformRequest() { }
        public bool AlignToTileGrid { get; init; }
        public int DeltaTileX { get; init; }
        public int DeltaTileY { get; init; }
        public float? EffectiveRotation { get; }
        public bool HasChanges { get; }
        public bool HasMoveOffset { get; }
        public float? Rotation { get; init; }
        public int? RotationIndex { get; init; }
        public float? RotationPitch { get; init; }
        public static ArcNET.Editor.EditorMapObjectTransformRequest MoveByOffset(int deltaTileX, int deltaTileY) { }
        public static ArcNET.Editor.EditorMapObjectTransformRequest Rotate(float rotation) { }
        public static ArcNET.Editor.EditorMapObjectTransformRequest RotatePitch(float rotationPitch) { }
        public static ArcNET.Editor.EditorMapObjectTransformRequest SnapToTileGrid() { }
        public static ArcNET.Editor.EditorMapObjectTransformRequest Transform(int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = false) { }
    }
    public sealed class EditorMapPaintableScene
    {
        public EditorMapPaintableScene() { }
        public required double HeightPixels { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapPaintableSceneItem> Items { get; init; }
        public required string MapName { get; init; }
        public required ArcNET.Editor.EditorMapRenderSpriteCoverage SpriteCoverage { get; init; }
        public ArcNET.Editor.IEditorMapRenderSpriteSource? SpriteSource { get; init; }
        public required ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public required double WidthPixels { get; init; }
        public System.Collections.Generic.IEnumerable<ArcNET.Editor.EditorMapPaintableSceneItem> EnumerateVisibleItems(ArcNET.Editor.EditorMapSceneViewportLayout viewport) { }
    }
    public static class EditorMapPaintableSceneBuilder
    {
        public static ArcNET.Editor.EditorMapPaintableScene Build(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorMapPlacementPreview? placementPreview = null, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null, ArcNET.Editor.EditorMapRenderSpriteCoverage? existingSpriteCoverage = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static ArcNET.Editor.EditorMapPaintableScene BuildPlacementOverlay(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorMapPlacementPreview? placementPreview, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public readonly struct EditorMapPaintableSceneGeometry : System.IEquatable<ArcNET.Editor.EditorMapPaintableSceneGeometry>
    {
        public EditorMapPaintableSceneGeometry(ArcNET.Editor.EditorMapPaintableSceneGeometryKind Kind, double CenterX, double CenterY, double Width, double Height) { }
        public double CenterX { get; init; }
        public double CenterY { get; init; }
        public double Height { get; init; }
        public ArcNET.Editor.EditorMapPaintableSceneGeometryKind Kind { get; init; }
        public double Width { get; init; }
    }
    public enum EditorMapPaintableSceneGeometryKind
    {
        Rectangle = 0,
        Diamond = 1,
    }
    public sealed class EditorMapPaintableSceneItem
    {
        public EditorMapPaintableSceneItem() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public ArcNET.Editor.EditorMapSpriteBlendMode BlendMode { get; init; }
        public ArcNET.Editor.EditorMapCommittedRenderLayer? CommittedRenderLayer { get; init; }
        public required int DrawOrder { get; init; }
        public ArcNET.Editor.EditorMapPaintableSceneGeometry? Geometry { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderPoint>? GeometryPoints { get; init; }
        public required double Height { get; init; }
        public bool IsRoofCovered { get; init; }
        public required ArcNET.Editor.EditorMapRenderQueueItemKind Kind { get; init; }
        public required double Left { get; init; }
        public ArcNET.Formats.SectorLightFlags? LightFlags { get; init; }
        public ArcNET.Editor.EditorMapObjectAlphaLerp? ObjectAlphaLerp { get; init; }
        public ArcNET.Editor.EditorMapObjectColorArray? ObjectColorArray { get; init; }
        public ArcNET.Editor.EditorMapRoofAlphaLerp? RoofAlphaLerp { get; init; }
        public required double SortKey { get; init; }
        public ArcNET.Editor.EditorMapRenderSprite? Sprite { get; init; }
        public ArcNET.Editor.EditorMapPaintableSceneSpriteDestinationRect? SpriteDestinationRect { get; init; }
        public ArcNET.Editor.EditorMapPaintableSceneSpriteReference? SpriteReference { get; init; }
        public ArcNET.Editor.EditorMapPaintableSceneSpriteSourceRect? SpriteSourceRect { get; init; }
        public required double SuggestedOpacity { get; init; }
        public uint? SuggestedTintColor { get; init; }
        public bool SuppressFallback { get; init; }
        public ArcNET.Editor.EditorMapTileLightDiagnostics? TileLightDiagnostics { get; init; }
        public ArcNET.Editor.EditorMapTileOverlayKind? TileOverlayKind { get; init; }
        public bool TintIgnoresLightVisibility { get; init; }
        public required double Top { get; init; }
        public bool UseGrayscalePaletteOverride { get; init; }
        public bool UseLightMaskTint { get; init; }
        public bool UseSubtractiveShadowBlend { get; init; }
        public required double Width { get; init; }
    }
    public readonly struct EditorMapPaintableSceneSpriteDestinationRect : System.IEquatable<ArcNET.Editor.EditorMapPaintableSceneSpriteDestinationRect>
    {
        public EditorMapPaintableSceneSpriteDestinationRect(double X, double Y, double Width, double Height) { }
        public double Height { get; init; }
        public double Width { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
    }
    public sealed class EditorMapPaintableSceneSpriteReference
    {
        public EditorMapPaintableSceneSpriteReference() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public string? AssetPath { get; init; }
        public required int CenterX { get; init; }
        public required int CenterY { get; init; }
        public int DeltaX { get; init; }
        public int DeltaY { get; init; }
        public required int FrameIndex { get; init; }
        public uint FrameRate { get; init; }
        public int FramesPerRotation { get; init; }
        public required int Height { get; init; }
        public bool IsShrunk { get; init; }
        public ArcNET.Editor.EditorMapRenderQueueItemKind? RenderItemKind { get; init; }
        public required int RotationIndex { get; init; }
        public int ScalePercent { get; init; }
        public required int Width { get; init; }
    }
    public readonly struct EditorMapPaintableSceneSpriteSourceRect : System.IEquatable<ArcNET.Editor.EditorMapPaintableSceneSpriteSourceRect>
    {
        public EditorMapPaintableSceneSpriteSourceRect(int X, int Y, int Width, int Height) { }
        public int Height { get; init; }
        public int Width { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
    }
    public sealed class EditorMapPlacementPreview
    {
        public EditorMapPlacementPreview() { }
        public required double HeightPixels { get; init; }
        public required string MapName { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapPlacementPreviewObject> Objects { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderQueueItem> RenderQueue { get; init; }
        public required double TileHeightPixels { get; init; }
        public required double TileWidthPixels { get; init; }
        public required ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public required double WidthPixels { get; init; }
    }
    public sealed class EditorMapPlacementPreviewObject
    {
        public EditorMapPlacementPreviewObject() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public int BlitScale { get; init; }
        public required ArcNET.Core.Primitives.ArtId CurrentArtId { get; init; }
        public required int DrawOrder { get; init; }
        public bool IsShrunk { get; init; }
        public required bool IsTileGridSnapped { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required ArcNET.GameObjects.ObjectType ObjectType { get; init; }
        public required ArcNET.Core.Primitives.GameObjectGuid ProtoId { get; init; }
        public required float Rotation { get; init; }
        public int RotationIndex { get; init; }
        public required float RotationPitch { get; init; }
        public required string SectorAssetPath { get; init; }
        public ArcNET.Editor.EditorMapObjectSpriteBounds? SpriteBounds { get; init; }
        public required ArcNET.Editor.EditorMapPlacementPreviewState State { get; init; }
        public required double SuggestedOpacity { get; init; }
        public uint? SuggestedTintColor { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
        public string? ValidationMessage { get; init; }
    }
    public enum EditorMapPlacementPreviewState
    {
        Valid = 0,
        BlockedTile = 1,
        OccupiedTile = 2,
    }
    public sealed class EditorMapPreview
    {
        public EditorMapPreview() { }
        public required int Height { get; init; }
        public required string Legend { get; init; }
        public required string MapName { get; init; }
        public required ArcNET.Editor.EditorMapPreviewMode Mode { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> Rows { get; init; }
        public required int Width { get; init; }
    }
    public static class EditorMapPreviewBuilder
    {
        public static ArcNET.Editor.EditorMapPreview Build(ArcNET.Editor.EditorMapProjection projection, ArcNET.Editor.EditorMapPreviewMode mode) { }
    }
    public enum EditorMapPreviewMode
    {
        Occupancy = 0,
        Objects = 1,
        Combined = 2,
        Roofs = 3,
        Lights = 4,
        Blocked = 5,
        Scripts = 6,
    }
    public sealed class EditorMapProjection
    {
        public EditorMapProjection() { }
        public required int Height { get; init; }
        public required string MapName { get; init; }
        public required int MaxSectorX { get; init; }
        public required int MaxSectorY { get; init; }
        public required int MinSectorX { get; init; }
        public required int MinSectorY { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSectorProjection> Sectors { get; init; }
        public required int UnpositionedSectorCount { get; init; }
        public required int Width { get; init; }
    }
    public sealed class EditorMapPropertiesDefinition
    {
        public EditorMapPropertiesDefinition() { }
        public required int ArtId { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required ulong LimitX { get; init; }
        public required ulong LimitY { get; init; }
    }
    public sealed class EditorMapRenderHit
    {
        public EditorMapRenderHit() { }
        public bool HasObjectHits { get; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectRenderItem> ObjectHits { get; init; }
        public required ArcNET.Editor.EditorMapRenderPoint RenderPoint { get; init; }
        public ArcNET.Core.Primitives.Location RoofCell { get; }
        public required string SectorAssetPath { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public readonly struct EditorMapRenderIndexEntry : System.IEquatable<ArcNET.Editor.EditorMapRenderIndexEntry>
    {
        public EditorMapRenderIndexEntry(ArcNET.Editor.EditorMapRenderQueueItemKind Kind, int PayloadIndex, double SortKey, int DrawOrder) { }
        public int DrawOrder { get; init; }
        public ArcNET.Editor.EditorMapRenderQueueItemKind Kind { get; init; }
        public int PayloadIndex { get; init; }
        public double SortKey { get; init; }
    }
    public readonly struct EditorMapRenderPoint : System.IEquatable<ArcNET.Editor.EditorMapRenderPoint>
    {
        public EditorMapRenderPoint(double X, double Y) { }
        public double X { get; init; }
        public double Y { get; init; }
    }
    public sealed class EditorMapRenderQueueItem
    {
        public EditorMapRenderQueueItem() { }
        public ArcNET.Editor.EditorMapCommittedRenderLayer? CommittedRenderLayer { get; init; }
        public required int DrawOrder { get; init; }
        public required ArcNET.Editor.EditorMapRenderQueueItemKind Kind { get; init; }
        public ArcNET.Editor.EditorMapLightRenderItem? Light { get; init; }
        public ArcNET.Editor.EditorMapObjectRenderItem? Object { get; init; }
        public ArcNET.Editor.EditorMapObjectAuxiliaryRenderItem? ObjectAuxiliaryItem { get; init; }
        public ArcNET.Editor.EditorMapPlacementPreviewObject? PlacementPreviewObject { get; init; }
        public ArcNET.Editor.EditorMapRoofRenderItem? Roof { get; init; }
        public required double SortKey { get; init; }
        public ArcNET.Editor.EditorMapFloorTileRenderItem? Tile { get; init; }
        public ArcNET.Editor.EditorMapTileOverlayRenderItem? TileOverlay { get; init; }
    }
    public enum EditorMapRenderQueueItemKind
    {
        FloorTile = 0,
        Object = 1,
        Roof = 2,
        PlacementPreviewObject = 3,
        TileOverlay = 4,
        ObjectAuxiliary = 5,
        Light = 6,
    }
    public sealed class EditorMapRenderSprite
    {
        public EditorMapRenderSprite() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required string AssetPath { get; init; }
        public required int CenterX { get; init; }
        public required int CenterY { get; init; }
        public int DeltaX { get; init; }
        public int DeltaY { get; init; }
        public required int FrameIndex { get; init; }
        public required uint FrameRate { get; init; }
        public int FramesPerRotation { get; init; }
        public required int Height { get; init; }
        public required byte[] PixelData { get; init; }
        public required ArcNET.Editor.EditorArtPreviewPixelFormat PixelFormat { get; init; }
        public ArcNET.Editor.EditorMapRenderQueueItemKind? RenderItemKind { get; init; }
        public required int RotationIndex { get; init; }
        public required int Stride { get; init; }
        public required int Width { get; init; }
    }
    public sealed class EditorMapRenderSpriteCoverage
    {
        public EditorMapRenderSpriteCoverage() { }
        public bool IsComplete { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> ReferencedArtIds { get; init; }
        public required int ReferencedSpriteReferenceCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderSpriteCoverageReference> ReferencedSpriteReferences { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> ResolvedArtIds { get; init; }
        public required int ResolvedSpriteReferenceCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderSpriteCoverageReference> ResolvedSpriteReferences { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> UnresolvedArtIds { get; init; }
        public required int UnresolvedSpriteReferenceCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderSpriteCoverageReference> UnresolvedSpriteReferences { get; init; }
        public static ArcNET.Editor.EditorMapRenderSpriteCoverage Empty { get; }
    }
    public sealed class EditorMapRenderSpriteCoverageReference
    {
        public EditorMapRenderSpriteCoverageReference() { }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required bool IsShrunk { get; init; }
        public required ArcNET.Editor.EditorMapRenderQueueItemKind RenderItemKind { get; init; }
        public required int RotationIndex { get; init; }
        public required int ScalePercent { get; init; }
        public ArcNET.Editor.EditorMapRenderSpriteRequest CreateRequest() { }
    }
    public sealed class EditorMapRenderSpriteMetrics
    {
        public EditorMapRenderSpriteMetrics() { }
        public string? AssetPath { get; init; }
        public required int CenterX { get; init; }
        public required int CenterY { get; init; }
        public int DeltaX { get; init; }
        public int DeltaY { get; init; }
        public int FrameIndex { get; init; }
        public uint FrameRate { get; init; }
        public int FramesPerRotation { get; init; }
        public required int Height { get; init; }
        public int RotationIndex { get; init; }
        public required int Width { get; init; }
        public static ArcNET.Editor.EditorMapRenderSpriteMetrics FromSprite(ArcNET.Editor.EditorMapRenderSprite sprite) { }
    }
    public sealed class EditorMapRenderSpriteRequest
    {
        public EditorMapRenderSpriteRequest() { }
        public int FrameIndex { get; init; }
        public bool IsShrunk { get; init; }
        public ArcNET.Editor.EditorMapRenderQueueItemKind? RenderItemKind { get; init; }
        public int RotationIndex { get; init; }
        public int ScalePercent { get; init; }
    }
    public sealed class EditorMapRenderViewportState
    {
        public EditorMapRenderViewportState() { }
        public double CenterRenderX { get; init; }
        public double CenterRenderY { get; init; }
        public double Zoom { get; init; }
    }
    public readonly struct EditorMapRoofAlphaLerp : System.IEquatable<ArcNET.Editor.EditorMapRoofAlphaLerp>
    {
        public EditorMapRoofAlphaLerp(byte TopLeft, byte TopRight, byte BottomLeft, byte BottomRight) { }
        public byte BottomLeft { get; init; }
        public byte BottomRight { get; init; }
        public byte TopLeft { get; init; }
        public byte TopRight { get; init; }
    }
    public sealed class EditorMapRoofRenderItem
    {
        public EditorMapRoofRenderItem() { }
        public required double AnchorX { get; init; }
        public required double AnchorY { get; init; }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public required int DrawOrder { get; init; }
        public int FootprintTileHeight { get; }
        public int FootprintTileWidth { get; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required ArcNET.Core.Primitives.Location RoofCell { get; init; }
        public required string SectorAssetPath { get; init; }
    }
    public sealed class EditorMapSceneHit
    {
        public EditorMapSceneHit() { }
        public bool HasObjectHits { get; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectPreview> ObjectHits { get; init; }
        public ArcNET.Core.Primitives.Location RoofCell { get; }
        public required string SectorAssetPath { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public sealed class EditorMapScenePreview
    {
        public EditorMapScenePreview() { }
        public required int Height { get; init; }
        public required string MapName { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSectorScenePreview> Sectors { get; init; }
        public required int UnpositionedSectorCount { get; init; }
        public required int Width { get; init; }
    }
    public static class EditorMapScenePreviewBuilder
    {
        public static ArcNET.Editor.EditorMapScenePreview Build(ArcNET.Editor.EditorMapProjection projection, System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Sector> sectorsByAssetPath, ArcNET.Formats.JmpFile? jumpPoints = null) { }
        public static ArcNET.Editor.EditorMapScenePreview Build(ArcNET.Editor.EditorMapProjection projection, System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Sector> sectorsByAssetPath, System.Func<ArcNET.Core.Primitives.ArtId, ArcNET.Formats.ArtFile?>? artResolver, ArcNET.Formats.JmpFile? jumpPoints = null) { }
        public static ArcNET.Editor.EditorMapObjectPreview BuildObjectPreview(ArcNET.Formats.MobData mob, System.Func<ArcNET.Core.Primitives.ArtId, ArcNET.Formats.ArtFile?>? artResolver = null) { }
        public static ArcNET.Editor.EditorMapSectorScenePreview BuildSector(ArcNET.Editor.EditorMapSectorProjection sectorProjection, ArcNET.Formats.Sector sector) { }
        public static ArcNET.Editor.EditorMapSectorScenePreview BuildSector(ArcNET.Editor.EditorMapSectorProjection sectorProjection, ArcNET.Formats.Sector sector, System.Func<ArcNET.Core.Primitives.ArtId, ArcNET.Formats.ArtFile?>? artResolver) { }
    }
    public static class EditorMapSceneRenderSpaceMath
    {
        public static ArcNET.Editor.EditorMapSceneViewportLayout CreateViewportLayout(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, double viewportWidth, double viewportHeight, ArcNET.Editor.EditorMapRenderViewportState? viewportState = null) { }
        public static ArcNET.Editor.EditorMapRenderViewportState CreateViewportState(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorProjectMapCameraState? camera = null) { }
        public static ArcNET.Editor.EditorMapRenderHit? HitTestScene(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorMapSceneViewportLayout layout, double viewportX, double viewportY) { }
        public static ArcNET.Editor.EditorProjectMapSelectionState? HitTestSceneSelection(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorMapSceneViewportLayout layout, double viewportX, double viewportY) { }
        public static ArcNET.Editor.EditorMapRenderPoint ProjectMapTileCenter(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, double mapTileX, double mapTileY) { }
        public static double RenderToViewportX(ArcNET.Editor.EditorMapSceneViewportLayout layout, double renderX) { }
        public static double RenderToViewportY(ArcNET.Editor.EditorMapSceneViewportLayout layout, double renderY) { }
        public static ArcNET.Editor.EditorMapTilePoint UnprojectMapTile(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, double renderX, double renderY) { }
        public static double ViewportToRenderX(ArcNET.Editor.EditorMapSceneViewportLayout layout, double viewportX) { }
        public static double ViewportToRenderY(ArcNET.Editor.EditorMapSceneViewportLayout layout, double viewportY) { }
    }
    public sealed class EditorMapSceneSectorHitGroup
    {
        public EditorMapSceneSectorHitGroup() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneHit> Hits { get; init; }
        public required int LocalX { get; init; }
        public required int LocalY { get; init; }
        public required string SectorAssetPath { get; init; }
        public int TileCount { get; }
    }
    public enum EditorMapSceneViewMode
    {
        TopDown = 0,
        Isometric = 1,
    }
    public sealed class EditorMapSceneViewportLayout
    {
        public EditorMapSceneViewportLayout() { }
        public required double CenterRenderX { get; init; }
        public required double CenterRenderY { get; init; }
        public required double SceneHeight { get; init; }
        public required double SceneWidth { get; init; }
        public required double ViewportHeight { get; init; }
        public required double ViewportWidth { get; init; }
        public double VisibleBottom { get; }
        public double VisibleLeft { get; }
        public double VisibleRight { get; }
        public double VisibleTop { get; }
        public required double Zoom { get; init; }
    }
    public enum EditorMapSectorDensityBand
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Peak = 4,
    }
    [System.Flags]
    public enum EditorMapSectorPreviewFlags
    {
        None = 0,
        Occupied = 1,
        HasRoofs = 2,
        HasLights = 4,
        HasBlockedTiles = 8,
        HasScripts = 16,
    }
    public sealed class EditorMapSectorProjection
    {
        public EditorMapSectorProjection() { }
        public ArcNET.Editor.EditorAssetEntry Asset { get; }
        public required ArcNET.Editor.EditorMapSectorDensityBand BlockedTileDensityBand { get; init; }
        public required int LocalX { get; init; }
        public required int LocalY { get; init; }
        public required ArcNET.Editor.EditorMapSectorDensityBand ObjectDensityBand { get; init; }
        public ArcNET.Editor.EditorMapSectorPreviewFlags PreviewFlags { get; }
        public required ArcNET.Editor.EditorSectorSummary Sector { get; init; }
        public required int SectorX { get; init; }
        public required int SectorY { get; init; }
    }
    public sealed class EditorMapSectorRenderSlice
    {
        public EditorMapSectorRenderSlice() { }
        public required ArcNET.Editor.EditorMapSectorRenderSliceBounds Bounds { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapLightRenderItem> Lights { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectRenderItem> Objects { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapTileOverlayRenderItem> Overlays { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRenderIndexEntry> Queue { get; init; }
        public long Revision { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapRoofRenderItem> Roofs { get; init; }
        public required string SectorAssetPath { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapFloorTileRenderItem> Tiles { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectRenderItem> GetObjectsAtTile(ArcNET.Core.Primitives.Location tile) { }
        public bool TryGetTile(ArcNET.Core.Primitives.Location tile, out ArcNET.Editor.EditorMapFloorTileRenderItem? item) { }
    }
    public readonly struct EditorMapSectorRenderSliceBounds : System.IEquatable<ArcNET.Editor.EditorMapSectorRenderSliceBounds>
    {
        public EditorMapSectorRenderSliceBounds(double Left, double Top, double Width, double Height, int MinMapTileX, int MinMapTileY, int MaxMapTileX, int MaxMapTileY) { }
        public double Bottom { get; }
        public double Height { get; init; }
        public double Left { get; init; }
        public int MaxMapTileX { get; init; }
        public int MaxMapTileY { get; init; }
        public int MinMapTileX { get; init; }
        public int MinMapTileY { get; init; }
        public double Right { get; }
        public double Top { get; init; }
        public double Width { get; init; }
        public bool Intersects(ArcNET.Editor.EditorMapSceneViewportLayout viewport) { }
    }
    public sealed class EditorMapSectorScenePreview
    {
        public EditorMapSectorScenePreview() { }
        public required string AssetPath { get; init; }
        public required uint[] BlockMask { get; init; }
        public required ArcNET.Editor.EditorMapSectorDensityBand BlockedTileDensityBand { get; init; }
        public System.Collections.Generic.HashSet<int> JumpPointTileIndices { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapJumpPointPreview> JumpPoints { get; init; }
        public int LightSchemeIdx { get; init; }
        public System.Collections.Generic.HashSet<int> LightTileIndices { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapLightPreview> Lights { get; init; }
        public required int LocalX { get; init; }
        public required int LocalY { get; init; }
        public required ArcNET.Editor.EditorMapSectorDensityBand ObjectDensityBand { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapObjectPreview> Objects { get; init; }
        public required ArcNET.Editor.EditorMapSectorPreviewFlags PreviewFlags { get; init; }
        public uint[]? RoofArtIds { get; init; }
        public int RoofHeight { get; }
        public ulong[]? RoofRowMasks { get; }
        public int RoofWidth { get; }
        public System.Collections.Generic.HashSet<int> ScriptedTileIndices { get; }
        public required int SectorX { get; init; }
        public required int SectorY { get; init; }
        public long TerrainRevision { get; }
        public required uint[] TileArtIds { get; init; }
        public int TileHeight { get; }
        public ulong[] TileRowMasks { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapTileScriptPreview> TileScripts { get; init; }
        public int TileWidth { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> UniqueTerrainFloorArtIds { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> UniqueTerrainLightArtIds { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> UniqueTerrainRoofArtIds { get; }
        public uint? GetRoofArtId(int roofX, int roofY) { }
        public uint GetTileArtId(int tileX, int tileY) { }
        public bool IsTileBlocked(int tileX, int tileY) { }
    }
    public enum EditorMapSpriteBlendMode
    {
        SourceOver = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 3,
    }
    public sealed class EditorMapTerrainPaletteSummary
    {
        public EditorMapTerrainPaletteSummary() { }
        public bool CanBrowse { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> Entries { get; init; }
        public bool HasSelectedEntry { get; }
        public required string MapName { get; init; }
        public required string MapPropertiesAssetPath { get; init; }
        public required string MapViewStateId { get; init; }
        public ArcNET.Editor.EditorTerrainPaletteEntry? SelectedEntry { get; init; }
        public required ArcNET.Editor.EditorProjectMapTerrainToolState ToolState { get; init; }
    }
    public sealed class EditorMapTerrainToolSummary
    {
        public EditorMapTerrainToolSummary() { }
        public bool CanApply { get; }
        public required string MapName { get; init; }
        public required string MapViewStateId { get; init; }
        public ArcNET.Editor.EditorTerrainPaletteEntry? SelectedEntry { get; init; }
        public required ArcNET.Editor.EditorProjectMapTerrainToolState ToolState { get; init; }
    }
    public sealed class EditorMapTileBounds
    {
        public EditorMapTileBounds() { }
        public double Height { get; }
        public required double MaxTileX { get; init; }
        public required double MaxTileY { get; init; }
        public required double MinTileX { get; init; }
        public required double MinTileY { get; init; }
        public double Width { get; }
    }
    public readonly struct EditorMapTileLightDiagnostics : System.IEquatable<ArcNET.Editor.EditorMapTileLightDiagnostics>
    {
        public EditorMapTileLightDiagnostics(uint? TopLeft, uint? TopCenter, uint? TopRight, uint? MiddleLeft, uint? MiddleCenter, uint? MiddleRight, uint? BottomLeft, uint? BottomCenter, uint? BottomRight) { }
        public uint? BottomCenter { get; init; }
        public uint? BottomLeft { get; init; }
        public uint? BottomRight { get; init; }
        public bool HasAnySample { get; }
        public bool HasInterpolationVariance { get; }
        public uint? MiddleCenter { get; init; }
        public uint? MiddleLeft { get; init; }
        public uint? MiddleRight { get; init; }
        public uint? TopCenter { get; init; }
        public uint? TopLeft { get; init; }
        public uint? TopRight { get; init; }
    }
    public enum EditorMapTileOverlayKind
    {
        BlockedTile = 0,
        Light = 1,
        Script = 2,
        JumpPoint = 3,
    }
    public sealed class EditorMapTileOverlayRenderItem
    {
        public EditorMapTileOverlayRenderItem() { }
        public required double CenterX { get; init; }
        public required double CenterY { get; init; }
        public required int DrawOrder { get; init; }
        public required ArcNET.Editor.EditorMapTileOverlayKind Kind { get; init; }
        public required int MapTileX { get; init; }
        public required int MapTileY { get; init; }
        public required string SectorAssetPath { get; init; }
        public required double SuggestedOpacity { get; init; }
        public required uint SuggestedTintColor { get; init; }
        public required ArcNET.Core.Primitives.Location Tile { get; init; }
    }
    public readonly struct EditorMapTilePoint : System.IEquatable<ArcNET.Editor.EditorMapTilePoint>
    {
        public EditorMapTilePoint(double MapTileX, double MapTileY) { }
        public double MapTileX { get; init; }
        public double MapTileY { get; init; }
    }
    public sealed class EditorMapTileScriptPreview
    {
        public EditorMapTileScriptPreview() { }
        public required uint NodeFlags { get; init; }
        public required uint ScriptCounters { get; init; }
        public required uint ScriptFlags { get; init; }
        public required int ScriptId { get; init; }
        public required int TileIndex { get; init; }
        public required int TileX { get; init; }
        public required int TileY { get; init; }
    }
    public sealed class EditorMapWorldEditComposeProgress
    {
        public EditorMapWorldEditComposeProgress() { }
        public required string Activity { get; init; }
        public string? DominantActivity { get; init; }
        public System.TimeSpan? DominantElapsed { get; init; }
        public required System.TimeSpan Elapsed { get; init; }
        public required float Progress { get; init; }
        public required System.TimeSpan StageElapsed { get; init; }
    }
    public sealed class EditorMapWorldEditScene
    {
        public EditorMapWorldEditScene() { }
        public string MapName { get; }
        public required ArcNET.Editor.EditorProjectMapViewState MapViewState { get; init; }
        public required ArcNET.Editor.EditorMapPaintableScene PaintableScene { get; init; }
        public ArcNET.Editor.EditorMapPlacementPreview? PlacementPreview { get; init; }
        public required ArcNET.Editor.EditorMapFloorRenderPreview SceneRender { get; init; }
        public required ArcNET.Editor.EditorMapRenderSpriteCoverage SpriteCoverage { get; init; }
        public required ArcNET.Editor.EditorMapSceneViewportLayout ViewportLayout { get; init; }
    }
    public sealed class EditorMapWorldEditSceneRequest
    {
        public EditorMapWorldEditSceneRequest() { }
        public ArcNET.Editor.EditorArtResolver? ArtResolver { get; init; }
        public string? ChangedSectorAssetPath { get; init; }
        public ArcNET.Editor.EditorMapFloorRenderPreview? ExistingPreview { get; init; }
        public ArcNET.Editor.EditorMapRenderSpriteCoverage? ExistingSpriteCoverage { get; init; }
        public int? FocusedObjectSectorRadius { get; init; }
        public int? FocusedTerrainSectorRadius { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementRequest? PlacementRequest { get; init; }
        public bool PreloadSceneSprites { get; init; }
        public ArcNET.Editor.EditorMapFloorRenderRequest? RenderRequest { get; init; }
        public ArcNET.Editor.IEditorMapRenderSpriteSource? SpriteSource { get; init; }
        public ArcNET.Editor.EditorMapRenderViewportState? Viewport { get; init; }
        public double? ViewportHeight { get; init; }
        public double? ViewportWidth { get; init; }
    }
    public sealed class EditorMapWorldEditShell
    {
        public EditorMapWorldEditShell() { }
        public required ArcNET.Editor.EditorProjectMapWorldEditActiveTool ActiveTool { get; init; }
        public int? FocusedObjectSectorRadius { get; init; }
        public int? FocusedTerrainSectorRadius { get; init; }
        public bool HasTrackedPlacementPreview { get; }
        public ArcNET.Formats.JmpFile? JumpPoints { get; init; }
        public required string MapName { get; init; }
        public required string MapViewStateId { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorSummary ObjectInspector { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorBlendingSummary ObjectInspectorBlending { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorContainerSummary ObjectInspectorContainer { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorCritterProgressionSummary ObjectInspectorCritterProgression { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorFlagsSummary ObjectInspectorFlags { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorGeneratorSummary ObjectInspectorGenerator { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorLightSummary ObjectInspectorLight { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorScriptAttachmentsSummary ObjectInspectorScriptAttachments { get; init; }
        public required ArcNET.Editor.EditorProjectMapObjectInspectorState ObjectInspectorState { get; init; }
        public required ArcNET.Editor.EditorMapObjectPaletteSummary ObjectPalette { get; init; }
        public required ArcNET.Editor.EditorMapObjectPlacementToolSummary ObjectPlacementTool { get; init; }
        public required ArcNET.Editor.EditorMapObjectSelectionSummary ObjectSelection { get; init; }
        public required ArcNET.Editor.EditorMapFloorRenderRequest RenderRequest { get; init; }
        public required ArcNET.Editor.EditorMapWorldEditScene Scene { get; init; }
        public required ArcNET.Editor.EditorMapTerrainPaletteSummary TerrainPalette { get; init; }
        public required ArcNET.Editor.EditorMapTerrainToolSummary TerrainTool { get; init; }
        public ArcNET.Editor.EditorMapPaintableScene? TrackedPlacementPaintableScene { get; init; }
        public ArcNET.Editor.EditorMapPlacementPreview? TrackedPlacementPreview { get; init; }
        public ArcNET.Editor.EditorMapPaintableScene? TrackedTerrainFacadePaintableScene { get; init; }
        public required ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
    }
    public sealed class EditorMapWorldEditShellRequest
    {
        public EditorMapWorldEditShellRequest() { }
        public ArcNET.Editor.EditorMapAmbientLightingState? AmbientLighting { get; init; }
        public ArcNET.Editor.EditorArtResolver? ArtResolver { get; init; }
        public int? FocusedObjectSectorRadius { get; init; }
        public int? FocusedTerrainSectorRadius { get; init; }
        public bool IncludeEditorObjectStateTint { get; init; }
        public bool IncludeFloorLightTint { get; init; }
        public bool IncludeFullObjectPaletteBrowse { get; init; }
        public bool IncludeTrackedPlacementPreview { get; init; }
        public string? ObjectPaletteCategory { get; init; }
        public string? ObjectPaletteSearchText { get; init; }
        public bool PreloadSceneSprites { get; init; }
        public ArcNET.Editor.IEditorMapRenderSpriteSource? SpriteSource { get; init; }
        public ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public double? ViewportHeight { get; init; }
        public double? ViewportWidth { get; init; }
    }
    public sealed class EditorMessageDefinition
    {
        public EditorMessageDefinition() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MessageEntry> Entries { get; init; }
        public required int EntryCount { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public int? MaxEntryIndex { get; init; }
        public int? MinEntryIndex { get; init; }
    }
    public sealed class EditorObjectInspectorBlendingSummary
    {
        public EditorObjectInspectorBlendingSummary() { }
        public int BlitAlpha { get; init; }
        public ArcNET.Core.Primitives.Color BlitColor { get; init; }
        public ArcNET.GameObjects.BlitFlags BlitFlags { get; init; }
        public int BlitScale { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public int Material { get; init; }
    }
    public sealed class EditorObjectInspectorBlendingUpdate
    {
        public EditorObjectInspectorBlendingUpdate() { }
        public int? BlitAlpha { get; init; }
        public ArcNET.Core.Primitives.Color? BlitColor { get; init; }
        public ArcNET.GameObjects.BlitFlags? BlitFlags { get; init; }
        public int? BlitScale { get; init; }
        public bool HasChanges { get; }
        public int? Material { get; init; }
    }
    public sealed class EditorObjectInspectorContainerSummary
    {
        public EditorObjectInspectorContainerSummary() { }
        public int? ContainedGoldQuantity { get; init; }
        public ArcNET.GameObjects.ContainerFlags ContainerFlags { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public System.Collections.Generic.IReadOnlyList<System.Guid> Inventory { get; init; }
        public bool IsContainerTarget { get; }
        public int KeyId { get; init; }
        public int LockDifficulty { get; init; }
    }
    public sealed class EditorObjectInspectorContainerUpdate
    {
        public EditorObjectInspectorContainerUpdate() { }
        public ArcNET.GameObjects.ContainerFlags? ContainerFlags { get; init; }
        public bool HasChanges { get; }
        public int? KeyId { get; init; }
        public int? LockDifficulty { get; init; }
    }
    public sealed class EditorObjectInspectorCritterProgressionSummary
    {
        public EditorObjectInspectorCritterProgressionSummary() { }
        public int Age { get; init; }
        public int Alignment { get; init; }
        public ArcNET.Core.Primitives.GameObjectGuid? CarriedGoldHandle { get; init; }
        public int? CarriedGoldQuantity { get; init; }
        public int ExperiencePoints { get; init; }
        public int FatePoints { get; init; }
        public int FatigueAdjustment { get; init; }
        public int FatiguePoints { get; init; }
        public int Gender { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public bool IsCritterTarget { get; }
        public int Level { get; init; }
        public int MagickPoints { get; init; }
        public int PoisonLevel { get; init; }
        public int Race { get; init; }
        public int SkillBackstab { get; init; }
        public int SkillBow { get; init; }
        public int SkillDisarmTraps { get; init; }
        public int SkillDodge { get; init; }
        public int SkillFirearms { get; init; }
        public int SkillGambling { get; init; }
        public int SkillHaggle { get; init; }
        public int SkillHeal { get; init; }
        public int SkillMelee { get; init; }
        public int SkillPersuasion { get; init; }
        public int SkillPickLocks { get; init; }
        public int SkillPickPocket { get; init; }
        public int SkillProwling { get; init; }
        public int SkillRepair { get; init; }
        public int SkillSpotTrap { get; init; }
        public int SkillThrowing { get; init; }
        public int SpellAir { get; init; }
        public int SpellConveyance { get; init; }
        public int SpellDivination { get; init; }
        public int SpellEarth { get; init; }
        public int SpellFire { get; init; }
        public int SpellForce { get; init; }
        public int SpellMastery { get; init; }
        public int SpellMental { get; init; }
        public int SpellMeta { get; init; }
        public int SpellMorph { get; init; }
        public int SpellNature { get; init; }
        public int SpellNecroBlack { get; init; }
        public int SpellNecroWhite { get; init; }
        public int SpellPhantasm { get; init; }
        public int SpellSummoning { get; init; }
        public int SpellTemporal { get; init; }
        public int SpellWater { get; init; }
        public int TechChemistry { get; init; }
        public int TechElectric { get; init; }
        public int TechExplosives { get; init; }
        public int TechGun { get; init; }
        public int TechHerbology { get; init; }
        public int TechMechanical { get; init; }
        public int TechPoints { get; init; }
        public int TechSmithy { get; init; }
        public int TechTherapeutics { get; init; }
        public int UnspentPoints { get; init; }
    }
    public sealed class EditorObjectInspectorCritterProgressionUpdate
    {
        public EditorObjectInspectorCritterProgressionUpdate() { }
        public int? Age { get; init; }
        public int? Alignment { get; init; }
        public int? ExperiencePoints { get; init; }
        public int? FatePoints { get; init; }
        public int? FatigueAdjustment { get; init; }
        public int? FatiguePoints { get; init; }
        public int? Gender { get; init; }
        public bool HasChanges { get; }
        public int? Level { get; init; }
        public int? MagickPoints { get; init; }
        public int? PoisonLevel { get; init; }
        public int? Race { get; init; }
        public int? SkillBackstab { get; init; }
        public int? SkillBow { get; init; }
        public int? SkillDisarmTraps { get; init; }
        public int? SkillDodge { get; init; }
        public int? SkillFirearms { get; init; }
        public int? SkillGambling { get; init; }
        public int? SkillHaggle { get; init; }
        public int? SkillHeal { get; init; }
        public int? SkillMelee { get; init; }
        public int? SkillPersuasion { get; init; }
        public int? SkillPickLocks { get; init; }
        public int? SkillPickPocket { get; init; }
        public int? SkillProwling { get; init; }
        public int? SkillRepair { get; init; }
        public int? SkillSpotTrap { get; init; }
        public int? SkillThrowing { get; init; }
        public int? SpellAir { get; init; }
        public int? SpellConveyance { get; init; }
        public int? SpellDivination { get; init; }
        public int? SpellEarth { get; init; }
        public int? SpellFire { get; init; }
        public int? SpellForce { get; init; }
        public int? SpellMastery { get; init; }
        public int? SpellMental { get; init; }
        public int? SpellMeta { get; init; }
        public int? SpellMorph { get; init; }
        public int? SpellNature { get; init; }
        public int? SpellNecroBlack { get; init; }
        public int? SpellNecroWhite { get; init; }
        public int? SpellPhantasm { get; init; }
        public int? SpellSummoning { get; init; }
        public int? SpellTemporal { get; init; }
        public int? SpellWater { get; init; }
        public int? TechChemistry { get; init; }
        public int? TechElectric { get; init; }
        public int? TechExplosives { get; init; }
        public int? TechGun { get; init; }
        public int? TechHerbology { get; init; }
        public int? TechMechanical { get; init; }
        public int? TechPoints { get; init; }
        public int? TechSmithy { get; init; }
        public int? TechTherapeutics { get; init; }
        public int? UnspentPoints { get; init; }
    }
    public sealed class EditorObjectInspectorFlagsSummary
    {
        public EditorObjectInspectorFlagsSummary() { }
        public int? AmmoFlags { get; init; }
        public ArcNET.GameObjects.ArmorFlags? ArmorFlags { get; init; }
        public ArcNET.GameObjects.ContainerFlags? ContainerFlags { get; init; }
        public ArcNET.GameObjects.CritterFlags? CritterFlags { get; init; }
        public ArcNET.GameObjects.CritterFlags2? CritterFlags2 { get; init; }
        public int? FoodFlags { get; init; }
        public int? GenericFlags { get; init; }
        public int? GoldFlags { get; init; }
        public bool HasTypeSpecificGroups { get; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public ArcNET.GameObjects.ItemFlags? ItemFlags { get; init; }
        public int? KeyRingFlags { get; init; }
        public ArcNET.GameObjects.NpcFlags? NpcFlags { get; init; }
        public ArcNET.GameObjects.ObjectFlags ObjectFlags { get; init; }
        public int? PcFlags { get; init; }
        public ArcNET.GameObjects.PortalFlags? PortalFlags { get; init; }
        public int? ProjectileCombatFlags { get; init; }
        public ArcNET.GameObjects.SceneryFlags? SceneryFlags { get; init; }
        public int? ScrollFlags { get; init; }
        public ArcNET.GameObjects.SpellFlags SpellFlags { get; init; }
        public int? TrapFlags { get; init; }
        public int? WallFlags { get; init; }
        public ArcNET.GameObjects.WeaponFlags? WeaponFlags { get; init; }
        public int? WrittenFlags { get; init; }
    }
    public sealed class EditorObjectInspectorFlagsUpdate
    {
        public EditorObjectInspectorFlagsUpdate() { }
        public int? AmmoFlags { get; init; }
        public ArcNET.GameObjects.ArmorFlags? ArmorFlags { get; init; }
        public ArcNET.GameObjects.ContainerFlags? ContainerFlags { get; init; }
        public ArcNET.GameObjects.CritterFlags? CritterFlags { get; init; }
        public ArcNET.GameObjects.CritterFlags2? CritterFlags2 { get; init; }
        public int? FoodFlags { get; init; }
        public int? GenericFlags { get; init; }
        public int? GoldFlags { get; init; }
        public bool HasChanges { get; }
        public ArcNET.GameObjects.ItemFlags? ItemFlags { get; init; }
        public int? KeyRingFlags { get; init; }
        public ArcNET.GameObjects.NpcFlags? NpcFlags { get; init; }
        public ArcNET.GameObjects.ObjectFlags? ObjectFlags { get; init; }
        public int? PcFlags { get; init; }
        public ArcNET.GameObjects.PortalFlags? PortalFlags { get; init; }
        public int? ProjectileCombatFlags { get; init; }
        public ArcNET.GameObjects.SceneryFlags? SceneryFlags { get; init; }
        public int? ScrollFlags { get; init; }
        public ArcNET.GameObjects.SpellFlags? SpellFlags { get; init; }
        public int? TrapFlags { get; init; }
        public int? WallFlags { get; init; }
        public ArcNET.GameObjects.WeaponFlags? WeaponFlags { get; init; }
        public int? WrittenFlags { get; init; }
    }
    public sealed class EditorObjectInspectorGeneratorSummary
    {
        public EditorObjectInspectorGeneratorSummary() { }
        public int GeneratorData { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public bool IsNpcTarget { get; }
    }
    public sealed class EditorObjectInspectorGeneratorUpdate
    {
        public EditorObjectInspectorGeneratorUpdate() { }
        public int? GeneratorData { get; init; }
        public bool HasChanges { get; }
    }
    public sealed class EditorObjectInspectorLightSummary
    {
        public EditorObjectInspectorLightSummary() { }
        public bool HasOverlayLights { get; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public ArcNET.Core.Primitives.ArtId LightArtId { get; init; }
        public ArcNET.Core.Primitives.Color LightColor { get; init; }
        public int LightFlags { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> OverlayLightArtIds { get; init; }
        public int OverlayLightColor { get; init; }
        public int OverlayLightFlags { get; init; }
    }
    public sealed class EditorObjectInspectorLightUpdate
    {
        public EditorObjectInspectorLightUpdate() { }
        public bool HasChanges { get; }
        public ArcNET.Core.Primitives.ArtId? LightArtId { get; init; }
        public ArcNET.Core.Primitives.Color? LightColor { get; init; }
        public int? LightFlags { get; init; }
        public System.Collections.Generic.IReadOnlyList<int>? OverlayLightArtIds { get; init; }
        public int? OverlayLightColor { get; init; }
        public int? OverlayLightFlags { get; init; }
    }
    public enum EditorObjectInspectorPane
    {
        Overview = 0,
        Flags = 1,
        ScriptAttachments = 2,
        Light = 3,
        CritterProgression = 4,
        Generator = 5,
        Blending = 6,
        Container = 7,
    }
    public sealed class EditorObjectInspectorPaneSummary
    {
        public EditorObjectInspectorPaneSummary() { }
        public required bool HasContract { get; init; }
        public required bool IsApplicable { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorPane Pane { get; init; }
        public string? UnavailableReason { get; init; }
    }
    public sealed class EditorObjectInspectorScriptAttachment
    {
        public EditorObjectInspectorScriptAttachment() { }
        public required ArcNET.Formats.ScriptAttachmentPoint AttachmentPoint { get; init; }
        public required uint Counters { get; init; }
        public required uint Flags { get; init; }
        public bool IsEmpty { get; }
        public bool IsMissingScript { get; }
        public ArcNET.Editor.EditorObjectInspectorScriptReference? Script { get; init; }
        public required int ScriptId { get; init; }
        public int SlotIndex { get; }
    }
    public sealed class EditorObjectInspectorScriptAttachmentsSummary
    {
        public EditorObjectInspectorScriptAttachmentsSummary() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectInspectorScriptAttachment> Attachments { get; init; }
        public bool HasMissingScripts { get; }
        public bool HasUnknownAttachmentSlots { get; }
        public required ArcNET.Editor.EditorObjectInspectorSummary Inspector { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectInspectorUnknownScriptAttachment> UnknownAttachments { get; init; }
    }
    public sealed class EditorObjectInspectorScriptReference
    {
        public EditorObjectInspectorScriptReference() { }
        public required int ActiveAttachmentCount { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ScriptAttachmentPoint> ActiveAttachmentPoints { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required string Description { get; init; }
        public required int EntryCount { get; init; }
        public required ArcNET.Formats.ScriptFlags Flags { get; init; }
        public bool HasUnknownAttachmentSlots { get; init; }
        public required int ScriptId { get; init; }
    }
    public sealed class EditorObjectInspectorSummary
    {
        public EditorObjectInspectorSummary() { }
        public bool CanInspect { get; }
        public bool HasProto { get; }
        public bool HasSelectedObject { get; }
        public bool HasSelectionContext { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectInspectorPaneSummary> Panes { get; init; }
        public ArcNET.Editor.EditorObjectPaletteEntry? Proto { get; init; }
        public int? ProtoNumber { get; init; }
        public ArcNET.Editor.EditorMapObjectPreview? SelectedObject { get; init; }
        public ArcNET.Editor.EditorMapObjectSelectionSummary? SelectionSummary { get; init; }
        public required ArcNET.Editor.EditorObjectInspectorTargetKind TargetKind { get; init; }
        public ArcNET.GameObjects.ObjectType? TargetObjectType { get; init; }
    }
    public enum EditorObjectInspectorTargetKind
    {
        None = 0,
        SelectedObject = 1,
        ProtoDefinition = 2,
    }
    public sealed class EditorObjectInspectorUnknownScriptAttachment
    {
        public EditorObjectInspectorUnknownScriptAttachment() { }
        public required uint Counters { get; init; }
        public required uint Flags { get; init; }
        public bool IsMissingScript { get; }
        public ArcNET.Editor.EditorObjectInspectorScriptReference? Script { get; init; }
        public required int ScriptId { get; init; }
        public required int SlotIndex { get; init; }
    }
    public sealed class EditorObjectPaletteEntry
    {
        public EditorObjectPaletteEntry() { }
        public string? ArtAssetPath { get; init; }
        public ArcNET.Editor.EditorArtDefinition? ArtDetail { get; init; }
        public ArcNET.Editor.EditorArtPreview? ArtPreview { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public string Category { get; }
        public ArcNET.Core.Primitives.ArtId? CurrentArtId { get; init; }
        public string? Description { get; init; }
        public int? DescriptionMessageIndex { get; init; }
        public string? DisplayName { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public bool HasArtBinding { get; }
        public int? NameMessageIndex { get; init; }
        public required ArcNET.GameObjects.ObjectType ObjectType { get; init; }
        public string? PaletteGroup { get; init; }
        public required int ProtoNumber { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementPreset CreatePlacementPreset(string presetId, string? name = null, string? description = null, int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = true) { }
        public ArcNET.Editor.EditorObjectPalettePlacementRequest CreatePlacementRequest(int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = true) { }
        public ArcNET.Editor.EditorMapObjectBrushRequest CreateReplaceRequest() { }
        public ArcNET.Editor.EditorMapObjectBrushRequest CreateStampRequest() { }
    }
    public sealed class EditorObjectPalettePlacementPreset
    {
        public EditorObjectPalettePlacementPreset() { }
        public string? Description { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPalettePlacementRequest> Entries { get; init; }
        public bool HasEntries { get; }
        public required string Name { get; init; }
        public required string PresetId { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementSet CreatePlacementSet() { }
        public static ArcNET.Editor.EditorObjectPalettePlacementPreset Create(string presetId, string name, string? description = null, params ArcNET.Editor.EditorObjectPalettePlacementRequest[] entries) { }
    }
    public sealed class EditorObjectPalettePlacementRequest
    {
        public EditorObjectPalettePlacementRequest() { }
        public bool AlignToTileGrid { get; init; }
        public int DeltaTileX { get; init; }
        public int DeltaTileY { get; init; }
        public bool HasInitialTransform { get; }
        public required int ProtoNumber { get; init; }
        public float? Rotation { get; init; }
        public float? RotationPitch { get; init; }
        public static ArcNET.Editor.EditorObjectPalettePlacementRequest Place(int protoNumber, int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = false) { }
    }
    public sealed class EditorObjectPalettePlacementSet
    {
        public EditorObjectPalettePlacementSet() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPalettePlacementRequest> Entries { get; init; }
        public bool HasEntries { get; }
        public string? Name { get; init; }
        public static ArcNET.Editor.EditorObjectPalettePlacementSet Create(string? name = null, params ArcNET.Editor.EditorObjectPalettePlacementRequest[] entries) { }
    }
    public sealed class EditorProject
    {
        public const int CurrentFormatVersion = 1;
        public EditorProject() { }
        public string? ActiveAssetPath { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectBookmark> Bookmarks { get; init; }
        public int FormatVersion { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectMapViewState> MapViewStates { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectOpenAsset> OpenAssets { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectToolState> ToolStates { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectViewState> ViewStates { get; init; }
        public required ArcNET.Editor.EditorProjectWorkspaceReference Workspace { get; init; }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspaceSession> LoadSessionAsync(System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorProjectLoadSessionResult> LoadSessionWithRestoreResultAsync(System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static ArcNET.Editor.EditorProject Create(ArcNET.Editor.EditorProjectWorkspaceReference workspace) { }
        public static ArcNET.Editor.EditorProject FromWorkspace(ArcNET.Editor.EditorWorkspace workspace) { }
    }
    public sealed class EditorProjectBookmark
    {
        public EditorProjectBookmark() { }
        public required string AssetPath { get; init; }
        public required string Id { get; init; }
        public string? LocationKey { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string?> Properties { get; init; }
        public string? Title { get; init; }
        public string? ViewId { get; init; }
    }
    public sealed class EditorProjectLoadSessionResult
    {
        public EditorProjectLoadSessionResult() { }
        public ArcNET.Editor.EditorSessionBootstrapSummary BootstrapSummary { get; }
        public required ArcNET.Editor.EditorProjectRestoreResult Restore { get; init; }
        public required ArcNET.Editor.EditorWorkspaceSession Session { get; init; }
    }
    public sealed class EditorProjectMapAreaSelectionState
    {
        public EditorProjectMapAreaSelectionState() { }
        public bool HasMultipleObjectSelection { get; }
        public bool HasObjectSelection { get; }
        public int Height { get; }
        public required int MaxMapTileX { get; init; }
        public required int MaxMapTileY { get; init; }
        public required int MinMapTileX { get; init; }
        public required int MinMapTileY { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> ObjectIds { get; init; }
        public int TileCount { get; }
        public int Width { get; }
        public bool ContainsMapTile(int mapTileX, int mapTileY) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "MapTileX",
                "MapTileY"})]
        public System.Collections.Generic.IEnumerable<System.ValueTuple<int, int>> EnumerateMapTiles() { }
    }
    public sealed class EditorProjectMapCameraState
    {
        public EditorProjectMapCameraState() { }
        public double CenterTileX { get; init; }
        public double CenterTileY { get; init; }
        public double Zoom { get; init; }
    }
    public sealed class EditorProjectMapObjectInspectorState
    {
        public EditorProjectMapObjectInspectorState() { }
        public ArcNET.Editor.EditorObjectInspectorPane ActivePane { get; init; }
        public int? PinnedProtoNumber { get; init; }
        public ArcNET.Editor.EditorProjectMapObjectInspectorTargetMode TargetMode { get; init; }
    }
    public enum EditorProjectMapObjectInspectorTargetMode
    {
        Selection = 0,
        ProtoDefinition = 1,
    }
    public enum EditorProjectMapObjectPlacementMode
    {
        SinglePlacement = 0,
        PlacementSet = 1,
        PlacementPreset = 2,
    }
    public sealed class EditorProjectMapObjectPlacementToolState
    {
        public EditorProjectMapObjectPlacementToolState() { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementMode Mode { get; init; }
        public string? PaletteCategory { get; init; }
        public string? PaletteSearchText { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementRequest? PlacementRequest { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementSet? PlacementSet { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPalettePlacementPreset> PresetLibrary { get; init; }
        public int? SelectedPaletteProtoNumber { get; init; }
        public string? SelectedPresetId { get; init; }
        public ArcNET.Editor.EditorObjectPalettePlacementPreset? FindSelectedPreset() { }
    }
    public sealed class EditorProjectMapPreviewState
    {
        public EditorProjectMapPreviewState() { }
        public ArcNET.Editor.EditorMapPreviewMode OutlineMode { get; init; }
        public bool ShowBlockedTiles { get; init; }
        public bool ShowJumpPoints { get; init; }
        public bool ShowLights { get; init; }
        public bool ShowObjects { get; init; }
        public bool ShowRoofs { get; init; }
        public bool ShowScripts { get; init; }
        public bool UseScenePreview { get; init; }
    }
    public sealed class EditorProjectMapSelectionState
    {
        public EditorProjectMapSelectionState() { }
        public ArcNET.Editor.EditorProjectMapAreaSelectionState? Area { get; init; }
        public bool HasAreaSelection { get; }
        public bool HasMultipleObjectSelection { get; }
        public bool HasObjectSelection { get; }
        public bool HasTileSelection { get; }
        public ArcNET.Core.Primitives.GameObjectGuid? ObjectId { get; init; }
        public string? SectorAssetPath { get; init; }
        public int SelectedObjectCount { get; }
        public string? SourceAssetPath { get; init; }
        public int? SourceObjectIndex { get; init; }
        public ArcNET.Core.Primitives.Location? Tile { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> GetSelectedObjectIds() { }
    }
    public sealed class EditorProjectMapTerrainToolState
    {
        public EditorProjectMapTerrainToolState() { }
        public string? MapPropertiesAssetPath { get; init; }
        public ulong PaletteX { get; init; }
        public ulong PaletteY { get; init; }
    }
    public sealed class EditorProjectMapViewState
    {
        public EditorProjectMapViewState() { }
        public ArcNET.Editor.EditorProjectMapCameraState Camera { get; init; }
        public required string Id { get; init; }
        public required string MapName { get; init; }
        public ArcNET.Editor.EditorProjectMapPreviewState Preview { get; init; }
        public ArcNET.Editor.EditorProjectMapSelectionState Selection { get; init; }
        public string? ViewId { get; init; }
        public ArcNET.Editor.EditorProjectMapWorldEditState WorldEdit { get; init; }
    }
    public enum EditorProjectMapWorldEditActiveTool
    {
        None = 0,
        TerrainPaint = 1,
        ObjectPlacement = 2,
    }
    public sealed class EditorProjectMapWorldEditShellState
    {
        public EditorProjectMapWorldEditShellState() { }
        public bool IncludeEditorObjectStateTint { get; init; }
        public bool IncludeFloorLightTint { get; init; }
        public bool IncludeTrackedPlacementPreview { get; init; }
        public string? ObjectPaletteCategory { get; init; }
        public string? ObjectPaletteSearchText { get; init; }
        public ArcNET.Editor.EditorMapSceneViewMode ViewMode { get; init; }
        public double? ViewportHeight { get; init; }
        public double? ViewportWidth { get; init; }
    }
    public sealed class EditorProjectMapWorldEditState
    {
        public EditorProjectMapWorldEditState() { }
        public ArcNET.Editor.EditorProjectMapWorldEditActiveTool ActiveTool { get; init; }
        public ArcNET.Editor.EditorProjectMapObjectInspectorState Inspector { get; init; }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState ObjectPlacement { get; init; }
        public ArcNET.Editor.EditorProjectMapWorldEditShellState Shell { get; init; }
        public ArcNET.Editor.EditorProjectMapTerrainToolState Terrain { get; init; }
    }
    public sealed class EditorProjectOpenAsset
    {
        public EditorProjectOpenAsset() { }
        public required string AssetPath { get; init; }
        public bool IsPinned { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string?> Properties { get; init; }
        public string? ViewId { get; init; }
    }
    public sealed class EditorProjectRestoreResult
    {
        public EditorProjectRestoreResult() { }
        public string? RequestedActiveAssetPath { get; init; }
        public string? RestoredActiveAssetPath { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> RestoredAssetPaths { get; init; }
        public required ArcNET.Editor.EditorSessionProjectStateSummary RestoredProjectState { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> SkippedAssetPaths { get; init; }
    }
    public static class EditorProjectStore
    {
        public static ArcNET.Editor.EditorProject Deserialize(string json) { }
        public static ArcNET.Editor.EditorProject Load(string path) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorProject> LoadAsync(string path, System.Threading.CancellationToken cancellationToken = default) { }
        public static void Save(string path, ArcNET.Editor.EditorProject project) { }
        public static System.Threading.Tasks.Task SaveAsync(string path, ArcNET.Editor.EditorProject project, System.Threading.CancellationToken cancellationToken = default) { }
        public static string Serialize(ArcNET.Editor.EditorProject project) { }
    }
    public sealed class EditorProjectToolState
    {
        public EditorProjectToolState() { }
        public System.Collections.Generic.IReadOnlyDictionary<string, string?> Properties { get; init; }
        public string? ScopeId { get; init; }
        public required string ToolId { get; init; }
    }
    public sealed class EditorProjectViewState
    {
        public EditorProjectViewState() { }
        public string? AssetPath { get; init; }
        public required string Id { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string?> Properties { get; init; }
        public string? ViewId { get; init; }
    }
    public enum EditorProjectWorkspaceKind
    {
        ContentDirectory = 0,
        GameInstall = 1,
    }
    public sealed class EditorProjectWorkspaceReference
    {
        public EditorProjectWorkspaceReference() { }
        public required ArcNET.Editor.EditorProjectWorkspaceKind Kind { get; init; }
        public string? ModuleName { get; init; }
        public required string RootPath { get; init; }
        public string? SaveFolder { get; init; }
        public string? SaveSlotName { get; init; }
        public ArcNET.Editor.EditorWorkspaceLoadOptions CreateLoadOptions() { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadAsync(System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static ArcNET.Editor.EditorProjectWorkspaceReference ForContentDirectory(string contentDirectory, string? saveFolder = null, string? saveSlotName = null) { }
        public static ArcNET.Editor.EditorProjectWorkspaceReference ForGameInstall(string gameDirectory, string? moduleName = null, string? saveFolder = null, string? saveSlotName = null) { }
    }
    public sealed class EditorProtoReference
    {
        public EditorProtoReference() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int Count { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required int ProtoNumber { get; init; }
    }
    public sealed class EditorScriptDefinition
    {
        public EditorScriptDefinition() { }
        public required int ActiveAttachmentCount { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ScriptAttachmentPoint> ActiveAttachmentPoints { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> ActiveAttachmentSlots { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required string Description { get; init; }
        public required int EntryCount { get; init; }
        public required ArcNET.Formats.ScriptFlags Flags { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public bool HasUnknownAttachmentSlots { get; }
        public required int ScriptId { get; init; }
    }
    public sealed class EditorScriptReference
    {
        public EditorScriptReference() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int Count { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required int ScriptId { get; init; }
    }
    public sealed class EditorSectorSummary
    {
        public EditorSectorSummary() { }
        public required int AmbientSchemeIndex { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int BlockedTileCount { get; init; }
        public required int DistinctTileArtCount { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required bool HasRoofs { get; init; }
        public required int LightCount { get; init; }
        public required int LightSchemeIndex { get; init; }
        public required string MapName { get; init; }
        public required int MusicSchemeIndex { get; init; }
        public required int ObjectCount { get; init; }
        public int? SectorScriptId { get; init; }
        public required int TileScriptCount { get; init; }
    }
    public sealed class EditorSessionBootstrapSummary
    {
        public EditorSessionBootstrapSummary() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionCommandSummary> Commands { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionHistoryCommandSummary> HistoryCommands { get; init; }
        public required ArcNET.Editor.EditorSessionProjectStateSummary ProjectState { get; init; }
        public ArcNET.Editor.EditorProjectRestoreResult? Restore { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedCommandSummary> StagedCommands { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> StagedTransactions { get; init; }
    }
    public sealed class EditorSessionChange
    {
        public EditorSessionChange() { }
        public required ArcNET.Editor.EditorSessionChangeKind Kind { get; init; }
        public required string Target { get; init; }
    }
    public sealed class EditorSessionChangeGroup
    {
        public string? Label { get; }
        public ArcNET.Editor.EditorWorkspace ApplyPendingChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession DiscardPendingChanges() { }
        public ArcNET.Editor.EditorWorkspace SavePendingChanges() { }
    }
    public enum EditorSessionChangeKind
    {
        Dialog = 0,
        Script = 1,
        Proto = 2,
        Mob = 3,
        Sector = 4,
        Save = 5,
        Message = 6,
    }
    public sealed class EditorSessionChangeKindSummary
    {
        public EditorSessionChangeKindSummary() { }
        public int Count { get; }
        public required ArcNET.Editor.EditorSessionChangeKind Kind { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> Targets { get; init; }
    }
    public enum EditorSessionCommandKind
    {
        Undo = 0,
        Redo = 1,
    }
    public enum EditorSessionCommandSourceKind
    {
        Staged = 0,
        History = 1,
    }
    public sealed class EditorSessionCommandSummary
    {
        public EditorSessionCommandSummary() { }
        public required bool CanExecute { get; init; }
        public ArcNET.Editor.EditorSessionHistoryCommandSummary? HistoryCommand { get; init; }
        public required ArcNET.Editor.EditorSessionCommandKind Kind { get; init; }
        public required string Label { get; init; }
        public required ArcNET.Editor.EditorSessionCommandSourceKind SourceKind { get; init; }
        public ArcNET.Editor.EditorSessionStagedCommandSummary? StagedCommand { get; init; }
    }
    public enum EditorSessionHistoryCommandKind
    {
        Undo = 0,
        Redo = 1,
    }
    public sealed class EditorSessionHistoryCommandSummary
    {
        public EditorSessionHistoryCommandSummary() { }
        public required bool CanExecute { get; init; }
        public required ArcNET.Editor.EditorSessionHistoryEntry Entry { get; init; }
        public required ArcNET.Editor.EditorSessionHistoryCommandKind Kind { get; init; }
        public required string Label { get; init; }
    }
    public sealed class EditorSessionHistoryEntry
    {
        public EditorSessionHistoryEntry() { }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> Changes { get; init; }
        public required string Label { get; init; }
        public required bool PersistedToDisk { get; init; }
        public required ArcNET.Editor.EditorSessionProjectStateSummary ProjectState { get; init; }
        public required System.DateTimeOffset RecordedAtUtc { get; init; }
    }
    public sealed class EditorSessionImpactSummary
    {
        public EditorSessionImpactSummary() { }
        public required System.Collections.Generic.IReadOnlyList<int> DefinedDialogIds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> DefinedProtoNumbers { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> DefinedScriptIds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChangeKind> DirectKinds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> DirectTargets { get; init; }
        public bool HasDirectTargets { get; }
        public bool HasMapCoverage { get; }
        public bool HasRelatedAssets { get; }
        public required System.Collections.Generic.IReadOnlyList<string> MapNames { get; init; }
        public required System.Collections.Generic.IReadOnlyList<uint> ReferencedArtIds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> ReferencedProtoNumbers { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> ReferencedScriptIds { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> RelatedAssetPaths { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChangeKind> RelatedKinds { get; init; }
    }
    public sealed class EditorSessionPendingChangeSummary
    {
        public EditorSessionPendingChangeSummary() { }
        public required ArcNET.Editor.EditorWorkspaceValidationReport BlockingValidation { get; init; }
        public bool CanRepairFromSession { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> Changes { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChangeKindSummary> Groups { get; init; }
        public bool HasChanges { get; }
        public required ArcNET.Editor.EditorSessionImpactSummary ImpactSummary { get; init; }
        public int RepairCandidateCount { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> RepairCandidates { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionPendingChangeTargetSummary> TargetSummaries { get; init; }
        public int TotalChangeCount { get; }
        public required ArcNET.Editor.EditorWorkspaceValidationReport Validation { get; init; }
    }
    public sealed class EditorSessionPendingChangeTargetSummary
    {
        public EditorSessionPendingChangeTargetSummary() { }
        public bool CanRepairFromSession { get; }
        public ArcNET.Editor.EditorAssetDependencySummary? DependencySummary { get; init; }
        public bool HasDependencySummary { get; }
        public required ArcNET.Editor.EditorSessionChangeKind Kind { get; init; }
        public int RepairCandidateCount { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> RepairCandidates { get; init; }
        public required string Target { get; init; }
    }
    public sealed class EditorSessionProjectStateSummary
    {
        public EditorSessionProjectStateSummary() { }
        public string? ActiveAssetPath { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectBookmark> Bookmarks { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectMapViewState> MapViewStates { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectOpenAsset> OpenAssets { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectToolState> ToolStates { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectViewState> ViewStates { get; init; }
    }
    public enum EditorSessionStagedCommandKind
    {
        Undo = 0,
        Redo = 1,
    }
    public sealed class EditorSessionStagedCommandSummary
    {
        public EditorSessionStagedCommandSummary() { }
        public required bool CanExecute { get; init; }
        public required bool IsDefault { get; init; }
        public required ArcNET.Editor.EditorSessionStagedCommandKind Kind { get; init; }
        public required string Label { get; init; }
        public required ArcNET.Editor.EditorSessionStagedTransactionSummary Transaction { get; init; }
    }
    public sealed class EditorSessionStagedHistoryScope
    {
        public EditorSessionStagedHistoryScope() { }
        public required bool CanRedo { get; init; }
        public required bool CanUndo { get; init; }
        public required bool HasPendingChanges { get; init; }
        public required ArcNET.Editor.EditorSessionStagedHistoryScopeKind Kind { get; init; }
        public string? Target { get; init; }
    }
    public enum EditorSessionStagedHistoryScopeKind
    {
        Dialog = 0,
        Script = 1,
        Save = 2,
        DirectAssets = 3,
    }
    public sealed class EditorSessionStagedTransactionSummary
    {
        public EditorSessionStagedTransactionSummary() { }
        public required System.Collections.Generic.IReadOnlyList<string> AffectedTargets { get; init; }
        public required ArcNET.Editor.EditorWorkspaceValidationReport BlockingValidation { get; init; }
        public required bool CanApplyFromSession { get; init; }
        public required bool CanApplyIndividually { get; init; }
        public required bool CanDiscardFromSession { get; init; }
        public required bool CanRedo { get; init; }
        public bool CanRepairFromSession { get; }
        public required bool CanSaveIndividually { get; init; }
        public required bool CanUndo { get; init; }
        public required bool HasPendingChanges { get; init; }
        public required ArcNET.Editor.EditorSessionImpactSummary ImpactSummary { get; init; }
        public required ArcNET.Editor.EditorSessionStagedHistoryScopeKind Kind { get; init; }
        public required string Label { get; init; }
        public int PendingChangeCount { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> PendingChanges { get; init; }
        public int RepairCandidateCount { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> RepairCandidates { get; init; }
        public string? Target { get; init; }
    }
    public sealed class EditorSessionValidationException : System.InvalidOperationException
    {
        public EditorSessionValidationException(ArcNET.Editor.EditorWorkspaceValidationReport validation) { }
        public EditorSessionValidationException(ArcNET.Editor.EditorWorkspaceValidationReport validation, ArcNET.Editor.EditorSessionImpactSummary impactSummary) { }
        public EditorSessionValidationException(ArcNET.Editor.EditorWorkspaceValidationReport validation, ArcNET.Editor.EditorSessionImpactSummary impactSummary, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> repairCandidates) { }
        public bool CanRepairFromSession { get; }
        public ArcNET.Editor.EditorSessionImpactSummary ImpactSummary { get; }
        public int RepairCandidateCount { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> RepairCandidates { get; }
        public ArcNET.Editor.EditorWorkspaceValidationReport Validation { get; }
    }
    public sealed class EditorSessionValidationRepairCandidate
    {
        public EditorSessionValidationRepairCandidate() { }
        public required string AssetPath { get; init; }
        public required string Description { get; init; }
        public required int DialogEntryNumber { get; init; }
        public required ArcNET.Editor.EditorSessionValidationRepairCandidateKind Kind { get; init; }
        public int? ProtoNumber { get; init; }
        public int? ReferencedProtoNumber { get; init; }
        public int? ReferencedScriptId { get; init; }
        public int? SuggestedIntelligenceRequirement { get; init; }
        public string? SuggestedProtoDisplayName { get; init; }
        public int? SuggestedResponseTargetNumber { get; init; }
        public string? SuggestedScriptDescription { get; init; }
        public required string Title { get; init; }
        public bool UseNameOverrideAsset { get; init; }
    }
    public enum EditorSessionValidationRepairCandidateKind
    {
        SetDialogEntryIntelligenceRequirement = 0,
        SetDialogResponseTarget = 1,
        SetScriptDescription = 2,
        ClearAssetScriptReference = 3,
        ClearAssetProtoReference = 4,
        SetProtoDisplayName = 5,
        RenumberDuplicateDialogEntryNumber = 6,
        ClearUnknownScriptAttachmentSlots = 7,
    }
    public sealed class EditorSkippedArchiveCandidate
    {
        public EditorSkippedArchiveCandidate() { }
        public required string Path { get; init; }
        public required string Reason { get; init; }
    }
    public sealed class EditorSkippedAsset
    {
        public EditorSkippedAsset() { }
        public required string AssetPath { get; init; }
        public required ArcNET.Formats.FileFormat Format { get; init; }
        public required string Reason { get; init; }
        public string? SourceEntryPath { get; init; }
        public required ArcNET.Editor.EditorAssetSourceKind SourceKind { get; init; }
        public required string SourcePath { get; init; }
    }
    public sealed class EditorTerrainDefinition
    {
        public EditorTerrainDefinition() { }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required ArcNET.Formats.TerrainType BaseTerrainType { get; init; }
        public required bool Compressed { get; init; }
        public required int DistinctTileCount { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public required long Height { get; init; }
        public required float Version { get; init; }
        public required long Width { get; init; }
    }
    public sealed class EditorTerrainPaletteEntry
    {
        public EditorTerrainPaletteEntry() { }
        public string? ArtAssetPath { get; init; }
        public ArcNET.Editor.EditorArtDefinition? ArtDetail { get; init; }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public ArcNET.Editor.EditorArtPreview? ArtPreview { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int BaseArtId { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public bool HasArtBinding { get; }
        public required ulong LimitX { get; init; }
        public required ulong LimitY { get; init; }
        public required ulong PaletteIndex { get; init; }
        public required ulong PaletteX { get; init; }
        public required ulong PaletteY { get; init; }
        public ArcNET.Editor.EditorMapLayerBrushRequest CreateTileArtBrushRequest() { }
    }
    public sealed class EditorTerrainPresetEntry
    {
        public EditorTerrainPresetEntry() { }
        public required string DisplayName { get; init; }
        public required int DistinctTileArtCount { get; init; }
        public string? PreviewArtAssetPath { get; init; }
        public string PrimaryTemplateSectorAssetPath { get; }
        public required int SceneryObjectCount { get; init; }
        public required System.Collections.Generic.IReadOnlyList<string> TemplateSectorAssetPaths { get; init; }
        public required string TerrainDirectoryName { get; init; }
        public ArcNET.Formats.TerrainType? TerrainType { get; }
        public required int TerrainTypeId { get; init; }
    }
    public sealed class EditorTileArtPaletteEntry
    {
        public EditorTileArtPaletteEntry() { }
        public string ArtAssetPath { get; }
        public ArcNET.Editor.EditorArtDefinition? ArtDetail { get; init; }
        public required ArcNET.Core.Primitives.ArtId ArtId { get; init; }
        public ArcNET.Editor.EditorArtPreview? ArtPreview { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required string DisplayName { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
        public ArcNET.Editor.EditorMapLayerBrushRequest CreateTileArtBrushRequest() { }
    }
    public static class EditorWallSpriteFallback
    {
        public static int BindFallbackAssets(ArcNET.Editor.EditorWorkspace workspace, ArcNET.Editor.EditorArtResolver artResolver, System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.ArtId> unresolvedArtIds) { }
    }
    public sealed class EditorWorkspace : System.IDisposable
    {
        public EditorWorkspace() { }
        public ArcNET.Editor.EditorAssetCatalog Assets { get; init; }
        public ArcNET.Editor.EditorAudioAssetCatalog AudioAssets { get; init; }
        public required string ContentDirectory { get; init; }
        public required ArcNET.GameData.GameDataStore GameData { get; init; }
        public string? GameDirectory { get; init; }
        public bool HasSaveLoaded { get; }
        public ArcNET.Editor.EditorAssetIndex Index { get; init; }
        public ArcNET.Core.ArcanumInstallationType? InstallationType { get; init; }
        public ArcNET.Editor.EditorWorkspaceLoadReport LoadReport { get; init; }
        public int LoadedArtCacheEntryCount { get; }
        public long LoadedArtCacheRetainedBytes { get; }
        public ArcNET.Editor.EditorWorkspaceModuleContext? Module { get; init; }
        public ArcNET.Editor.LoadedSave? Save { get; init; }
        public string? SaveFolder { get; init; }
        public string? SaveSlotName { get; init; }
        public ArcNET.Editor.EditorWorkspaceValidationReport Validation { get; init; }
        public ArcNET.Editor.EditorArtPreview CreateArtPreview(string assetPath, ArcNET.Editor.EditorArtPreviewOptions? options = null) { }
        public ArcNET.Editor.EditorArtResolver CreateArtResolver() { }
        public ArcNET.Editor.EditorArtResolver CreateArtResolver(ArcNET.Editor.EditorArtResolverBindingStrategy bindingStrategy) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorArtResolver> CreateArtResolverAsync(System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorArtResolver> CreateArtResolverAsync(ArcNET.Editor.EditorArtResolverBindingStrategy bindingStrategy, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorAudioPreview CreateAudioPreview(string assetPath) { }
        public ArcNET.Editor.DialogEditor CreateDialogEditor(string assetPath) { }
        public ArcNET.Editor.EditorWorkspaceMapRenderSpriteSource CreateMapRenderSpriteSource(ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions? previewOptions = null) { }
        public ArcNET.Editor.EditorWorkspaceMapRenderSpriteSource CreateMapRenderSpriteSource(ArcNET.Editor.EditorArtResolverBindingStrategy bindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? previewOptions = null) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspaceMapRenderSpriteSource> CreateMapRenderSpriteSourceAsync(ArcNET.Editor.EditorArtResolverBindingStrategy bindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? previewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName, ArcNET.Editor.EditorArtResolver artResolver) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName, System.Func<ArcNET.Core.Primitives.ArtId, ArcNET.Formats.ArtFile?>? artResolver) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName, ArcNET.Editor.EditorArtResolver artResolver, System.Collections.Generic.IReadOnlySet<string>? objectSectorAssetPaths) { }
        public ArcNET.Editor.EditorMapScenePreview CreateMapScenePreview(string mapName, System.Func<ArcNET.Core.Primitives.ArtId, ArcNET.Formats.ArtFile?>? artResolver, System.Collections.Generic.IReadOnlySet<string>? objectSectorAssetPaths) { }
        public ArcNET.Editor.EditorProject CreateProject() { }
        public ArcNET.Editor.SaveGameEditor CreateSaveEditor() { }
        public ArcNET.Editor.ScriptEditor CreateScriptEditor(string assetPath) { }
        public ArcNET.Editor.EditorWorkspaceSession CreateSession() { }
        public void Dispose() { }
        public ArcNET.Formats.ArtFile? FindArt(string assetPath) { }
        public ArcNET.Editor.EditorAudioAssetEntry? FindAudioAsset(string assetPath) { }
        public ArcNET.Editor.EditorAudioDefinition? FindAudioDetail(string assetPath) { }
        public ArcNET.Formats.DlgFile? FindDialog(string assetPath) { }
        public ArcNET.Formats.FacadeWalk? FindFacadeWalk(string assetPath) { }
        public ArcNET.Formats.JmpFile? FindJumpFile(string assetPath) { }
        public ArcNET.Formats.JmpFile? FindMapJumpFile(string mapName) { }
        public ArcNET.Formats.MapProperties? FindMapProperties(string assetPath) { }
        public ArcNET.Editor.EditorMessageDefinition? FindMessageDetail(string assetPath) { }
        public ArcNET.Formats.MesFile? FindMessageFile(string assetPath) { }
        public ArcNET.Editor.EditorObjectInspectorBlendingSummary? FindObjectInspectorBlendingSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorCritterProgressionSummary? FindObjectInspectorCritterProgressionSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorFlagsSummary? FindObjectInspectorFlagsSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorGeneratorSummary? FindObjectInspectorGeneratorSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorLightSummary? FindObjectInspectorLightSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorScriptAttachmentsSummary? FindObjectInspectorScriptAttachmentsSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectInspectorSummary? FindObjectInspectorSummary(int protoNumber) { }
        public ArcNET.Editor.EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber) { }
        public ArcNET.Editor.EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber, ArcNET.Editor.EditorArtResolver artResolver) { }
        public ArcNET.Editor.EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy) { }
        public ArcNET.Editor.EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber, ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public ArcNET.Editor.EditorObjectPaletteEntry? FindObjectPaletteEntry(int protoNumber, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public ArcNET.Formats.ProtoData? FindProto(string assetPath) { }
        public ArcNET.Formats.ScrFile? FindScript(string assetPath) { }
        public ArcNET.Formats.Sector? FindSector(string assetPath) { }
        public ArcNET.Formats.TerrainData? FindTerrain(string assetPath) { }
        public ArcNET.Editor.EditorTerrainPaletteEntry? FindTerrainPaletteEntry(ArcNET.Editor.EditorProjectMapTerrainToolState toolState) { }
        public ArcNET.Editor.EditorTerrainPaletteEntry? FindTerrainPaletteEntry(ArcNET.Editor.EditorProjectMapTerrainToolState toolState, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public ArcNET.Editor.EditorTerrainPaletteEntry? FindTerrainPaletteEntry(string mapPropertiesAssetPath, ulong paletteX, ulong paletteY) { }
        public ArcNET.Editor.EditorTerrainPaletteEntry? FindTerrainPaletteEntry(string mapPropertiesAssetPath, ulong paletteX, ulong paletteY, ArcNET.Editor.EditorArtResolver? artResolver, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public ArcNET.Editor.EditorTerrainPaletteEntry? FindTerrainPaletteEntry(string mapPropertiesAssetPath, ulong paletteX, ulong paletteY, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public ArcNET.Editor.EditorCapabilitySummary GetCapabilities() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> GetObjectPalette() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> GetObjectPalette(ArcNET.Editor.EditorArtResolver artResolver) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> GetObjectPalette(ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> GetObjectPalette(ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> GetObjectPalette(ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> GetObjectPaletteAsync(System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> GetObjectPaletteAsync(ArcNET.Editor.EditorArtResolver artResolver, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> GetObjectPaletteAsync(ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> GetObjectPaletteAsync(ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> GetObjectPaletteAsync(ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> GetTerrainPalette(string mapPropertiesAssetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> GetTerrainPalette(string mapPropertiesAssetPath, ArcNET.Editor.EditorArtResolver? artResolver, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> GetTerrainPalette(string mapPropertiesAssetPath, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> GetTerrainPaletteAsync(string mapPropertiesAssetPath, ArcNET.Editor.EditorArtResolver? artResolver, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> GetTerrainPaletteAsync(string mapPropertiesAssetPath, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> GetTerrainPaletteForMap(string mapName) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> GetTerrainPaletteForMap(string mapName, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> GetTerrainPaletteForMapAsync(string mapName, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> GetTerrainPaletteForMapAsync(string mapName, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPresetEntry> GetTerrainPresetPalette() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTileArtPaletteEntry> GetTileArtPalette() { }
        public ArcNET.Editor.EditorWorldAreaCatalog GetWorldAreaCatalog() { }
        public System.Threading.Tasks.Task PreloadArtsAsync(System.Collections.Generic.IEnumerable<string> assetPaths, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorWorkspaceDefaultMap? ResolveDefaultMap() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAudioDefinition> SearchAudioDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMessageDefinition> SearchMessageDetails(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> SearchObjectPalette(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> SearchObjectPalette(string text, ArcNET.Editor.EditorArtResolver artResolver) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> SearchObjectPalette(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> SearchObjectPalette(string text, ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry> SearchObjectPalette(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> SearchObjectPaletteAsync(string text, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> SearchObjectPaletteAsync(string text, ArcNET.Editor.EditorArtResolver artResolver, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> SearchObjectPaletteAsync(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> SearchObjectPaletteAsync(string text, ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPaletteEntry>> SearchObjectPaletteAsync(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> SearchTerrainPalette(string text) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> SearchTerrainPalette(string text, ArcNET.Editor.EditorArtResolver? artResolver, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry> SearchTerrainPalette(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(string text, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(string text, ArcNET.Editor.EditorArtResolver? artResolver, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainPaletteEntry>> SearchTerrainPaletteAsync(string text, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public sealed class EditorWorkspaceDefaultMap
    {
        public EditorWorkspaceDefaultMap() { }
        public bool IsFallback { get; }
        public required string MapName { get; init; }
        public int? SaveMapId { get; init; }
        public required ArcNET.Editor.EditorWorkspaceDefaultMapSource Source { get; init; }
    }
    public enum EditorWorkspaceDefaultMapSource
    {
        SaveInfoMapId = 0,
        ConventionalMap01 = 1,
        SingleIndexedMap = 2,
        FirstIndexedMap = 3,
    }
    public sealed class EditorWorkspaceLoadOptions
    {
        public EditorWorkspaceLoadOptions() { }
        public string? GameDirectory { get; init; }
        public bool LoadArtMetadata { get; init; }
        public string? ModuleName { get; init; }
        public string? SaveFolder { get; init; }
        public string? SaveSlotName { get; init; }
    }
    public sealed class EditorWorkspaceLoadProgress
    {
        public EditorWorkspaceLoadProgress() { }
        public required string Activity { get; init; }
        public int? CompletedUnits { get; init; }
        public required System.TimeSpan Elapsed { get; init; }
        public System.DateTimeOffset? EstimatedCompletionTime { get; init; }
        public System.TimeSpan? EstimatedRemaining { get; init; }
        public required float OverallProgress { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorWorkspaceLoadStageTiming> StageTimings { get; init; }
        public int? TotalUnits { get; init; }
        public string? UnitLabel { get; init; }
    }
    public sealed class EditorWorkspaceLoadReport
    {
        public EditorWorkspaceLoadReport() { }
        public bool HasSkippedInputs { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSkippedAsset> SkippedAssets { get; init; }
        public static ArcNET.Editor.EditorWorkspaceLoadReport Empty { get; }
    }
    public sealed class EditorWorkspaceLoadStageTiming : System.IEquatable<ArcNET.Editor.EditorWorkspaceLoadStageTiming>
    {
        public EditorWorkspaceLoadStageTiming() { }
        public required long ElapsedMs { get; init; }
        public bool IsDominant { get; init; }
        public int? ItemCount { get; init; }
        public required string StageName { get; init; }
        public string? UnitLabel { get; init; }
    }
    public static class EditorWorkspaceLoader
    {
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadAsync(string contentDirectory, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default, System.IProgress<ArcNET.Editor.EditorWorkspaceLoadProgress>? loadProgress = null) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadFromGameInstallAsync(string gameDir, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default, System.IProgress<ArcNET.Editor.EditorWorkspaceLoadProgress>? loadProgress = null) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadFromModuleDirectoryAsync(string moduleDirectory, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default, System.IProgress<ArcNET.Editor.EditorWorkspaceLoadProgress>? loadProgress = null) { }
    }
    public sealed class EditorWorkspaceMapRenderSpriteSource : ArcNET.Editor.IEditorMapRenderSpriteSource
    {
        public EditorWorkspaceMapRenderSpriteSource(ArcNET.Editor.EditorWorkspace workspace, ArcNET.Editor.EditorArtResolver artResolver, ArcNET.Editor.EditorArtPreviewOptions? previewOptions = null) { }
        public int CachedFrameCount { get; }
        public long CachedFrameRetainedBytes { get; }
        public bool CanResolve(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null) { }
        public ArcNET.Editor.EditorMapRenderSpriteMetrics? GetSpriteMetrics(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null) { }
        public System.Threading.Tasks.Task PreloadAsync(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task PreloadAsync(System.Collections.Generic.IEnumerable<ArcNET.Editor.EditorMapRenderQueueItem> items, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorMapRenderSprite? Resolve(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null) { }
    }
    public sealed class EditorWorkspaceModuleContext
    {
        public EditorWorkspaceModuleContext() { }
        public required System.Collections.Generic.IReadOnlyList<string> ArchivePaths { get; init; }
        public required string ModuleDirectory { get; init; }
        public required string ModuleName { get; init; }
        public string? SaveDirectory { get; init; }
    }
    public sealed class EditorWorkspaceSession
    {
        public EditorWorkspaceSession(ArcNET.Editor.EditorWorkspace workspace) { }
        public string? ActiveAssetPath { get; }
        public bool CanApplyPendingChanges { get; }
        public bool CanDiscardPendingChanges { get; }
        public bool CanRedo { get; }
        public bool CanRedoDirectAssetChanges { get; }
        public bool CanRedoStagedChanges { get; }
        public bool CanUndo { get; }
        public bool CanUndoDirectAssetChanges { get; }
        public bool CanUndoStagedChanges { get; }
        public bool HasPendingChanges { get; }
        public ArcNET.Editor.EditorWorkspace Workspace { get; }
        public ArcNET.Editor.EditorSessionChange AddSectorLight(string assetPath, ArcNET.Formats.SectorLight light) { }
        public ArcNET.Editor.EditorSessionChange AddSectorObject(string assetPath, ArcNET.Formats.MobData obj) { }
        public ArcNET.Formats.MobData AddSectorObjectFromProto(string assetPath, int protoNumber, int tileX, int tileY) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> AddSectorObjectsFromProto(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, int protoNumber) { }
        public ArcNET.Editor.EditorSessionChange AddSectorTileScript(string assetPath, ArcNET.Formats.TileScript tileScript) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState AppendTrackedObjectPaletteSelectionToPlacementSet(string mapViewStateId, int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = true, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapViewState ApplyMapFocusTarget(string mapViewStateId, ArcNET.Editor.EditorMapFocusTarget target, string? viewId = null) { }
        public ArcNET.Editor.EditorWorkspace ApplyPendingChanges() { }
        public ArcNET.Editor.EditorWorkspace ApplyPendingChanges(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public ArcNET.Editor.EditorWorkspace ApplyPendingChanges(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> stagedTransactions) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplySectorLayerBrush(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorMapLayerBrushRequest request) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplySectorLayerBrush(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorMapLayerBrushRequest request) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplySectorLayerBrush(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorMapLayerBrushRequest request) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectBrush(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorMapObjectBrushRequest request) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectBrush(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorMapObjectBrushRequest request) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectBrush(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorMapObjectBrushRequest request) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacement(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementRequest request) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacement(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementRequest request) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacement(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementRequest request) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementPreset(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementPreset preset) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementPreset(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementPreset preset) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementPreset(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementPreset preset) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementSet(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementSet(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplySectorObjectPalettePlacementSet(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectTransform(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorMapObjectTransformRequest request) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectTransform(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorMapObjectTransformRequest request) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplySectorObjectTransform(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorMapObjectTransformRequest request) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplyTerrainPaletteEntry(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorTerrainPaletteEntry entry) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplyTerrainPaletteEntry(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorTerrainPaletteEntry entry) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplyTerrainPaletteEntry(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorTerrainPaletteEntry entry) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplyTerrainPreset(System.Collections.Generic.IReadOnlyList<string> sectorAssetPaths, ArcNET.Editor.EditorTerrainPresetEntry entry) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplyTrackedObjectBrush(string mapViewStateId, ArcNET.Editor.EditorMapObjectBrushRequest request) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplyTrackedObjectPlacementTool(string mapViewStateId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.MobData> ApplyTrackedObjectPlacementTool(string mapViewStateId, ArcNET.Editor.EditorProjectMapSelectionState selection) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ApplyTrackedObjectTransform(string mapViewStateId, ArcNET.Editor.EditorMapObjectTransformRequest request) { }
        public ArcNET.Editor.EditorMapLayerBrushResult ApplyTrackedTerrainTool(string mapViewStateId) { }
        public ArcNET.Editor.EditorSessionChange ApplyValidationRepairCandidate(ArcNET.Editor.EditorSessionValidationRepairCandidate candidate) { }
        public ArcNET.Editor.EditorSessionChangeGroup BeginChangeGroup(string? label = null) { }
        public ArcNET.Editor.EditorSessionChange? ClearProtoInspectorScriptAttachment(int protoNumber, ArcNET.Formats.ScriptAttachmentPoint attachmentPoint) { }
        public ArcNET.Editor.EditorSessionChange? ClearTrackedObjectInspectorScriptAttachment(string mapViewStateId, ArcNET.Formats.ScriptAttachmentPoint attachmentPoint) { }
        public void CloseAllAssets(bool discardPendingChanges = false) { }
        public bool CloseAsset(string assetPath, bool discardPendingChanges = false) { }
        public ArcNET.Editor.EditorProjectMapViewState CreateDefaultMapViewState(string id, string? viewId = null) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapWorldEditScene> CreateDefaultMapWorldEditSceneAsync(string id = "default-map", string? viewId = null, ArcNET.Editor.EditorMapWorldEditSceneRequest? request = null, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorMapFloorRenderPreview CreateMapFloorRenderPreview(ArcNET.Editor.EditorProjectMapViewState mapViewState, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapFloorRenderPreview CreateMapFloorRenderPreview(string mapViewStateId, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapWorldEditScene> CreateMapWorldEditSceneAsync(ArcNET.Editor.EditorProjectMapViewState mapViewState, ArcNET.Editor.EditorMapWorldEditSceneRequest? request = null, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapWorldEditScene> CreateMapWorldEditSceneAsync(string mapViewStateId, ArcNET.Editor.EditorMapWorldEditSceneRequest? request = null, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorProject CreateProject(string? activeAssetPath = null, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectOpenAsset>? openAssets = null, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectBookmark>? bookmarks = null, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectMapViewState>? mapViewStates = null, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectViewState>? viewStates = null, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectToolState>? toolStates = null) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapWorldEditShell> CreateTrackedMapWorldEditShellAsync(string mapViewStateId, ArcNET.Editor.EditorMapWorldEditShellRequest? request = null, System.Threading.CancellationToken cancellationToken = default, System.IProgress<ArcNET.Editor.EditorMapWorldEditComposeProgress>? progress = null) { }
        public ArcNET.Editor.EditorWorkspaceSession DiscardPendingChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession DiscardPendingChanges(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public ArcNET.Editor.EditorWorkspaceSession DiscardPendingChanges(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> stagedTransactions) { }
        public ArcNET.Editor.EditorMapObjectBrushResult EraseTrackedSelectedObjects(string mapViewStateId) { }
        public ArcNET.Editor.EditorWorkspace ExecuteCommand(ArcNET.Editor.EditorSessionCommandSummary command) { }
        public ArcNET.Editor.EditorWorkspace ExecuteHistoryCommand(ArcNET.Editor.EditorSessionHistoryCommandSummary command) { }
        public ArcNET.Editor.EditorWorkspaceSession ExecuteStagedCommand(ArcNET.Editor.EditorSessionStagedCommandSummary command) { }
        public ArcNET.Editor.EditorObjectPalettePlacementPreset? FindTrackedObjectPlacementPreset(string mapViewStateId, string presetId) { }
        public ArcNET.Editor.EditorMapFocusTarget FocusMapTarget(string mapViewStateId, string query, string? viewId = null) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedCommandSummary> GetAvailableStagedCommandSummaries() { }
        public ArcNET.Editor.EditorSessionBootstrapSummary GetBootstrapSummary() { }
        public ArcNET.Editor.EditorSessionBootstrapSummary GetBootstrapSummary(ArcNET.Editor.EditorProjectRestoreResult restore) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionCommandSummary> GetCommandSummaries() { }
        public ArcNET.Editor.EditorSessionCommandSummary? GetDefaultRedoCommandSummary() { }
        public ArcNET.Editor.EditorSessionHistoryCommandSummary? GetDefaultRedoHistoryCommandSummary() { }
        public ArcNET.Editor.EditorSessionStagedCommandSummary? GetDefaultRedoStagedCommandSummary() { }
        public ArcNET.Editor.EditorSessionCommandSummary? GetDefaultUndoCommandSummary() { }
        public ArcNET.Editor.EditorSessionHistoryCommandSummary? GetDefaultUndoHistoryCommandSummary() { }
        public ArcNET.Editor.EditorSessionStagedCommandSummary? GetDefaultUndoStagedCommandSummary() { }
        public ArcNET.Editor.DialogEditor GetDialogEditor(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionHistoryCommandSummary> GetHistoryCommandSummaries() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectMapViewState> GetMapViewStates() { }
        public ArcNET.Editor.EditorProjectMapWorldEditState GetMapWorldEditState(string mapViewStateId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectOpenAsset> GetOpenAssets() { }
        public ArcNET.Editor.EditorSessionPendingChangeSummary GetPendingChangeSummary() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> GetPendingChanges() { }
        public ArcNET.Editor.EditorWorkspaceValidationReport GetPendingValidation() { }
        public ArcNET.Editor.EditorWorkspaceValidationReport GetPendingValidation(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public ArcNET.Editor.EditorWorkspaceValidationReport GetPendingValidation(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> stagedTransactions) { }
        public ArcNET.Editor.EditorSessionStagedHistoryScope? GetPreferredRedoStagedHistoryScope() { }
        public ArcNET.Editor.EditorSessionStagedTransactionSummary? GetPreferredRedoStagedTransactionSummary() { }
        public ArcNET.Editor.EditorSessionStagedHistoryScope? GetPreferredUndoStagedHistoryScope() { }
        public ArcNET.Editor.EditorSessionStagedTransactionSummary? GetPreferredUndoStagedTransactionSummary() { }
        public ArcNET.Editor.EditorSessionProjectStateSummary GetProjectStateSummary() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionHistoryEntry> GetRedoHistory() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedCommandSummary> GetRedoStagedCommandSummaries() { }
        public ArcNET.Editor.SaveGameEditor GetSaveEditor() { }
        public ArcNET.Editor.ScriptEditor GetScriptEditor(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedCommandSummary> GetStagedCommandSummaries() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedHistoryScope> GetStagedHistoryScopes() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> GetStagedTransactionSummaries() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectToolState> GetToolStates() { }
        public ArcNET.Editor.EditorObjectInspectorBlendingSummary GetTrackedObjectInspectorBlendingSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorBlendingSummary GetTrackedObjectInspectorBlendingSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorContainerSummary GetTrackedObjectInspectorContainerSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorContainerSummary GetTrackedObjectInspectorContainerSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorCritterProgressionSummary GetTrackedObjectInspectorCritterProgressionSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorCritterProgressionSummary GetTrackedObjectInspectorCritterProgressionSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorFlagsSummary GetTrackedObjectInspectorFlagsSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorFlagsSummary GetTrackedObjectInspectorFlagsSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorGeneratorSummary GetTrackedObjectInspectorGeneratorSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorGeneratorSummary GetTrackedObjectInspectorGeneratorSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorLightSummary GetTrackedObjectInspectorLightSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorLightSummary GetTrackedObjectInspectorLightSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorScriptAttachmentsSummary GetTrackedObjectInspectorScriptAttachmentsSummary(ArcNET.Editor.EditorObjectInspectorSummary inspector) { }
        public ArcNET.Editor.EditorObjectInspectorScriptAttachmentsSummary GetTrackedObjectInspectorScriptAttachmentsSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorProjectMapObjectInspectorState GetTrackedObjectInspectorState(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorSummary GetTrackedObjectInspectorSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorObjectInspectorSummary GetTrackedObjectInspectorSummary(string mapViewStateId, ArcNET.Editor.EditorMapObjectSelectionSummary selectionSummary) { }
        public ArcNET.Editor.EditorMapObjectPaletteSummary GetTrackedObjectPaletteSummary(string mapViewStateId, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false) { }
        public ArcNET.Editor.EditorMapObjectPaletteSummary GetTrackedObjectPaletteSummary(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false) { }
        public ArcNET.Editor.EditorMapObjectPaletteSummary GetTrackedObjectPaletteSummary(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapObjectPaletteSummary> GetTrackedObjectPaletteSummaryAsync(string mapViewStateId, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapObjectPaletteSummary> GetTrackedObjectPaletteSummaryAsync(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapObjectPaletteSummary> GetTrackedObjectPaletteSummaryAsync(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions artPreviewOptions, string? searchText = null, string? category = null, bool includeFullPaletteWhenSearchIsEmpty = false, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPalettePlacementPreset> GetTrackedObjectPlacementPresetLibrary(string mapViewStateId) { }
        public ArcNET.Editor.EditorMapObjectPlacementToolSummary GetTrackedObjectPlacementToolSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorMapObjectSelectionSummary GetTrackedObjectSelectionSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorMapObjectSelectionSummary GetTrackedObjectSelectionSummary(string mapViewStateId, ArcNET.Editor.EditorMapFloorRenderPreview sceneRender) { }
        public ArcNET.Editor.EditorMapTerrainPaletteSummary GetTrackedTerrainPaletteSummary(string mapViewStateId) { }
        public ArcNET.Editor.EditorMapTerrainPaletteSummary GetTrackedTerrainPaletteSummary(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapTerrainPaletteSummary> GetTrackedTerrainPaletteSummaryAsync(string mapViewStateId, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task<ArcNET.Editor.EditorMapTerrainPaletteSummary> GetTrackedTerrainPaletteSummaryAsync(string mapViewStateId, ArcNET.Editor.EditorArtResolverBindingStrategy artBindingStrategy, ArcNET.Editor.EditorArtPreviewOptions? artPreviewOptions = null, System.Threading.CancellationToken cancellationToken = default) { }
        public ArcNET.Editor.EditorMapTerrainToolSummary GetTrackedTerrainToolSummary(string mapViewStateId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionHistoryEntry> GetUndoHistory() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedCommandSummary> GetUndoStagedCommandSummaries() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> GetValidationRepairCandidates() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> GetValidationRepairCandidates(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionValidationRepairCandidate> GetValidationRepairCandidates(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> stagedTransactions) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProjectViewState> GetViewStates() { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "IsBlocked",
                "Reason"})]
        public System.ValueTuple<bool, string?> IsPlacementBlocked(string mapViewStateId, ArcNET.Editor.EditorProjectMapSelectionState selection) { }
        public ArcNET.Editor.EditorSessionChange? MoveSectorObject(string assetPath, ArcNET.Core.Primitives.GameObjectGuid objectId, int tileX, int tileY) { }
        public ArcNET.Editor.EditorMapObjectBrushResult MoveTrackedSelectedObjects(string mapViewStateId, int deltaTileX, int deltaTileY) { }
        public ArcNET.Editor.EditorProjectOpenAsset OpenAsset(ArcNET.Editor.EditorProjectOpenAsset openAsset) { }
        public ArcNET.Editor.EditorProjectOpenAsset OpenAsset(string assetPath) { }
        public ArcNET.Editor.EditorMapPaintableScene? PreviewSectorLayerBrush(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorMapLayerBrushRequest request, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(ArcNET.Editor.EditorProjectMapViewState mapViewState, ArcNET.Editor.EditorObjectPalettePlacementRequest request, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementRequest request, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementRequest request, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementRequest request, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(ArcNET.Editor.EditorMapScenePreview scenePreview, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementRequest request, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(ArcNET.Editor.EditorProjectMapViewState mapViewState, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(ArcNET.Editor.EditorMapScenePreview scenePreview, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(ArcNET.Editor.EditorProjectMapViewState mapViewState, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapAreaSelectionState areaSelection, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(ArcNET.Editor.EditorMapScenePreview scenePreview, ArcNET.Editor.EditorProjectMapSelectionState selection, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(ArcNET.Editor.EditorMapScenePreview scenePreview, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPaintableScene? PreviewTerrainPreset(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, System.Collections.Generic.IReadOnlyList<string> sectorAssetPaths, ArcNET.Editor.EditorTerrainPresetEntry entry, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewTrackedObjectPlacementTool(string mapViewStateId, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewTrackedObjectPlacementTool(string mapViewStateId, ArcNET.Editor.EditorProjectMapSelectionState? selection, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPlacementPreview PreviewTrackedObjectPlacementToolOverlay(string mapViewStateId, ArcNET.Editor.EditorProjectMapSelectionState? selection, ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.EditorMapFloorRenderRequest? renderRequest = null) { }
        public ArcNET.Editor.EditorMapPaintableScene? PreviewTrackedTerrainFacadePaintableScene(string mapViewStateId, ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, ArcNET.Editor.IEditorMapRenderSpriteSource? spriteSource = null) { }
        public ArcNET.Editor.EditorWorkspace Redo() { }
        public ArcNET.Editor.EditorWorkspaceSession RedoDirectAssetChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession RedoStagedChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession RedoStagedChanges(ArcNET.Editor.EditorSessionStagedHistoryScope scope) { }
        public ArcNET.Editor.EditorWorkspaceSession RedoStagedChanges(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public bool RemoveMapViewState(string id) { }
        public ArcNET.Editor.EditorSessionChange RemoveSectorLight(string assetPath, int index) { }
        public ArcNET.Editor.EditorSessionChange RemoveSectorObject(string assetPath, ArcNET.Core.Primitives.GameObjectGuid objectId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Core.Primitives.GameObjectGuid> RemoveSectorObjects(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups) { }
        public ArcNET.Editor.EditorSessionChange RemoveSectorTileScript(string assetPath, int index) { }
        public bool RemoveToolState(string toolId, string? scopeId = null) { }
        public bool RemoveTrackedObjectPlacementPreset(string mapViewStateId, string presetId, bool activateTool = false) { }
        public bool RemoveViewState(string id) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> ReplaceArtReferences(uint sourceArtId, uint targetArtId) { }
        public ArcNET.Editor.EditorSessionChange? ReplaceSectorLight(string assetPath, int index, ArcNET.Formats.SectorLight light) { }
        public ArcNET.Editor.EditorSessionChange? ReplaceSectorTileScript(string assetPath, int index, ArcNET.Formats.TileScript tileScript) { }
        public ArcNET.Editor.EditorMapObjectBrushResult ReplaceTrackedSelectedObjects(string mapViewStateId, int protoNumber) { }
        public ArcNET.Editor.EditorProjectRestoreResult RestoreProject(ArcNET.Editor.EditorProject project) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> RetargetProtoReferences(int sourceProtoNumber, int targetProtoNumber) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> RetargetScriptReferences(int sourceScriptId, int targetScriptId) { }
        public ArcNET.Editor.EditorMapObjectBrushResult RotatePitchTrackedSelectedObjects(string mapViewStateId, float rotationPitch) { }
        public ArcNET.Editor.EditorMapObjectBrushResult RotateTrackedSelectedObjects(string mapViewStateId, float rotation) { }
        public ArcNET.Editor.EditorWorkspace SavePendingChanges() { }
        public ArcNET.Editor.EditorWorkspace SavePendingChanges(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
        public ArcNET.Editor.EditorWorkspace SavePendingChanges(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionStagedTransactionSummary> stagedTransactions) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SelectTrackedObjectPaletteEntry(string mapViewStateId, int protoNumber, string? searchText = null, string? category = null, bool activateTool = false) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SelectTrackedObjectPlacementPreset(string mapViewStateId, string presetId, bool activateTool = true) { }
        public void SetActiveAsset(string? assetPath) { }
        public ArcNET.Editor.EditorProjectMapViewState SetMapViewState(ArcNET.Editor.EditorProjectMapViewState mapViewState) { }
        public ArcNET.Editor.EditorProjectMapWorldEditState SetMapWorldEditState(string mapViewStateId, ArcNET.Editor.EditorProjectMapWorldEditState worldEditState) { }
        public ArcNET.Editor.EditorSessionChange? SetMessageEntry(string assetPath, int messageIndex, string text, string? soundId = null) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoDisplayName(int protoNumber, string displayName, bool useNameOverrideAsset = false) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorBlending(int protoNumber, ArcNET.Editor.EditorObjectInspectorBlendingUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorContainer(int protoNumber, ArcNET.Editor.EditorObjectInspectorContainerUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorCritterProgression(int protoNumber, ArcNET.Editor.EditorObjectInspectorCritterProgressionUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorFlags(int protoNumber, ArcNET.Editor.EditorObjectInspectorFlagsUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorGenerator(int protoNumber, ArcNET.Editor.EditorObjectInspectorGeneratorUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorLight(int protoNumber, ArcNET.Editor.EditorObjectInspectorLightUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetProtoInspectorScriptAttachment(int protoNumber, ArcNET.Formats.ScriptAttachmentPoint attachmentPoint, int scriptId, uint flags = 0, uint counters = 0) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> SetSectorBlockedTile(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, bool blocked) { }
        public ArcNET.Editor.EditorSessionChange? SetSectorBlockedTile(string assetPath, int tileX, int tileY, bool blocked) { }
        public ArcNET.Editor.EditorSessionChange? SetSectorObjectRotation(string assetPath, ArcNET.Core.Primitives.GameObjectGuid objectId, float rotation) { }
        public ArcNET.Editor.EditorSessionChange? SetSectorObjectRotationPitch(string assetPath, ArcNET.Core.Primitives.GameObjectGuid objectId, float rotationPitch) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> SetSectorRoofArt(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, uint artId) { }
        public ArcNET.Editor.EditorSessionChange? SetSectorRoofArt(string assetPath, int roofX, int roofY, uint artId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSessionChange> SetSectorTileArt(System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapSceneSectorHitGroup> sectorHitGroups, uint artId) { }
        public ArcNET.Editor.EditorSessionChange? SetSectorTileArt(string assetPath, int tileX, int tileY, uint artId) { }
        public ArcNET.Editor.EditorProjectToolState SetToolState(ArcNET.Editor.EditorProjectToolState toolState) { }
        public ArcNET.Editor.EditorProjectMapWorldEditShellState SetTrackedMapWorldEditShellPreferences(string mapViewStateId, ArcNET.Editor.EditorMapWorldEditShellRequest request) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorBlending(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorBlendingUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorContainer(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorContainerUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorCritterProgression(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorCritterProgressionUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorFlags(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorFlagsUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorGenerator(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorGeneratorUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorLight(string mapViewStateId, ArcNET.Editor.EditorObjectInspectorLightUpdate update) { }
        public ArcNET.Editor.EditorSessionChange? SetTrackedObjectInspectorScriptAttachment(string mapViewStateId, ArcNET.Formats.ScriptAttachmentPoint attachmentPoint, int scriptId, uint flags = 0, uint counters = 0) { }
        public ArcNET.Editor.EditorProjectMapObjectInspectorState SetTrackedObjectInspectorState(string mapViewStateId, ArcNET.Editor.EditorProjectMapObjectInspectorState inspectorState) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPaletteBrowserFilter(string mapViewStateId, string? searchText = null, string? category = null, bool activateTool = false) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementEntry(string mapViewStateId, ArcNET.Editor.EditorObjectPaletteEntry entry, int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = true, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementEntry(string mapViewStateId, int protoNumber, int deltaTileX = 0, int deltaTileY = 0, float? rotation = default, float? rotationPitch = default, bool alignToTileGrid = true, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementPreset(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementPreset preset, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementPresetLibrary(string mapViewStateId, System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorObjectPalettePlacementPreset> presetLibrary, string? selectedPresetId = null, bool activateTool = false) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementRequest(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementRequest request, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementSet(string mapViewStateId, ArcNET.Editor.EditorObjectPalettePlacementSet placementSet, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapTerrainToolState SetTrackedTerrainPaletteEntry(string mapViewStateId, ArcNET.Editor.EditorTerrainPaletteEntry entry, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectMapTerrainToolState SetTrackedTerrainPaletteEntry(string mapViewStateId, ulong paletteX, ulong paletteY, string? mapPropertiesAssetPath = null, bool activateTool = true) { }
        public ArcNET.Editor.EditorProjectViewState SetViewState(ArcNET.Editor.EditorProjectViewState viewState) { }
        public ArcNET.Editor.EditorWorkspace Undo() { }
        public ArcNET.Editor.EditorWorkspaceSession UndoDirectAssetChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession UndoStagedChanges() { }
        public ArcNET.Editor.EditorWorkspaceSession UndoStagedChanges(ArcNET.Editor.EditorSessionStagedHistoryScope scope) { }
        public ArcNET.Editor.EditorWorkspaceSession UndoStagedChanges(ArcNET.Editor.EditorSessionStagedTransactionSummary stagedTransaction) { }
    }
    public enum EditorWorkspaceValidationCode
    {
        MissingProtoDefinition = 0,
        MissingProtoDisplayName = 1,
        MissingScriptDefinition = 2,
    }
    public sealed class EditorWorkspaceValidationIssue : System.IEquatable<ArcNET.Editor.EditorWorkspaceValidationIssue>
    {
        public EditorWorkspaceValidationIssue() { }
        public string? AssetPath { get; init; }
        public ArcNET.Editor.EditorWorkspaceValidationCode? Code { get; init; }
        public required string Message { get; init; }
        public int? ReferencedProtoNumber { get; init; }
        public int? ReferencedScriptId { get; init; }
        public required ArcNET.Editor.EditorWorkspaceValidationSeverity Severity { get; init; }
        public override string ToString() { }
    }
    public sealed class EditorWorkspaceValidationReport
    {
        public EditorWorkspaceValidationReport() { }
        public bool HasErrors { get; }
        public bool HasIssues { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorWorkspaceValidationIssue> Issues { get; init; }
        public static ArcNET.Editor.EditorWorkspaceValidationReport Empty { get; }
    }
    public enum EditorWorkspaceValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
    public sealed class EditorWorldAreaCatalog
    {
        public EditorWorldAreaCatalog() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorWorldAreaEntry> Areas { get; init; }
        public string? WorldSceneMapName { get; init; }
        public ArcNET.Editor.EditorWorldAreaEntry? FindArea(int areaId) { }
        public ArcNET.Editor.EditorWorldAreaEntry? FindAreaForMap(string mapName) { }
    }
    public sealed class EditorWorldAreaEntry
    {
        public EditorWorldAreaEntry() { }
        public required int AreaId { get; init; }
        public string? Description { get; init; }
        public required string DisplayName { get; init; }
        public bool HasWorldCoordinates { get; }
        public bool IsWorldMapVisible { get; init; }
        public int LabelOffsetX { get; init; }
        public int LabelOffsetY { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorWorldAreaMapEntry> MapEntries { get; init; }
        public int? Radius { get; init; }
        public required int WorldX { get; init; }
        public required int WorldY { get; init; }
    }
    public sealed class EditorWorldAreaMapEntry
    {
        public EditorWorldAreaMapEntry() { }
        public int EntryTileX { get; init; }
        public int EntryTileY { get; init; }
        public required string MapName { get; init; }
        public string? Type { get; init; }
        public int? WorldMapId { get; init; }
    }
    public interface IArtIndex
    {
        ArcNET.Editor.EditorArtDefinition? FindArtDetail(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtReference> FindArtReferences(uint artId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtDefinition> SearchArtDetails(string text);
    }
    public interface IAssetDependencyIndex
    {
        ArcNET.Editor.EditorAssetDependencySummary? FindAssetDependencySummary(string assetPath);
    }
    public interface IDialogIndex
    {
        ArcNET.Editor.EditorAssetEntry? FindDialogDefinition(int dialogId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindDialogDefinitions(int dialogId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogDefinition> FindDialogDetails(int dialogId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogDefinition> SearchDialogDetails(string text);
    }
    public interface IEditorMapRenderSpriteSource
    {
        bool CanResolve(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null);
        ArcNET.Editor.EditorMapRenderSpriteMetrics? GetSpriteMetrics(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null);
        System.Threading.Tasks.Task PreloadAsync(ArcNET.Editor.EditorMapFloorRenderPreview sceneRender, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task PreloadAsync(System.Collections.Generic.IEnumerable<ArcNET.Editor.EditorMapRenderQueueItem> items, System.Threading.CancellationToken cancellationToken = default);
        ArcNET.Editor.EditorMapRenderSprite? Resolve(ArcNET.Core.Primitives.ArtId artId, ArcNET.Editor.EditorMapRenderSpriteRequest? request = null);
    }
    public interface IFacadeWalkIndex
    {
        ArcNET.Editor.EditorFacadeWalkDefinition? FindFacadeWalkDetail(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorFacadeWalkDefinition> SearchFacadeWalkDetails(string text);
    }
    public interface IJumpIndex
    {
        ArcNET.Editor.EditorJumpDefinition? FindJumpDetail(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorJumpDefinition> SearchJumpDetails(string text);
    }
    public interface IMapIndex
    {
        System.Collections.Generic.IReadOnlyList<string> MapNames { get; }
        string? FindAssetMap(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMapAssets(string mapName);
        ArcNET.Editor.EditorMapProjection? FindMapProjection(string mapName);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindMapSectors(string mapName);
        ArcNET.Editor.EditorSectorSummary? FindSectorSummary(string assetPath);
        System.Collections.Generic.IReadOnlyList<string> SearchMapNames(string text);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> SearchSectors(string text);
    }
    public interface IMapPropertiesIndex
    {
        ArcNET.Editor.EditorMapPropertiesDefinition? FindMapPropertiesDetail(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorMapPropertiesDefinition> SearchMapPropertiesDetails(string text);
    }
    public interface IMessageIndex
    {
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMessageAssets(int messageIndex);
    }
    public interface IProtoIndex
    {
        ArcNET.Editor.EditorAssetEntry? FindProtoDefinition(int protoNumber);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProtoReference> FindProtoReferences(int protoNumber);
    }
    public interface ISchemeIndex
    {
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindAmbientSchemeSectors(int ambientSchemeIndex);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindLightSchemeSectors(int lightSchemeIndex);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSectorSummary> FindMusicSchemeSectors(int musicSchemeIndex);
    }
    public interface IScriptIndex
    {
        ArcNET.Editor.EditorAssetEntry? FindScriptDefinition(int scriptId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindScriptDefinitions(int scriptId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptDefinition> FindScriptDetails(int scriptId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptReference> FindScriptReferences(int scriptId);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptDefinition> SearchScriptDetails(string text);
    }
    public interface ITerrainIndex
    {
        ArcNET.Editor.EditorTerrainDefinition? FindTerrainDetail(string assetPath);
        System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorTerrainDefinition> SearchTerrainDetails(string text);
    }
    public sealed class LoadedSave
    {
        public LoadedSave() { }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Data2SavFile> Data2SavFiles { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.DataSavFile> DataSavFiles { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.DlgFile> Dialogs { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, byte[]> Files { get; init; }
        public required ArcNET.Formats.SaveIndex Index { get; init; }
        public required ArcNET.Formats.SaveInfo Info { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.JmpFile> JumpFiles { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MapProperties> MapPropertiesList { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MesFile> Messages { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobileMdFile> MobileMds { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobileMdyFile> MobileMdys { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobData> Mobiles { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, string> ParseErrors { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, byte[]> RawFiles { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.ScrFile> Scripts { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Sector> Sectors { get; init; }
        public required System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.TownMapFog> TownMapFogs { get; init; }
    }
    public sealed class MobDataBuilder
    {
        public MobDataBuilder(ArcNET.Formats.MobData existing) { }
        public MobDataBuilder(ArcNET.GameObjects.ObjectType type, ArcNET.Core.Primitives.GameObjectGuid objectId, ArcNET.Core.Primitives.GameObjectGuid protoId) { }
        public ArcNET.Formats.MobData Build() { }
        public ArcNET.Editor.MobDataBuilder WithLocation(int tileX, int tileY) { }
        public ArcNET.Editor.MobDataBuilder WithOffset(int offsetX, int offsetY) { }
        public ArcNET.Editor.MobDataBuilder WithProperty(ArcNET.Formats.ObjectProperty property) { }
        public ArcNET.Editor.MobDataBuilder WithRotation(float rotation) { }
        public ArcNET.Editor.MobDataBuilder WithRotationPitch(float rotationPitch) { }
        public ArcNET.Editor.MobDataBuilder WithoutProperty(ArcNET.GameObjects.ObjectField field) { }
    }
    public sealed class SaveGameEditor
    {
        public SaveGameEditor(ArcNET.Editor.LoadedSave save) { }
        public bool CanRedo { get; }
        public bool CanUndo { get; }
        public bool HasPendingChanges { get; }
        public ArcNET.Editor.LoadedSave CommitPendingChanges() { }
        public ArcNET.Editor.SaveGameEditor DiscardPendingChanges() { }
        public ArcNET.Formats.Data2SavFile? GetCurrentData2Sav(string path) { }
        public ArcNET.Formats.DataSavFile? GetCurrentDataSav(string path) { }
        public ArcNET.Formats.JmpFile? GetCurrentJumpFile(string path) { }
        public ArcNET.Formats.MapProperties? GetCurrentMapProperties(string path) { }
        public ArcNET.Formats.MesFile? GetCurrentMessageFile(string path) { }
        public System.ReadOnlyMemory<byte>? GetCurrentRawFile(string path) { }
        public ArcNET.Formats.SaveInfo GetCurrentSaveInfo() { }
        public ArcNET.Formats.TownMapFog? GetCurrentTownMapFog(string path) { }
        public ArcNET.Formats.Data2SavFile? GetPendingData2Sav(string path) { }
        public ArcNET.Formats.DataSavFile? GetPendingDataSav(string path) { }
        public ArcNET.Formats.JmpFile? GetPendingJumpFile(string path) { }
        public ArcNET.Formats.MapProperties? GetPendingMapProperties(string path) { }
        public ArcNET.Formats.MesFile? GetPendingMessageFile(string path) { }
        public ArcNET.Formats.MobileMdyFile? GetPendingMobileMdy(string mdyPath) { }
        public System.ReadOnlyMemory<byte>? GetPendingRawFile(string path) { }
        public ArcNET.Formats.SaveInfo? GetPendingSaveInfo() { }
        public ArcNET.Formats.TownMapFog? GetPendingTownMapFog(string path) { }
        public ArcNET.Editor.SaveGameEditor Redo() { }
        public void Save(string saveFolder, string slotName) { }
        public void Save(string gsiPath, string tfaiPath, string tfafPath) { }
        public System.Threading.Tasks.Task SaveAsync(string saveFolder, string slotName, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task SaveAsync(string gsiPath, string tfaiPath, string tfafPath, System.Threading.CancellationToken cancellationToken = default) { }
        public bool TryFindCharacter(System.Func<ArcNET.Editor.CharacterRecord, bool> predicate, out ArcNET.Editor.CharacterRecord character, out string mdyPath) { }
        public bool TryFindPendingPlayerCharacter(out ArcNET.Editor.CharacterRecord character) { }
        public bool TryFindPlayerCharacter(out ArcNET.Editor.CharacterRecord character) { }
        public bool TryFindPlayerCharacter(out ArcNET.Editor.CharacterRecord character, out string mdyPath) { }
        public ArcNET.Editor.SaveGameEditor Undo() { }
        public ArcNET.Editor.SaveGameEditor WithCharacter(string mdyPath, System.Func<ArcNET.Editor.CharacterRecord, bool> predicate, ArcNET.Editor.CharacterRecord updated) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, ArcNET.Formats.Data2SavFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, System.Action<ArcNET.Formats.Data2SavFile.Builder> update) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, System.Func<ArcNET.Formats.Data2SavFile, ArcNET.Formats.Data2SavFile> update) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, ArcNET.Formats.DataSavFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, System.Action<ArcNET.Formats.DataSavFile.Builder> update) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, System.Func<ArcNET.Formats.DataSavFile, ArcNET.Formats.DataSavFile> update) { }
        public ArcNET.Editor.SaveGameEditor WithJumpFile(string path, ArcNET.Formats.JmpFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithJumpFile(string path, System.Func<ArcNET.Formats.JmpFile, ArcNET.Formats.JmpFile> update) { }
        public ArcNET.Editor.SaveGameEditor WithMapProperties(string path, ArcNET.Formats.MapProperties updated) { }
        public ArcNET.Editor.SaveGameEditor WithMapProperties(string path, System.Func<ArcNET.Formats.MapProperties, ArcNET.Formats.MapProperties> update) { }
        public ArcNET.Editor.SaveGameEditor WithMessageFile(string path, ArcNET.Formats.MesFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithMessageFile(string path, System.Func<ArcNET.Formats.MesFile, ArcNET.Formats.MesFile> update) { }
        public ArcNET.Editor.SaveGameEditor WithPlayerCharacter(ArcNET.Editor.CharacterRecord updated) { }
        public ArcNET.Editor.SaveGameEditor WithPlayerCharacter(System.Func<ArcNET.Editor.CharacterRecord, ArcNET.Editor.CharacterRecord> update) { }
        public ArcNET.Editor.SaveGameEditor WithRawFile(string path, System.Func<System.ReadOnlyMemory<byte>, byte[]> update) { }
        public ArcNET.Editor.SaveGameEditor WithRawFile(string path, byte[] updatedBytes) { }
        public ArcNET.Editor.SaveGameEditor WithSaveInfo(ArcNET.Formats.SaveInfo updated) { }
        public ArcNET.Editor.SaveGameEditor WithSaveInfo(System.Func<ArcNET.Formats.SaveInfo, ArcNET.Formats.SaveInfo> update) { }
        public ArcNET.Editor.SaveGameEditor WithTownMapFog(string path, ArcNET.Formats.TownMapFog updated) { }
        public ArcNET.Editor.SaveGameEditor WithTownMapFog(string path, System.Func<ArcNET.Formats.TownMapFog, ArcNET.Formats.TownMapFog> update) { }
    }
    public static class SaveGameLoader
    {
        public static ArcNET.Editor.LoadedSave Load(string saveFolder, string slotName) { }
        public static ArcNET.Editor.LoadedSave Load(string gsiPath, string tfaiPath, string tfafPath) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.LoadedSave> LoadAsync(string saveFolder, string slotName, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.LoadedSave> LoadAsync(string gsiPath, string tfaiPath, string tfafPath, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public sealed class SaveGameUpdates : System.IEquatable<ArcNET.Editor.SaveGameUpdates>
    {
        public SaveGameUpdates() { }
        public System.Collections.Generic.IReadOnlyDictionary<string, byte[]>? RawFileUpdates { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Data2SavFile>? UpdatedData2SavFiles { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.DataSavFile>? UpdatedDataSavFiles { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.DlgFile>? UpdatedDialogs { get; init; }
        public ArcNET.Formats.SaveInfo? UpdatedInfo { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.JmpFile>? UpdatedJumpFiles { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MapProperties>? UpdatedMapProperties { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MesFile>? UpdatedMessages { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobileMdFile>? UpdatedMobileMds { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobileMdyFile>? UpdatedMobileMdys { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.MobData>? UpdatedMobiles { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.ScrFile>? UpdatedScripts { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.Sector>? UpdatedSectors { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Formats.TownMapFog>? UpdatedTownMapFogs { get; init; }
    }
    public static class SaveGameValidator
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.SaveValidationIssue> Validate(ArcNET.Editor.LoadedSave save) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.SaveValidationIssue> ValidateMob(string virtualPath, ArcNET.Formats.MobData mob) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.SaveValidationIssue> ValidateMobileMd(string virtualPath, ArcNET.Formats.MobileMdFile md) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.SaveValidationIssue> ValidateMobileMdy(string virtualPath, ArcNET.Formats.MobileMdyFile mdy) { }
    }
    public static class SaveGameWriter
    {
        public static void Save(ArcNET.Editor.LoadedSave original, string saveFolder, string slotName, ArcNET.Editor.SaveGameUpdates? updates = null) { }
        public static void Save(ArcNET.Editor.LoadedSave original, string gsiPath, string tfaiPath, string tfafPath, ArcNET.Editor.SaveGameUpdates? updates = null) { }
        public static System.Threading.Tasks.Task SaveAsync(ArcNET.Editor.LoadedSave original, string saveFolder, string slotName, ArcNET.Editor.SaveGameUpdates? updates = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static System.Threading.Tasks.Task SaveAsync(ArcNET.Editor.LoadedSave original, string gsiPath, string tfaiPath, string tfafPath, ArcNET.Editor.SaveGameUpdates? updates = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public sealed class SaveValidationIssue : System.IEquatable<ArcNET.Editor.SaveValidationIssue>
    {
        public SaveValidationIssue() { }
        public string? FilePath { get; init; }
        public required string Message { get; init; }
        public required ArcNET.Editor.SaveValidationSeverity Severity { get; init; }
        public override string ToString() { }
    }
    public enum SaveValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
    public sealed class ScriptBuilder
    {
        public ScriptBuilder() { }
        public ScriptBuilder(ArcNET.Formats.ScrFile existing) { }
        public ArcNET.Editor.ScriptBuilder AddCondition(ArcNET.Formats.ScriptConditionData condition) { }
        public ArcNET.Editor.ScriptBuilder AddCondition(ArcNET.Formats.ScriptConditionType conditionType, ArcNET.Formats.ScriptActionType actionType = 0, ArcNET.Formats.ScriptActionType elseActionType = 0) { }
        public ArcNET.Formats.ScrFile Build() { }
        public ArcNET.Editor.ScriptBuilder RemoveCondition(int index) { }
        public ArcNET.Editor.ScriptBuilder ReplaceCondition(int index, ArcNET.Formats.ScriptConditionData condition) { }
        public ArcNET.Editor.ScriptBuilder ReplaceCondition(int index, ArcNET.Formats.ScriptConditionType conditionType, ArcNET.Formats.ScriptActionType actionType = 0, ArcNET.Formats.ScriptActionType elseActionType = 0) { }
        public ArcNET.Editor.ScriptBuilder SetActionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public ArcNET.Editor.ScriptBuilder SetConditionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public ArcNET.Editor.ScriptBuilder SetElseActionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.ScriptValidationIssue> Validate() { }
        public ArcNET.Editor.ScriptBuilder WithDescription(string description) { }
        public ArcNET.Editor.ScriptBuilder WithFlags(ArcNET.Formats.ScriptFlags flags) { }
        public ArcNET.Editor.ScriptBuilder WithHeaderCounters(uint counters) { }
        public ArcNET.Editor.ScriptBuilder WithHeaderFlags(uint flags) { }
    }
    public sealed class ScriptEditor
    {
        public ScriptEditor(ArcNET.Formats.ScrFile script) { }
        public bool CanRedo { get; }
        public bool CanUndo { get; }
        public bool HasPendingChanges { get; }
        public ArcNET.Editor.ScriptEditor AddCondition(ArcNET.Formats.ScriptConditionData condition) { }
        public ArcNET.Editor.ScriptEditor AddCondition(ArcNET.Formats.ScriptConditionType conditionType, ArcNET.Formats.ScriptActionType actionType = 0, ArcNET.Formats.ScriptActionType elseActionType = 0) { }
        public ArcNET.Formats.ScrFile CommitPendingChanges() { }
        public ArcNET.Editor.ScriptEditor DiscardPendingChanges() { }
        public ArcNET.Editor.ScriptEditor Edit(System.Action<ArcNET.Editor.ScriptBuilder> update) { }
        public ArcNET.Formats.ScrFile GetCurrentScript() { }
        public ArcNET.Formats.ScrFile? GetPendingScript() { }
        public ArcNET.Editor.ScriptEditor Redo() { }
        public ArcNET.Editor.ScriptEditor RemoveCondition(int index) { }
        public ArcNET.Editor.ScriptEditor ReplaceCondition(int index, ArcNET.Formats.ScriptConditionData condition) { }
        public ArcNET.Editor.ScriptEditor ReplaceCondition(int index, ArcNET.Formats.ScriptConditionType conditionType, ArcNET.Formats.ScriptActionType actionType = 0, ArcNET.Formats.ScriptActionType elseActionType = 0) { }
        public ArcNET.Editor.ScriptEditor SetActionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public ArcNET.Editor.ScriptEditor SetConditionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public ArcNET.Editor.ScriptEditor SetElseActionOperands(int index, System.ReadOnlySpan<ArcNET.Editor.ScriptOperand> operands) { }
        public ArcNET.Editor.ScriptEditor Undo() { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.ScriptValidationIssue> Validate() { }
        public ArcNET.Editor.ScriptEditor WithDescription(string description) { }
        public ArcNET.Editor.ScriptEditor WithFlags(ArcNET.Formats.ScriptFlags flags) { }
        public ArcNET.Editor.ScriptEditor WithHeaderCounters(uint counters) { }
        public ArcNET.Editor.ScriptEditor WithHeaderFlags(uint flags) { }
        public ArcNET.Editor.ScriptEditor WithScript(ArcNET.Formats.ScrFile script) { }
        public ArcNET.Editor.ScriptEditor WithScript(System.Func<ArcNET.Formats.ScrFile, ArcNET.Formats.ScrFile> update) { }
    }
    public readonly struct ScriptOperand : System.IEquatable<ArcNET.Editor.ScriptOperand>
    {
        public ScriptOperand(byte Type, int Value) { }
        public byte Type { get; init; }
        public int Value { get; init; }
        public static ArcNET.Editor.ScriptOperand FromFocusObject(ArcNET.Formats.ScriptFocusObject focusObject, int value = 0) { }
        public static ArcNET.Editor.ScriptOperand FromRaw(byte type, int value) { }
        public static ArcNET.Editor.ScriptOperand FromValueType(ArcNET.Formats.ScriptValueType type, int value) { }
    }
    public enum ScriptValidationCode
    {
        DescriptionTooLong = 0,
        DescriptionContainsNonAscii = 1,
        UnknownAttachmentSlot = 2,
    }
    public sealed class ScriptValidationIssue : System.IEquatable<ArcNET.Editor.ScriptValidationIssue>
    {
        public ScriptValidationIssue() { }
        public int? AttachmentSlotIndex { get; init; }
        public required ArcNET.Editor.ScriptValidationCode Code { get; init; }
        public required string Message { get; init; }
        public required ArcNET.Editor.ScriptValidationSeverity Severity { get; init; }
        public override string ToString() { }
    }
    public enum ScriptValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
    public static class ScriptValidator
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.ScriptValidationIssue> Validate(ArcNET.Formats.ScrFile script) { }
    }
    public sealed class SectorBuilder
    {
        public SectorBuilder() { }
        public SectorBuilder(ArcNET.Formats.Sector sector) { }
        public ArcNET.Editor.SectorBuilder AddLight(ArcNET.Formats.SectorLight light) { }
        public ArcNET.Editor.SectorBuilder AddObject(ArcNET.Formats.MobData obj) { }
        public ArcNET.Editor.SectorBuilder AddTileScript(ArcNET.Formats.TileScript script) { }
        public ArcNET.Formats.Sector Build() { }
        public ArcNET.Editor.SectorBuilder ClearLights() { }
        public ArcNET.Editor.SectorBuilder ClearObjects() { }
        public ArcNET.Editor.SectorBuilder ClearRoofs() { }
        public ArcNET.Editor.SectorBuilder RemoveLight(int index) { }
        public ArcNET.Editor.SectorBuilder RemoveObject(int index) { }
        public ArcNET.Editor.SectorBuilder RemoveTileScript(int index) { }
        public ArcNET.Editor.SectorBuilder ReplaceLight(int index, ArcNET.Formats.SectorLight light) { }
        public ArcNET.Editor.SectorBuilder ReplaceObject(int index, ArcNET.Formats.MobData obj) { }
        public ArcNET.Editor.SectorBuilder ReplaceTileScript(int index, ArcNET.Formats.TileScript script) { }
        public ArcNET.Editor.SectorBuilder SetBlocked(int tileX, int tileY, bool blocked) { }
        public ArcNET.Editor.SectorBuilder SetRoof(int roofX, int roofY, uint artId) { }
        public ArcNET.Editor.SectorBuilder SetTile(int tileX, int tileY, uint artId) { }
        public ArcNET.Editor.SectorBuilder WithAptitudeAdjustment(int value) { }
        public ArcNET.Editor.SectorBuilder WithLightSchemeIdx(int value) { }
        public ArcNET.Editor.SectorBuilder WithSectorScript(ArcNET.GameObjects.GameObjectScript? script) { }
        public ArcNET.Editor.SectorBuilder WithSoundList(ArcNET.Formats.SectorSoundList soundList) { }
        public ArcNET.Editor.SectorBuilder WithTownmapInfo(int value) { }
    }
}
namespace ArcNET.Editor.Runtime
{
    public enum CharacterSheetPropertyId
    {
        HpBonus = 27,
        HpLoss = 29,
        MpBonus = 224,
        MpLoss = 226,
        Name = 270,
        Flags = 19,
    }
    public static class CharacterSheetRuntimeLayout
    {
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.Runtime.RuntimeFieldDescriptor> BasicSkillsFields { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.Runtime.RuntimeFieldDescriptor> MainStatsFields { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.Runtime.RuntimeFieldDescriptor> SpellAndTechFields { get; }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Editor.Runtime.RuntimeFieldDescriptor> TechSkillsFields { get; }
    }
    public enum CharacterSheetSubstructureId
    {
        MainStats = 220,
        BasicSkills = 221,
        TechSkills = 222,
        SpellAndTech = 223,
    }
    public readonly struct RuntimeFieldDescriptor : System.IEquatable<ArcNET.Editor.Runtime.RuntimeFieldDescriptor>
    {
        public RuntimeFieldDescriptor(string Name, int Offset) { }
        public string Name { get; init; }
        public int Offset { get; init; }
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
        public static string ResolvePath(string gameDir, string relativePath) { }
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
    public sealed class MobFieldPatch : ArcNET.BinaryPatch.Patches.ObjectFieldPatchBase
    {
        protected override System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> ParseProperties(System.ReadOnlyMemory<byte> data) { }
        protected override byte[] ParseTransformSerialize(System.ReadOnlyMemory<byte> original, System.Func<System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>> transform) { }
        public static ArcNET.BinaryPatch.Patches.MobFieldPatch Custom(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, System.Func<ArcNET.Formats.ObjectProperty, bool>? needsApplyPredicate, System.Func<ArcNET.Formats.ObjectProperty, ArcNET.Formats.ObjectProperty> transform) { }
        public static ArcNET.BinaryPatch.Patches.MobFieldPatch SetInt32(string id, string description, string relativePath, ArcNET.GameObjects.ObjectField field, int expectedValue, int newValue) { }
    }
    public abstract class ObjectFieldPatchBase : ArcNET.BinaryPatch.IBinaryPatch
    {
        protected ObjectFieldPatchBase(string id, string description, ArcNET.BinaryPatch.PatchTarget target, ArcNET.GameObjects.ObjectField field, System.Func<ArcNET.Formats.ObjectProperty, bool>? predicate, System.Func<ArcNET.Formats.ObjectProperty, ArcNET.Formats.ObjectProperty> transform) { }
        public string Description { get; }
        public string Id { get; }
        public string PatchSummary { get; }
        public ArcNET.BinaryPatch.PatchTarget Target { get; }
        public byte[] Apply(System.ReadOnlyMemory<byte> original) { }
        public bool NeedsApply(System.ReadOnlyMemory<byte> original) { }
        protected abstract System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> ParseProperties(System.ReadOnlyMemory<byte> data);
        protected abstract byte[] ParseTransformSerialize(System.ReadOnlyMemory<byte> original, System.Func<System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>> transform);
    }
    public sealed class ProtoFieldPatch : ArcNET.BinaryPatch.Patches.ObjectFieldPatchBase
    {
        protected override System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty> ParseProperties(System.ReadOnlyMemory<byte> data) { }
        protected override byte[] ParseTransformSerialize(System.ReadOnlyMemory<byte> original, System.Func<System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.ObjectProperty>> transform) { }
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

## ArcNET.Diagnostics.FileTime

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace ArcNET.Diagnostics
{
    public readonly struct AlignedQuadRunSummary : System.IEquatable<ArcNET.Diagnostics.AlignedQuadRunSummary>
    {
        public AlignedQuadRunSummary(int StartRow, int Length, ArcNET.Diagnostics.AlignedQuadSignature Signature, int FirstA, int LastA) { }
        public int FirstA { get; init; }
        public int LastA { get; init; }
        public int Length { get; init; }
        public ArcNET.Diagnostics.AlignedQuadSignature Signature { get; init; }
        public int StartRow { get; init; }
    }
    public readonly struct AlignedQuadSignature : System.IEquatable<ArcNET.Diagnostics.AlignedQuadSignature>
    {
        public AlignedQuadSignature(int B, int C, int D) { }
        public int B { get; init; }
        public int C { get; init; }
        public int D { get; init; }
    }
    public readonly struct AlignedQuadSignatureSummary : System.IEquatable<ArcNET.Diagnostics.AlignedQuadSignatureSummary>
    {
        public AlignedQuadSignatureSummary(ArcNET.Diagnostics.AlignedQuadSignature Signature, int Count, int FirstRow, int LastRow, int FirstA, int LastA, int LongestRunLength, int LongestRunStart, int LongestRunFirstA, int LongestRunLastA) { }
        public int Count { get; init; }
        public int FirstA { get; init; }
        public int FirstRow { get; init; }
        public int LastA { get; init; }
        public int LastRow { get; init; }
        public int LongestRunFirstA { get; init; }
        public int LongestRunLastA { get; init; }
        public int LongestRunLength { get; init; }
        public int LongestRunStart { get; init; }
        public ArcNET.Diagnostics.AlignedQuadSignature Signature { get; init; }
    }
    public readonly struct AlignedQuadSummary : System.IEquatable<ArcNET.Diagnostics.AlignedQuadSummary>
    {
        public AlignedQuadSummary(
                    int StartInt,
                    int QuadCount,
                    int RemainderInts,
                    int DistinctSignatures,
                    int SectionCount,
                    int ZeroSectionCount,
                    int LongestZeroSectionStart,
                    int LongestZeroSectionLength,
                    int FrontMatterRowCount,
                    int FrontMatterSectionCount,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> FrontMatterRuns,
                    int TailRowStart,
                    int TailRowCount,
                    int TailSectionCount,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TailRuns,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> LeadingRuns,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TrailingRuns,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadSignatureSummary> TopSignatures,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TopRuns) { }
        public int DistinctSignatures { get; init; }
        public int FrontMatterRowCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> FrontMatterRuns { get; init; }
        public int FrontMatterSectionCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> LeadingRuns { get; init; }
        public int LongestZeroSectionLength { get; init; }
        public int LongestZeroSectionStart { get; init; }
        public int QuadCount { get; init; }
        public int RemainderInts { get; init; }
        public int SectionCount { get; init; }
        public int StartInt { get; init; }
        public int TailRowCount { get; init; }
        public int TailRowStart { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TailRuns { get; init; }
        public int TailSectionCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TopRuns { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadSignatureSummary> TopSignatures { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.AlignedQuadRunSummary> TrailingRuns { get; init; }
        public int ZeroSectionCount { get; init; }
    }
    public sealed class AmmoItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.AmmoItemAnalysisSnapshot>
    {
        public AmmoItemAnalysisSnapshot(int? Quantity, int? AmmoType) { }
        public int? AmmoType { get; init; }
        public int? Quantity { get; init; }
    }
    public sealed class ArmorItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.ArmorItemAnalysisSnapshot>
    {
        public ArmorItemAnalysisSnapshot(int? ArmorClassAdjustment, int? MagicArmorClassAdjustment, int? SilentMoveAdjustment) { }
        public int? ArmorClassAdjustment { get; init; }
        public int? MagicArmorClassAdjustment { get; init; }
        public int? SilentMoveAdjustment { get; init; }
    }
    public static class ArtDumper
    {
        public static string Dump(ArcNET.Formats.ArtFile art) { }
        public static void Dump(ArcNET.Formats.ArtFile art, System.IO.TextWriter writer) { }
    }
    public sealed class BytePattern
    {
        public byte?[] Bytes { get; }
        public int Length { get; }
        public string NormalizedText { get; }
        public int[] FindMatches(System.ReadOnlySpan<byte> haystack) { }
        public static ArcNET.Diagnostics.BytePattern Parse(string text) { }
    }
    public readonly struct CatalogAddressResolution : System.IEquatable<ArcNET.Diagnostics.CatalogAddressResolution>
    {
        public CatalogAddressResolution(uint Rva, string Resolution) { }
        public string Resolution { get; init; }
        public uint Rva { get; init; }
    }
    public sealed class CatalogAddressResolveRequest : System.IEquatable<ArcNET.Diagnostics.CatalogAddressResolveRequest>
    {
        public CatalogAddressResolveRequest(string Key, int PreferredRva, string Operation, string ModuleFileName, int ModuleSize, byte[] ModuleBytes, ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile, bool ForceSignatureFallback, System.Collections.Generic.IReadOnlyDictionary<string, string> SignaturesByNormalizedKey, string AddressCacheKey) { }
        public string AddressCacheKey { get; init; }
        public bool ForceSignatureFallback { get; init; }
        public string Key { get; init; }
        public byte[] ModuleBytes { get; init; }
        public string ModuleFileName { get; init; }
        public int ModuleSize { get; init; }
        public string Operation { get; init; }
        public int PreferredRva { get; init; }
        public ArcNET.Diagnostics.Contracts.RuntimeProfileSnapshot RuntimeProfile { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> SignaturesByNormalizedKey { get; init; }
    }
    public static class CatalogAddressResolver
    {
        public const string ForceSignatureFallbackEnvironmentVariable = "ARCNET_DIAGNOSTICS_FORCE_CATALOG_SIGNATURES";
        public const string LegacyForceSignatureFallbackEnvironmentVariable = "ARCNET_LIVELAB_FORCE_CATALOG_SIGNATURES";
        public static string NormalizeKey(string value) { }
        public static ArcNET.Diagnostics.CatalogAddressResolution Resolve(ArcNET.Diagnostics.CatalogAddressResolveRequest request) { }
        public static bool ShouldForceSignatureFallback() { }
    }
    public static class CatalogSignatureCatalog
    {
        public static int Count { get; }
        public static System.Collections.Generic.IReadOnlyList<string> Keys { get; }
        public static System.Collections.Generic.IReadOnlyDictionary<string, string> SignaturesByNormalizedKey { get; }
        public static bool HasSignature(string key) { }
        public static bool TryGetPattern(string key, out string pattern) { }
    }
    public static class CeSourceCatalogLoader
    {
        public const string SourceRootEnvironmentVariable = "ARCNET_ARCANUM_CE_SOURCE";
        public static ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceCatalog Load(string sourceRoot) { }
        public static ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceCatalog LoadDefault() { }
        public static string ResolveDefaultSourceRoot() { }
        public static string ResolveSourceRoot(string sourceRoot) { }
        public sealed class CeSourceCatalog
        {
            public CeSourceCatalog(string sourceRoot, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceFunction> functions) { }
            public int DuplicateNameCount { get; }
            public int FunctionCount { get; }
            public ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceFunction[] Functions { get; }
            public string SourceRoot { get; }
            public int UniqueNameCount { get; }
            public ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceFunction[] FindMatches(string token) { }
            public System.Collections.Generic.IEnumerable<ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceFunction> Query(string? filter, string? area) { }
        }
        public readonly struct CeSourceFunction : System.IEquatable<ArcNET.Diagnostics.CeSourceCatalogLoader.CeSourceFunction>
        {
            public CeSourceFunction(string Name, string RelativePath, int LineNumber, string Area, bool IsStatic, string Signature) { }
            public string Area { get; init; }
            public bool IsStatic { get; init; }
            public int LineNumber { get; init; }
            public string Name { get; init; }
            public string RelativePath { get; init; }
            public string Signature { get; init; }
        }
    }
    public sealed class CharacterSarAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarAuditSnapshot>
    {
        public CharacterSarAuditSnapshot(int Offset, int TotalBytes, int ElementSize, int ElementCount, int BitsetWordCount, int BitsetId, int BitSlotCount, string Fingerprint, string Annotation, System.Collections.Generic.IReadOnlyList<int> SampleValues, System.Collections.Generic.IReadOnlyList<int> BitSlots) { }
        public string Annotation { get; init; }
        public int BitSlotCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BitSlots { get; init; }
        public int BitsetId { get; init; }
        public int BitsetWordCount { get; init; }
        public int ElementCount { get; init; }
        public int ElementSize { get; init; }
        public string Fingerprint { get; init; }
        public int Offset { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> SampleValues { get; init; }
        public int TotalBytes { get; init; }
    }
    public static class CharacterSarDiagnostics
    {
        public static string AnnotateBsId(int bitsetId) { }
        public static string AnnotateFingerprint(string fingerprint) { }
        public static string AnnotateSarValue(ArcNET.Diagnostics.CharacterSarEntrySnapshot sar) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Idx",
                "VA",
                "VB"})]
        public static System.Collections.Generic.List<System.ValueTuple<int, int, int>> CompareElements(ArcNET.Diagnostics.CharacterSarEntrySnapshot entryA, ArcNET.Diagnostics.CharacterSarEntrySnapshot entryB) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarAuditSnapshot> CreateAuditSnapshots(byte[] rawBytes, int limit) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarDumpEntrySnapshot> CreateDumpEntries(byte[] rawBytes) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Slot",
                "Value"})]
        public static System.Collections.Generic.List<System.ValueTuple<int, int>> DecodeReputation(ArcNET.Diagnostics.CharacterSarEntrySnapshot sar, int[] reputationRaw) { }
        public static string FormatElements(byte[] rawBytes, int dataOffset, int elementSize, int elementCount, int maxShow = 32) { }
        public static string FormatInt32List(System.Collections.Generic.IReadOnlyList<int> values, int maxShow = 2147483647) { }
        public static string FormatInt32Preview(System.Collections.Generic.IReadOnlyList<int> values, int maxShow) { }
        public static string FormatSlotList(System.Collections.Generic.IReadOnlyList<int> slots, int maxShow = 2147483647) { }
        public static string GetElementLabel(string fingerprint, int index) { }
        public static bool IsPointerLike(int value) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarPairing> MatchGroups(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> entriesA, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> entriesB) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> Parse(byte[] rawBytes, int startOffset = 12) { }
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Semantic",
                "PointerCount",
                "Idx",
                "VA",
                "VB"})]
        public static System.ValueTuple<System.Collections.Generic.List<System.ValueTuple<int, int, int>>, int> PartitionElementDiffs([System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Idx",
                "VA",
                "VB"})] System.Collections.Generic.List<System.ValueTuple<int, int, int>> diffs) { }
    }
    public sealed class CharacterSarDiffEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarDiffEntrySnapshot>
    {
        public CharacterSarDiffEntrySnapshot(ArcNET.Diagnostics.CharacterSarDiffKind Kind, string Fingerprint, int OccurrenceIndex, int OccurrenceCount, string Annotation, int? BeforeElementCount, int? AfterElementCount, string? BeforeValueSummary, string? AfterValueSummary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementValueDiffSnapshot> ChangedElements) { }
        public int? AfterElementCount { get; init; }
        public string? AfterValueSummary { get; init; }
        public string Annotation { get; init; }
        public int? BeforeElementCount { get; init; }
        public string? BeforeValueSummary { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementValueDiffSnapshot> ChangedElements { get; init; }
        public string Fingerprint { get; init; }
        public ArcNET.Diagnostics.CharacterSarDiffKind Kind { get; init; }
        public int OccurrenceCount { get; init; }
        public int OccurrenceIndex { get; init; }
    }
    public enum CharacterSarDiffKind
    {
        ElementValuesChanged = 0,
        SummaryChanged = 1,
        Added = 2,
        Removed = 3,
    }
    public static class CharacterSarDiffService
    {
        public static ArcNET.Diagnostics.CharacterSarDiffSnapshot Compare(byte[] rawA, byte[] rawB) { }
        public static ArcNET.Diagnostics.CharacterSarDiffSnapshot Compare(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> sarsA, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> sarsB, byte[] rawA, byte[] rawB) { }
    }
    public sealed class CharacterSarDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarDiffSnapshot>
    {
        public CharacterSarDiffSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarDiffEntrySnapshot> Entries) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarDiffEntrySnapshot> Entries { get; init; }
        public bool HasChanges { get; }
    }
    public sealed class CharacterSarDumpEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarDumpEntrySnapshot>
    {
        public CharacterSarDumpEntrySnapshot(int BitsetId, int ElementSize, int ElementCount, int BitsetWordCount, string Annotation, string ValuePreview, bool IsFiller) { }
        public string Annotation { get; init; }
        public int BitsetId { get; init; }
        public int BitsetWordCount { get; init; }
        public int ElementCount { get; init; }
        public int ElementSize { get; init; }
        public bool IsFiller { get; init; }
        public string ValuePreview { get; init; }
    }
    public sealed class CharacterSarElementHexSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarElementHexSnapshot>
    {
        public CharacterSarElementHexSnapshot(int Index, string Hex) { }
        public string Hex { get; init; }
        public int Index { get; init; }
    }
    public readonly struct CharacterSarElementValueDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarElementValueDiffSnapshot>
    {
        public CharacterSarElementValueDiffSnapshot(int Index, int BeforeValue, int AfterValue) { }
        public int AfterValue { get; init; }
        public int BeforeValue { get; init; }
        public int Index { get; init; }
    }
    public sealed class CharacterSarEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarEntrySnapshot>
    {
        public CharacterSarEntrySnapshot(int Offset, int TotalBytes, int DataOffset, int ElementSize, int ElementCount, int BitsetWordCount, int BitsetId, int BitSlotCount, string Fingerprint, System.Collections.Generic.IReadOnlyList<int> Values, System.Collections.Generic.IReadOnlyList<int> BitSlots, bool IsFiller) { }
        public int BitSlotCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BitSlots { get; init; }
        public int BitsetId { get; init; }
        public int BitsetWordCount { get; init; }
        public int DataOffset { get; init; }
        public int ElementCount { get; init; }
        public int ElementSize { get; init; }
        public string Fingerprint { get; init; }
        public bool IsFiller { get; init; }
        public int Offset { get; init; }
        public int TotalBytes { get; init; }
        public string ValueSummary { get; }
        public System.Collections.Generic.IReadOnlyList<int> Values { get; init; }
    }
    public sealed class CharacterSarFullDumpEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarFullDumpEntrySnapshot>
    {
        public CharacterSarFullDumpEntrySnapshot(int Offset, int BitsetId, int ElementSize, int ElementCount, int BitsetWordCount, string Fingerprint, string Annotation, bool IsFiller, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarInt32RowSnapshot> Int32Rows, string? ByteHex, string? ByteAscii, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementHexSnapshot> ElementHexes, int OmittedElementCount, System.Collections.Generic.IReadOnlyList<int> BitSlots) { }
        public string Annotation { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BitSlots { get; init; }
        public int BitsetId { get; init; }
        public int BitsetWordCount { get; init; }
        public string? ByteAscii { get; init; }
        public string? ByteHex { get; init; }
        public int ElementCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementHexSnapshot> ElementHexes { get; init; }
        public int ElementSize { get; init; }
        public string Fingerprint { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarInt32RowSnapshot> Int32Rows { get; init; }
        public bool IsFiller { get; init; }
        public int Offset { get; init; }
        public int OmittedElementCount { get; init; }
    }
    public static class CharacterSarFullDumpService
    {
        public static ArcNET.Diagnostics.CharacterSarFullDumpSnapshot Create(byte[] rawBytes, int? bitsetWordCountFilter = default) { }
    }
    public sealed class CharacterSarFullDumpSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarFullDumpSnapshot>
    {
        public CharacterSarFullDumpSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarFullDumpEntrySnapshot> Entries) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarFullDumpEntrySnapshot> Entries { get; init; }
        public int PrintedCount { get; }
    }
    public sealed class CharacterSarInt32RowSnapshot : System.IEquatable<ArcNET.Diagnostics.CharacterSarInt32RowSnapshot>
    {
        public CharacterSarInt32RowSnapshot(int StartIndex, System.Collections.Generic.IReadOnlyList<int> Values) { }
        public int StartIndex { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Values { get; init; }
    }
    public readonly struct CharacterSarPairing : System.IEquatable<ArcNET.Diagnostics.CharacterSarPairing>
    {
        public CharacterSarPairing(int IndexA, int IndexB, int Score) { }
        public int IndexA { get; init; }
        public int IndexB { get; init; }
        public int Score { get; init; }
    }
    public readonly struct CodeAnchor : System.IEquatable<ArcNET.Diagnostics.CodeAnchor>
    {
        public CodeAnchor(int Rva, string Key) { }
        public string Key { get; init; }
        public int Rva { get; init; }
    }
    public static class CodeCatalog
    {
        public static string DefaultModuleFileName { get; }
        public static string FormatModuleAddress(uint rva) { }
        public static string FormatModuleAddress(string moduleFileName, uint rva) { }
        public static string FormatModuleOffset(uint rva) { }
        public static string FormatModuleOffset(string moduleFileName, uint rva) { }
        public static bool TryResolveAnchor(uint rva, out ArcNET.Diagnostics.ResolvedCodeAnchor resolvedAnchor) { }
    }
    public enum DiagnosticIssueSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
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
    public sealed class FoodItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.FoodItemAnalysisSnapshot>
    {
        public FoodItemAnalysisSnapshot(int? Flags) { }
        public int? Flags { get; init; }
    }
    public sealed class GenericItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.GenericItemAnalysisSnapshot>
    {
        public GenericItemAnalysisSnapshot(int? UsageBonus, int? UsesRemaining) { }
        public int? UsageBonus { get; init; }
        public int? UsesRemaining { get; init; }
    }
    public sealed class GoldItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.GoldItemAnalysisSnapshot>
    {
        public GoldItemAnalysisSnapshot(int? Quantity) { }
        public int? Quantity { get; init; }
    }
    public static class ItemDumper
    {
        public static string DumpContainerItems(ArcNET.Archive.DatArchive archive, string containerMobPath, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation = 0) { }
        public static string DumpContainerItems(ArcNET.Formats.MobData container, ArcNET.Archive.DatArchive archiveForItems, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, ArcNET.Core.ArcanumInstallationType installation = 0) { }
        public static void DumpContainerItems(ArcNET.Archive.DatArchive archive, string containerMobPath, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, System.IO.TextWriter writer, ArcNET.Core.ArcanumInstallationType installation = 0) { }
        public static void DumpContainerItems(ArcNET.Formats.MobData container, ArcNET.Archive.DatArchive archiveForItems, string mapDirPrefix, System.Collections.Generic.Dictionary<int, string> nameLookup, System.IO.TextWriter writer, ArcNET.Core.ArcanumInstallationType installation = 0) { }
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
    public sealed class KeyItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.KeyItemAnalysisSnapshot>
    {
        public KeyItemAnalysisSnapshot(int? KeyId) { }
        public int? KeyId { get; init; }
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
    public static class MobItemAnalysisService
    {
        public static ArcNET.Diagnostics.MobItemAnalysisSnapshot Analyze(ArcNET.Formats.MobData mob) { }
    }
    public sealed class MobItemAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.MobItemAnalysisSnapshot>
    {
        public MobItemAnalysisSnapshot(ArcNET.GameObjects.ObjectType ObjectType, int? Weight, int? Worth, int? ItemFlags, System.Collections.Generic.IReadOnlyList<string> ItemFlagNames, int? Discipline, string? DisciplineLabel, int? Complexity, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobItemSpellEffectSnapshot> SpellEffects, ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot? Specific) { }
        public int? Complexity { get; init; }
        public int? Discipline { get; init; }
        public string? DisciplineLabel { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> ItemFlagNames { get; init; }
        public int? ItemFlags { get; init; }
        public ArcNET.GameObjects.ObjectType ObjectType { get; init; }
        public ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot? Specific { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobItemSpellEffectSnapshot> SpellEffects { get; init; }
        public int? Weight { get; init; }
        public int? Worth { get; init; }
    }
    public abstract class MobItemSpecificAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot>
    {
        protected MobItemSpecificAnalysisSnapshot() { }
    }
    public sealed class MobItemSpellEffectSnapshot : System.IEquatable<ArcNET.Diagnostics.MobItemSpellEffectSnapshot>
    {
        public MobItemSpellEffectSnapshot(int Slot, int SpellId) { }
        public int Slot { get; init; }
        public int SpellId { get; init; }
    }
    public sealed class MobileMdyAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.MobileMdyAuditSnapshot>
    {
        public MobileMdyAuditSnapshot(string Path, int RecordCount, int MobRecordCount, int CharacterRecordCount, int DuplicateObjectIdCount, int PropertyCount, int PropertyParseNoteCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobileMdyObjectTypeSnapshot> ObjectTypes) { }
        public int CharacterRecordCount { get; init; }
        public int DuplicateObjectIdCount { get; init; }
        public int MobRecordCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobileMdyObjectTypeSnapshot> ObjectTypes { get; init; }
        public string Path { get; init; }
        public int PropertyCount { get; init; }
        public int PropertyParseNoteCount { get; init; }
        public int RecordCount { get; init; }
    }
    public sealed class MobileMdyObjectTypeSnapshot : System.IEquatable<ArcNET.Diagnostics.MobileMdyObjectTypeSnapshot>
    {
        public MobileMdyObjectTypeSnapshot(string ObjectType, int Count) { }
        public int Count { get; init; }
        public string ObjectType { get; init; }
    }
    public static class ModuleAddressFormatter
    {
        public static string FormatModuleAddress(string moduleFileName, uint rva) { }
        public static string FormatModuleOffset(string moduleFileName, uint rva) { }
    }
    public readonly struct ModuleFunctionSymbol : System.IEquatable<ArcNET.Diagnostics.ModuleFunctionSymbol>
    {
        public ModuleFunctionSymbol(string Name, uint Rva, uint Size) { }
        public string Name { get; init; }
        public uint Rva { get; init; }
        public uint Size { get; init; }
    }
    public sealed class ModuleSymbolCatalog
    {
        public ModuleSymbolCatalog(string modulePath, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ModuleFunctionSymbol> symbols) { }
        public int DuplicateNameCount { get; }
        public int FunctionCount { get; }
        public string ModuleFileName { get; }
        public string ModulePath { get; }
        public ArcNET.Diagnostics.ModuleFunctionSymbol[] Symbols { get; }
        public int UniqueNameCount { get; }
        public ArcNET.Diagnostics.ModuleFunctionSymbol[] FindMatches(string token) { }
        public System.Collections.Generic.IEnumerable<ArcNET.Diagnostics.ModuleFunctionSymbol> Query(string? filter, int limit, bool duplicatesOnly) { }
        public ArcNET.Diagnostics.ModuleFunctionSymbol ResolveUnique(string token) { }
        public bool TryResolveUnique(string token, out ArcNET.Diagnostics.ModuleFunctionSymbol symbol) { }
    }
    public sealed class ObjectFieldUsageSnapshot : System.IEquatable<ArcNET.Diagnostics.ObjectFieldUsageSnapshot>
    {
        public ObjectFieldUsageSnapshot(string Field, int Count, int ParseNoteCount, long TotalRawBytes) { }
        public int Count { get; init; }
        public string Field { get; init; }
        public int ParseNoteCount { get; init; }
        public long TotalRawBytes { get; init; }
    }
    public sealed class PlayerCharacterAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerCharacterAuditSnapshot>
    {
        public PlayerCharacterAuditSnapshot(
                    string SourcePath,
                    int RecordSize,
                    bool HasCompleteData,
                    string? Name,
                    int Level,
                    int Gold,
                    int Arrows,
                    int Bullets,
                    int PowerCells,
                    int TotalKills,
                    int QuestCount,
                    int RumorsCount,
                    int BlessingCount,
                    int CurseCount,
                    int SchematicsCount,
                    int ReputationCount,
                    int EffectsCount,
                    int? PositionAid,
                    int? PositionLocation,
                    int? PositionOffsetX,
                    int HpDamage,
                    int FatigueDamage,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarAuditSnapshot> Sars) { }
        public int Arrows { get; init; }
        public int BlessingCount { get; init; }
        public int Bullets { get; init; }
        public int CurseCount { get; init; }
        public int EffectsCount { get; init; }
        public int FatigueDamage { get; init; }
        public int Gold { get; init; }
        public bool HasCompleteData { get; init; }
        public int HpDamage { get; init; }
        public int Level { get; init; }
        public string? Name { get; init; }
        public int? PositionAid { get; init; }
        public int? PositionLocation { get; init; }
        public int? PositionOffsetX { get; init; }
        public int PowerCells { get; init; }
        public int QuestCount { get; init; }
        public int RecordSize { get; init; }
        public int ReputationCount { get; init; }
        public int RumorsCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarAuditSnapshot> Sars { get; init; }
        public int SchematicsCount { get; init; }
        public string SourcePath { get; init; }
        public int TotalKills { get; init; }
    }
    public sealed class PlayerCharacterProgressionSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerCharacterProgressionSnapshot>
    {
        public PlayerCharacterProgressionSnapshot(
                    string? Name,
                    int Level,
                    int ExperiencePoints,
                    int Alignment,
                    int FatePoints,
                    int Race,
                    int Gender,
                    int Age,
                    int PoisonLevel,
                    int UnspentPoints,
                    int MagickPoints,
                    int TechPoints,
                    int Gold,
                    int Arrows,
                    int TotalKills,
                    int Bullets,
                    int PowerCells,
                    int HpDamage,
                    int FatigueDamage) { }
        public int Age { get; init; }
        public int Alignment { get; init; }
        public int Arrows { get; init; }
        public int Bullets { get; init; }
        public int ExperiencePoints { get; init; }
        public int FatePoints { get; init; }
        public int FatigueDamage { get; init; }
        public int Gender { get; init; }
        public int Gold { get; init; }
        public int HpDamage { get; init; }
        public int Level { get; init; }
        public int MagickPoints { get; init; }
        public string? Name { get; init; }
        public int PoisonLevel { get; init; }
        public int PowerCells { get; init; }
        public int Race { get; init; }
        public int TechPoints { get; init; }
        public int TotalKills { get; init; }
        public int UnspentPoints { get; init; }
    }
    public sealed class PlayerCharacterSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerCharacterSnapshot>
    {
        public PlayerCharacterSnapshot(string Path, string? Name, int Level, int Race, int RawBytesLength, bool HasCompleteData, bool IsSelectedPlayer, int QuestCount, int ReputationCount, int BlessingCount, int CurseCount, int SchematicsCount, int RumorsCount) { }
        public int BlessingCount { get; init; }
        public int CurseCount { get; init; }
        public bool HasCompleteData { get; init; }
        public bool IsSelectedPlayer { get; init; }
        public int Level { get; init; }
        public string? Name { get; init; }
        public string Path { get; init; }
        public int QuestCount { get; init; }
        public int Race { get; init; }
        public int RawBytesLength { get; init; }
        public int ReputationCount { get; init; }
        public int RumorsCount { get; init; }
        public int SchematicsCount { get; init; }
    }
    public sealed class PlayerIndexedValueSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerIndexedValueSnapshot>
    {
        public PlayerIndexedValueSnapshot(int Index, string Label, int Value) { }
        public int Index { get; init; }
        public string Label { get; init; }
        public int Value { get; init; }
    }
    public sealed class PlayerProgressionChangeSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerProgressionChangeSnapshot>
    {
        public PlayerProgressionChangeSnapshot(string Category, string Key, string Description) { }
        public string Category { get; init; }
        public string Description { get; init; }
        public string Key { get; init; }
    }
    public sealed class PlayerProgressionHistorySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerProgressionHistorySnapshot>
    {
        public PlayerProgressionHistorySnapshot(int FirstSlot, int LastSlot, ArcNET.Diagnostics.QuestLabelCatalogSnapshot? QuestCatalog, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerProgressionSlotSnapshot> Slots) { }
        public int FirstSlot { get; init; }
        public int LastSlot { get; init; }
        public ArcNET.Diagnostics.QuestLabelCatalogSnapshot? QuestCatalog { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerProgressionSlotSnapshot> Slots { get; init; }
    }
    public sealed class PlayerProgressionSlotSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerProgressionSlotSnapshot>
    {
        public PlayerProgressionSlotSnapshot(int Slot, string SlotStem, string LeaderName, int LeaderLevel, bool IsBaseline, ArcNET.Diagnostics.PlayerProgressionStateSnapshot? State, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerProgressionChangeSnapshot> Changes) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerProgressionChangeSnapshot> Changes { get; init; }
        public bool IsBaseline { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public int Slot { get; init; }
        public string SlotStem { get; init; }
        public ArcNET.Diagnostics.PlayerProgressionStateSnapshot? State { get; init; }
    }
    public sealed class PlayerProgressionStateSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerProgressionStateSnapshot>
    {
        public PlayerProgressionStateSnapshot(
                    string Path,
                    string? Name,
                    int Level,
                    int Xp,
                    int Alignment,
                    int Fate,
                    int MagicPoints,
                    int TechPoints,
                    int Gold,
                    int QuestCount,
                    int RumorsCount,
                    int BlessingCount,
                    int CurseCount,
                    int SchematicsCount,
                    int HpDamage,
                    int FatigueDamage,
                    int Bullets,
                    int PowerCells,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerQuestEntrySnapshot> Quests,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BaseStats,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BasicSkills,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> SpellTech) { }
        public int Alignment { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BaseStats { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BasicSkills { get; init; }
        public int BlessingCount { get; init; }
        public int Bullets { get; init; }
        public int CurseCount { get; init; }
        public int Fate { get; init; }
        public int FatigueDamage { get; init; }
        public int Gold { get; init; }
        public int HpDamage { get; init; }
        public int Level { get; init; }
        public int MagicPoints { get; init; }
        public string? Name { get; init; }
        public string Path { get; init; }
        public int PowerCells { get; init; }
        public int QuestCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerQuestEntrySnapshot> Quests { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation { get; init; }
        public int RumorsCount { get; init; }
        public int SchematicsCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> SpellTech { get; init; }
        public int TechPoints { get; init; }
        public int Xp { get; init; }
    }
    public sealed class PlayerQuestBookSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerQuestBookSnapshot>
    {
        public PlayerQuestBookSnapshot(System.DateTimeOffset CapturedAt, string LeaderName, int LeaderLevel, ArcNET.Diagnostics.QuestLabelCatalogSnapshot? QuestCatalog, ArcNET.Diagnostics.PlayerCharacterSnapshot? Player, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerCharacterSnapshot> QuestCharacters, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerQuestEntrySnapshot> Quests, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation, System.Collections.Generic.IReadOnlyList<int> Blessings, System.Collections.Generic.IReadOnlyList<int> Curses, System.Collections.Generic.IReadOnlyList<int> Schematics) { }
        public System.Collections.Generic.IReadOnlyList<int> Blessings { get; init; }
        public System.DateTimeOffset CapturedAt { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Curses { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public ArcNET.Diagnostics.PlayerCharacterSnapshot? Player { get; init; }
        public ArcNET.Diagnostics.QuestLabelCatalogSnapshot? QuestCatalog { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerCharacterSnapshot> QuestCharacters { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerQuestEntrySnapshot> Quests { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Schematics { get; init; }
    }
    public sealed class PlayerQuestEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerQuestEntrySnapshot>
    {
        public PlayerQuestEntrySnapshot(int ProtoId, string? Label, int Context, int Timestamp, int State, string StateDescription) { }
        public int Context { get; init; }
        public string? Label { get; init; }
        public int ProtoId { get; init; }
        public int State { get; init; }
        public string StateDescription { get; init; }
        public int Timestamp { get; init; }
    }
    public sealed class PlayerReputationEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerReputationEntrySnapshot>
    {
        public PlayerReputationEntrySnapshot(int Slot, int Value) { }
        public int Slot { get; init; }
        public int Value { get; init; }
    }
    public static class PlayerSarAnalysisService
    {
        public static ArcNET.Diagnostics.PlayerSarLifecycleAnalysisSnapshot CreateLifecycleAnalysis(ArcNET.Diagnostics.PlayerSarHistorySnapshot history) { }
        public static ArcNET.Diagnostics.PlayerSarTransitionAnalysisSnapshot CreateTransitionAnalysis(ArcNET.Diagnostics.PlayerSarHistorySnapshot history) { }
    }
    public sealed class PlayerSarFingerprintAggregateSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarFingerprintAggregateSnapshot>
    {
        public PlayerSarFingerprintAggregateSnapshot(string Fingerprint, string Annotation, int FirstSlot, int LastSlot, int MinDuplicateCount, int MaxDuplicateCount, int TrackCount, int RecurringTrackCount, int SingleSlotTrackCount, int ChangedTrackCount) { }
        public string Annotation { get; init; }
        public int ChangedTrackCount { get; init; }
        public string Fingerprint { get; init; }
        public int FirstSlot { get; init; }
        public int LastSlot { get; init; }
        public int MaxDuplicateCount { get; init; }
        public int MinDuplicateCount { get; init; }
        public int RecurringTrackCount { get; init; }
        public int SingleSlotTrackCount { get; init; }
        public int TrackCount { get; init; }
    }
    public sealed class PlayerSarFingerprintCountSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarFingerprintCountSnapshot>
    {
        public PlayerSarFingerprintCountSnapshot(string Fingerprint, int Count) { }
        public int Count { get; init; }
        public string Fingerprint { get; init; }
    }
    public sealed class PlayerSarFingerprintSummaryRowSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarFingerprintSummaryRowSnapshot>
    {
        public PlayerSarFingerprintSummaryRowSnapshot(string Fingerprint, string Annotation, string SlotSpan, string DuplicateRange, int TrackCount, int RecurringTrackCount, int SingleSlotTrackCount, int ChangedTrackCount) { }
        public string Annotation { get; init; }
        public int ChangedTrackCount { get; init; }
        public string DuplicateRange { get; init; }
        public string Fingerprint { get; init; }
        public int RecurringTrackCount { get; init; }
        public int SingleSlotTrackCount { get; init; }
        public string SlotSpan { get; init; }
        public int TrackCount { get; init; }
    }
    public static class PlayerSarHistoryService
    {
        public static ArcNET.Diagnostics.PlayerSarHistorySnapshot Create(string saveDir, int firstSlot, int lastSlot, System.Action<string>? log = null) { }
    }
    public sealed class PlayerSarHistorySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarHistorySnapshot>
    {
        public PlayerSarHistorySnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarSlotSnapshot> Slots, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackSnapshot> Tracks) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarSlotSnapshot> Slots { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackSnapshot> Tracks { get; init; }
    }
    public sealed class PlayerSarLifecycleAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarLifecycleAnalysisSnapshot>
    {
        public PlayerSarLifecycleAnalysisSnapshot(int TotalSlots, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarLifecycleTrackSummarySnapshot> Tracks, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintAggregateSnapshot> Fingerprints) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintAggregateSnapshot> Fingerprints { get; init; }
        public int TotalSlots { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarLifecycleTrackSummarySnapshot> Tracks { get; init; }
    }
    public enum PlayerSarLifecycleKind
    {
        AllSlots = 0,
        OnlySlot = 1,
        AppearedAndDisappeared = 2,
        Appeared = 3,
        Disappeared = 4,
        PartialRange = 5,
    }
    public sealed class PlayerSarLifecycleReportSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarLifecycleReportSnapshot>
    {
        public PlayerSarLifecycleReportSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintSummaryRowSnapshot> Fingerprints, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackDetailRowSnapshot> Tracks, int OmittedTrackRowCount, int OmittedSingletonFingerprintCount) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintSummaryRowSnapshot> Fingerprints { get; init; }
        public int OmittedSingletonFingerprintCount { get; init; }
        public int OmittedTrackRowCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackDetailRowSnapshot> Tracks { get; init; }
    }
    public sealed class PlayerSarLifecycleTrackSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarLifecycleTrackSummarySnapshot>
    {
        public PlayerSarLifecycleTrackSummarySnapshot(string FingerprintKey, string Fingerprint, int PresentCount, int FirstSlot, int LastSlot, int MinElementCount, int MaxElementCount, bool ElementCountGrows, System.Collections.Generic.IReadOnlyList<int> BitsetWordCounts, System.Collections.Generic.IReadOnlyList<int> BitsetIds, string FirstValueSummary, string LastValueSummary, bool ValueChanged, string ValueAnnotation, ArcNET.Diagnostics.PlayerSarLifecycleKind Lifecycle) { }
        public System.Collections.Generic.IReadOnlyList<int> BitsetIds { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BitsetWordCounts { get; init; }
        public bool ElementCountGrows { get; init; }
        public string Fingerprint { get; init; }
        public string FingerprintKey { get; init; }
        public int FirstSlot { get; init; }
        public string FirstValueSummary { get; init; }
        public int LastSlot { get; init; }
        public string LastValueSummary { get; init; }
        public ArcNET.Diagnostics.PlayerSarLifecycleKind Lifecycle { get; init; }
        public int MaxElementCount { get; init; }
        public int MinElementCount { get; init; }
        public int PresentCount { get; init; }
        public string ValueAnnotation { get; init; }
        public bool ValueChanged { get; init; }
    }
    public static class PlayerSarReportService
    {
        public static ArcNET.Diagnostics.PlayerSarLifecycleReportSnapshot CreateLifecycleReport(ArcNET.Diagnostics.PlayerSarHistorySnapshot history, ArcNET.Diagnostics.PlayerSarLifecycleAnalysisSnapshot? analysis = null) { }
        public static ArcNET.Diagnostics.PlayerSarTransitionReportSnapshot CreateTransitionReport(ArcNET.Diagnostics.PlayerSarTransitionAnalysisSnapshot analysis) { }
    }
    public sealed class PlayerSarSlotSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarSlotSnapshot>
    {
        public PlayerSarSlotSnapshot(int Slot, int Level, int RawBytesLength, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> Sars, string SaveName) { }
        public int Level { get; init; }
        public int RawBytesLength { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarEntrySnapshot> Sars { get; init; }
        public string SaveName { get; init; }
        public int Slot { get; init; }
    }
    public sealed class PlayerSarTrackDetailRowSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTrackDetailRowSnapshot>
    {
        public PlayerSarTrackDetailRowSnapshot(string FingerprintKey, string Fingerprint, string Annotation, string Lifecycle, string ElementCountRange, string BitsetWordCounts, string BitsetIdLabel, string FirstValueSummary, string LastValueSummary, bool ValueChanged, int PresentCount) { }
        public string Annotation { get; init; }
        public string BitsetIdLabel { get; init; }
        public string BitsetWordCounts { get; init; }
        public string ElementCountRange { get; init; }
        public string Fingerprint { get; init; }
        public string FingerprintKey { get; init; }
        public string FirstValueSummary { get; init; }
        public string LastValueSummary { get; init; }
        public string Lifecycle { get; init; }
        public int PresentCount { get; init; }
        public bool ValueChanged { get; init; }
    }
    public sealed class PlayerSarTrackPointSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTrackPointSnapshot>
    {
        public PlayerSarTrackPointSnapshot(int Slot, ArcNET.Diagnostics.CharacterSarEntrySnapshot Sar) { }
        public ArcNET.Diagnostics.CharacterSarEntrySnapshot Sar { get; init; }
        public int Slot { get; init; }
    }
    public sealed class PlayerSarTrackSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTrackSnapshot>
    {
        public PlayerSarTrackSnapshot(string FingerprintKey, string Fingerprint, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackPointSnapshot> History) { }
        public string Fingerprint { get; init; }
        public string FingerprintKey { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTrackPointSnapshot> History { get; init; }
    }
    public sealed class PlayerSarTransitionAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionAnalysisSnapshot>
    {
        public PlayerSarTransitionAnalysisSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionSnapshot> Transitions) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionSnapshot> Transitions { get; init; }
    }
    public enum PlayerSarTransitionChangeKind
    {
        Added = 0,
        Removed = 1,
        Moved = 2,
        Changed = 3,
    }
    public sealed class PlayerSarTransitionChangeSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionChangeSnapshot>
    {
        public PlayerSarTransitionChangeSnapshot(ArcNET.Diagnostics.PlayerSarTransitionChangeKind Kind, string Label, string Fingerprint, string Annotation, int? BeforeElementCount, int? AfterElementCount, string? BeforeValueSummary, string? AfterValueSummary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementValueDiffSnapshot> ElementDiffs, System.Collections.Generic.IReadOnlyList<int> BeforeBitSlots, System.Collections.Generic.IReadOnlyList<int> AfterBitSlots, int PointerNoiseSuppressedCount) { }
        public System.Collections.Generic.IReadOnlyList<int> AfterBitSlots { get; init; }
        public int? AfterElementCount { get; init; }
        public string? AfterValueSummary { get; init; }
        public string Annotation { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BeforeBitSlots { get; init; }
        public int? BeforeElementCount { get; init; }
        public string? BeforeValueSummary { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarElementValueDiffSnapshot> ElementDiffs { get; init; }
        public string Fingerprint { get; init; }
        public ArcNET.Diagnostics.PlayerSarTransitionChangeKind Kind { get; init; }
        public string Label { get; init; }
        public int PointerNoiseSuppressedCount { get; init; }
    }
    public sealed class PlayerSarTransitionChangedEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionChangedEntrySnapshot>
    {
        public PlayerSarTransitionChangedEntrySnapshot(string Label, string Fingerprint, string Annotation, int? BeforeElementCount, int? AfterElementCount, string? BeforeValueSummary, string? AfterValueSummary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionFieldDiffSnapshot> FieldDiffs, System.Collections.Generic.IReadOnlyList<int> BeforeBitSlots, System.Collections.Generic.IReadOnlyList<int> AfterBitSlots, int PointerNoiseSuppressedCount) { }
        public System.Collections.Generic.IReadOnlyList<int> AfterBitSlots { get; init; }
        public int? AfterElementCount { get; init; }
        public string? AfterValueSummary { get; init; }
        public string Annotation { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> BeforeBitSlots { get; init; }
        public int? BeforeElementCount { get; init; }
        public string? BeforeValueSummary { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionFieldDiffSnapshot> FieldDiffs { get; init; }
        public string Fingerprint { get; init; }
        public string Label { get; init; }
        public int PointerNoiseSuppressedCount { get; init; }
    }
    public sealed class PlayerSarTransitionFieldDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionFieldDiffSnapshot>
    {
        public PlayerSarTransitionFieldDiffSnapshot(string FieldLabel, int BeforeValue, int AfterValue) { }
        public int AfterValue { get; init; }
        public int BeforeValue { get; init; }
        public string FieldLabel { get; init; }
    }
    public sealed class PlayerSarTransitionListEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot>
    {
        public PlayerSarTransitionListEntrySnapshot(string Label, string Fingerprint, string Annotation) { }
        public string Annotation { get; init; }
        public string Fingerprint { get; init; }
        public string Label { get; init; }
    }
    public sealed class PlayerSarTransitionReportEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionReportEntrySnapshot>
    {
        public PlayerSarTransitionReportEntrySnapshot(int FromSlot, int ToSlot, int FromLevel, int ToLevel, int FromRawBytesLength, int ToRawBytesLength, bool IsDiscontinuous, ArcNET.Diagnostics.PlayerSarTransitionSummarySnapshot Summary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Added, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Removed, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Moved, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionChangedEntrySnapshot> Changed) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Added { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionChangedEntrySnapshot> Changed { get; init; }
        public int FromLevel { get; init; }
        public int FromRawBytesLength { get; init; }
        public int FromSlot { get; init; }
        public bool IsDiscontinuous { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Moved { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionListEntrySnapshot> Removed { get; init; }
        public ArcNET.Diagnostics.PlayerSarTransitionSummarySnapshot Summary { get; init; }
        public int ToLevel { get; init; }
        public int ToRawBytesLength { get; init; }
        public int ToSlot { get; init; }
    }
    public sealed class PlayerSarTransitionReportSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionReportSnapshot>
    {
        public PlayerSarTransitionReportSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionReportEntrySnapshot> Transitions) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionReportEntrySnapshot> Transitions { get; init; }
    }
    public sealed class PlayerSarTransitionSnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionSnapshot>
    {
        public PlayerSarTransitionSnapshot(int FromSlot, int ToSlot, int FromLevel, int ToLevel, int FromRawBytesLength, int ToRawBytesLength, bool IsDiscontinuous, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionChangeSnapshot> Changes) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarTransitionChangeSnapshot> Changes { get; init; }
        public int FromLevel { get; init; }
        public int FromRawBytesLength { get; init; }
        public int FromSlot { get; init; }
        public bool IsDiscontinuous { get; init; }
        public int ToLevel { get; init; }
        public int ToRawBytesLength { get; init; }
        public int ToSlot { get; init; }
    }
    public sealed class PlayerSarTransitionSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.PlayerSarTransitionSummarySnapshot>
    {
        public PlayerSarTransitionSummarySnapshot(int AddedCount, int RemovedCount, int MovedCount, int ChangedCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintCountSnapshot> MovedFingerprints, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintCountSnapshot> ChangedFingerprints) { }
        public int AddedCount { get; init; }
        public int ChangedCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintCountSnapshot> ChangedFingerprints { get; init; }
        public int MovedCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerSarFingerprintCountSnapshot> MovedFingerprints { get; init; }
        public int RemovedCount { get; init; }
    }
    public static class ProtoDumper
    {
        public static string Dump(ArcNET.Formats.ProtoData proto) { }
        public static void Dump(ArcNET.Formats.ProtoData proto, System.IO.TextWriter writer) { }
    }
    public static class QuestLabelCatalogLoader
    {
        public static ArcNET.Diagnostics.QuestLabelCatalogSnapshot? TryLoadFromSaveDirectory(string saveDir) { }
    }
    public sealed class QuestLabelCatalogSnapshot : System.IEquatable<ArcNET.Diagnostics.QuestLabelCatalogSnapshot>
    {
        public QuestLabelCatalogSnapshot(string Source, System.Collections.Generic.IReadOnlyDictionary<int, string> Labels) { }
        public System.Collections.Generic.IReadOnlyDictionary<int, string> Labels { get; init; }
        public string Source { get; init; }
        public string? Resolve(int protoId) { }
    }
    public static class QuestStateFormatter
    {
        public static string Format(int state) { }
    }
    public readonly struct ResolvedCodeAnchor : System.IEquatable<ArcNET.Diagnostics.ResolvedCodeAnchor>
    {
        public ResolvedCodeAnchor(ArcNET.Diagnostics.CodeAnchor Anchor, uint Delta) { }
        public ArcNET.Diagnostics.CodeAnchor Anchor { get; init; }
        public uint Delta { get; init; }
        public string DisplayLabel { get; }
    }
    public static class RuntimeOffsets
    {
        public const int ActionPointsRva = 2081332;
        public const int AreaGetLastKnownAreaRva = 831744;
        public const int AreaIsKnownRva = 831312;
        public const int AreaResetLastKnownAreaRva = 831840;
        public const int AreaSetKnownRva = 831440;
        public const int BackgroundClearRva = 796240;
        public const int BackgroundDescriptionGetBodyRva = 795952;
        public const int BackgroundDescriptionGetNameRva = 796048;
        public const int BackgroundEducateFollowersRva = 797008;
        public const int BackgroundGetDescriptionRva = 795824;
        public const int BackgroundGetRva = 796304;
        public const int BackgroundSetRva = 796128;
        public const int BackgroundTextGetRva = 796368;
        public const int BlessAddRva = 803568;
        public const int BlessGetLogbookDataRva = 803328;
        public const int BlessRemoveRva = 803920;
        public const int CharacterSheetPropertyHookRva = 35527;
        public const int CharacterSheetSubstructureHookRva = 35910;
        public const int CombatTurnBasedWhosTurnSetRva = 749168;
        public const int CritterGiveXpRva = 389392;
        public const int CritterKillRva = 383232;
        public const int CurrentCharacterSheetIdRva = 2416656;
        public const int CurseAddRva = 802368;
        public const int CurseGetLogbookDataRva = 802128;
        public const int CurseRemoveRva = 802720;
        public const int EffectAddRva = 958320;
        public const int EffectCountEffectsOfTypeRva = 959648;
        public const int EffectRemoveAllCausedByRva = 959424;
        public const int EffectRemoveAllTypedRva = 958976;
        public const int EffectRemoveInternalRva = 959776;
        public const int EffectRemoveOneCausedByRva = 959200;
        public const int EffectRemoveOneTypedRva = 958720;
        public const int GamelibDrawGameRva = 18160;
        public const int GamelibDrawRva = 11856;
        public const int GamelibInvalidateRectRva = 11568;
        public const int ItemEquippedRva = 423856;
        public const int ItemFindKeyRingRva = 405920;
        public const int ItemForceRemoveRva = 424032;
        public const int ItemGetKeysRva = 405776;
        public const int ItemInsertRva = 419392;
        public const int ItemUnequippedRva = 425136;
        public const int LevelRecalcRva = 682432;
        public const int LightDrawRva = 885584;
        public const int LogbookAddInjuryRva = 1005200;
        public const int LogbookAddKillRva = 1004336;
        public const int LogbookGetKillsRva = 1004944;
        public const int MagicTechRunInfoPointerRva = 2651872;
        public const int MapOpenInGameRva = 64624;
        public const string ModuleName = "Arcanum.exe";
        public const int ObjArrayFieldInt32GetRva = 29392;
        public const int ObjArrayFieldInt32SetRva = 29504;
        public const int ObjArrayFieldInt64SetRva = 30128;
        public const int ObjArrayFieldLengthSetRva = 31264;
        public const int ObjArrayFieldObjSetRva = 30640;
        public const int ObjArrayFieldPcQuestSetRva = 31072;
        public const int ObjArrayFieldScriptGetRva = 30784;
        public const int ObjArrayFieldScriptSetRva = 30880;
        public const int ObjArrayFieldUInt32SetRva = 29920;
        public const int ObjFieldHandleSetRva = 28448;
        public const int ObjFieldInt32GetRva = 27808;
        public const int ObjFieldInt32SetRva = 27968;
        public const int ObjFieldInt64SetRva = 28176;
        public const int ObjHandleIndexShift = 29;
        public const ulong ObjHandleMarkerMask = 7ul;
        public const ulong ObjHandleMarkerValue = 2ul;
        public const ulong ObjHandleSequenceMask = 8388607ul;
        public const int ObjHandleSequenceShift = 3;
        public const int ObjPoolBucketSize = 8192;
        public const int ObjPoolBucketsRva = 2111164;
        public const int ObjPoolElementByteSizeRva = 2111204;
        public const int ObjPoolEntryHeaderByteSize = 4;
        public const int ObjectCreateRva = 248736;
        public const int ObjectDestroyRva = 248992;
        public const int ObjectDrawRva = 242576;
        public const int ObjectGetResistanceRva = 251600;
        public const int ObjectHoverDrawRva = 247440;
        public const int ObjectScriptExecuteRva = 268672;
        public const string ProcessName = "Arcanum";
        public const int PrototypeHandleByObjectTypeRva = 427376;
        public const int PrototypeHandleByProtoNumberRva = 427424;
        public const int QuestGetLogbookDataRva = 807648;
        public const int QuestGlobalStateGetRva = 807328;
        public const int QuestGlobalStateSetRva = 807360;
        public const int QuestStateGetRva = 806064;
        public const int QuestStateSetRva = 806176;
        public const int ReactionAdjRva = 789984;
        public const int ReputationAddRva = 793888;
        public const int ReputationGetLogbookDataRva = 793648;
        public const int ReputationRemoveRva = 794128;
        public const int RoofDrawRva = 233792;
        public const int RumorGetLogbookDataRva = 809536;
        public const int RumorKnownGetRva = 809168;
        public const int RumorKnownSetRva = 808928;
        public const int RumorQstateGetRva = 808528;
        public const int RumorQstateSetRva = 808592;
        public const int ScriptGlobalFlagGetRva = 281840;
        public const int ScriptGlobalFlagSetRva = 281888;
        public const int ScriptGlobalVarGetRva = 281728;
        public const int ScriptGlobalVarSetRva = 281744;
        public const int ScriptLocalCounterGetRva = 282576;
        public const int ScriptLocalCounterSetRva = 282656;
        public const int ScriptLocalFlagGetRva = 282384;
        public const int ScriptLocalFlagSetRva = 282464;
        public const int ScriptPcFlagGetRva = 282160;
        public const int ScriptPcFlagSetRva = 282256;
        public const int ScriptPcVarGetRva = 282032;
        public const int ScriptPcVarSetRva = 282096;
        public const int ScriptStoryStateGetRva = 282768;
        public const int ScriptStoryStateSetRva = 282784;
        public const int SpellAddEndExclusiveRva = 727376;
        public const int SpellAddRva = 726928;
        public const int SpellCollegeLevelGetRva = 727728;
        public const int SpellCollegeLevelSetRva = 727856;
        public const int SpellRemoveEndExclusiveRva = 727616;
        public const int SpellRemoveRva = 727472;
        public const int StatBaseGetRva = 722752;
        public const int StatBaseSetRva = 723328;
        public const int TechLearnSchematicRva = 721184;
        public const int TeleportDoRva = 865152;
        public const int TextBubbleDrawRva = 876304;
        public const int TextConversationDrawRva = 824816;
        public const int TextFloaterDrawRva = 873232;
        public const int TigVideoFlipRva = 1177840;
        public const int TigWindowBlitArtRva = 1170048;
        public const int TigWindowComposeDirtyRectRva = 1167440;
        public const int TigWindowCopyFromVBufferRva = 1171104;
        public const int TigWindowDisplayRva = 1167168;
        public const int TigWindowInvalidateRectRva = 1172528;
        public const int TileDrawRva = 878928;
        public const int TimeEventAddDelayRva = 374784;
        public const int TimeEventNotifyPcTeleportedRva = 378656;
        public const int UiShowInvenLootRva = 393936;
        public const int UiSpellAddRva = 394368;
        public const int UiSpellMaintainAddRva = 394400;
        public const int UiSpellMaintainEndRva = 394432;
        public const int UiStartDialogRva = 395744;
        public const int UpdateFollowerLevelRva = 683184;
        public const int WmapLoadWorldmapInfoRva = 1444512;
        public const int WmapRndEncounterCheckRva = 1413136;
        public const int WmapUiEncounterStartRva = 1443616;
    }
    public sealed class SaveBinaryDiffPreviewSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveBinaryDiffPreviewSnapshot>
    {
        public SaveBinaryDiffPreviewSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionPreviewSnapshot> Regions, int OmittedRegionCount) { }
        public int OmittedRegionCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionPreviewSnapshot> Regions { get; init; }
    }
    public sealed class SaveBinaryDiffRegionPreviewSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveBinaryDiffRegionPreviewSnapshot>
    {
        public SaveBinaryDiffRegionPreviewSnapshot(int Offset, int Length, int ChangedByteCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryHexRowSnapshot> Rows) { }
        public int ChangedByteCount { get; init; }
        public int Length { get; init; }
        public int Offset { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryHexRowSnapshot> Rows { get; init; }
    }
    public sealed class SaveBinaryDiffRegionSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveBinaryDiffRegionSnapshot>
    {
        public SaveBinaryDiffRegionSnapshot(int Offset, byte[] BeforeBytes, byte[] AfterBytes, int ChangedByteCount) { }
        public byte[] AfterBytes { get; init; }
        public byte[] BeforeBytes { get; init; }
        public int ChangedByteCount { get; init; }
        public int Offset { get; init; }
    }
    public static class SaveBinaryDiffService
    {
        public static ArcNET.Diagnostics.SaveBinaryDiffSetSnapshot CompareInnerFiles(System.Collections.Generic.IReadOnlyDictionary<string, byte[]> filesA, System.Collections.Generic.IReadOnlyDictionary<string, byte[]> filesB) { }
        public static ArcNET.Diagnostics.SaveBinaryDiffPreviewSnapshot CreatePreview(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionSnapshot> regions, int maxRegions = 20, int bytesPerRow = 8) { }
        public static System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionSnapshot> FindDiffRegions(byte[] beforeBytes, byte[] afterBytes, int contextBytes = 8, int mergeGap = 16) { }
    }
    public sealed class SaveBinaryDiffSetSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveBinaryDiffSetSnapshot>
    {
        public SaveBinaryDiffSetSnapshot(int TotalFiles, int ChangedFileCount, int IdenticalFileCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveInnerFileDiffSnapshot> Files) { }
        public int ChangedFileCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveInnerFileDiffSnapshot> Files { get; init; }
        public int IdenticalFileCount { get; init; }
        public int TotalFiles { get; init; }
    }
    public sealed class SaveBinaryHexRowSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveBinaryHexRowSnapshot>
    {
        public SaveBinaryHexRowSnapshot(int AbsoluteOffset, string BeforeHex, string BeforeAscii, string AfterHex, string AfterAscii) { }
        public int AbsoluteOffset { get; init; }
        public string AfterAscii { get; init; }
        public string AfterHex { get; init; }
        public string BeforeAscii { get; init; }
        public string BeforeHex { get; init; }
    }
    public sealed class SaveCharacterCatalogRecordSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCharacterCatalogRecordSnapshot>
    {
        public SaveCharacterCatalogRecordSnapshot(
                    string SourcePath,
                    int RecordIndex,
                    bool HasCompleteData,
                    string? Name,
                    int Level,
                    int ExperiencePoints,
                    int Alignment,
                    int RaceId,
                    string RaceName,
                    int GenderId,
                    string GenderName,
                    int MagickPoints,
                    int TechPoints,
                    int Gold,
                    int Bullets,
                    int PowerCells,
                    int HpDamage,
                    int FatigueDamage,
                    int RawBytesLength,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> NonZeroBasicSkills) { }
        public int Alignment { get; init; }
        public int Bullets { get; init; }
        public int ExperiencePoints { get; init; }
        public int FatigueDamage { get; init; }
        public int GenderId { get; init; }
        public string GenderName { get; init; }
        public int Gold { get; init; }
        public bool HasCompleteData { get; init; }
        public int HpDamage { get; init; }
        public int Level { get; init; }
        public int MagickPoints { get; init; }
        public string? Name { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> NonZeroBasicSkills { get; init; }
        public int PowerCells { get; init; }
        public int RaceId { get; init; }
        public string RaceName { get; init; }
        public int RawBytesLength { get; init; }
        public int RecordIndex { get; init; }
        public string SourcePath { get; init; }
        public int TechPoints { get; init; }
    }
    public static class SaveCharacterCatalogService
    {
        public static ArcNET.Diagnostics.SaveCharacterCatalogSnapshot Create(ArcNET.Editor.LoadedSave save) { }
    }
    public sealed class SaveCharacterCatalogSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCharacterCatalogSnapshot>
    {
        public SaveCharacterCatalogSnapshot(System.DateTimeOffset CapturedAt, string LeaderName, int LeaderLevel, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCharacterCatalogRecordSnapshot> Records) { }
        public System.DateTimeOffset CapturedAt { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCharacterCatalogRecordSnapshot> Records { get; init; }
    }
    public static class SaveCharacterSarDumpService
    {
        public static ArcNET.Diagnostics.SaveCharacterSarDumpSnapshot Create(ArcNET.Editor.LoadedSave save) { }
    }
    public sealed class SaveCharacterSarDumpSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCharacterSarDumpSnapshot>
    {
        public SaveCharacterSarDumpSnapshot(System.DateTimeOffset CapturedAt, string LeaderName, int LeaderLevel, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCharacterSarRecordSnapshot> Records) { }
        public System.DateTimeOffset CapturedAt { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCharacterSarRecordSnapshot> Records { get; init; }
    }
    public sealed class SaveCharacterSarRecordSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCharacterSarRecordSnapshot>
    {
        public SaveCharacterSarRecordSnapshot(
                    string SourcePath,
                    bool HasCompleteData,
                    string? Name,
                    int Level,
                    int RawBytesLength,
                    int Gold,
                    int Arrows,
                    int Bullets,
                    int PowerCells,
                    int TotalKills,
                    int PortraitIndex,
                    int MaxFollowers,
                    int HpDamage,
                    int FatigueDamage,
                    int QuestCount,
                    int? QuestDataRawBytesLength,
                    int? QuestBitsetWordCount,
                    System.Collections.Generic.IReadOnlyList<int> QuestSlotIds,
                    System.Collections.Generic.IReadOnlyList<int> Reputation,
                    System.Collections.Generic.IReadOnlyList<int> Blessings,
                    System.Collections.Generic.IReadOnlyList<int> Curses,
                    System.Collections.Generic.IReadOnlyList<int> Schematics,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarDumpEntrySnapshot> Sars) { }
        public int Arrows { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Blessings { get; init; }
        public int Bullets { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Curses { get; init; }
        public int FatigueDamage { get; init; }
        public int Gold { get; init; }
        public bool HasCompleteData { get; init; }
        public int HpDamage { get; init; }
        public int Level { get; init; }
        public int MaxFollowers { get; init; }
        public string? Name { get; init; }
        public int PortraitIndex { get; init; }
        public int PowerCells { get; init; }
        public int? QuestBitsetWordCount { get; init; }
        public int QuestCount { get; init; }
        public int? QuestDataRawBytesLength { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> QuestSlotIds { get; init; }
        public int RawBytesLength { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Reputation { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.CharacterSarDumpEntrySnapshot> Sars { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Schematics { get; init; }
        public string SourcePath { get; init; }
        public int TotalKills { get; init; }
    }
    public sealed class SaveCompactDifAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCompactDifAnalysisSnapshot>
    {
        public SaveCompactDifAnalysisSnapshot(int Magic, ArcNET.Diagnostics.SaveCompactDifVariant Variant, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCompactDifRecordSnapshot> Records, int? TrailingValue, bool MissingStartSentinel) { }
        public int Magic { get; init; }
        public bool MissingStartSentinel { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveCompactDifRecordSnapshot> Records { get; init; }
        public int? TrailingValue { get; init; }
        public ArcNET.Diagnostics.SaveCompactDifVariant Variant { get; init; }
    }
    public sealed class SaveCompactDifFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveCompactDifFileDetailSnapshot>
    {
        public SaveCompactDifFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveCompactDifAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveCompactDifAnalysisSnapshot Analysis { get; init; }
    }
    public sealed class SaveCompactDifRecordSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveCompactDifRecordSnapshot>
    {
        public SaveCompactDifRecordSnapshot(int Index, int? B, int DataLength) { }
        public int? B { get; init; }
        public int DataLength { get; init; }
        public int Index { get; init; }
    }
    public enum SaveCompactDifVariant
    {
        VariantAWithB = 0,
        VariantBWithoutB = 1,
        VariantCMagic18 = 2,
    }
    public sealed class SaveDestroyedObjectsAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveDestroyedObjectsAnalysisSnapshot>
    {
        public SaveDestroyedObjectsAnalysisSnapshot(int ByteLength, bool HasAlignmentWarning, System.Collections.Generic.IReadOnlyList<string> ObjectIds) { }
        public int ByteLength { get; init; }
        public bool HasAlignmentWarning { get; init; }
        public System.Collections.Generic.IReadOnlyList<string> ObjectIds { get; init; }
    }
    public sealed class SaveDestroyedObjectsFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveDestroyedObjectsFileDetailSnapshot>
    {
        public SaveDestroyedObjectsFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveDestroyedObjectsAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveDestroyedObjectsAnalysisSnapshot Analysis { get; init; }
    }
    public static class SaveDumper
    {
        public static string Dump(string saveDir) { }
        public static void Dump(string saveDir, System.IO.TextWriter writer) { }
        public static string Dump(string gsiPath, string tfaiPath, string tfafPath) { }
        public static void Dump(string gsiPath, string tfaiPath, string tfafPath, System.IO.TextWriter writer) { }
    }
    public sealed class SaveDynamicMobileAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveDynamicMobileAnalysisSnapshot>
    {
        public SaveDynamicMobileAnalysisSnapshot(int ObjectCount, int SkippedSentinelCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveDynamicMobileEntrySnapshot> Entries) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveDynamicMobileEntrySnapshot> Entries { get; init; }
        public int ObjectCount { get; init; }
        public int SkippedSentinelCount { get; init; }
    }
    public sealed class SaveDynamicMobileEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveDynamicMobileEntrySnapshot>
    {
        public SaveDynamicMobileEntrySnapshot(int Index, int Offset, ArcNET.Formats.MobData? Mob, string? ParseError) { }
        public int Index { get; init; }
        public ArcNET.Formats.MobData? Mob { get; init; }
        public int Offset { get; init; }
        public string? ParseError { get; init; }
    }
    public sealed class SaveDynamicMobileFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveDynamicMobileFileDetailSnapshot>
    {
        public SaveDynamicMobileFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveDynamicMobileAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveDynamicMobileAnalysisSnapshot Analysis { get; init; }
    }
    public static class SaveEmbeddedFileAnalysisService
    {
        public static ArcNET.Diagnostics.SaveCompactDifAnalysisSnapshot AnalyzeCompactDif(byte[] data) { }
        public static ArcNET.Diagnostics.SaveDestroyedObjectsAnalysisSnapshot AnalyzeDestroyedObjects(System.ReadOnlyMemory<byte> memory) { }
        public static ArcNET.Diagnostics.SaveDynamicMobileAnalysisSnapshot AnalyzeDynamicMobiles(System.ReadOnlyMemory<byte> memory) { }
        public static ArcNET.Diagnostics.SaveModifiedObjectsAnalysisSnapshot AnalyzeModifiedObjects(System.ReadOnlyMemory<byte> memory) { }
        public static ArcNET.Diagnostics.SaveTimeEventAnalysisSnapshot AnalyzeTimeEvents(System.ReadOnlyMemory<byte> memory) { }
        public static ArcNET.Diagnostics.SaveTownMapFogFileAnalysisSnapshot AnalyzeTownMapFog(System.ReadOnlyMemory<byte> memory) { }
        public static bool IsCompactDifFormat(byte[] data) { }
        public static ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot? TryAnalyze(string fileName, byte[] data) { }
    }
    public enum SaveEmbeddedFileDetailKind
    {
        DynamicMobiles = 0,
        CompactDif = 1,
        DestroyedObjects = 2,
        ModifiedObjects = 3,
        TimeEvents = 4,
        TownMapFog = 5,
    }
    public abstract class SaveEmbeddedFileDetailSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot>
    {
        protected SaveEmbeddedFileDetailSnapshot(ArcNET.Diagnostics.SaveEmbeddedFileDetailKind Kind, string FileName) { }
        public string FileName { get; init; }
        public ArcNET.Diagnostics.SaveEmbeddedFileDetailKind Kind { get; init; }
    }
    public sealed class SaveEmbeddedFileExtensionSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveEmbeddedFileExtensionSnapshot>
    {
        public SaveEmbeddedFileExtensionSnapshot(string Extension, string DisplayExtension, int Count, long TotalBytes) { }
        public int Count { get; init; }
        public string DisplayExtension { get; init; }
        public string Extension { get; init; }
        public long TotalBytes { get; init; }
    }
    public sealed class SaveExploredAreaCoverageSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveExploredAreaCoverageSnapshot>
    {
        public SaveExploredAreaCoverageSnapshot(string Area, int RevealedTiles, int TotalTiles, double CoveragePercent) { }
        public string Area { get; init; }
        public double CoveragePercent { get; init; }
        public int RevealedTiles { get; init; }
        public int TotalTiles { get; init; }
    }
    public sealed class SaveFileAuditRequest : System.IEquatable<ArcNET.Diagnostics.SaveFileAuditRequest>
    {
        public SaveFileAuditRequest(ArcNET.Editor.LoadedSave Save) { }
        public int CharacterSarLimit { get; init; }
        public int FieldLimit { get; init; }
        public int MobileMdyLimit { get; init; }
        public ArcNET.Editor.LoadedSave Save { get; init; }
        public int ValidationIssueLimit { get; init; }
    }
    public static class SaveFileAuditService
    {
        public static ArcNET.Diagnostics.SaveFileAuditSnapshot Create(ArcNET.Diagnostics.SaveFileAuditRequest request) { }
        public static ArcNET.Diagnostics.SaveFileAuditSnapshot Create(string gsiPath, string tfaiPath, string tfafPath) { }
    }
    public sealed class SaveFileAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveFileAuditSnapshot>
    {
        public SaveFileAuditSnapshot(System.DateTimeOffset CapturedAt, string LeaderName, int LeaderLevel, int MapId, ArcNET.Diagnostics.SaveTypedAssetSummarySnapshot Assets, ArcNET.Diagnostics.SaveValidationSummarySnapshot Validation, ArcNET.Diagnostics.SaveObjectFieldAuditSnapshot Objects, ArcNET.Diagnostics.PlayerCharacterAuditSnapshot? PlayerCharacter, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveParseErrorSnapshot> ParseErrors, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveValidationIssueSnapshot> ValidationIssues, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobileMdyAuditSnapshot> MobileMdys, int ValidationIssueLimit, int MobileMdyLimit) { }
        public ArcNET.Diagnostics.SaveTypedAssetSummarySnapshot Assets { get; init; }
        public System.DateTimeOffset CapturedAt { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public int MapId { get; init; }
        public int MobileMdyLimit { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.MobileMdyAuditSnapshot> MobileMdys { get; init; }
        public ArcNET.Diagnostics.SaveObjectFieldAuditSnapshot Objects { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveParseErrorSnapshot> ParseErrors { get; init; }
        public ArcNET.Diagnostics.PlayerCharacterAuditSnapshot? PlayerCharacter { get; init; }
        public ArcNET.Diagnostics.SaveValidationSummarySnapshot Validation { get; init; }
        public int ValidationIssueLimit { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveValidationIssueSnapshot> ValidationIssues { get; init; }
    }
    public sealed class SaveGameClockSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGameClockSnapshot>
    {
        public SaveGameClockSnapshot(int DayNumber, int Hours, int Minutes, int Seconds) { }
        public int DayNumber { get; init; }
        public int Hours { get; init; }
        public int Minutes { get; init; }
        public int Seconds { get; init; }
    }
    public readonly struct SaveGlobalAlignedQuadDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalAlignedQuadDiffSnapshot>
    {
        public SaveGlobalAlignedQuadDiffSnapshot(int BeforeSectionCount, int AfterSectionCount, int BeforeZeroSectionCount, int AfterZeroSectionCount, int BeforeLongestZeroSectionStart, int BeforeLongestZeroSectionLength, int AfterLongestZeroSectionStart, int AfterLongestZeroSectionLength, ArcNET.Diagnostics.SaveGlobalFrontMatterDiffSnapshot? FrontMatter, ArcNET.Diagnostics.SaveGlobalTailDiffSnapshot? Tail) { }
        public int AfterLongestZeroSectionLength { get; init; }
        public int AfterLongestZeroSectionStart { get; init; }
        public int AfterSectionCount { get; init; }
        public int AfterZeroSectionCount { get; init; }
        public int BeforeLongestZeroSectionLength { get; init; }
        public int BeforeLongestZeroSectionStart { get; init; }
        public int BeforeSectionCount { get; init; }
        public int BeforeZeroSectionCount { get; init; }
        public ArcNET.Diagnostics.SaveGlobalFrontMatterDiffSnapshot? FrontMatter { get; init; }
        public ArcNET.Diagnostics.SaveGlobalTailDiffSnapshot? Tail { get; init; }
    }
    public static class SaveGlobalAnalysisService
    {
        public static System.Collections.Generic.IReadOnlyList<string> KnownFileNames { get; }
        public static ArcNET.Diagnostics.SaveGlobalFileSnapshot Analyze(string fileName, byte[] bytes, ArcNET.Formats.DataSavFile? dataSav, ArcNET.Formats.Data2SavFile? data2Sav) { }
        public static ArcNET.Diagnostics.SaveGlobalSlotSnapshot CreateSlotSnapshot(int slot, string slotStem, ArcNET.Editor.LoadedSave save) { }
        public static int GetData2PrefixIntCount(in ArcNET.Diagnostics.SaveIdPairTableSnapshot saveIdPairs) { }
        public static int GetData2SuffixIntCount(int totalInts, in ArcNET.Diagnostics.SaveIdPairTableSnapshot saveIdPairs) { }
    }
    public sealed class SaveGlobalAsciiCandidateSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalAsciiCandidateSnapshot>
    {
        public SaveGlobalAsciiCandidateSnapshot(int Offset, int Length, string Text) { }
        public int Length { get; init; }
        public int Offset { get; init; }
        public string Text { get; init; }
    }
    public readonly struct SaveGlobalChangedIntSampleSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalChangedIntSampleSnapshot>
    {
        public SaveGlobalChangedIntSampleSnapshot(int Index, int BeforeValue, int AfterValue) { }
        public int AfterValue { get; init; }
        public int BeforeValue { get; init; }
        public int Index { get; init; }
    }
    public readonly struct SaveGlobalContiguousIntWindow : System.IEquatable<ArcNET.Diagnostics.SaveGlobalContiguousIntWindow>
    {
        public SaveGlobalContiguousIntWindow(int StartInt, int RemovedInts, int AddedInts, int CommonSuffixInts) { }
        public int AddedInts { get; init; }
        public int CommonSuffixInts { get; init; }
        public int RemovedInts { get; init; }
        public int StartInt { get; init; }
    }
    public readonly struct SaveGlobalData2RegionFamilySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalData2RegionFamilySnapshot>
    {
        public SaveGlobalData2RegionFamilySnapshot(System.Collections.Generic.IReadOnlyList<int> Slots, int IntCount, string Sequence, string Preview) { }
        public int IntCount { get; init; }
        public string Preview { get; init; }
        public string Sequence { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Slots { get; init; }
    }
    public sealed class SaveGlobalData2RegionPreviewSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalData2RegionPreviewSnapshot>
    {
        public SaveGlobalData2RegionPreviewSnapshot(int IntCount, System.Collections.Generic.IReadOnlyList<int> HeadValues, System.Collections.Generic.IReadOnlyList<int> TailValues) { }
        public System.Collections.Generic.IReadOnlyList<int> HeadValues { get; init; }
        public int IntCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> TailValues { get; init; }
    }
    public static class SaveGlobalDiffService
    {
        public static ArcNET.Diagnostics.SaveGlobalFileDiffSnapshot Compare(in ArcNET.Diagnostics.SaveGlobalFileSnapshot before, in ArcNET.Diagnostics.SaveGlobalFileSnapshot after, int maxChangedSamples = 6, int maxWindowInts = 16, int minWindowSuffixInts = 16, int maxSaveIdPairPreview = 10) { }
    }
    public static class SaveGlobalDumpService
    {
        public static ArcNET.Diagnostics.SaveGlobalFileDumpSnapshot Create(in ArcNET.Diagnostics.SaveGlobalFileSnapshot file, int maxQuadPreviewRows = 6, int maxHexRows = 16, int maxPreviewInts = 64, int firstNonZeroEntries = 20, int lastNonZeroEntries = 10, int maxAsciiPreviewStrings = 10, int maxSaveIdPairPreview = 16, int maxData2RegionPreviewInts = 8) { }
    }
    public readonly struct SaveGlobalFileDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalFileDiffSnapshot>
    {
        public SaveGlobalFileDiffSnapshot(
                    bool IsIdentical,
                    int BeforeByteLength,
                    int AfterByteLength,
                    int BeforeHeader0,
                    int BeforeHeader1,
                    int AfterHeader0,
                    int AfterHeader1,
                    int BeforeNonZeroCount,
                    int AfterNonZeroCount,
                    int ChangedInts,
                    int PrefixInts,
                    int AddedInts,
                    int RemovedInts,
                    int ChangedTailBytes,
                    int BeforeTrailingBytes,
                    int AfterTrailingBytes,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalChangedIntSampleSnapshot> ChangedSamples,
                    ArcNET.Diagnostics.SaveGlobalContiguousIntWindow? Window,
                    ArcNET.Diagnostics.SaveGlobalAlignedQuadDiffSnapshot? AlignedQuad,
                    ArcNET.Diagnostics.SaveGlobalSaveIdPairDiffSnapshot? SaveIdPairs) { }
        public int AddedInts { get; init; }
        public int AfterByteLength { get; init; }
        public int AfterHeader0 { get; init; }
        public int AfterHeader1 { get; init; }
        public int AfterNonZeroCount { get; init; }
        public int AfterTrailingBytes { get; init; }
        public ArcNET.Diagnostics.SaveGlobalAlignedQuadDiffSnapshot? AlignedQuad { get; init; }
        public int BeforeByteLength { get; init; }
        public int BeforeHeader0 { get; init; }
        public int BeforeHeader1 { get; init; }
        public int BeforeNonZeroCount { get; init; }
        public int BeforeTrailingBytes { get; init; }
        public int ChangedInts { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalChangedIntSampleSnapshot> ChangedSamples { get; init; }
        public int ChangedTailBytes { get; init; }
        public bool IsIdentical { get; init; }
        public int PrefixInts { get; init; }
        public int RemovedInts { get; init; }
        public ArcNET.Diagnostics.SaveGlobalSaveIdPairDiffSnapshot? SaveIdPairs { get; init; }
        public ArcNET.Diagnostics.SaveGlobalContiguousIntWindow? Window { get; init; }
    }
    public sealed class SaveGlobalFileDumpSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalFileDumpSnapshot>
    {
        public SaveGlobalFileDumpSnapshot(int Header0, int Header1, int TotalInts, int TrailingBytes, int BeefCafeCount, int MinusOneCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalQuadPreviewRowSnapshot> QuadPreviewRows, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalHexPreviewRowSnapshot> HexRows, int HexOmittedBytes, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIntPreviewRowSnapshot> IntRows, ArcNET.Diagnostics.SaveGlobalNonZeroSummarySnapshot NonZeroSummary, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalAsciiCandidateSnapshot> AsciiCandidates, ArcNET.Diagnostics.SaveGlobalSaveIdPairDetailsSnapshot? SaveIdPairDetails) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalAsciiCandidateSnapshot> AsciiCandidates { get; init; }
        public int BeefCafeCount { get; init; }
        public int Header0 { get; init; }
        public int Header1 { get; init; }
        public int HexOmittedBytes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalHexPreviewRowSnapshot> HexRows { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIntPreviewRowSnapshot> IntRows { get; init; }
        public int MinusOneCount { get; init; }
        public ArcNET.Diagnostics.SaveGlobalNonZeroSummarySnapshot NonZeroSummary { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalQuadPreviewRowSnapshot> QuadPreviewRows { get; init; }
        public ArcNET.Diagnostics.SaveGlobalSaveIdPairDetailsSnapshot? SaveIdPairDetails { get; init; }
        public int TotalInts { get; init; }
        public int TrailingBytes { get; init; }
    }
    public readonly struct SaveGlobalFileSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalFileSnapshot>
    {
        public SaveGlobalFileSnapshot(byte[] Bytes, int Header0, int Header1, int TotalInts, int TrailingBytes, int NonZeroCount, int BeefCafeCount, int MinusOneCount, ArcNET.Diagnostics.SaveIdPairTableSnapshot? SaveIdPairs, ArcNET.Diagnostics.AlignedQuadSummary? QuadSummary, ArcNET.Formats.Data2SavFile? Data2Sav) { }
        public int BeefCafeCount { get; init; }
        public byte[] Bytes { get; init; }
        public ArcNET.Formats.Data2SavFile? Data2Sav { get; init; }
        public int Header0 { get; init; }
        public int Header1 { get; init; }
        public int MinusOneCount { get; init; }
        public int NonZeroCount { get; init; }
        public ArcNET.Diagnostics.AlignedQuadSummary? QuadSummary { get; init; }
        public ArcNET.Diagnostics.SaveIdPairTableSnapshot? SaveIdPairs { get; init; }
        public int TotalInts { get; init; }
        public int TrailingBytes { get; init; }
    }
    public readonly struct SaveGlobalFrontMatterDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalFrontMatterDiffSnapshot>
    {
        public SaveGlobalFrontMatterDiffSnapshot(int BeforeRowCount, int AfterRowCount, int BeforeSectionCount, int AfterSectionCount, int SamePrefixCount, ArcNET.Diagnostics.AlignedQuadRunSummary? BeforeNextRun, ArcNET.Diagnostics.AlignedQuadRunSummary? AfterNextRun) { }
        public ArcNET.Diagnostics.AlignedQuadRunSummary? AfterNextRun { get; init; }
        public int AfterRowCount { get; init; }
        public int AfterSectionCount { get; init; }
        public ArcNET.Diagnostics.AlignedQuadRunSummary? BeforeNextRun { get; init; }
        public int BeforeRowCount { get; init; }
        public int BeforeSectionCount { get; init; }
        public int SamePrefixCount { get; init; }
    }
    public readonly struct SaveGlobalFrontMatterFamilySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalFrontMatterFamilySnapshot>
    {
        public SaveGlobalFrontMatterFamilySnapshot(System.Collections.Generic.IReadOnlyList<int> Slots, int RowCount, int SectionCount, string Sequence) { }
        public int RowCount { get; init; }
        public int SectionCount { get; init; }
        public string Sequence { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Slots { get; init; }
    }
    public readonly struct SaveGlobalHexPreviewRowSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalHexPreviewRowSnapshot>
    {
        public SaveGlobalHexPreviewRowSnapshot(int Offset, byte[] Bytes) { }
        public byte[] Bytes { get; init; }
        public int Offset { get; init; }
    }
    public readonly struct SaveGlobalHotIndexHitSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalHotIndexHitSnapshot>
    {
        public SaveGlobalHotIndexHitSnapshot(int Index, int Hits) { }
        public int Hits { get; init; }
        public int Index { get; init; }
    }
    public readonly struct SaveGlobalIndexedIntSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot>
    {
        public SaveGlobalIndexedIntSnapshot(int Index, int Value) { }
        public int Index { get; init; }
        public int Value { get; init; }
    }
    public static class SaveGlobalInt32Reader
    {
        public static int CountValue(byte[] bytes, int totalInts, int match) { }
        public static int ReadInt32(byte[] bytes, int intIndex) { }
    }
    public readonly struct SaveGlobalIntPreviewRowSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalIntPreviewRowSnapshot>
    {
        public SaveGlobalIntPreviewRowSnapshot(int StartIndex, System.Collections.Generic.IReadOnlyList<int> Values) { }
        public int StartIndex { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Values { get; init; }
    }
    public sealed class SaveGlobalNonZeroSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalNonZeroSummarySnapshot>
    {
        public SaveGlobalNonZeroSummarySnapshot(int Count, int TotalInts, double Density, bool IsDense, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> Entries, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> FirstEntries, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> LastEntries) { }
        public int Count { get; init; }
        public double Density { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> Entries { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> FirstEntries { get; init; }
        public bool IsDense { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalIndexedIntSnapshot> LastEntries { get; init; }
        public int TotalInts { get; init; }
    }
    public readonly struct SaveGlobalQuadPreviewRowSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalQuadPreviewRowSnapshot>
    {
        public SaveGlobalQuadPreviewRowSnapshot(int RowIndex, int A, int B, int C, int D) { }
        public int A { get; init; }
        public int B { get; init; }
        public int C { get; init; }
        public int D { get; init; }
        public int RowIndex { get; init; }
    }
    public static class SaveGlobalRangeAnalysisService
    {
        public static ArcNET.Diagnostics.SaveGlobalRangeAnalysisSnapshot Analyze(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalSlotSnapshot> snapshots, System.Collections.Generic.IReadOnlyList<string>? fileNames = null) { }
        public static int CountChangedIntRegion(byte[] beforeBytes, int beforeStartInt, int beforeCount, byte[] afterBytes, int afterStartInt, int afterCount) { }
        public static ArcNET.Diagnostics.SaveGlobalContiguousIntWindow? TryDetectContiguousIntWindow(in ArcNET.Diagnostics.SaveGlobalFileSnapshot before, in ArcNET.Diagnostics.SaveGlobalFileSnapshot after, int prefixInts, int maxWindowInts = 16, int minWindowSuffixInts = 16) { }
    }
    public readonly struct SaveGlobalRangeAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalRangeAnalysisSnapshot>
    {
        public SaveGlobalRangeAnalysisSnapshot(System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalHotIndexHitSnapshot>> HotIndices, System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalWindowPatternHitSnapshot>> WindowPatterns, System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalWindowTraceHitSnapshot>> WindowTraces, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalFrontMatterFamilySnapshot> FrontMatterFamilies, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalTailFamilySnapshot> TailFamilies, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalData2RegionFamilySnapshot> PrefixFamilies, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalData2RegionFamilySnapshot> SuffixFamilies) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalFrontMatterFamilySnapshot> FrontMatterFamilies { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalHotIndexHitSnapshot>> HotIndices { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalData2RegionFamilySnapshot> PrefixFamilies { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalData2RegionFamilySnapshot> SuffixFamilies { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalTailFamilySnapshot> TailFamilies { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalWindowPatternHitSnapshot>> WindowPatterns { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalWindowTraceHitSnapshot>> WindowTraces { get; init; }
    }
    public sealed class SaveGlobalSaveIdPairDetailsSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalSaveIdPairDetailsSnapshot>
    {
        public SaveGlobalSaveIdPairDetailsSnapshot(int StartInt, int EndInt, int PairCount, int FirstId, int LastId, int NonZeroPairs, int MaxValue, ArcNET.Diagnostics.SaveGlobalData2RegionPreviewSnapshot? PrefixPreview, ArcNET.Diagnostics.SaveGlobalData2RegionPreviewSnapshot? SuffixPreview, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueSnapshot> NonZeroPairPreview, int OmittedNonZeroPairCount) { }
        public int EndInt { get; init; }
        public int FirstId { get; init; }
        public int LastId { get; init; }
        public int MaxValue { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueSnapshot> NonZeroPairPreview { get; init; }
        public int NonZeroPairs { get; init; }
        public int OmittedNonZeroPairCount { get; init; }
        public int PairCount { get; init; }
        public ArcNET.Diagnostics.SaveGlobalData2RegionPreviewSnapshot? PrefixPreview { get; init; }
        public int StartInt { get; init; }
        public ArcNET.Diagnostics.SaveGlobalData2RegionPreviewSnapshot? SuffixPreview { get; init; }
    }
    public readonly struct SaveGlobalSaveIdPairDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalSaveIdPairDiffSnapshot>
    {
        public SaveGlobalSaveIdPairDiffSnapshot(int BeforeStartInt, int AfterStartInt, int BeforePairCount, int AfterPairCount, int BeforeNonZeroPairs, int AfterNonZeroPairs, int BeforePrefixIntCount, int AfterPrefixIntCount, int PrefixChangedInts, int BeforeSuffixIntCount, int AfterSuffixIntCount, int SuffixChangedInts, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueDiffSnapshot> ChangedPairs, int TotalChangedPairs) { }
        public int AfterNonZeroPairs { get; init; }
        public int AfterPairCount { get; init; }
        public int AfterPrefixIntCount { get; init; }
        public int AfterStartInt { get; init; }
        public int AfterSuffixIntCount { get; init; }
        public int BeforeNonZeroPairs { get; init; }
        public int BeforePairCount { get; init; }
        public int BeforePrefixIntCount { get; init; }
        public int BeforeStartInt { get; init; }
        public int BeforeSuffixIntCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueDiffSnapshot> ChangedPairs { get; init; }
        public int PrefixChangedInts { get; init; }
        public int SuffixChangedInts { get; init; }
        public int TotalChangedPairs { get; init; }
    }
    public readonly struct SaveGlobalSaveIdPairValueDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueDiffSnapshot>
    {
        public SaveGlobalSaveIdPairValueDiffSnapshot(int Id, int? BeforeValue, int? AfterValue) { }
        public int? AfterValue { get; init; }
        public int? BeforeValue { get; init; }
        public int Id { get; init; }
    }
    public readonly struct SaveGlobalSaveIdPairValueSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalSaveIdPairValueSnapshot>
    {
        public SaveGlobalSaveIdPairValueSnapshot(int Id, int Value) { }
        public int Id { get; init; }
        public int Value { get; init; }
    }
    public readonly struct SaveGlobalSlotSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalSlotSnapshot>
    {
        public SaveGlobalSlotSnapshot(int Slot, string SlotStem, string LeaderName, int LeaderLevel, System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Diagnostics.SaveGlobalFileSnapshot> Files, ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? Player, ArcNET.Diagnostics.SaveTownMapFogSnapshot TownMapFogs) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Diagnostics.SaveGlobalFileSnapshot> Files { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? Player { get; init; }
        public int Slot { get; init; }
        public string SlotStem { get; init; }
        public ArcNET.Diagnostics.SaveTownMapFogSnapshot TownMapFogs { get; init; }
    }
    public readonly struct SaveGlobalTailDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalTailDiffSnapshot>
    {
        public SaveGlobalTailDiffSnapshot(int BeforeStartRow, int AfterStartRow, int BeforeRowCount, int AfterRowCount, int BeforeSectionCount, int AfterSectionCount, int SamePrefixCount, ArcNET.Diagnostics.AlignedQuadRunSummary? BeforeNextRun, ArcNET.Diagnostics.AlignedQuadRunSummary? AfterNextRun) { }
        public ArcNET.Diagnostics.AlignedQuadRunSummary? AfterNextRun { get; init; }
        public int AfterRowCount { get; init; }
        public int AfterSectionCount { get; init; }
        public int AfterStartRow { get; init; }
        public ArcNET.Diagnostics.AlignedQuadRunSummary? BeforeNextRun { get; init; }
        public int BeforeRowCount { get; init; }
        public int BeforeSectionCount { get; init; }
        public int BeforeStartRow { get; init; }
        public int SamePrefixCount { get; init; }
    }
    public readonly struct SaveGlobalTailFamilySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalTailFamilySnapshot>
    {
        public SaveGlobalTailFamilySnapshot(System.Collections.Generic.IReadOnlyList<int> Slots, int RowCount, int SectionCount, string Sequence) { }
        public int RowCount { get; init; }
        public int SectionCount { get; init; }
        public string Sequence { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Slots { get; init; }
    }
    public readonly struct SaveGlobalWindowPatternHitSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalWindowPatternHitSnapshot>
    {
        public SaveGlobalWindowPatternHitSnapshot(int StartInt, int RemovedInts, int AddedInts, int Hits) { }
        public int AddedInts { get; init; }
        public int Hits { get; init; }
        public int RemovedInts { get; init; }
        public int StartInt { get; init; }
    }
    public readonly struct SaveGlobalWindowTraceHitSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGlobalWindowTraceHitSnapshot>
    {
        public SaveGlobalWindowTraceHitSnapshot(int StartInt, int Width, int Hits) { }
        public int Hits { get; init; }
        public int StartInt { get; init; }
        public int Width { get; init; }
    }
    public sealed class SaveGoldItemEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGoldItemEntrySnapshot>
    {
        public SaveGoldItemEntrySnapshot(byte[] ObjectIdBytes, int Quantity, bool HasParent, bool FoundInPlayerCharacter, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemPropertySnapshot> PositiveInt32Properties) { }
        public bool FoundInPlayerCharacter { get; init; }
        public bool HasParent { get; init; }
        public byte[] ObjectIdBytes { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemPropertySnapshot> PositiveInt32Properties { get; init; }
        public int Quantity { get; init; }
    }
    public sealed class SaveGoldItemFileSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGoldItemFileSnapshot>
    {
        public SaveGoldItemFileSnapshot(string Path, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemEntrySnapshot> Items) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemEntrySnapshot> Items { get; init; }
        public string Path { get; init; }
    }
    public static class SaveGoldItemInspectionService
    {
        public static ArcNET.Diagnostics.SaveGoldItemInspectionSnapshot Create(ArcNET.Editor.LoadedSave save) { }
    }
    public sealed class SaveGoldItemInspectionSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGoldItemInspectionSnapshot>
    {
        public SaveGoldItemInspectionSnapshot(string LeaderName, int LeaderLevel, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemFileSnapshot> Files) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveGoldItemFileSnapshot> Files { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
    }
    public sealed class SaveGoldItemPropertySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveGoldItemPropertySnapshot>
    {
        public SaveGoldItemPropertySnapshot(int Field, int Value) { }
        public int Field { get; init; }
        public int Value { get; init; }
    }
    public readonly struct SaveIdPairTableSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveIdPairTableSnapshot>
    {
        public SaveIdPairTableSnapshot(int StartInt, int PairCount, int EndInt, int FirstId, int LastId, int NonZeroPairs, int MaxValue, System.Collections.Generic.IReadOnlyDictionary<int, int> Values) { }
        public int EndInt { get; init; }
        public int FirstId { get; init; }
        public int LastId { get; init; }
        public int MaxValue { get; init; }
        public int NonZeroPairs { get; init; }
        public int PairCount { get; init; }
        public int StartInt { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<int, int> Values { get; init; }
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
    public sealed class SaveInnerFileDiffSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveInnerFileDiffSnapshot>
    {
        public SaveInnerFileDiffSnapshot(string Path, bool OnlyInA, bool OnlyInB, int SizeA, int SizeB, byte[] BytesA, byte[] BytesB, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionSnapshot> Regions, int ChangedByteCount) { }
        public byte[] BytesA { get; init; }
        public byte[] BytesB { get; init; }
        public int ChangedByteCount { get; init; }
        public bool OnlyInA { get; init; }
        public bool OnlyInB { get; init; }
        public string Path { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveBinaryDiffRegionSnapshot> Regions { get; init; }
        public int SizeA { get; init; }
        public int SizeB { get; init; }
    }
    public sealed class SaveMapWorldStateSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveMapWorldStateSnapshot>
    {
        public SaveMapWorldStateSnapshot(string MapName, int DestroyedObjectCount, int ModifiedObjectCount, int DynamicMobileCount, int ObjectDiffCount) { }
        public int DestroyedObjectCount { get; init; }
        public int DynamicMobileCount { get; init; }
        public string MapName { get; init; }
        public int ModifiedObjectCount { get; init; }
        public int ObjectDiffCount { get; init; }
    }
    public sealed class SaveModifiedObjectEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveModifiedObjectEntrySnapshot>
    {
        public SaveModifiedObjectEntrySnapshot(int Index, string FileObjectId, ArcNET.Formats.MobData? Mob, string? ParseError, string? Warning) { }
        public string FileObjectId { get; init; }
        public int Index { get; init; }
        public ArcNET.Formats.MobData? Mob { get; init; }
        public string? ParseError { get; init; }
        public string? Warning { get; init; }
    }
    public sealed class SaveModifiedObjectsAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveModifiedObjectsAnalysisSnapshot>
    {
        public SaveModifiedObjectsAnalysisSnapshot(System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveModifiedObjectEntrySnapshot> Entries, string? TerminalWarning) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveModifiedObjectEntrySnapshot> Entries { get; init; }
        public string? TerminalWarning { get; init; }
    }
    public sealed class SaveModifiedObjectsFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveModifiedObjectsFileDetailSnapshot>
    {
        public SaveModifiedObjectsFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveModifiedObjectsAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveModifiedObjectsAnalysisSnapshot Analysis { get; init; }
    }
    public sealed class SaveObjectFieldAuditSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveObjectFieldAuditSnapshot>
    {
        public SaveObjectFieldAuditSnapshot(int ObjectCount, int MobileFileCount, int MobileMdyMobCount, int DistinctFieldCount, int TotalPropertyCount, int ParseNoteCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldUsageSnapshot> TopFields, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldUsageSnapshot> LinkFields) { }
        public int DistinctFieldCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldUsageSnapshot> LinkFields { get; init; }
        public int MobileFileCount { get; init; }
        public int MobileMdyMobCount { get; init; }
        public int ObjectCount { get; init; }
        public int ParseNoteCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.ObjectFieldUsageSnapshot> TopFields { get; init; }
        public int TotalPropertyCount { get; init; }
    }
    public sealed class SaveParseErrorSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveParseErrorSnapshot>
    {
        public SaveParseErrorSnapshot(string FilePath, string Message) { }
        public string FilePath { get; init; }
        public string Message { get; init; }
    }
    public sealed class SavePlayerCharacterResolution : System.IEquatable<ArcNET.Diagnostics.SavePlayerCharacterResolution>
    {
        public SavePlayerCharacterResolution(string Path, ArcNET.Formats.CharacterMdyRecord Record) { }
        public string Path { get; init; }
        public ArcNET.Formats.CharacterMdyRecord Record { get; init; }
    }
    public static class SavePlayerCharacterResolver
    {
        public static ArcNET.Diagnostics.SavePlayerCharacterResolution? Resolve(ArcNET.Editor.LoadedSave save) { }
    }
    public static class SavePlayerCharacterSummaryService
    {
        public static ArcNET.Diagnostics.SavePlayerCharacterSummarySnapshot? Create(ArcNET.Editor.LoadedSave save) { }
    }
    public sealed class SavePlayerCharacterSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SavePlayerCharacterSummarySnapshot>
    {
        public SavePlayerCharacterSummarySnapshot(
                    System.DateTimeOffset CapturedAt,
                    string LeaderName,
                    int LeaderLevel,
                    string Path,
                    bool HasCompleteData,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> PrimaryAttributes,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> DerivedStats,
                    ArcNET.Diagnostics.PlayerCharacterProgressionSnapshot Progression,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BasicSkills,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> TechSkills,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> SpellColleges,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> TechDisciplines,
                    ArcNET.Diagnostics.SaveQuestLogSummarySnapshot QuestLog,
                    System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation,
                    System.Collections.Generic.IReadOnlyList<int> Blessings,
                    System.Collections.Generic.IReadOnlyList<int> Curses,
                    System.Collections.Generic.IReadOnlyList<int> Schematics,
                    ArcNET.Diagnostics.SaveRumorSummarySnapshot Rumors) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> BasicSkills { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Blessings { get; init; }
        public System.DateTimeOffset CapturedAt { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Curses { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> DerivedStats { get; init; }
        public bool HasCompleteData { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public string Path { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> PrimaryAttributes { get; init; }
        public ArcNET.Diagnostics.PlayerCharacterProgressionSnapshot Progression { get; init; }
        public ArcNET.Diagnostics.SaveQuestLogSummarySnapshot QuestLog { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerReputationEntrySnapshot> Reputation { get; init; }
        public ArcNET.Diagnostics.SaveRumorSummarySnapshot Rumors { get; init; }
        public System.Collections.Generic.IReadOnlyList<int> Schematics { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> SpellColleges { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> TechDisciplines { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.PlayerIndexedValueSnapshot> TechSkills { get; init; }
    }
    public static class SavePlayerProgressionHistoryService
    {
        public const string TrackedFieldsSummary = "lv, XP, align, fate, magicPts, techPts, gold, quests, quest-state deltas, rumors," +
            " blessings, curses, schematics, hp_dmg, fat_dmg, bullets, powerCells, reputation" +
            ", SpellTech ranks, base stats, basic skills";
        public static ArcNET.Diagnostics.PlayerProgressionHistorySnapshot Create(string saveDir, int firstSlot, int lastSlot, ArcNET.Diagnostics.QuestLabelCatalogSnapshot? questCatalog = null) { }
    }
    public static class SavePlayerQuestBookService
    {
        public static ArcNET.Diagnostics.PlayerQuestBookSnapshot Create(ArcNET.Editor.LoadedSave save, ArcNET.Diagnostics.QuestLabelCatalogSnapshot? questCatalog = null) { }
    }
    public sealed class SaveQuestLogSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveQuestLogSummarySnapshot>
    {
        public SaveQuestLogSummarySnapshot(int Count, int? RawBytesLength, int? BitsetWordCount) { }
        public int? BitsetWordCount { get; init; }
        public int Count { get; init; }
        public int? RawBytesLength { get; init; }
    }
    public sealed class SaveRumorSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveRumorSummarySnapshot>
    {
        public SaveRumorSummarySnapshot(int Count, int? RawBytesLength) { }
        public int Count { get; init; }
        public int? RawBytesLength { get; init; }
    }
    public static class SaveSlotLoadService
    {
        public static ArcNET.Diagnostics.SaveSlotLoadSnapshot Load(string saveDir, int slot) { }
        public static ArcNET.Diagnostics.SaveSlotLoadSnapshot Load(string saveDir, string slotText) { }
        public static ArcNET.Editor.LoadedSave LoadFiles(string gsiPath, string tfaiPath, string tfafPath) { }
    }
    public sealed class SaveSlotLoadSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveSlotLoadSnapshot>
    {
        public SaveSlotLoadSnapshot(int Slot, string SlotStem, ArcNET.Editor.LoadedSave Save) { }
        public ArcNET.Editor.LoadedSave Save { get; init; }
        public int Slot { get; init; }
        public string SlotStem { get; init; }
    }
    public static class SaveStructureAnalysisService
    {
        public static ArcNET.Diagnostics.SaveStructureAnalysisSnapshot Create(ArcNET.Editor.LoadedSave save) { }
        public static ArcNET.Diagnostics.SaveStructureAnalysisSnapshot Create(string gsiPath, string tfaiPath, string tfafPath) { }
    }
    public sealed class SaveStructureAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveStructureAnalysisSnapshot>
    {
        public SaveStructureAnalysisSnapshot(string DisplayName, string LeaderName, int LeaderLevel, int LeaderPortraitId, string ModuleName, int MapId, int LeaderTileX, int LeaderTileY, ArcNET.Diagnostics.SaveGameClockSnapshot GameTime, int TotalFileCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveEmbeddedFileExtensionSnapshot> Extensions, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveExploredAreaCoverageSnapshot> ExploredAreas, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveMapWorldStateSnapshot> Maps) { }
        public string DisplayName { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveExploredAreaCoverageSnapshot> ExploredAreas { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveEmbeddedFileExtensionSnapshot> Extensions { get; init; }
        public ArcNET.Diagnostics.SaveGameClockSnapshot GameTime { get; init; }
        public int LeaderLevel { get; init; }
        public string LeaderName { get; init; }
        public int LeaderPortraitId { get; init; }
        public int LeaderTileX { get; init; }
        public int LeaderTileY { get; init; }
        public int MapId { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveMapWorldStateSnapshot> Maps { get; init; }
        public string ModuleName { get; init; }
        public int TotalFileCount { get; init; }
    }
    public sealed class SaveTimeEventAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTimeEventAnalysisSnapshot>
    {
        public SaveTimeEventAnalysisSnapshot(bool IsTooShort, int ByteLength, int DeclaredCount, System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveTimeEventEntrySnapshot> Entries, bool HasMoreEntries) { }
        public int ByteLength { get; init; }
        public int DeclaredCount { get; init; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Diagnostics.SaveTimeEventEntrySnapshot> Entries { get; init; }
        public bool HasMoreEntries { get; init; }
        public bool IsTooShort { get; init; }
    }
    public sealed class SaveTimeEventEntrySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTimeEventEntrySnapshot>
    {
        public SaveTimeEventEntrySnapshot(int Index, int Days, int Milliseconds, int Type) { }
        public int Days { get; init; }
        public int Index { get; init; }
        public int Milliseconds { get; init; }
        public int Type { get; init; }
    }
    public sealed class SaveTimeEventFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveTimeEventFileDetailSnapshot>
    {
        public SaveTimeEventFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveTimeEventAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveTimeEventAnalysisSnapshot Analysis { get; init; }
    }
    public sealed class SaveTownMapFogDeltaSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTownMapFogDeltaSnapshot>
    {
        public SaveTownMapFogDeltaSnapshot(int ChangedFiles, int RevealedTileDelta) { }
        public int ChangedFiles { get; init; }
        public int RevealedTileDelta { get; init; }
    }
    public sealed class SaveTownMapFogFileAnalysisSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTownMapFogFileAnalysisSnapshot>
    {
        public SaveTownMapFogFileAnalysisSnapshot(int ByteLength, int RevealedTiles, int TotalTiles, double CoveragePercent) { }
        public int ByteLength { get; init; }
        public double CoveragePercent { get; init; }
        public int RevealedTiles { get; init; }
        public int TotalTiles { get; init; }
    }
    public sealed class SaveTownMapFogFileDetailSnapshot : ArcNET.Diagnostics.SaveEmbeddedFileDetailSnapshot, System.IEquatable<ArcNET.Diagnostics.SaveTownMapFogFileDetailSnapshot>
    {
        public SaveTownMapFogFileDetailSnapshot(string FileName, ArcNET.Diagnostics.SaveTownMapFogFileAnalysisSnapshot Analysis) { }
        public ArcNET.Diagnostics.SaveTownMapFogFileAnalysisSnapshot Analysis { get; init; }
    }
    public sealed class SaveTownMapFogFileSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTownMapFogFileSnapshot>
    {
        public SaveTownMapFogFileSnapshot(byte[] Bytes, int RevealedTiles) { }
        public byte[] Bytes { get; init; }
        public int RevealedTiles { get; init; }
    }
    public sealed class SaveTownMapFogSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTownMapFogSnapshot>
    {
        public SaveTownMapFogSnapshot(int FileCount, int RevealedTiles, System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Diagnostics.SaveTownMapFogFileSnapshot> Files) { }
        public int FileCount { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<string, ArcNET.Diagnostics.SaveTownMapFogFileSnapshot> Files { get; init; }
        public int RevealedTiles { get; init; }
    }
    public sealed class SaveTypedAssetSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedAssetSummarySnapshot>
    {
        public SaveTypedAssetSummarySnapshot(int TotalFileCount, int RawFileCount, int ParseErrorCount, int MobCount, int MobileMdCount, int MobileMdyCount, int SectorCount, int JumpFileCount, int MapPropertiesCount, int MessageCount, int TownMapFogCount, int DataSavCount, int Data2SavCount, int ScriptCount, int DialogCount) { }
        public int Data2SavCount { get; init; }
        public int DataSavCount { get; init; }
        public int DialogCount { get; init; }
        public int JumpFileCount { get; init; }
        public int MapPropertiesCount { get; init; }
        public int MessageCount { get; init; }
        public int MobCount { get; init; }
        public int MobileMdCount { get; init; }
        public int MobileMdyCount { get; init; }
        public int ParseErrorCount { get; init; }
        public int RawFileCount { get; init; }
        public int ScriptCount { get; init; }
        public int SectorCount { get; init; }
        public int TotalFileCount { get; init; }
        public int TownMapFogCount { get; init; }
    }
    public static class SaveTypedContextAnalysisService
    {
        public static ArcNET.Diagnostics.SaveTypedContextDeltaSnapshot CreateDelta(ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? beforePlayer, ArcNET.Diagnostics.SaveTownMapFogSnapshot beforeTownMapFogs, ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? afterPlayer, ArcNET.Diagnostics.SaveTownMapFogSnapshot afterTownMapFogs) { }
        public static ArcNET.Diagnostics.SaveTypedContextOverviewSnapshot CreateOverview(ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? player, ArcNET.Diagnostics.SaveTownMapFogSnapshot townMapFogs) { }
    }
    public sealed class SaveTypedContextDeltaSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedContextDeltaSnapshot>
    {
        public SaveTypedContextDeltaSnapshot(ArcNET.Diagnostics.SaveTypedPlayerDeltaSnapshot Player, ArcNET.Diagnostics.SaveTownMapFogDeltaSnapshot TownMapFogs) { }
        public ArcNET.Diagnostics.SaveTypedPlayerDeltaSnapshot Player { get; init; }
        public ArcNET.Diagnostics.SaveTownMapFogDeltaSnapshot TownMapFogs { get; init; }
    }
    public sealed class SaveTypedContextOverviewSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedContextOverviewSnapshot>
    {
        public SaveTypedContextOverviewSnapshot(bool HasPlayer, int QuestCount, int RumorsCount, int Blessings, int Curses, int Schematics, int? ReputationCount, int TownMapFogFileCount, int RevealedTiles) { }
        public int Blessings { get; init; }
        public int Curses { get; init; }
        public bool HasPlayer { get; init; }
        public int QuestCount { get; init; }
        public int? ReputationCount { get; init; }
        public int RevealedTiles { get; init; }
        public int RumorsCount { get; init; }
        public int Schematics { get; init; }
        public int TownMapFogFileCount { get; init; }
    }
    public static class SaveTypedContextService
    {
        public static ArcNET.Diagnostics.SaveTypedContextSnapshot Create(ArcNET.Editor.LoadedSave save) { }
    }
    public sealed class SaveTypedContextSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedContextSnapshot>
    {
        public SaveTypedContextSnapshot(System.DateTimeOffset CapturedAt, ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? Player, ArcNET.Diagnostics.SaveTownMapFogSnapshot TownMapFogs) { }
        public System.DateTimeOffset CapturedAt { get; init; }
        public ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot? Player { get; init; }
        public ArcNET.Diagnostics.SaveTownMapFogSnapshot TownMapFogs { get; init; }
    }
    public enum SaveTypedPlayerDeltaKind
    {
        Missing = 0,
        Added = 1,
        Removed = 2,
        Changed = 3,
    }
    public sealed class SaveTypedPlayerDeltaSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedPlayerDeltaSnapshot>
    {
        public SaveTypedPlayerDeltaSnapshot(ArcNET.Diagnostics.SaveTypedPlayerDeltaKind Kind, int QuestDelta, int RumorsDelta, int BlessingsDelta, int CursesDelta, int SchematicsDelta, ArcNET.Diagnostics.SaveTypedReputationDeltaSnapshot Reputation) { }
        public int BlessingsDelta { get; init; }
        public int CursesDelta { get; init; }
        public ArcNET.Diagnostics.SaveTypedPlayerDeltaKind Kind { get; init; }
        public int QuestDelta { get; init; }
        public ArcNET.Diagnostics.SaveTypedReputationDeltaSnapshot Reputation { get; init; }
        public int RumorsDelta { get; init; }
        public int SchematicsDelta { get; init; }
    }
    public sealed class SaveTypedPlayerStateSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedPlayerStateSnapshot>
    {
        public SaveTypedPlayerStateSnapshot(int QuestCount, int RumorsCount, int Blessings, int Curses, int Schematics, System.Collections.Generic.IReadOnlyDictionary<int, int>? Reputation) { }
        public int Blessings { get; init; }
        public int Curses { get; init; }
        public int QuestCount { get; init; }
        public System.Collections.Generic.IReadOnlyDictionary<int, int>? Reputation { get; init; }
        public int RumorsCount { get; init; }
        public int Schematics { get; init; }
    }
    public enum SaveTypedReputationDeltaKind
    {
        Absent = 0,
        Added = 1,
        Removed = 2,
        Changed = 3,
    }
    public sealed class SaveTypedReputationDeltaSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveTypedReputationDeltaSnapshot>
    {
        public SaveTypedReputationDeltaSnapshot(ArcNET.Diagnostics.SaveTypedReputationDeltaKind Kind, int Count, System.Collections.Generic.IReadOnlyList<int> ChangedSlots) { }
        public System.Collections.Generic.IReadOnlyList<int> ChangedSlots { get; init; }
        public int Count { get; init; }
        public ArcNET.Diagnostics.SaveTypedReputationDeltaKind Kind { get; init; }
    }
    public sealed class SaveValidationIssueSnapshot : System.IEquatable<ArcNET.Diagnostics.SaveValidationIssueSnapshot>
    {
        public SaveValidationIssueSnapshot(ArcNET.Diagnostics.DiagnosticIssueSeverity Severity, string? FilePath, string Message) { }
        public string? FilePath { get; init; }
        public string Message { get; init; }
        public ArcNET.Diagnostics.DiagnosticIssueSeverity Severity { get; init; }
    }
    public sealed class SaveValidationSummarySnapshot : System.IEquatable<ArcNET.Diagnostics.SaveValidationSummarySnapshot>
    {
        public SaveValidationSummarySnapshot(int IssueCount, int ErrorCount, int WarningCount, int InfoCount, int FileCountWithIssues) { }
        public int ErrorCount { get; init; }
        public int FileCountWithIssues { get; init; }
        public int InfoCount { get; init; }
        public int IssueCount { get; init; }
        public int WarningCount { get; init; }
    }
    public static class ScriptDumper
    {
        public static string Dump(ArcNET.Formats.ScrFile scr) { }
        public static void Dump(ArcNET.Formats.ScrFile scr, System.IO.TextWriter writer) { }
    }
    public sealed class ScrollItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.ScrollItemAnalysisSnapshot>
    {
        public ScrollItemAnalysisSnapshot(int? Flags) { }
        public int? Flags { get; init; }
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
    public sealed class WeaponItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.WeaponItemAnalysisSnapshot>
    {
        public WeaponItemAnalysisSnapshot(int? DamageLower, int? DamageUpper, int? MagicDamageBonus, int? Speed, int? MagicSpeedBonus, int? Range, int? BonusToHit, int? MagicHitBonus, int? MinStrength) { }
        public int? BonusToHit { get; init; }
        public int? DamageLower { get; init; }
        public int? DamageUpper { get; init; }
        public int? MagicDamageBonus { get; init; }
        public int? MagicHitBonus { get; init; }
        public int? MagicSpeedBonus { get; init; }
        public int? MinStrength { get; init; }
        public int? Range { get; init; }
        public int? Speed { get; init; }
    }
    public sealed class WrittenItemAnalysisSnapshot : ArcNET.Diagnostics.MobItemSpecificAnalysisSnapshot, System.IEquatable<ArcNET.Diagnostics.WrittenItemAnalysisSnapshot>
    {
        public WrittenItemAnalysisSnapshot(int? Subtype, string? SubtypeLabel, int? StartLine, int? EndLine) { }
        public int? EndLine { get; init; }
        public int? StartLine { get; init; }
        public int? Subtype { get; init; }
        public string? SubtypeLabel { get; init; }
    }
}```
