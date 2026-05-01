namespace ArcNET.Editor;

/// <summary>
/// Map-local density band derived from one projected sector count relative to other
/// positioned sectors in the same projection.
/// </summary>
public enum EditorMapSectorDensityBand
{
    /// <summary>
    /// No density is present for the metric.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low density relative to the current map projection.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium density relative to the current map projection.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High density relative to the current map projection.
    /// </summary>
    High = 3,

    /// <summary>
    /// Peak density relative to the current map projection.
    /// </summary>
    Peak = 4,
}
