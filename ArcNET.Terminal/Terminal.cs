using Spectre.Console;

namespace ArcNET.Terminal
{
    public static class Terminal
    {
        public static void RenderLogo()
        {
            AnsiConsole.Render(
                new FigletText("ArcNET v0.0.1")
                    .LeftAligned()
                    .Color(Color.Green));
        }

        public static string GetMainMenuChoice()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do[/]?")
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more choices)[/]")
                    .AddChoices("Extract game data", "Parse extracted game data", "Install High-Res patch",
                        "Uninstall High-Res patch", "Reinstall High-Res Patch", "Launch Arcanum.exe"));
        }
    }
}