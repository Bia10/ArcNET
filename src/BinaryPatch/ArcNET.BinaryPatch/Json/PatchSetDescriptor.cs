using System.Text.Json.Serialization;

namespace ArcNET.BinaryPatch.Json;

/// <summary>Internal DTO representing the top-level JSON object in a patch file.</summary>
internal sealed class PatchSetDescriptor
{
    [JsonRequired]
    public required string Name { get; init; }

    [JsonRequired]
    public required string Version { get; init; }

    [JsonRequired]
    public required List<PatchDescriptor> Patches { get; init; }
}

/// <summary>Internal DTO for a single patch entry within a JSON patch file.</summary>
/// <remarks>
/// Not all fields are valid for every <c>type</c>:
/// <list type="table">
///   <listheader><term>type</term><description>Required fields</description></listheader>
///   <item><term>ProtoFieldSetInt32 / MobFieldSetInt32</term><description>field, expectedValue, newValue</description></item>
///   <item><term>ProtoFieldClearInt32 / MobFieldClearInt32</term><description>field, newValue</description></item>
///   <item><term>RawAtOffset</term><description>offset, expectedHex, newHex</description></item>
/// </list>
/// </remarks>
internal sealed class PatchDescriptor
{
    /// <summary>Patch type discriminator. See class remarks for valid values.</summary>
    [JsonRequired]
    public required string Type { get; init; }

    /// <summary>Unique, URL-safe patch identifier.</summary>
    [JsonRequired]
    public required string Id { get; init; }

    /// <summary>Human-readable patch description.</summary>
    [JsonRequired]
    public required string Description { get; init; }

    /// <summary>Target file path relative to the game directory, using forward slashes.</summary>
    [JsonRequired]
    public required string RelativePath { get; init; }

    // ── Proto / Mob field patches ──────────────────────────────────────────

    /// <summary><see cref="GameObjects.ObjectField"/> enum member name (exact, case-sensitive).</summary>
    public string? Field { get; init; }

    /// <summary>
    /// For <c>SetInt32</c>: the expected current value; <see cref="IBinaryPatch.NeedsApply"/>
    /// returns <see langword="true"/> only when the field holds this exact value.
    /// </summary>
    public int? ExpectedValue { get; init; }

    /// <summary>The value to write into the field.</summary>
    public int? NewValue { get; init; }

    // ── Raw offset patches ─────────────────────────────────────────────────

    /// <summary>Zero-based byte offset into the target file.</summary>
    public int? Offset { get; init; }

    /// <summary>Hex-encoded bytes expected at <see cref="Offset"/> in the unpatched file.</summary>
    public string? ExpectedHex { get; init; }

    /// <summary>Hex-encoded replacement bytes (must have the same length as <see cref="ExpectedHex"/>).</summary>
    public string? NewHex { get; init; }

    // ── DAT archive source (optional) ──────────────────────────────────────

    /// <summary>
    /// Path of the DAT archive relative to the game root, using forward slashes
    /// (e.g. <c>modules/Arcanum.dat</c>).
    /// When set together with <see cref="DatEntryPath"/>, <see cref="BinaryPatcher"/>
    /// extracts the target file from this archive when the loose override does not exist.
    /// Revert deletes the loose override file so the game falls back to the DAT version.
    /// </summary>
    public string? SourceDatPath { get; init; }

    /// <summary>
    /// Virtual path of the entry inside <see cref="SourceDatPath"/>, using backslashes as
    /// stored in the DAT directory (e.g.
    /// <c>maps\Cave of the Bangellian Scourge\G_47CF0385_28D8_4048_95D4_CD578DECE993.mob</c>).
    /// Required when <see cref="SourceDatPath"/> is set.
    /// </summary>
    public string? DatEntryPath { get; init; }
}
