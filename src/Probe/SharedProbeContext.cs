using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace Probe;

internal sealed class SharedProbeContext
{
    private const string DefaultSlot4 = "0013";

    public LoadedSave Save { get; }
    public byte[] TfafBytes { get; }
    public string SlotStem { get; }
    public IReadOnlyList<(string path, MobileMdFile md, int idx, MobileMdRecord rec, MobData data)> AllPcs { get; }
    public IReadOnlyList<KeyValuePair<string, MobData>> PcMobFiles { get; }

    private SharedProbeContext(
        LoadedSave save,
        byte[] tfafBytes,
        string slotStem,
        List<(string path, MobileMdFile md, int idx, MobileMdRecord rec, MobData data)> allPcs,
        List<KeyValuePair<string, MobData>> pcMobFiles
    )
    {
        Save = save;
        TfafBytes = tfafBytes;
        SlotStem = slotStem;
        AllPcs = allPcs;
        PcMobFiles = pcMobFiles;
    }

    public static string ResolveSlot4(string[] args) => args.Length > 0 ? args[0].PadLeft(4, '0')[..4] : DefaultSlot4;

    public static SharedProbeContext Load(string saveDir, string slot4)
    {
        var slotStem = $"Slot{slot4}";
        var gsiPattern = slotStem + "*.gsi";

        Console.WriteLine($"=== Loading {slotStem} ===");
        var gsiPath =
            Directory.GetFiles(saveDir, gsiPattern).FirstOrDefault()
            ?? throw new FileNotFoundException($"No GSI matching {gsiPattern} in {saveDir}");

        var save = SaveGameLoader.Load(
            gsiPath,
            Path.Combine(saveDir, slotStem + ".tfai"),
            Path.Combine(saveDir, slotStem + ".tfaf")
        );
        var tfafBytes = File.ReadAllBytes(Path.Combine(saveDir, slotStem + ".tfaf"));

        Console.WriteLine($"  {save.Info.LeaderName} lv={save.Info.LeaderLevel}  TFAF={tfafBytes.Length}B");
        Console.WriteLine($"  MobileMdys={save.MobileMdys.Count}  ParseErrors={save.ParseErrors.Count}");
        foreach (
            var (path, error) in save.ParseErrors.Where(entry =>
                entry.Key.Contains("mobile.mdy", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            Console.WriteLine($"  ParseError mobile.mdy [{path}]: {error[..Math.Min(150, error.Length)]}");
        }

        var allPcs = new List<(string path, MobileMdFile md, int idx, MobileMdRecord rec, MobData data)>();
        foreach (var (path, md) in save.MobileMds)
        {
            for (var index = 0; index < md.Records.Count; index++)
            {
                if (md.Records[index].Data?.Header.GameObjectType == ObjectType.Pc)
                    allPcs.Add((path, md, index, md.Records[index], md.Records[index].Data!));
            }
        }

        Console.WriteLine($"  PC instances in mobile.md: {allPcs.Count}");
        var pcMobFiles = save.Mobiles.Where(entry => entry.Value.Header.GameObjectType == ObjectType.Pc).ToList();

        return new SharedProbeContext(save, tfafBytes, slotStem, allPcs, pcMobFiles);
    }

    public Dictionary<string, MobileMdFile> BuildUpdated(Func<MobData, MobData> fn)
    {
        var result = new Dictionary<string, MobileMdFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in AllPcs.Select(x => x.path).Distinct())
        {
            var (_, md, _, _, _) = AllPcs.First(x => x.path == path);
            var records = new List<MobileMdRecord>(md.Records);
            foreach (var (_, _, index, record, existingPc) in AllPcs.Where(x => x.path == path))
            {
                records[index] = new MobileMdRecord
                {
                    MapObjectId = record.MapObjectId,
                    Version = record.Version,
                    RawMobBytes = record.RawMobBytes,
                    Data = fn(existingPc),
                    TailBytes = record.TailBytes,
                    IsCompact = record.IsCompact,
                };
            }

            result[path] = new MobileMdFile { Records = records };
        }

        return result;
    }

    public static (string GsiPath, string TfaiPath, string TfafPath) GetLegacyOutputPaths(string saveDir) =>
        (
            Path.Combine(saveDir, "Slot0171ARCNET_TEST.gsi"),
            Path.Combine(saveDir, "Slot0171.tfai"),
            Path.Combine(saveDir, "Slot0171.tfaf")
        );

    public static void CompareBytes(string label, byte[] a, byte[] b)
    {
        if (a.AsSpan().SequenceEqual(b))
        {
            Console.WriteLine($"  {label}: IDENTICAL ({a.Length}B)");
            return;
        }

        var diffs = 0;
        for (var index = 0; index < Math.Min(a.Length, b.Length); index++)
        {
            if (a[index] != b[index])
                diffs++;
        }

        Console.WriteLine($"  {label}: DIFFERS a={a.Length}B b={b.Length}B diffs={diffs}");
    }
}
