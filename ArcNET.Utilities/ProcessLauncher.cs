using System;
using System.Diagnostics;
using Spectre.Console;

namespace ArcNET.Utilities
{
    public class ProcessLauncher
    {
        private static string _exePath;
        private static string _cmdArgs;
        //private static readonly Version ExeVersion = Version.Parse(FileVersionInfo.GetVersionInfo(_exePath).FileVersion
        //?? throw new InvalidOperationException("Version unknown."));

        public struct CmdArguments
        {
            public const string InstallHighRes = "--nogame Files/HighRes.tp2 --yes --reinstall";
            public const string UninstallHighRes = "--nogame Files/HighRes.tp2 --uninstall";
        }

        public ProcessLauncher(string exePath, string cmdArgs)
        {
            _exePath = exePath;
            _cmdArgs = cmdArgs;
        }

        public void Launch()
        {
            AnsiConsoleExtensions.Log($"Attempt to start:\n {_exePath} \n with args:\n {_cmdArgs}", "info");
            try
            {
               //using var exeProcess = Process.Start(_exePath, _cmdArguments);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }
    }
}