namespace ArcNET.Diagnostics.Windows;

[Flags]
internal enum ProcessAccess : uint
{
    VmOperation = 0x0008,
    VmRead = 0x0010,
    VmWrite = 0x0020,
    QueryInformation = 0x0400,
}
