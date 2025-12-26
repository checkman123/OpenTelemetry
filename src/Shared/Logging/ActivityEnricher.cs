using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Logging;

public class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(activity.TraceId.ToString()))
        {
            var traceId = propertyFactory.CreateProperty("trace_id", activity.TraceId.ToString());
            logEvent.AddPropertyIfAbsent(traceId);
        }

        if (!string.IsNullOrWhiteSpace(activity.SpanId.ToString()))
        {
            var spanId = propertyFactory.CreateProperty("span_id", activity.SpanId.ToString());
            logEvent.AddPropertyIfAbsent(spanId);
        }
    }
}
