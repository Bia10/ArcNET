using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate bool SymEnumSymbolsProc(nint symbolInfo, uint symbolSize, nint userContext);
