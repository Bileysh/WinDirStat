using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace Volumetric.UITests.E2E;

public static class WinAppDriverServer
{
    public const int Port = 4723;
    public static readonly Uri Uri = new($"http://127.0.0.1:{Port}");

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

    public static string? ExecutablePath { get; } = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        }
        .Select(root => Path.Combine(root, "Windows Application Driver", "WinAppDriver.exe"))
        .FirstOrDefault(File.Exists);

    public static bool IsDeveloperModeEnabled =>
        Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
            "AllowDevelopmentWithoutDevLicense", 0) is 1;

    public static bool IsListening()
    {
        using var client = new TcpClient();
        try
        {
            return client.ConnectAsync("127.0.0.1", Port).Wait(TimeSpan.FromMilliseconds(300)) && client.Connected;
        }
        catch (AggregateException)
        {
            return false;
        }
    }

    public static Process? EnsureRunning()
    {
        if (IsListening())
        {
            return null;
        }

        if (ExecutablePath is null)
        {
            throw new InvalidOperationException("WinAppDriver.exe is not installed.");
        }

        var output = new StringBuilder();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(ExecutablePath, Port.ToString())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode
            }
        };
        DataReceivedEventHandler append = (_, e) =>
        {
            lock (output)
            {
                output.AppendLine(e.Data);
            }
        };
        process.OutputDataReceived += append;
        process.ErrorDataReceived += append;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < StartupTimeout)
        {
            if (IsListening())
            {
                return process;
            }

            if (process.HasExited)
            {
                process.WaitForExit();
                lock (output)
                {
                    throw new InvalidOperationException(
                        $"WinAppDriver.exe exited with code {process.ExitCode}: {output}");
                }
            }

            Thread.Sleep(200);
        }

        process.Kill();
        throw new TimeoutException($"WinAppDriver.exe did not start listening on port {Port} within {StartupTimeout}.");
    }
}
