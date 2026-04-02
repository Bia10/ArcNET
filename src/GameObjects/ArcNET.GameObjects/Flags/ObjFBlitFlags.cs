namespace ArcNET.GameObjects;

/// <summary>Rendering blit flags controlling how an object is drawn.</summary>
[Flags]
public enum ObjFBlitFlags : uint
{
    /// <summary>Object is rendered with additive blending.</summary>
    BlendAdd = 1 << 0,
}
