using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct SymbolInfoNative
{
    internal uint SizeOfStruct;
    internal uint TypeIndex;
    internal ulong Reserved1;
    internal ulong Reserved2;
    internal uint Index;
    internal uint Size;
    internal ulong ModBase;
    internal uint Flags;
    internal ulong Value;
    internal ulong Address;
    internal uint Register;
    internal uint Scope;
    internal uint Tag;
    internal uint NameLen;
    internal uint MaxNameLen;
    internal byte Name;
}
