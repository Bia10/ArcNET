namespace ArcNET.Core;

/// <summary>Identifies the release variant of an Arcanum installation.</summary>
public enum ArcanumInstallationType : byte
{
    /// <summary>
    /// Original retail release (GOG or CD image, unmodified).
    /// Proto IDs in mob and proto files match <c>description.mes</c> keys 1:1.
    /// </summary>
    Vanilla = 0,

    /// <summary>
    /// Universal Arcanum Patcher (UAP) installation.
    /// UAP prepended 20 new proto entries to every category, shifting every vanilla
    /// proto ID from N to N + <see cref="ArcanumInstallation.UapProtoIdOffset"/>.
    /// <c>description.mes</c> was not updated and still uses vanilla IDs as keys.
    /// To resolve a name for a UAP proto:
    /// <c>description.mes[protoId − <see cref="ArcanumInstallation.UapProtoIdOffset"/>]</c>.
    /// </summary>
    UniversalArcanumPatcher = 1,
}
