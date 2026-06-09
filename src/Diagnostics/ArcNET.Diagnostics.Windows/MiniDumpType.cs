namespace ArcNET.Diagnostics.Windows;

[Flags]
internal enum MiniDumpType : uint
{
    Normal = 0x00000000,
    WithDataSegs = 0x00000001,
    WithFullMemory = 0x00000002,
    WithHandleData = 0x00000004,
    WithUnloadedModules = 0x00000020,
    WithIndirectlyReferencedMemory = 0x00000040,
    WithThreadInfo = 0x00001000,
}
