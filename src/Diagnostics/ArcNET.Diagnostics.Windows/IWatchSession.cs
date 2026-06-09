using ArcNET.Diagnostics.Windows;

namespace ArcNET.Diagnostics.Windows;

public interface IWatchSession : IDisposable
{
    RuntimeWatchReadResult ReadSince(uint lastSequence);
}
