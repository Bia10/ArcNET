namespace ArcNET.Diagnostics.Windows;

public interface ISessionConnection : IDisposable
{
    int ProcessId { get; }

    string ProcessName { get; }

    string ModulePath { get; }

    nint ModuleBase { get; }

    int ModuleSize { get; }

    bool HasExited { get; }
}
