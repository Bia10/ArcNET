namespace ArcNET.Archive;

internal static class DatArchivePath
{
    public static string Normalize(string virtualPath) => virtualPath.Replace('/', '\\');
}
