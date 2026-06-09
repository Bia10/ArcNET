using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface ISheetBackend : IHandleBackend
{
    SheetDataSnapshot ReadSheetData(int processId, RuntimeProfileSnapshot runtimeProfile, ulong handle);
}
