using ArcNET.Archive;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;
using Spectre.Console;

namespace ArcNET.App;

internal static class ContainerMobDumpWorkflow
{
    public static void Dump(DatArchive archive, string gameDir, string mapPath)
    {
        if (string.IsNullOrWhiteSpace(mapPath))
        {
            AnsiConsole.MarkupLine("[red]Map path prefix is required (e.g. maps\\\\SomeMap\\\\).[/]");
            return;
        }

        var allEntries = CollectEntries(archive, gameDir, mapPath);
        AnsiConsole.MarkupLine($"[bold]Found {allEntries.Count} mob files in cave.[/]");

        foreach (var entryPath in allEntries)
        {
            if (!TryLoadMob(archive, gameDir, entryPath, out var data, out var source, out var mob))
                continue;

            if (mob.Header.GameObjectType != ObjectType.Container)
                continue;

            WriteContainerReport(mob, data.Length, source, entryPath);
        }

        AnsiConsole.MarkupLine("\n[green]Done.[/]");
    }

    private static List<string> CollectEntries(DatArchive archive, string gameDir, string mapPath)
    {
        var datEntries = archive
            .Entries.Select(e => e.Path)
            .Where(path =>
                path.StartsWith(mapPath, StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var looseDir = Path.Combine(gameDir, "modules", "Arcanum", mapPath);
        var looseEntries = Directory.Exists(looseDir)
            ? Directory
                .EnumerateFiles(looseDir, "*.mob", SearchOption.TopDirectoryOnly)
                .Select(file => mapPath + Path.GetFileName(file))
                .Where(path => !datEntries.Contains(path, StringComparer.OrdinalIgnoreCase))
                .ToList()
            : [];

        return datEntries.Concat(looseEntries).OrderBy(path => path).ToList();
    }

    private static bool TryLoadMob(
        DatArchive archive,
        string gameDir,
        string entryPath,
        out byte[] data,
        out string source,
        out MobData mob
    )
    {
        if (!TryReadEntryBytes(archive, gameDir, entryPath, out data, out source))
        {
            mob = null!;
            return false;
        }

        try
        {
            mob = MobFormat.ParseMemory(data);
            return true;
        }
        catch (Exception ex)
        {
            mob = null!;
            AnsiConsole.MarkupLine(
                $"[yellow]SKIP {Markup.Escape(Path.GetFileName(entryPath))}: parse error — {Markup.Escape(ex.Message)}[/]"
            );
            return false;
        }
    }

    private static bool TryReadEntryBytes(
        DatArchive archive,
        string gameDir,
        string entryPath,
        out byte[] data,
        out string source
    )
    {
        var loosePath = Path.Combine(gameDir, "modules", "Arcanum", entryPath);
        if (File.Exists(loosePath))
        {
            data = File.ReadAllBytes(loosePath);
            source = $"loose: {Path.GetFileName(entryPath)}";
            return true;
        }

        try
        {
            data = archive.GetEntryData(entryPath).ToArray();
            source = $"DAT: {Path.GetFileName(entryPath)}";
            return true;
        }
        catch (KeyNotFoundException)
        {
            data = [];
            source = string.Empty;
            return false;
        }
    }

    private static void WriteContainerReport(MobData mob, int dataLength, string source, string entryPath)
    {
        var invProp = mob.Properties.FirstOrDefault(property =>
            property.Field == ObjectField.ObjFContainerInventoryListIdx
        );
        var srcProp = mob.Properties.FirstOrDefault(property =>
            property.Field == ObjectField.ObjFContainerInventorySource
        );
        var numProp = mob.Properties.FirstOrDefault(property =>
            property.Field == ObjectField.ObjFContainerInventoryNum
        );

        AnsiConsole.MarkupLine($"\n[bold cyan]{Markup.Escape(source)}[/]  ({dataLength}B)");

        var invNum = numProp?.RawBytes.Length == 4 ? numProp.GetInt32() : -1;
        var invSrc = srcProp?.RawBytes.Length == 4 ? srcProp.GetInt32() : -1;
        AnsiConsole.MarkupLine($"  Type={mob.Header.GameObjectType}  InvNum={invNum}  InvSrc={invSrc}");

        if (invProp is null)
        {
            AnsiConsole.MarkupLine("  [grey]No inventory list field present.[/]");
            return;
        }

        try
        {
            WriteInventoryItems(invProp.GetObjectIdArrayFull());
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"  [red]Failed to decode inventory list for {Markup.Escape(Path.GetFileName(entryPath))}: {Markup.Escape(ex.Message)}[/]"
            );
        }
    }

    private static void WriteInventoryItems((short OidType, int ProtoOrData1, Guid Id)[] items)
    {
        AnsiConsole.MarkupLine($"  [green]Inventory items ({items.Length}):[/]");
        for (var index = 0; index < items.Length; index++)
        {
            var (oidType, protoOrData1, guid) = items[index];
            var typeLabel = oidType switch
            {
                GameObjectGuid.OidTypeA => "A",
                GameObjectGuid.OidTypeGuid => "GUID",
                GameObjectGuid.OidTypeHandle => "HANDLE",
                _ => oidType.ToString(),
            };
            var idInfo = oidType == GameObjectGuid.OidTypeA ? $"proto={protoOrData1, 6}" : $"d.a=0x{protoOrData1:X8}";
            Console.WriteLine($"  [{index, 3}] type={typeLabel, -6} {idInfo}  guid={guid}");
        }
    }
}
