namespace ArcNET.GameObjects;

/// <summary>Rendering blit flags controlling how an object is drawn.</summary>
[Flags]
public enum ObjFBlitFlags : byte
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Object is rendered with additive blending.</summary>
    BlendAdd = 0x1,
}
