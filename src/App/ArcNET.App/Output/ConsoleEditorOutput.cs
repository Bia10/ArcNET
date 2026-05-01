namespace ArcNET.App.Output;

internal sealed class ConsoleEditorOutput : IEditorOutput
{
    public void WriteLine(string value) => Console.WriteLine(value);

    public void WriteError(string value) => Console.Error.WriteLine(value);
}
