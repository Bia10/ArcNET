namespace ArcNET.GameObjects;

/// <summary>Rendering blit flags controlling how an object is drawn.</summary>
[Flags]
public enum BlitFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Object is rendered with additive blending.</summary>
    BlendAdd = 0x00000010,

    /// <summary>Object is rendered with subtractive blending.</summary>
    BlendSub = 0x00000020,

    /// <summary>Object is rendered with multiplicative blending.</summary>
    BlendMul = 0x00000040,

    /// <summary>Object is rendered with one constant alpha value.</summary>
    BlendAlphaConst = 0x00000100,

    /// <summary>Object is rendered with source alpha.</summary>
    BlendAlphaSrc = 0x00000200,

    /// <summary>Object is rendered with one constant color.</summary>
    BlendColorConst = 0x00002000,
}
