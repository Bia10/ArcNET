using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

public interface IHandleBackend
{
    LivePlayerLocatorResult LocatePlayers(int processId);

    LiveObjectIdentity InspectHandle(int processId, ulong handle);
}
