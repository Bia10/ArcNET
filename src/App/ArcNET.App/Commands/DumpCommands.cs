using ArcNET.Dumpers;
using ArcNET.Formats;
using ConsoleAppFramework;

namespace ArcNET.App;

/// <summary>
/// <c>dump</c> command group — parse and print Arcanum binary file formats.
/// Usage: <c>arcnet dump &lt;subcommand&gt; &lt;file&gt; [--json]</c>
/// </summary>
public sealed class DumpCommands
{
    /// <summary>Dump an ART sprite file.</summary>
    public void Art([Argument] string file, bool json = false) =>
        DumpFile(file, "art", ArtFormat.ParseFile, ArtDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a DLG dialogue file.</summary>
    public void Dialog([Argument] string file, bool json = false) =>
        DumpFile(file, "dialog", DialogFormat.ParseFile, DialogDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a FacadeWalk walkability file.</summary>
    public void FacWalk([Argument] string file, bool json = false) =>
        DumpFile(file, "facwalk", FacWalkFormat.ParseFile, FacWalkDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a JMP jump-point file.</summary>
    public void Jmp([Argument] string file, bool json = false) =>
        DumpFile(file, "jmp", JmpFormat.ParseFile, JmpDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a PRP map-properties file.</summary>
    public void MapProps([Argument] string file, bool json = false) =>
        DumpFile(file, "map-props", MapPropertiesFormat.ParseFile, MapPropertiesDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a MES message file.</summary>
    public void Message([Argument] string file, bool json = false) =>
        DumpFile(file, "message", MessageFormat.ParseFile, MessageDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a PRO prototype file (text output only).</summary>
    public void Proto([Argument] string file) => DumpFile(file, "proto", ProtoFormat.ParseFile, ProtoDumper.Dump);

    /// <summary>Dump a TFAI save-game index file.</summary>
    public void SaveIndex([Argument] string file, bool json = false) =>
        DumpFile(file, "save-index", SaveIndexFormat.ParseFile, SaveIndexDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a GSI save-game info file.</summary>
    public void SaveInfo([Argument] string file, bool json = false) =>
        DumpFile(file, "save-info", SaveInfoFormat.ParseFile, SaveInfoDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump an SCR compiled script file.</summary>
    public void Script([Argument] string file, bool json = false) =>
        DumpFile(file, "script", ScriptFormat.ParseFile, ScriptDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a SEC sector file.</summary>
    public void Sector([Argument] string file, bool json = false) =>
        DumpFile(file, "sector", SectorFormat.ParseFile, SectorDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a TDF terrain file.</summary>
    public void Terrain([Argument] string file, bool json = false) =>
        DumpFile(file, "terrain", TerrainFormat.ParseFile, TerrainDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a text-data key-value file.</summary>
    public void TextData([Argument] string file, bool json = false) =>
        DumpFile(file, "text-data", TextDataFormat.ParseFile, TextDataDumper.Dump, AgentOutput.Project, json);

    /// <summary>Dump a complete save game directory (GSI + TFAI + TFAF).</summary>
    public void Save([Argument] string directory)
    {
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Directory not found: {directory}");
            Environment.Exit(1);
            return;
        }

        try
        {
            Console.Write(SaveDumper.Dump(directory));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Save dump failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // ── Shared helper ─────────────────────────────────────────────────────────

    private static void DumpFile<T>(
        string file,
        string format,
        Func<string, T> parse,
        Func<T, string> dump,
        Func<T, object>? project = null,
        bool json = false
    )
    {
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"File not found: {file}");
            Environment.Exit(1);
            return;
        }

        try
        {
            var parsed = parse(file);
            if (json && project is not null)
                AgentOutput.Write(format, file, project(parsed));
            else if (json)
                Console.Write(dump(parsed));
            else
                Console.Write(dump(parsed));
        }
        catch (Exception ex)
        {
            if (json)
                AgentOutput.WriteError(file, ex);
            else
            {
                Console.Error.WriteLine($"Failed to parse '{file}': {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
