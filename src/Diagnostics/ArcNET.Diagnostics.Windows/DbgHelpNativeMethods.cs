using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

internal static partial class DbgHelpNativeMethods
{
    [LibraryImport("Dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool MiniDumpWriteDump(
        nint processHandle,
        int processId,
        nint fileHandle,
        MiniDumpType dumpType,
        nint exceptionParam,
        nint userStreamParam,
        nint callbackParam
    );

    [LibraryImport(
        "Dbghelp.dll",
        EntryPoint = "SymInitializeW",
        StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SymInitialize(
        nint processHandle,
        string? userSearchPath,
        [MarshalAs(UnmanagedType.Bool)] bool invadeProcess
    );

    [LibraryImport("Dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SymCleanup(nint processHandle);

    [LibraryImport("Dbghelp.dll", SetLastError = true)]
    internal static partial DbgHelpSymbolOptions SymSetOptions(DbgHelpSymbolOptions symOptions);

    [LibraryImport(
        "Dbghelp.dll",
        EntryPoint = "SymLoadModuleExW",
        StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true
    )]
    internal static partial ulong SymLoadModuleEx(
        nint processHandle,
        nint fileHandle,
        string imageName,
        string? moduleName,
        ulong baseOfDll,
        uint dllSize,
        nint data,
        uint flags
    );

    [LibraryImport("Dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SymEnumSymbols(
        nint processHandle,
        ulong baseOfDll,
        [MarshalAs(UnmanagedType.LPStr)] string mask,
        SymEnumSymbolsProc callback,
        nint userContext
    );
}
