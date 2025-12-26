using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TestOpenTelemetry.Services;

public class DownstreamGraphQLClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DownstreamGraphQLClient> _logger;

    public DownstreamGraphQLClient(IHttpClientFactory httpClientFactory, ILogger<DownstreamGraphQLClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<T?> PostAsync<T>(string clientName, string query, object? variables, CancellationToken cancellationToken) where T : class
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        request.Content = JsonContent.Create(new { query, variables });

        PropagateTraceContext(request);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Downstream GraphQL call failed client={ClientName} status={StatusCode}", clientName, response.StatusCode);
            return default;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<GraphQLResponse<T>>(raw, SerializerOptions);

        if (payload?.Errors is { Count: > 0 })
        {
            _logger.LogWarning("Downstream GraphQL returned errors client={ClientName} errorCount={ErrorCount} raw={Raw}", clientName, payload.Errors.Count, raw);
            var joined = string.Join("; ", payload.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Downstream GraphQL returned errors for {clientName}: {joined}");
        }

        if (payload?.Data == null)
        {
            _logger.LogWarning("Downstream GraphQL returned null data client={ClientName} raw={Raw}", clientName, raw);
            throw new InvalidOperationException($"Downstream GraphQL returned null data for {clientName}");
        }

        return payload?.Data;
    }

    private static void PropagateTraceContext(HttpRequestMessage request)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        if (!request.Headers.Contains("traceparent"))
        {
            request.Headers.TryAddWithoutValidation("traceparent", activity.Id);
        }

        if (activity.TraceStateString != null && !request.Headers.Contains("tracestate"))
        {
            request.Headers.TryAddWithoutValidation("tracestate", activity.TraceStateString);
        }
    }

    private sealed class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public List<GraphQLError> Errors { get; set; } = new();
    }

    private sealed class GraphQLError
    {
        public string? Message { get; set; }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
