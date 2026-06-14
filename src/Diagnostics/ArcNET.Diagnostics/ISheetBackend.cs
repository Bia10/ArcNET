using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface ISheetBackend : IHandleBackend
{
    SheetDataSnapshot ReadSheetData(int processId, RuntimeProfileSnapshot runtimeProfile, ulong handle);
}
