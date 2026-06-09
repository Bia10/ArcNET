namespace ArcNET.Diagnostics.Windows;

public interface IProcessMemory
{
    nint ResolveRva(int rva);

    int ReadInt32(nint address);

    nint ReadPointer32(nint address);

    void ReadBytes(nint address, byte[] buffer);
}
