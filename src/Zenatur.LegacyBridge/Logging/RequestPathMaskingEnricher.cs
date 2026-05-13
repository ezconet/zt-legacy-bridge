using Serilog.Core;
using Serilog.Events;
using Zenatur.LegacyBridge.Application.Common;

namespace Zenatur.LegacyBridge.Logging;

public sealed class RequestPathMaskingEnricher : ILogEventEnricher
{
    private static readonly string[] PathProperties = { "RequestPath", "Path" };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf)
    {
        foreach (var name in PathProperties)
        {
            if (logEvent.Properties.TryGetValue(name, out var prop)
                && prop is ScalarValue { Value: string raw })
            {
                logEvent.AddOrUpdateProperty(pf.CreateProperty(name, PiiMask.Path(raw)));
            }
        }
    }
}
