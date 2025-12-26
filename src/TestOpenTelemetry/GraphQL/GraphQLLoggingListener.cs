using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TestOpenTelemetry.GraphQL;

public class GraphQLLoggingListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLLoggingListener> _logger;

    public GraphQLLoggingListener(ILogger<GraphQLLoggingListener> logger)
    {
        _logger = logger;
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var activity = Activity.Current;

        return new Scope(_logger, context, activity);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IRequestContext _context;
        private readonly Activity? _activity;

        public Scope(ILogger logger, IRequestContext context, Activity? activity)
        {
            _logger = logger;
            _context = context;
            _activity = activity;
        }

        public void Dispose()
        {
            var operationName = _context.Request.OperationName
                               ?? _context.Operation?.Name
                               ?? "anonymous";
            var operationType = _context.Operation?.Type.ToString() ?? "unknown";
            var result = _context.Result as IQueryResult;
            var errorCount = result?.Errors?.Count ?? 0;
            var success = errorCount == 0 && _context.Exception is null;
            var documentHash = _context.DocumentId?.ToString() ?? "none";

            var isIntrospection = string.Equals(operationName, "IntrospectionQuery", StringComparison.OrdinalIgnoreCase);

            var level = isIntrospection ? LogLevel.Debug : (success ? LogLevel.Information : LogLevel.Warning);

            if (!_logger.IsEnabled(level))
            {
                return;
            }

            _logger.Log(level,
                "GraphQL operation completed {OperationType} {OperationName} success={Success} errors={ErrorCount} documentHash={DocumentHash} traceId={TraceId} spanId={SpanId}",
                operationType,
                operationName,
                success,
                errorCount,
                documentHash,
                _activity?.TraceId.ToString(),
                _activity?.SpanId.ToString());
        }
    }
}
