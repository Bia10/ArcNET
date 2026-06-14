namespace ArcNET.Diagnostics;

public interface IInterceptBackend
{
    IInterceptSession StartIntercept(int processId, RuntimeInterceptionDefinition definition);
}
