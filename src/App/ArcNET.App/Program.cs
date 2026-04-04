using ArcNET.App;
using ConsoleAppFramework;

var app = ConsoleApp.Create();
app.Add<DumpCommands>("dump");
app.Add<FixCommands>("fix");
app.Add<DataCommands>("data");
await app.RunAsync(args);
