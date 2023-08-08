using LibGit2Sharp;
using System.IO;
using System.Linq;
using Utils.Console;
using Utils.FileSystem;

namespace ArcNET.Utilities;

public static class GitHub
{
    public static void CloneHighResPatch(string resultLocation)
    {
        const string repo = "https://github.com/ArcNET-Modding/HighResPatch.git";
        ConsoleExtensions.Log($"Cloning repo: {repo} into: \n {resultLocation}", "info");
        Repository.Clone(repo, resultLocation);
        var files = Directory.EnumerateFiles(resultLocation, "*.*", SearchOption.AllDirectories).ToList();
        if (files.Count == 0) return;

        ConsoleExtensions.Log($"Cloned {files.Count} files", "success");
        string highResPath = Path.Combine(resultLocation, "HighRes");
        new DirectoryInfo(highResPath).CopyTo(resultLocation);

        var foldersToDelete = new DirectoryInfo(highResPath);
        ConsoleExtensions.Log($"Removing empty folder: {foldersToDelete}", "info");
        foldersToDelete.ExistsDelete();
    }
}