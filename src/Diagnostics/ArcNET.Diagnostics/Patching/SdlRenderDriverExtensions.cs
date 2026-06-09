namespace ArcNET.Patch;

/// <summary>
/// Helpers for converting debugger-facing renderer choices into SDL launch hints.
/// </summary>
public static class SdlRenderDriverExtensions
{
    public static string? ToHintValue(this SdlRenderDriver driver) =>
        driver switch
        {
            SdlRenderDriver.Auto => null,
            SdlRenderDriver.Software => "software",
            SdlRenderDriver.Direct3D => "direct3d",
            SdlRenderDriver.Direct3D11 => "direct3d11",
            SdlRenderDriver.Direct3D12 => "direct3d12",
            SdlRenderDriver.OpenGL => "opengl",
            SdlRenderDriver.Vulkan => "vulkan",
            SdlRenderDriver.Gpu => "gpu",
            _ => throw new ArgumentOutOfRangeException(nameof(driver), driver, "Unsupported SDL render driver."),
        };

    public static bool TryParse(string text, out SdlRenderDriver driver)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "auto":
                driver = SdlRenderDriver.Auto;
                return true;
            case "software":
            case "sw":
                driver = SdlRenderDriver.Software;
                return true;
            case "direct3d":
            case "d3d":
                driver = SdlRenderDriver.Direct3D;
                return true;
            case "direct3d11":
            case "d3d11":
            case "dx11":
                driver = SdlRenderDriver.Direct3D11;
                return true;
            case "direct3d12":
            case "d3d12":
            case "dx12":
                driver = SdlRenderDriver.Direct3D12;
                return true;
            case "opengl":
            case "gl":
                driver = SdlRenderDriver.OpenGL;
                return true;
            case "vulkan":
            case "vk":
                driver = SdlRenderDriver.Vulkan;
                return true;
            case "gpu":
            case "hardware":
            case "hw":
                driver = SdlRenderDriver.Gpu;
                return true;
            default:
                driver = default;
                return false;
        }
    }
}
