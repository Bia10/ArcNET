using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IHandleBackend
{
    LivePlayerLocatorResult LocatePlayers(int processId);

    LiveObjectIdentity InspectHandle(int processId, ulong handle);
}
