namespace ArcNET.Diagnostics;

public static class ModuleAddressFormatter
{
    public static string FormatModuleOffset(string moduleFileName, uint rva) => $"{moduleFileName}+0x{rva:X8}";

    public static string FormatModuleAddress(string moduleFileName, uint rva) =>
        FormatModuleOffset(moduleFileName, rva);
}
