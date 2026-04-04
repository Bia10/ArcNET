using ConsoleAppFramework;

namespace ArcNET.App;

/// <summary>
/// <c>data</c> command group — query and export game data from the Arcanum installation.
/// Usage: <c>arcnet data &lt;subcommand&gt; &lt;gameDir&gt; [args...]</c>
/// </summary>
public sealed class DataCommands
{
    /// <summary>List all map directories found inside Arcanum.dat.</summary>
    public async Task ListMaps([Argument] string gameDir) => await AppCommands.RunListMapsAsync(gameDir);

    /// <summary>
    /// Dump all mob files for a given map directory prefix inside Arcanum.dat.
    /// <paramref name="mapPath"/> is a backslash-separated prefix, e.g. <c>maps\SomeMap\</c>.
    /// </summary>
    public async Task DumpMobs([Argument] string gameDir, [Argument] string mapPath) =>
        await AppCommands.RunDumpMobFilesAsync(gameDir, mapPath);

    /// <summary>
    /// Dump all mobs and protos for a map prefix to a text file.
    /// <paramref name="mapPrefix"/> is a backslash-separated prefix, e.g. <c>maps\SomeMap\</c>.
    /// </summary>
    public async Task DumpMap([Argument] string gameDir, [Argument] string mapPrefix, [Argument] string output) =>
        await AppCommands.RunDumpMapAsync(gameDir, mapPrefix, output);
}
