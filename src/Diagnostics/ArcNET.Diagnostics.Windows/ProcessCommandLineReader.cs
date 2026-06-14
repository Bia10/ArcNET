using System.Runtime.InteropServices;

namespace ArcNET.Diagnostics.Windows;

internal static class ProcessCommandLineReader
{
    public static bool TryRead(int processId, out string? commandLine)
    {
        commandLine = null;

        var processHandle = Kernel32NativeMethods.OpenProcess(ProcessAccess.QueryInformation, false, processId);
        if (processHandle == 0)
            return false;

        try
        {
            return TryRead(processHandle, out commandLine);
        }
        finally
        {
            _ = Kernel32NativeMethods.CloseHandle(processHandle);
        }
    }

    private static bool TryRead(nint processHandle, out string? commandLine)
    {
        commandLine = null;

        var status = NtDllNativeMethods.NtQueryInformationProcess(
            processHandle,
            ProcessInformationClass.ProcessCommandLineInformation,
            0,
            0,
            out var returnLength
        );
        if (status < 0 && returnLength <= 0)
            return false;

        var bufferLength = Math.Max(returnLength, MinimumBufferLength);
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var buffer = Marshal.AllocHGlobal(bufferLength);
            try
            {
                status = NtDllNativeMethods.NtQueryInformationProcess(
                    processHandle,
                    ProcessInformationClass.ProcessCommandLineInformation,
                    buffer,
                    bufferLength,
                    out returnLength
                );
                if (status == StatusInfoLengthMismatch && returnLength > bufferLength)
                {
                    bufferLength = returnLength;
                    continue;
                }

                if (status < 0)
                    return false;

                var commandLineText = Marshal.PtrToStructure<UnicodeStringNative>(buffer);
                if (commandLineText.Length == 0 || commandLineText.Buffer == 0)
                {
                    commandLine = string.Empty;
                    return true;
                }

                commandLine = Marshal.PtrToStringUni(commandLineText.Buffer, commandLineText.Length / sizeof(char));
                return commandLine is not null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return false;
    }

    private const int MinimumBufferLength = 1024;
    private const int MaxAttempts = 3;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
}
