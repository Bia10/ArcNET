namespace ArcNET.App.Output;

internal interface IEditorOutput
{
    void WriteLine(string value);

    void WriteError(string value);
}
