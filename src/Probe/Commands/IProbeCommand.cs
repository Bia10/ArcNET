namespace Probe.Commands;

internal interface IProbeCommand
{
    Task RunAsync(string saveDir, string[] args);
}
