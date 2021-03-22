using Spectre.Console;
using System;

namespace ArcNET.Utilities
{
    public static class AnsiConsoleExtensions
    {
        public static void Log(string message, string severity)
        {
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
                        AnsiConsole.MarkupLine($"[green]{timeStampFull}-{severity.ToUpper()}:[/] {message} [green]...[/]");
                        break;
                    case "debug":
                        AnsiConsole.MarkupLine($"[grey]{timeStampFull}-{severity.ToUpper()}:[/] {message} [grey]...[/]");
                        break;
                    case "info":
                        AnsiConsole.MarkupLine($"[white]{timeStampFull}-{severity.ToUpper()}:[/] {message} [white]...[/]");
                        break;
                    case "warn":
                        AnsiConsole.MarkupLine($"[darkorange]{timeStampFull}-{severity.ToUpper()}:[/] {message} [darkorange]...[/]");
                        break;
                    case "error":
                        AnsiConsole.MarkupLine($"[red1]{timeStampFull}-{severity.ToUpper()}:[/] {message} [red1]...[/]");
                        break;
                    case "critical":
                        AnsiConsole.MarkupLine($"[red3_1]{timeStampFull}-{severity.ToUpper()}:[/] {message} [red3_1]...[/]");
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