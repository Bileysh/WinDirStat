using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeAppLogger : IAppLogger
{
    public void Debug(string messageTemplate, params object?[] args)
    {
    }

    public void Information(string messageTemplate, params object?[] args)
    {
    }

    public void Warning(string messageTemplate, params object?[] args)
    {
    }

    public void Warning(Exception exception, string messageTemplate, params object?[] args)
    {
    }

    public void Error(Exception exception, string messageTemplate, params object?[] args)
    {
    }
}
