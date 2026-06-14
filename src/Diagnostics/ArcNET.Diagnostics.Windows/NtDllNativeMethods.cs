using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

internal static partial class NtDllNativeMethods
{
    [LibraryImport("ntdll.dll")]
    internal static partial int NtQueryInformationProcess(
        nint processHandle,
        ProcessInformationClass processInformationClass,
        nint processInformation,
        int processInformationLength,
        out int returnLength
    );
}
