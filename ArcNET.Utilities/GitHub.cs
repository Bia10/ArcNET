using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace ArcNET.Utilities
{
    public static class GitHub
    {
        private static void Copy(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir)) 
                Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        private static void DeleteReadOnly(this FileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo is DirectoryInfo directoryInfo)
            {
                foreach (var childInfo in directoryInfo.GetFileSystemInfos())
                    childInfo.DeleteReadOnly();
            }

            fileSystemInfo.Attributes = FileAttributes.Normal;
            fileSystemInfo.Delete();
        }

        public static void CloneHighResPatch(string resultLocation)
        {
            const string repo = "https://github.com/ArcNET-Modding/HighResPatch.git";
            AnsiConsoleExtensions.Log($"Cloning repo: {repo} into: \n {resultLocation}", "info");
            Repository.Clone(repo, resultLocation);
            var files = Directory.EnumerateFiles(resultLocation, "*.*", SearchOption.AllDirectories).ToList();
            if (files.Count == 0) return;

            AnsiConsoleExtensions.Log($"Cloned {files.Count} files", "success");
            var highResPath = Path.Combine(resultLocation, "HighRes");
            Copy(highResPath, resultLocation);

            var foldersToDelete = new DirectoryInfo(highResPath);
            AnsiConsoleExtensions.Log($"Removing empty folder: {foldersToDelete}", "info");
            if (foldersToDelete.Exists)
                DeleteReadOnly(foldersToDelete);
        }
    }
}