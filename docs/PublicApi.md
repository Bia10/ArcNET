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
        public static ArcNET.Formats.ObjectProperty WithObjectIdArrayFull(this ArcNET.Formats.ObjectProperty property, [System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "OidType",
                "ProtoOrData1",
                "Id"})] System.ReadOnlySpan<System.ValueTuple<short, int, System.Guid>> ids) { }
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
        public static ArcNET.Formats.ObjectProperty ForObjectIdArray(ArcNET.GameObjects.ObjectField field, System.ReadOnlySpan<System.Guid> ids) { }
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
    public static class BitArrayObjectExtensions
    {
        public static bool HasField(this byte[] bitmap, ArcNET.GameObjects.ObjectField field) { }
        public static void SetField(this byte[] bitmap, ArcNET.GameObjects.ObjectField field, bool value) { }
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
        ObjFConditions = 41,
        ObjFConditionArg0 = 42,
        ObjFPermanentMods = 43,
        ObjFInitiative = 44,
        ObjFDispatcher = 45,
        ObjFSubinitiative = 46,
        ObjFSecretdoorFlags = 47,
        ObjFSecretdoorEffectName = 48,
        ObjFSecretdoorDc = 49,
        ObjFPadI7 = 50,
        ObjFPadI8 = 51,
        ObjFPadI9 = 52,
        ObjFPadI0 = 53,
        ObjFOffsetZ = 54,
        ObjFRotationPitch = 55,
        ObjFPadF3 = 56,
        ObjFPadF4 = 57,
        ObjFPadF5 = 58,
        ObjFPadF6 = 59,
        ObjFPadF7 = 60,
        ObjFPadF8 = 61,
        ObjFPadF9 = 62,
        ObjFPadF0 = 63,
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
        ObjFNpcHostileListIdx = 152,
    }
    public static class ObjectFieldBitmapSize
    {
        public static int For(ArcNET.GameObjects.ObjectType type) { }
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
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFBlitFlags> BlitFlags { get; init; }
        public int Category { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFCritterFlags> CritterFlags { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFCritterFlags2> CritterFlags2 { get; init; }
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
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFNpcFlags> NpcFlags { get; init; }
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFFlags> ObjectFlags { get; init; }
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
        public System.Collections.Generic.List<ArcNET.GameObjects.ObjFSpellFlags> SpellFlags { get; init; }
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
        public ArcNET.GameObjects.ObjFArmorFlags ArmorFlags { get; }
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
        public ArcNET.GameObjects.ObjFFlags ObjectFlags { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public int[] OverlayBack { get; }
        public int[] OverlayFore { get; }
        public int[] OverlayLightAid { get; }
        public int OverlayLightColor { get; }
        public int OverlayLightFlags { get; }
        public int[] ResistanceIdx { get; }
        public ArcNET.GameObjects.GameObjectScript[] ScriptsIdx { get; }
        public ArcNET.Core.Primitives.ArtId Shadow { get; }
        public int SoundEffect { get; }
        public ArcNET.GameObjects.ObjFSpellFlags SpellFlags { get; }
        public int[] Underlay { get; }
        protected void ReadCommonFields(ref ArcNET.Core.SpanReader reader, byte[] bitmap, bool isPrototype) { }
        protected void WriteCommonFields(ref ArcNET.Core.SpanWriter writer, byte[] bitmap, bool isPrototype) { }
    }
    public sealed class ObjectContainer : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectContainer() { }
        public ArcNET.GameObjects.ObjFContainerFlags ContainerFlags { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] InventoryList { get; }
        public int InventorySource { get; }
        public int KeyId { get; }
        public int LockDifficulty { get; }
        public int NotifyNpc { get; }
    }
    public class ObjectCritter : ArcNET.GameObjects.Types.ObjectCommon
    {
        public ObjectCritter() { }
        public int CritterArrows { get; }
        public int CritterAutoLevelScheme { get; }
        public int[] CritterBasicSkill { get; }
        public int CritterBullets { get; }
        public int CritterCritHitChart { get; }
        public int CritterDeathTime { get; }
        public int CritterDescriptionUnknown { get; }
        public int[] CritterEffectCause { get; }
        public int[] CritterEffects { get; }
        public int CritterFatigueAdj { get; }
        public int CritterFatigueDamage { get; }
        public int CritterFatiguePts { get; }
        public ArcNET.GameObjects.ObjFCritterFlags CritterFlags { get; }
        public ArcNET.GameObjects.ObjFCritterFlags2 CritterFlags2 { get; }
        public ArcNET.Core.Primitives.GameObjectGuid CritterFleeingFrom { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterFollowers { get; }
        public int CritterFuel { get; }
        public int CritterGold { get; }
        public ArcNET.Core.Primitives.GameObjectGuid[] CritterInventoryList { get; }
        public int CritterInventorySource { get; }
        public int CritterPortrait { get; }
        public int CritterPowerCells { get; }
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
        public ArcNET.GameObjects.ObjFItemFlags ItemFlags { get; }
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
        public ArcNET.GameObjects.ObjFNpcFlags NpcFlags { get; }
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
        public ArcNET.GameObjects.ObjFPortalFlags PortalFlags { get; }
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
        public ArcNET.GameObjects.ObjFSceneryFlags SceneryFlags { get; }
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
        public ArcNET.GameObjects.ObjFWeaponFlags WeaponFlags { get; }
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
        public System.Collections.Generic.IReadOnlyList<ArcNET.Formats.DlgFile> Dialogs { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<ArcNET.Formats.DlgFile>> DialogsBySource { get; }
        public System.Collections.Generic.IReadOnlySet<ArcNET.Core.Primitives.GameObjectGuid> DirtyObjects { get; }
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
        public event System.EventHandler<ArcNET.Core.Primitives.GameObjectGuid>? ObjectChanged;
        public void AddDialog(ArcNET.Formats.DlgFile dialog) { }
        public void AddMessage(ArcNET.Formats.MessageEntry entry) { }
        public void AddMob(ArcNET.Formats.MobData mob) { }
        public void AddObject(ArcNET.GameObjects.GameObjectHeader header) { }
        public void AddProto(ArcNET.Formats.ProtoData proto) { }
        public void AddScript(ArcNET.Formats.ScrFile script) { }
        public void AddSector(ArcNET.Formats.Sector sector) { }
        public void Clear() { }
        public void ClearDirty() { }
        public ArcNET.GameObjects.GameObjectHeader? FindByGuid(in ArcNET.Core.Primitives.GameObjectGuid id) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> FindByProtoId(in ArcNET.Core.Primitives.GameObjectGuid protoId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.GameObjects.GameObjectHeader> FindByType(ArcNET.GameObjects.ObjectType type) { }
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
        public ArcNET.Editor.CharacterBuilder WithGold(int amount) { }
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
        public ArcNET.Editor.DialogBuilder AddEntry(ArcNET.Formats.DialogEntry entry) { }
        public ArcNET.Formats.DlgFile Build() { }
        public ArcNET.Editor.DialogBuilder RemoveEntry(int num) { }
        public ArcNET.Editor.DialogBuilder UpdateEntry(int num, System.Func<ArcNET.Formats.DialogEntry, ArcNET.Formats.DialogEntry> update) { }
    }
    public sealed class EditorArtReference
    {
        public EditorArtReference() { }
        public required uint ArtId { get; init; }
        public required ArcNET.Editor.EditorAssetEntry Asset { get; init; }
        public required int Count { get; init; }
        public ArcNET.Formats.FileFormat Format { get; }
    }
    public sealed class EditorAssetCatalog
    {
        public int Count { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> Entries { get; }
        public static ArcNET.Editor.EditorAssetCatalog Empty { get; }
        public ArcNET.Editor.EditorAssetEntry? Find(string assetPath) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindByFormat(ArcNET.Formats.FileFormat format) { }
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
    public sealed class EditorAssetIndex
    {
        public System.Collections.Generic.IReadOnlyList<string> MapNames { get; }
        public static ArcNET.Editor.EditorAssetIndex Empty { get; }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorArtReference> FindArtReferences(uint artId) { }
        public string? FindAssetMap(string assetPath) { }
        public ArcNET.Editor.EditorAssetEntry? FindDialogDefinition(int dialogId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindDialogDefinitions(int dialogId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorDialogDefinition> FindDialogDetails(int dialogId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMapAssets(string mapName) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindMessageAssets(int messageIndex) { }
        public ArcNET.Editor.EditorAssetEntry? FindProtoDefinition(int protoNumber) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorProtoReference> FindProtoReferences(int protoNumber) { }
        public ArcNET.Editor.EditorAssetEntry? FindScriptDefinition(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorAssetEntry> FindScriptDefinitions(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptDefinition> FindScriptDetails(int scriptId) { }
        public System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorScriptReference> FindScriptReferences(int scriptId) { }
    }
    public enum EditorAssetSourceKind
    {
        LooseFile = 0,
        DatArchive = 1,
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
        public required int NpcEntryCount { get; init; }
        public required int PcOptionCount { get; init; }
        public required System.Collections.Generic.IReadOnlyList<int> RootEntryNumbers { get; init; }
        public required int TerminalEntryCount { get; init; }
        public required int TransitionCount { get; init; }
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
    public sealed class EditorWorkspace
    {
        public EditorWorkspace() { }
        public ArcNET.Editor.EditorAssetCatalog Assets { get; init; }
        public required string ContentDirectory { get; init; }
        public required ArcNET.GameData.GameDataStore GameData { get; init; }
        public string? GameDirectory { get; init; }
        public bool HasSaveLoaded { get; }
        public ArcNET.Editor.EditorAssetIndex Index { get; init; }
        public ArcNET.Core.ArcanumInstallationType? InstallationType { get; init; }
        public ArcNET.Editor.EditorWorkspaceLoadReport LoadReport { get; init; }
        public ArcNET.Editor.LoadedSave? Save { get; init; }
        public string? SaveFolder { get; init; }
        public string? SaveSlotName { get; init; }
        public ArcNET.Editor.EditorWorkspaceValidationReport Validation { get; init; }
        public ArcNET.Editor.SaveGameEditor CreateSaveEditor() { }
    }
    public sealed class EditorWorkspaceLoadOptions
    {
        public EditorWorkspaceLoadOptions() { }
        public string? GameDirectory { get; init; }
        public string? SaveFolder { get; init; }
        public string? SaveSlotName { get; init; }
    }
    public sealed class EditorWorkspaceLoadReport
    {
        public EditorWorkspaceLoadReport() { }
        public bool HasSkippedInputs { get; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; init; }
        public required System.Collections.Generic.IReadOnlyList<ArcNET.Editor.EditorSkippedAsset> SkippedAssets { get; init; }
        public static ArcNET.Editor.EditorWorkspaceLoadReport Empty { get; }
    }
    public static class EditorWorkspaceLoader
    {
        public static ArcNET.Editor.EditorWorkspace Load(string contentDirectory, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadAsync(string contentDirectory, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
        public static ArcNET.Editor.EditorWorkspace LoadFromGameInstall(string gameDir, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null) { }
        public static System.Threading.Tasks.Task<ArcNET.Editor.EditorWorkspace> LoadFromGameInstallAsync(string gameDir, ArcNET.Editor.EditorWorkspaceLoadOptions? options = null, System.IProgress<float>? progress = null, System.Threading.CancellationToken cancellationToken = default) { }
    }
    public sealed class EditorWorkspaceValidationIssue : System.IEquatable<ArcNET.Editor.EditorWorkspaceValidationIssue>
    {
        public EditorWorkspaceValidationIssue() { }
        public string? AssetPath { get; init; }
        public required string Message { get; init; }
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
        public ArcNET.Editor.MobDataBuilder WithProperty(ArcNET.Formats.ObjectProperty property) { }
        public ArcNET.Editor.MobDataBuilder WithoutProperty(ArcNET.GameObjects.ObjectField field) { }
    }
    public sealed class SaveGameEditor
    {
        public SaveGameEditor(ArcNET.Editor.LoadedSave save) { }
        public ArcNET.Formats.Data2SavFile? GetCurrentData2Sav(string path) { }
        public ArcNET.Formats.DataSavFile? GetCurrentDataSav(string path) { }
        public ArcNET.Formats.MesFile? GetCurrentMessageFile(string path) { }
        public System.ReadOnlyMemory<byte>? GetCurrentRawFile(string path) { }
        public ArcNET.Formats.SaveInfo GetCurrentSaveInfo() { }
        public ArcNET.Formats.TownMapFog? GetCurrentTownMapFog(string path) { }
        public ArcNET.Formats.Data2SavFile? GetPendingData2Sav(string path) { }
        public ArcNET.Formats.DataSavFile? GetPendingDataSav(string path) { }
        public ArcNET.Formats.MesFile? GetPendingMessageFile(string path) { }
        public ArcNET.Formats.MobileMdyFile? GetPendingMobileMdy(string mdyPath) { }
        public System.ReadOnlyMemory<byte>? GetPendingRawFile(string path) { }
        public ArcNET.Formats.SaveInfo? GetPendingSaveInfo() { }
        public ArcNET.Formats.TownMapFog? GetPendingTownMapFog(string path) { }
        public void Save(string saveFolder, string slotName) { }
        public void Save(string gsiPath, string tfaiPath, string tfafPath) { }
        public System.Threading.Tasks.Task SaveAsync(string saveFolder, string slotName, System.Threading.CancellationToken cancellationToken = default) { }
        public System.Threading.Tasks.Task SaveAsync(string gsiPath, string tfaiPath, string tfafPath, System.Threading.CancellationToken cancellationToken = default) { }
        public bool TryFindCharacter(System.Func<ArcNET.Editor.CharacterRecord, bool> predicate, out ArcNET.Editor.CharacterRecord character, out string mdyPath) { }
        public bool TryFindPendingPlayerCharacter(out ArcNET.Editor.CharacterRecord character) { }
        public bool TryFindPlayerCharacter(out ArcNET.Editor.CharacterRecord character) { }
        public bool TryFindPlayerCharacter(out ArcNET.Editor.CharacterRecord character, out string mdyPath) { }
        public ArcNET.Editor.SaveGameEditor WithCharacter(string mdyPath, System.Func<ArcNET.Editor.CharacterRecord, bool> predicate, ArcNET.Editor.CharacterRecord updated) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, ArcNET.Formats.Data2SavFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, System.Action<ArcNET.Formats.Data2SavFile.Builder> update) { }
        public ArcNET.Editor.SaveGameEditor WithData2Sav(string path, System.Func<ArcNET.Formats.Data2SavFile, ArcNET.Formats.Data2SavFile> update) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, ArcNET.Formats.DataSavFile updated) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, System.Action<ArcNET.Formats.DataSavFile.Builder> update) { }
        public ArcNET.Editor.SaveGameEditor WithDataSav(string path, System.Func<ArcNET.Formats.DataSavFile, ArcNET.Formats.DataSavFile> update) { }
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
        public ArcNET.Formats.ScrFile Build() { }
        public ArcNET.Editor.ScriptBuilder RemoveCondition(int index) { }
        public ArcNET.Editor.ScriptBuilder ReplaceCondition(int index, ArcNET.Formats.ScriptConditionData condition) { }
        public ArcNET.Editor.ScriptBuilder WithDescription(string description) { }
        public ArcNET.Editor.ScriptBuilder WithFlags(ArcNET.Formats.ScriptFlags flags) { }
        public ArcNET.Editor.ScriptBuilder WithHeaderCounters(uint counters) { }
        public ArcNET.Editor.ScriptBuilder WithHeaderFlags(uint flags) { }
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
    public static class SaveDumper
    {
        public static string Dump(string saveDir) { }
        public static void Dump(string saveDir, System.IO.TextWriter writer) { }
        public static string Dump(string gsiPath, string tfaiPath, string tfafPath) { }
        public static void Dump(string gsiPath, string tfaiPath, string tfafPath, System.IO.TextWriter writer) { }
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
