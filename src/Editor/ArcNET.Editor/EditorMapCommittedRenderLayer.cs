namespace ArcNET.Editor;

/// <summary>
/// CE-style committed scene layer classification carried through host render metadata.
/// </summary>
public enum EditorMapCommittedRenderLayer
{
    Ground = 0,
    GroundDecal = 1,
    Wall = 2,
    Scenery = 3,
    Mobile = 4,
    Roof = 5,
}
