namespace ArcNET.Diagnostics.Windows;

[Flags]
internal enum DbgHelpSymbolOptions : uint
{
    Undname = 0x00000002,
    DeferredLoads = 0x00000004,
    LoadLines = 0x00000010,
}
