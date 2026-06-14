using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct UnicodeStringNative
{
    public ushort Length;
    public ushort MaximumLength;
    public nint Buffer;
}
