using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class InventoryPageCapture
{
    private const int CharacterAggregateOffset = 0x50;
    private const int AggregateMainStatsOffset = 0x2C;
    private const int AggregateBasicSkillsOffset = 0x30;
    private const int AggregateSummaryNodeOffset = 0x34;
    private const int AggregateListRootOffset = 0x70;

    private const int SummaryCurrentWeightOffset = 0x1C;
    private const int SummaryCapacityOffset = 0x34;

    private const int ListRootListContainerOffset = 0x08;

    private static readonly int[] ListContainerChildOffsets = [0x40, 0x48, 0x50];

    public static InventoryPageSnapshot Create(ProcessMemory memory, CapturedPointers pointers) =>
        CreateFromResolvedAggregate(
            memory,
            pointers.Character,
            ReadPointer(memory, pointers.Character, CharacterAggregateOffset, "aggregate-root", [])
        );

    public static InventoryPageSnapshot CreateFromAggregateRoot(
        ProcessMemory memory,
        nint aggregateRoot,
        nint character = 0
    ) => CreateFromResolvedAggregate(memory, character, aggregateRoot);

    private static InventoryPageSnapshot CreateFromResolvedAggregate(
        ProcessMemory memory,
        nint character,
        nint aggregateRoot
    )
    {
        var missing = new List<string>();

        var mainStats = ReadPointer(memory, aggregateRoot, AggregateMainStatsOffset, "aggregate-main-stats", missing);
        var basicSkills = ReadPointer(
            memory,
            aggregateRoot,
            AggregateBasicSkillsOffset,
            "aggregate-basic-skills",
            missing
        );
        var summaryNode = ReadPointer(memory, aggregateRoot, AggregateSummaryNodeOffset, "summary-node", missing);
        var listRoot = ReadPointer(memory, aggregateRoot, AggregateListRootOffset, "list-root", missing);
        var listContainer = ReadPointer(memory, listRoot, ListRootListContainerOffset, "list-container", missing);

        var listChildAddresses = new List<nint>(ListContainerChildOffsets.Length);
        foreach (var offset in ListContainerChildOffsets)
            listChildAddresses.Add(ReadPointer(memory, listContainer, offset, $"list-child+0x{offset:X}", missing));

        return new InventoryPageSnapshot
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            ProcessId = memory.ProcessId,
            ProcessName = memory.ProcessName,
            ModuleBase = ProcessMemory.FormatAddress(memory.ModuleBase),
            ModulePath = memory.ModulePath,
            CurrentCharacterSheetId = memory.ReadUInt32(
                memory.ResolveRva(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva)
            ),
            Pointers = new InventoryPointerSnapshot
            {
                Character = ProcessMemory.FormatAddress(character),
                AggregateRoot = ProcessMemory.FormatAddress(aggregateRoot),
                MainStats = ProcessMemory.FormatAddress(mainStats),
                BasicSkills = ProcessMemory.FormatAddress(basicSkills),
                SummaryNode = ProcessMemory.FormatAddress(summaryNode),
                ListRoot = ProcessMemory.FormatAddress(listRoot),
                ListContainer = ProcessMemory.FormatAddress(listContainer),
                ListChildren = listChildAddresses.Select(ProcessMemory.FormatAddress).ToList(),
            },
            Summary = new InventorySummarySnapshot
            {
                CurrentWeightCandidate = ReadScalar(
                    memory,
                    summaryNode,
                    SummaryCurrentWeightOffset,
                    "summary-current-weight",
                    missing
                ),
                CapacityCandidate = ReadScalar(memory, summaryNode, SummaryCapacityOffset, "summary-capacity", missing),
            },
            AggregateWindow = ReadWindow(memory, "AggregateWindow", aggregateRoot, 40, "aggregate-window", missing),
            SummaryWindow = ReadWindow(memory, "SummaryWindow", summaryNode, 24, "summary-window", missing),
            ListRootWindow = ReadWindow(memory, "ListRootWindow", listRoot, 24, "list-root-window", missing),
            ListContainerWindow = ReadWindow(
                memory,
                "ListContainerWindow",
                listContainer,
                32,
                "list-container-window",
                missing
            ),
            ListChildWindows = listChildAddresses
                .Select(
                    (address, index) =>
                        ReadWindow(memory, $"ListChild{index}", address, 32, $"list-child-{index}", missing)
                )
                .ToList(),
            MissingCaptures = missing,
        };
    }

    private static nint ReadPointer(
        ProcessMemory memory,
        nint baseAddress,
        int offset,
        string captureName,
        ICollection<string> missing
    )
    {
        if (baseAddress == 0)
        {
            missing.Add(captureName);
            return 0;
        }

        return memory.ReadPointer32(baseAddress + offset);
    }

    private static RuntimeScalarSnapshot? ReadScalar(
        ProcessMemory memory,
        nint baseAddress,
        int offset,
        string captureName,
        ICollection<string> missing
    )
    {
        if (baseAddress == 0)
        {
            missing.Add(captureName);
            return null;
        }

        var address = baseAddress + offset;
        return new RuntimeScalarSnapshot
        {
            Address = ProcessMemory.FormatAddress(address),
            Value = memory.ReadInt32(address),
        };
    }

    private static InventoryWindowSnapshot ReadWindow(
        ProcessMemory memory,
        string name,
        nint address,
        int count,
        string captureName,
        ICollection<string> missing
    )
    {
        if (address == 0)
        {
            missing.Add(captureName);
            return new InventoryWindowSnapshot
            {
                Name = name,
                Address = ProcessMemory.FormatAddress(address),
                Values = [],
            };
        }

        var values = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var offset = index * sizeof(int);
                var valueAddress = address + offset;
                var value = memory.ReadInt32(valueAddress);
                return new InventoryValueSnapshot
                {
                    Index = index,
                    Offset = offset,
                    Address = ProcessMemory.FormatAddress(valueAddress),
                    Value = value,
                    Hex = $"0x{unchecked((uint)value):X8}",
                };
            })
            .ToList();

        return new InventoryWindowSnapshot
        {
            Name = name,
            Address = ProcessMemory.FormatAddress(address),
            Values = values,
        };
    }
}
