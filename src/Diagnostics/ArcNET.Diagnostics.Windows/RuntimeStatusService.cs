using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public static class RuntimeStatusService
{
    public static RuntimeStatusSnapshot Inspect(ProcessMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);

        var fingerprint = RuntimeProfileService.ReadFingerprint(
            memory.ProcessName,
            memory.ProcessId,
            memory.ModulePath,
            memory.ModuleBase,
            memory.ModuleSize
        );
        var runtimeProfile = RuntimeProfileService.Resolve(
            memory.ProcessName,
            memory.ProcessId,
            memory.ModulePath,
            memory.ModuleBase,
            memory.ModuleSize
        );
        var capabilities = DiagnosticsCapabilityPolicy.Create(runtimeProfile, hasModuleSymbols: false);
        var displayName = $"{memory.ProcessName}.exe (PID {memory.ProcessId})";

        uint? currentCharacterSheetId = null;
        int? actionPoints = null;
        List<string> notes = [runtimeProfile.Notes];
        if (runtimeProfile.SupportsCatalogRvas)
        {
            currentCharacterSheetId = memory.ReadUInt32(memory.ResolveRva(RuntimeOffsets.CurrentCharacterSheetIdRva));
            actionPoints = memory.ReadInt32(memory.ResolveRva(RuntimeOffsets.ActionPointsRva));
        }
        else
        {
            notes.Add(
                "Catalog-backed runtime offsets are unavailable, so character-sheet and action-point reads stay disabled."
            );
        }

        notes.AddRange(capabilities.Warnings);
        return new RuntimeStatusSnapshot(
            DateTimeOffset.UtcNow,
            displayName,
            memory.ModulePath,
            ProcessMemory.FormatAddress(memory.ModuleBase),
            fingerprint,
            runtimeProfile,
            capabilities,
            currentCharacterSheetId,
            actionPoints,
            [.. notes.Where(static note => !string.IsNullOrWhiteSpace(note)).Distinct(StringComparer.Ordinal)]
        );
    }

    public static int ReadActionPoints(ProcessMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory);
        EnsureCatalogSupport(memory, "Action point read");
        return memory.ReadInt32(memory.ResolveRva(RuntimeOffsets.ActionPointsRva));
    }

    public static ActionPointsMutationSnapshot WriteActionPoints(ProcessMemory memory, int value)
    {
        ArgumentNullException.ThrowIfNull(memory);
        EnsureCatalogSupport(memory, "Action point write");

        var address = memory.ResolveRva(RuntimeOffsets.ActionPointsRva);
        var before = memory.ReadInt32(address);
        memory.WriteInt32(address, value);
        var after = memory.ReadInt32(address);
        return new ActionPointsMutationSnapshot(
            DateTimeOffset.UtcNow,
            memory.ProcessId,
            memory.ProcessName,
            memory.ModulePath,
            ProcessMemory.FormatAddress(address),
            before,
            after
        );
    }

    private static void EnsureCatalogSupport(ProcessMemory memory, string operation) =>
        _ = RuntimeProfileService.RequireCatalogSupport(
            memory.ProcessName,
            memory.ProcessId,
            memory.ModulePath,
            memory.ModuleBase,
            memory.ModuleSize,
            operation
        );
}
