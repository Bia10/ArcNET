using ConsoleAppFramework;

namespace ArcNET.App;

/// <summary>
/// <c>fix</c> command group — apply, revert, and verify game-data bug corrections.
/// Usage: <c>arcnet fix &lt;subcommand&gt; &lt;gameDir&gt;</c>
/// </summary>
public sealed class FixCommands
{
    /// <summary>Apply all game-data bug fixes to the Arcanum installation.</summary>
    public async Task Apply([Argument] string gameDir) => await AppCommands.RunApplyGameDataFixesAsync(gameDir);

    /// <summary>Revert previously applied game-data bug fixes.</summary>
    public async Task Revert([Argument] string gameDir) => await AppCommands.RunRevertGameDataFixesAsync(gameDir);

    /// <summary>Check the current patch state and verify file hashes.</summary>
    public async Task Check([Argument] string gameDir) => await AppCommands.RunCheckPatchStatusAsync(gameDir);
}
