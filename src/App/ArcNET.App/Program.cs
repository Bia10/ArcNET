using ArcNET.App;
using Spectre.Console;

AnsiConsole.Write(new FigletText("ArcNET") { Color = Spectre.Console.Color.Green });

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[green]What would you like to do[/]?")
        .PageSize(5)
        .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
        .AddChoices(AppCommands.ParseExtractedData, AppCommands.InstallHighResPatch, AppCommands.UninstallHighResPatch)
);

await AppCommands.RunAsync(choice);
