using Spectre.Console;
using System;

namespace ArcNET.Utilities
{
    public static class AnsiConsoleExtensions
    {
        public static void Log(string message, string severity)
        {
            var escapedMsg = Markup.Escape(message);
            var year = DateTime.Now.Year.ToString("0000");
            var month = DateTime.Now.Month.ToString("00");
            var day = DateTime.Now.Day.ToString("00");
            var hour = DateTime.Now.Hour.ToString("00");
            var minute = DateTime.Now.Minute.ToString("00");
            var second = DateTime.Now.Second.ToString("00");
            var milliseconds = DateTime.Now.Millisecond.ToString("000");
            var timeStampFull = year + "/" + month + "/" + day + "-" + hour + ":" + minute + ":" + second + ":" + milliseconds;

            try
            {
                switch (severity)
                {

                    case "success":
                        AnsiConsole.MarkupLine($"[green]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [green]...[/]");
                        break;
                    case "debug":
                        AnsiConsole.MarkupLine($"[grey]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [grey]...[/]");
                        break;
                    case "info":
                        AnsiConsole.MarkupLine($"[white]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [white]...[/]");
                        break;
                    case "warn":
                        AnsiConsole.MarkupLine($"[darkorange]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [darkorange]...[/]");
                        break;
                    case "error":
                        AnsiConsole.MarkupLine($"[red1]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [red1]...[/]");
                        break;
                    case "critical":
                        AnsiConsole.MarkupLine($"[red3_1]{timeStampFull}-{severity.ToUpper()}:[/] {escapedMsg} [red3_1]...[/]");
                        break;

                    default:
                        var ex = new Exception("Unsupported log severity!");
                        AnsiConsole.WriteException(ex);
                        break;
                }
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
                throw;
            }
        }
    }
}