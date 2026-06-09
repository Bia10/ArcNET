namespace ArcNET.Patch;

/// <summary>
/// Preferred SDL render driver for CE-style launch-time renderer selection.
/// </summary>
public enum SdlRenderDriver : byte
{
    Auto = 0,
    Software = 1,
    Direct3D = 2,
    Direct3D11 = 3,
    Direct3D12 = 4,
    OpenGL = 5,
    Vulkan = 6,
    Gpu = 7,
}
