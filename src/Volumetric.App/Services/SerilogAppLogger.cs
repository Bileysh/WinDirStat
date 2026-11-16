using Serilog;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class SerilogAppLogger : IAppLogger
{
    public void Debug(string messageTemplate, params object?[] args) => Log.Debug(messageTemplate, args);

    public void Information(string messageTemplate, params object?[] args) => Log.Information(messageTemplate, args);

    public void Warning(string messageTemplate, params object?[] args) => Log.Warning(messageTemplate, args);

    public void Warning(Exception exception, string messageTemplate, params object?[] args) =>
        Log.Warning(exception, messageTemplate, args);

    public void Error(Exception exception, string messageTemplate, params object?[] args) =>
        Log.Error(exception, messageTemplate, args);
}
