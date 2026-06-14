namespace ArcNET.GameData.Workspace.Tests;

internal sealed class TemporaryDirectory(string rootPath) : IDisposable
{
    public string RootPath { get; } = rootPath;

    public static TemporaryDirectory Create()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "ArcNET.GameData.Workspace.Tests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(rootPath);
        return new TemporaryDirectory(rootPath);
    }

    public string CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public string CreateFile(string relativePath) => CreateFile(relativePath, []);

    public string CreateFile(string relativePath, ReadOnlySpan<byte> bytes)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, bytes.ToArray());
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
    }
}
