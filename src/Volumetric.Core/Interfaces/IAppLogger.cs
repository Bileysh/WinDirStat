namespace Volumetric.Core.Interfaces;

public interface IAppLogger
{
    void Debug(string messageTemplate, params object?[] args);
    void Information(string messageTemplate, params object?[] args);
    void Warning(string messageTemplate, params object?[] args);
    void Warning(Exception exception, string messageTemplate, params object?[] args);
    void Error(Exception exception, string messageTemplate, params object?[] args);
}
