using ArcNET.App;
using Spectre.Console;

// Non-interactive CLI mode: arcnet dump-mobs <gameDir> <mapPath>
if (args is ["dump-mobs", var gameDir, var mapPath])
{
    await AppCommands.RunDumpMobFilesAsync(gameDir, mapPath);
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

AnsiConsole.Write(new FigletText("ArcNET") { Color = Spectre.Console.Color.Green });

const string exit = "Exit";

while (true)
{
    AnsiConsole.WriteLine();
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[green]What would you like to do[/]?")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
            .AddChoices(
                AppCommands.ParseExtractedData,
                AppCommands.InstallHighResPatch,
                AppCommands.UninstallHighResPatch,
                AppCommands.ApplyGameDataFixes,
                AppCommands.RevertGameDataFixes,
                AppCommands.CheckPatchStatus,
                AppCommands.DumpMobFiles,
                exit
            )
    );

    if (choice == exit)
        break;

    await AppCommands.RunAsync(choice);
}
