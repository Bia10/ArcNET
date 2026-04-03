using ArcNET.App;
using ArcNET.Dumpers;
using ArcNET.Formats;
using Spectre.Console;

// Non-interactive CLI mode: arcnet dump-mobs <gameDir> <mapPath>
if (args is ["dump-mobs", var gameDir, var mapPath])
{
    await AppCommands.RunDumpMobFilesAsync(gameDir, mapPath);
    return;
}

// Non-interactive CLI mode: arcnet list-maps <gameDir>
if (args is ["list-maps", var gameDirListMaps])
{
    await AppCommands.RunListMapsAsync(gameDirListMaps);
    return;
}

// Non-interactive CLI mode: arcnet dump-map <gameDir> <mapPrefix> <outputFile>
if (args is ["dump-map", var gameDirDump, var mapPrefixDump, var outputFileDump])
{
    await AppCommands.RunDumpMapAsync(gameDirDump, mapPrefixDump, outputFileDump);
    return;
}

// Non-interactive CLI mode: arcnet revert-fixes <gameDir>
if (args is ["revert-fixes", var gameDirRevert])
{
    await AppCommands.RunRevertGameDataFixesAsync(gameDirRevert);
    return;
}

// Non-interactive CLI mode: arcnet apply-fixes <gameDir>
if (args is ["apply-fixes", var gameDirApply])
{
    await AppCommands.RunApplyGameDataFixesAsync(gameDirApply);
    return;
}

// ── Single-file dumper commands ────────────────────────────────────────────
// Usage: arcnet dump-<format> <file>
// Output is written to stdout; redirect with > to save to a file.

if (args is ["dump-art", var dumpArtFile])
{
    await AppCommands.RunDumpFileAsync(AppCommands.DumpArt, p => ArtDumper.Dump(ArtFormat.ParseFile(p)), dumpArtFile);
    return;
}
if (args is ["dump-dialog", var dumpDialogFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpDialog,
        p => DialogDumper.Dump(DialogFormat.ParseFile(p)),
        dumpDialogFile
    );
    return;
}
if (args is ["dump-facwalk", var dumpFacWalkFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpFacWalk,
        p => FacWalkDumper.Dump(FacWalkFormat.ParseFile(p)),
        dumpFacWalkFile
    );
    return;
}
if (args is ["dump-jmp", var dumpJmpFile])
{
    await AppCommands.RunDumpFileAsync(AppCommands.DumpJmp, p => JmpDumper.Dump(JmpFormat.ParseFile(p)), dumpJmpFile);
    return;
}
if (args is ["dump-map-props", var dumpMapPropsFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpMapProps,
        p => MapPropertiesDumper.Dump(MapPropertiesFormat.ParseFile(p)),
        dumpMapPropsFile
    );
    return;
}
if (args is ["dump-message", var dumpMessageFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpMessage,
        p => MessageDumper.Dump(MessageFormat.ParseFile(p)),
        dumpMessageFile
    );
    return;
}
if (args is ["dump-proto", var dumpProtoFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpProto,
        p => ProtoDumper.Dump(ProtoFormat.ParseFile(p)),
        dumpProtoFile
    );
    return;
}
if (args is ["dump-saveindex", var dumpSaveIndexFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpSaveIndex,
        p => SaveIndexDumper.Dump(SaveIndexFormat.ParseFile(p)),
        dumpSaveIndexFile
    );
    return;
}
if (args is ["dump-saveinfo", var dumpSaveInfoFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpSaveInfo,
        p => SaveInfoDumper.Dump(SaveInfoFormat.ParseFile(p)),
        dumpSaveInfoFile
    );
    return;
}
if (args is ["dump-script", var dumpScriptFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpScript,
        p => ScriptDumper.Dump(ScriptFormat.ParseFile(p)),
        dumpScriptFile
    );
    return;
}
if (args is ["dump-sector", var dumpSectorFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpSector,
        p => SectorDumper.Dump(SectorFormat.ParseFile(p)),
        dumpSectorFile
    );
    return;
}
if (args is ["dump-terrain", var dumpTerrainFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpTerrain,
        p => TerrainDumper.Dump(TerrainFormat.ParseFile(p)),
        dumpTerrainFile
    );
    return;
}
if (args is ["dump-textdata", var dumpTextDataFile])
{
    await AppCommands.RunDumpFileAsync(
        AppCommands.DumpTextData,
        p => TextDataDumper.Dump(TextDataFormat.ParseFile(p)),
        dumpTextDataFile
    );
    return;
}

AnsiConsole.Write(new FigletText("ArcNET") { Color = Spectre.Console.Color.Green });

const string exit = "Exit";

while (true)
{
    AnsiConsole.WriteLine();
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[green]What would you like to do[/]?")
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
            .AddChoices(
                AppCommands.ParseExtractedData,
                AppCommands.InstallHighResPatch,
                AppCommands.UninstallHighResPatch,
                AppCommands.ApplyGameDataFixes,
                AppCommands.RevertGameDataFixes,
                AppCommands.CheckPatchStatus,
                AppCommands.DumpMobFiles,
                AppCommands.DumpArt,
                AppCommands.DumpDialog,
                AppCommands.DumpFacWalk,
                AppCommands.DumpJmp,
                AppCommands.DumpMapProps,
                AppCommands.DumpMessage,
                AppCommands.DumpProto,
                AppCommands.DumpSaveIndex,
                AppCommands.DumpSaveInfo,
                AppCommands.DumpScript,
                AppCommands.DumpSector,
                AppCommands.DumpTerrain,
                AppCommands.DumpTextData,
                exit
            )
    );

    if (choice == exit)
        break;

    await AppCommands.RunAsync(choice);
}
