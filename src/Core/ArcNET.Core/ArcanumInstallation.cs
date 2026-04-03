namespace ArcNET.Core;

/// <summary>Identifies the release variant of an Arcanum installation.</summary>
public enum ArcanumInstallationType
{
    /// <summary>
    /// Original retail release (GOG or CD image, unmodified).
    /// Proto IDs in mob and proto files match <c>description.mes</c> keys 1:1.
    /// </summary>
    Vanilla,

    /// <summary>
    /// Universal Arcanum Patcher (UAP) installation.
    /// UAP prepended 20 new proto entries to every category, shifting every vanilla
    /// proto ID from N to N + <see cref="ArcanumInstallation.UapProtoIdOffset"/>.
    /// <c>description.mes</c> was not updated and still uses vanilla IDs as keys.
    /// To resolve a name for a UAP proto:
    /// <c>description.mes[protoId − <see cref="ArcanumInstallation.UapProtoIdOffset"/>]</c>.
    /// </summary>
    UniversalArcanumPatcher,
}

/// <summary>
/// Detection and conversion helpers for Arcanum installation variants.
/// </summary>
public static class ArcanumInstallation
{
    /// <summary>
    /// Amount by which UAP shifted every vanilla proto ID upward.
    /// UAP reserved IDs 1–20 for its own new protos; all original vanilla
    /// protos moved from ID N to N + <see cref="UapProtoIdOffset"/>.
    /// </summary>
    public const int UapProtoIdOffset = 20;

    /// <summary>
    /// Relative path (from game root) of the first UAP diff archive.
    /// Its presence is the definitive indicator of a UAP installation.
    /// </summary>
    private const string UapSentinelRelPath = "modules/Arcanum.PATCH0";

    /// <summary>
    /// Detects the installation type by checking for the UAP sentinel file
    /// (<c>modules/Arcanum.PATCH0</c>) inside <paramref name="gameDir"/>.
    /// Returns <see cref="ArcanumInstallationType.UniversalArcanumPatcher"/> when found,
    /// <see cref="ArcanumInstallationType.Vanilla"/> otherwise.
    /// </summary>
    public static ArcanumInstallationType Detect(string gameDir)
    {
        var sentinel = Path.Combine(gameDir, UapSentinelRelPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(sentinel)
            ? ArcanumInstallationType.UniversalArcanumPatcher
            : ArcanumInstallationType.Vanilla;
    }

    /// <summary>
    /// Translates a proto ID from the installation's addressing scheme into the
    /// vanilla proto ID used as a key in <c>description.mes</c>.
    /// For <see cref="ArcanumInstallationType.Vanilla"/> the ID is returned unchanged.
    /// For <see cref="ArcanumInstallationType.UniversalArcanumPatcher"/> subtracts
    /// <see cref="UapProtoIdOffset"/>.
    /// </summary>
    public static int ToVanillaProtoId(int protoId, ArcanumInstallationType installation) =>
        installation == ArcanumInstallationType.UniversalArcanumPatcher ? protoId - UapProtoIdOffset : protoId;
}
