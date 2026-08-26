#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Reporting.NETCore;

internal sealed class WindowsRestrictedProcess : IDisposable
{
    private readonly SafeFileHandle input;
    private readonly SafeFileHandle output;
    private readonly SafeFileHandle error;

    private WindowsRestrictedProcess(Process process, SafeFileHandle input, SafeFileHandle output, SafeFileHandle error)
    {
        Process = process;
        this.input = input;
        this.output = output;
        this.error = error;
        StandardInput = new StreamWriter(new FileStream(input, FileAccess.Write, 4096, false), Console.InputEncoding) { AutoFlush = true };
        StandardOutput = new StreamReader(new FileStream(output, FileAccess.Read, 4096, false), Console.OutputEncoding);
        StandardError = new StreamReader(new FileStream(error, FileAccess.Read, 4096, false), Console.OutputEncoding);
    }

    public Process Process { get; }
    public StreamWriter StandardInput { get; }
    public StreamReader StandardOutput { get; }
    public StreamReader StandardError { get; }

    public static WindowsRestrictedProcess Start(ProcessStartInfo start)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (start.FileName.Length == 0 || !Path.IsPathFullyQualified(start.FileName))
            throw new ReportWorkerSecurityException("The report worker executable must be an absolute path.");

        CreatePipe(out var childInput, out var parentInput);
        CreatePipe(out var parentOutput, out var childOutput);
        CreatePipe(out var parentError, out var childError);
        try
        {
            SetHandleInformation(parentInput, 1, 0);
            SetHandleInformation(parentOutput, 1, 0);
            SetHandleInformation(parentError, 1, 0);
            using var token = CreateRestrictedToken();
            var environment = CreateEnvironment(token.DangerousGetHandle());
            var commandLine = BuildCommandLine(start);
            var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>(), dwFlags = 0x100, hStdInput = childInput, hStdOutput = childOutput, hStdError = childError };
            if (!CreateProcessAsUser(token.DangerousGetHandle(), start.FileName, commandLine, IntPtr.Zero, IntPtr.Zero, true,
                0x00000400 | 0x00000010 | 0x00000200, environment, start.WorkingDirectory, ref startup, out var processInfo))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The restricted report-worker process could not be started.");
            CloseHandle(processInfo.hThread);
            CloseHandle(childInput);
            CloseHandle(childOutput);
            CloseHandle(childError);
            DestroyEnvironmentBlock(environment);
            var process = Process.GetProcessById(processInfo.dwProcessId);
            CloseHandle(processInfo.hProcess);
            return new WindowsRestrictedProcess(process, new SafeFileHandle(parentInput, true), new SafeFileHandle(parentOutput, true), new SafeFileHandle(parentError, true));
        }
        catch
        {
            CloseHandle(childInput); CloseHandle(parentInput); CloseHandle(childOutput); CloseHandle(parentOutput); CloseHandle(childError); CloseHandle(parentError);
            throw;
        }
    }

    private static SafeAccessTokenHandle CreateRestrictedToken()
    {
        if (!OpenProcessToken(GetCurrentProcess(), 0x0002 | 0x0008 | 0x0001 | 0x0004 | 0x0020, out var source))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "The current Windows token could not be opened.");
        using (source)
        {
            if (!CreateRestrictedTokenNative(source.DangerousGetHandle(), 1, 0, IntPtr.Zero, 0, IntPtr.Zero, 0, IntPtr.Zero, out var restricted))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "A restricted Windows token could not be created.");
            return new SafeAccessTokenHandle(restricted);
        }
    }

    private static IntPtr CreateEnvironment(IntPtr token)
    {
        if (!CreateEnvironmentBlock(out var environment, token, false))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "A restricted worker environment could not be created.");
        return environment;
    }

    private static string BuildCommandLine(ProcessStartInfo start)
    {
        var command = Quote(start.FileName);
        foreach (var argument in start.ArgumentList) command += " " + Quote(argument);
        return command;
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    public void Dispose()
    {
        StandardInput.Dispose(); StandardOutput.Dispose(); StandardError.Dispose();
        input.Dispose(); output.Dispose(); error.Dispose();
        Process.Dispose();
    }

    private static void CreatePipe(out IntPtr read, out IntPtr write)
    {
        if (!CreatePipeNative(out read, out write, IntPtr.Zero, 0)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Worker pipe creation failed.");
    }

    [DllImport("kernel32.dll", EntryPoint = "CreatePipe", SetLastError = true)] private static extern bool CreatePipeNative(out IntPtr read, out IntPtr write, IntPtr attributes, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", EntryPoint = "CreateRestrictedToken", SetLastError = true)] private static extern bool CreateRestrictedTokenNative(IntPtr existing, uint flags, uint disableSidCount, IntPtr disableSids, uint deletePrivilegeCount, IntPtr deletePrivileges, uint restrictSidCount, IntPtr restrictSids, out IntPtr token);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateProcessAsUser(IntPtr token, string application, string commandLine, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string? directory, ref STARTUPINFO startup, out PROCESS_INFORMATION processInfo);
    [DllImport("userenv.dll", SetLastError = true)] private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);
    [DllImport("userenv.dll", SetLastError = true)] private static extern bool DestroyEnvironmentBlock(IntPtr environment);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct STARTUPINFO { public int cb; public string? lpReserved; public string? lpDesktop; public string? lpTitle; public int dwX; public int dwY; public int dwXSize; public int dwYSize; public int dwXCountChars; public int dwYCountChars; public int dwFillAttribute; public int dwFlags; public short wShowWindow; public short cbReserved2; public IntPtr lpReserved2; public IntPtr hStdInput; public IntPtr hStdOutput; public IntPtr hStdError; }
    [StructLayout(LayoutKind.Sequential)] private struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }
}
