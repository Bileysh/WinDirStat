using System.Diagnostics;
using Serilog;

namespace Volumetric_App;

internal static class StartupTimeline
{
    private static readonly DateTime ProcessStart = Process.GetCurrentProcess().StartTime;

    public static void Mark(string phase) =>
        Log.Information("Startup: {Phase} at {ElapsedMs} ms after process start", phase,
            (int)(DateTime.Now - ProcessStart).TotalMilliseconds);
}
