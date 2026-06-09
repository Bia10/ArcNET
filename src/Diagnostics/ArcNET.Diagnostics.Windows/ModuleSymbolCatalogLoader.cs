using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

public static class ModuleSymbolCatalogLoader
{
    public static ModuleSymbolCatalog Load(string modulePath)
    {
        var fullPath = Path.GetFullPath(modulePath);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"Module '{fullPath}' does not exist.", fullPath);

        var cacheKey = $"{fullPath}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
        return s_catalogs.GetOrAdd(cacheKey, _ => BuildCatalog(fullPath));
    }

    private static ModuleSymbolCatalog BuildCatalog(string modulePath)
    {
        lock (s_dbgHelpGate)
        {
            var currentProcessHandle = Process.GetCurrentProcess().Handle;
            DbgHelpNativeMethods.SymSetOptions(
                DbgHelpSymbolOptions.DeferredLoads | DbgHelpSymbolOptions.LoadLines | DbgHelpSymbolOptions.Undname
            );
            if (!DbgHelpNativeMethods.SymInitialize(currentProcessHandle, userSearchPath: null, invadeProcess: false))
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Unable to initialize DbgHelp for '{modulePath}'.");
            }

            try
            {
                var moduleBase = DbgHelpNativeMethods.SymLoadModuleEx(
                    currentProcessHandle,
                    fileHandle: 0,
                    modulePath,
                    moduleName: Path.GetFileNameWithoutExtension(modulePath),
                    baseOfDll: OfflineSymbolBase,
                    dllSize: 0,
                    data: 0,
                    flags: 0
                );
                if (moduleBase == 0)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Unable to load symbols for '{modulePath}'.");
                }

                List<ModuleFunctionSymbol> symbols = [];
                var gcHandle = GCHandle.Alloc(symbols);
                try
                {
                    if (
                        !DbgHelpNativeMethods.SymEnumSymbols(
                            currentProcessHandle,
                            moduleBase,
                            "*",
                            s_enumSymbolsCallback,
                            GCHandle.ToIntPtr(gcHandle)
                        )
                    )
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Unable to enumerate symbols for '{modulePath}'.");
                    }
                }
                finally
                {
                    if (gcHandle.IsAllocated)
                        gcHandle.Free();
                }

                var orderedSymbols = symbols
                    .Where(static symbol => symbol.Rva < int.MaxValue)
                    .OrderBy(static symbol => symbol.Rva)
                    .ToArray();
                return new ModuleSymbolCatalog(modulePath, orderedSymbols);
            }
            finally
            {
                _ = DbgHelpNativeMethods.SymCleanup(currentProcessHandle);
            }
        }
    }

    private static bool OnEnumSymbol(nint symbolInfoAddress, uint _, nint userContext)
    {
        var gcHandle = GCHandle.FromIntPtr(userContext);
        var symbols = (List<ModuleFunctionSymbol>)gcHandle.Target!;
        var symbolInfo = Marshal.PtrToStructure<SymbolInfoNative>(symbolInfoAddress);
        if (
            symbolInfo.Tag != SymTagFunction
            || symbolInfo.Address < OfflineSymbolBase
            || symbolInfo.ModBase != OfflineSymbolBase
        )
        {
            return true;
        }

        var rva = checked((uint)(symbolInfo.Address - OfflineSymbolBase));
        var name = ReadSymbolName(symbolInfoAddress, symbolInfo.NameLen);
        if (name.Length == 0)
            return true;

        symbols.Add(new ModuleFunctionSymbol(name, rva, symbolInfo.Size));
        return true;
    }

    private static string ReadSymbolName(nint symbolInfoAddress, uint nameLength)
    {
        if (nameLength == 0)
            return string.Empty;

        var nameOffset = Marshal.OffsetOf<SymbolInfoNative>(nameof(SymbolInfoNative.Name)).ToInt32();
        var buffer = new byte[nameLength];
        Marshal.Copy(symbolInfoAddress + nameOffset, buffer, 0, checked((int)nameLength));
        return Encoding.ASCII.GetString(buffer);
    }

    private static readonly object s_dbgHelpGate = new();
    private static readonly ConcurrentDictionary<string, ModuleSymbolCatalog> s_catalogs = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly SymEnumSymbolsProc s_enumSymbolsCallback = OnEnumSymbol;

    private const ulong OfflineSymbolBase = 0x10000000;
    private const uint SymTagFunction = 5;
}
