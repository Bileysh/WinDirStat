using Serilog;

namespace WinDirStat_App.Services;

public static class AppLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WinDirStat_logs");

    public static void Initialize(string processRole)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("ProcessRole", processRole)
            .WriteTo.File(
                Path.Combine(LogDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate:
                "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{ProcessRole}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "AppDomain unhandled exception (IsTerminating={IsTerminating})",
                e.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        Log.Information("=== {ProcessRole} process started (PID {Pid}) ===", processRole, Environment.ProcessId);
    }

    public static void Shutdown() => Log.CloseAndFlush();
}