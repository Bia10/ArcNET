namespace ArcNET.LiveLab;

internal sealed class InventoryPageSnapshot
{
    public required DateTimeOffset CapturedAtUtc { get; init; }

    public required int ProcessId { get; init; }

    public required string ProcessName { get; init; }

    public required string ModuleBase { get; init; }

    public required string ModulePath { get; init; }

    public required uint CurrentCharacterSheetId { get; init; }

    public required InventoryPointerSnapshot Pointers { get; init; }

    public required InventorySummarySnapshot Summary { get; init; }

    public required InventoryWindowSnapshot AggregateWindow { get; init; }

    public required InventoryWindowSnapshot SummaryWindow { get; init; }

    public required InventoryWindowSnapshot ListRootWindow { get; init; }

    public required InventoryWindowSnapshot ListContainerWindow { get; init; }

    public required List<InventoryWindowSnapshot> ListChildWindows { get; init; }

    public required List<string> MissingCaptures { get; init; }
}

internal sealed class InventoryPointerSnapshot
{
    public required string Character { get; init; }

    public required string AggregateRoot { get; init; }

    public required string MainStats { get; init; }

    public required string BasicSkills { get; init; }

    public required string SummaryNode { get; init; }

    public required string ListRoot { get; init; }

    public required string ListContainer { get; init; }

    public required List<string> ListChildren { get; init; }
}

internal sealed class InventorySummarySnapshot
{
    public RuntimeScalarSnapshot? CurrentWeightCandidate { get; init; }

    public RuntimeScalarSnapshot? CapacityCandidate { get; init; }
}

internal sealed class InventoryWindowSnapshot
{
    public required string Name { get; init; }

    public required string Address { get; init; }

    public required List<InventoryValueSnapshot> Values { get; init; }
}

internal sealed class InventoryValueSnapshot
{
    public required int Index { get; init; }

    public required int Offset { get; init; }

    public required string Address { get; init; }

    public required int Value { get; init; }

    public required string Hex { get; init; }
}
