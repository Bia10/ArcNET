namespace ArcNET.Diagnostics.Windows;

[Flags]
internal enum AllocationType : uint
{
    Commit = 0x1000,
    Reserve = 0x2000,
    Release = 0x8000,
}
