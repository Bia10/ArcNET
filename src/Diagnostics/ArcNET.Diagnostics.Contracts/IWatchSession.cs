namespace ArcNET.Diagnostics;

public interface IWatchSession : IDisposable
{
    RuntimeWatchReadResult ReadSince(uint lastSequence);
}
