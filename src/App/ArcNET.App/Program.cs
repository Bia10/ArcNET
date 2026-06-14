using ArcNET.App;
using ConsoleAppFramework;

if (args.Length > 0 && string.Equals(args[0], "editor", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("The editor command group is no longer available from the CLI.");
    Environment.ExitCode = 1;
    return;
}

var app = ConsoleApp.Create();
app.Add<DumpCommands>("dump");
app.Add<FixCommands>("fix");
app.Add<DataCommands>("data");
await app.RunAsync(args);
