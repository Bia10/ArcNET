using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace ArcNET.Utilities
{
    public class ProcessLauncher
    {
        public static string ExePath;
        public string CmdArgs;
        //private static readonly Version ExeVersion = Version.Parse(FileVersionInfo.GetVersionInfo(_exePath).FileVersion
        //?? throw new InvalidOperationException("Version unknown."));

        public struct CmdArguments
        {
            public const string InstallHighRes = "--nogame Files/HighRes.tp2 --yes --reinstall";
            public const string UninstallHighRes = "--nogame Files/HighRes.tp2 --uninstall";
        }

        public ProcessLauncher(string exePath, string cmdArgs)
        {
            ExePath = exePath;
            CmdArgs = cmdArgs;
        }

        public void Launch()
        {
            AnsiConsoleExtensions.Log($"Attempt to start:\n {ExePath} \n with args:\n {CmdArgs}", "info");

            var exeFile = new FileInfo(ExePath);
            var workDir = exeFile.DirectoryName;
            if (workDir == null)
            {
                AnsiConsoleExtensions.Log($"Work directory null, cannot launch!", "error");
                return;
            }

            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = ExePath,
                        Arguments = CmdArgs,
                        WorkingDirectory = workDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.OutputDataReceived += OutputHandler;
                process.ErrorDataReceived += ErrorHandler;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            AnsiConsoleExtensions.Log(outLine.Data,"info");
        }
        private static void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            AnsiConsoleExtensions.Log(outLine.Data, "error");
        }
    }
}