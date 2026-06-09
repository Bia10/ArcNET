using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

public interface IInterceptBackend
{
    IInterceptSession StartIntercept(int processId, RuntimeInterceptionDefinition definition);
}
