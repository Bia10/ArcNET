namespace ArcNET.Diagnostics.Windows;

[Flags]
internal enum MemoryProtection : uint
{
    ReadWrite = 0x04,
    ExecuteReadWrite = 0x40,
}
