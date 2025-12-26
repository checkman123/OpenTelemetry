# TestOpenTelemetry (GraphQL gateway + Serilog + OpenTelemetry)

ASP.NET Core 8 host (`TestOpenTelemetry`) exposing two GraphQL schemas:
- **InventoryService** (class library): inventory domain, in-memory repository, Kafka producer/consumer (`inventory-events`), GraphQL Query/Mutation/Subscription.
- **UserService** (class library): user domain, in-memory repository, validation, GraphQL Query/Mutation.
- **Observability stack** (docker-compose): Kafka + Zookeeper, OpenTelemetry Collector, Prometheus, Tempo, Loki, Grafana.

## Logging (Serilog)
- Serilog is the primary logger (structured JSON to console + rolling file `logs/testopentelemetry-log-.json`, 14-day retention).
- Enrichment: `service.name`, `service.version`, `environment`, `RequestId`, `ConnectionId`, Activity `trace_id` / `span_id` for log/trace correlation.
- Request logging middleware adjusts level: 4xx→Warning, 5xx/exception→Error; includes method/path/status/elapsed ms.
- Tuning: `Serilog:MinimumLevel` overrides (e.g., `Microsoft`, `HotChocolate`) in `appsettings*.json` or env vars.
- GraphQL operation logging: one log per operation (query/mutation) with operationType, operationName, success/failure, errorCount, trace/span IDs; introspection logged at Debug only; document hash logged instead of full query (no custom duration timing).
- Repository Debug logging: all repository methods emit Debug logs with module tags (`module=inventory`, `module=users`) and durationMs. Enable by setting Debug levels (e.g., `appsettings.Development.json` already raises default to Debug).

## Running the system
1) Start infra (Kafka + OTel + Grafana):
   ```bash
   docker compose up -d
   ```
2) Run the host:
   ```bash
   dotnet run --project src/TestOpenTelemetry
   ```
   - OTel endpoint override: `OpenTelemetry__Endpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT` (default `http://localhost:4317`).
   - Kafka config for inventory: `Kafka__BootstrapServers` (default `localhost:29092`), `Kafka__Topic` (`inventory-events`), `Kafka__GroupId` (`inventory-service`).
   - Environment/URLs: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS` (default Kestrel ports).
3) Health check: `GET http://localhost:5000/healthz` (adjust if URLs differ).

## Endpoints (host)
- GraphQL (single gateway schema): `http://localhost:5000/graphql`
- Banana Cake Pop UI (Development only): `http://localhost:5000/graphql/ui`
- Health: `http://localhost:5000/healthz`

## Example GraphQL — Inventory (via /graphql)
```graphql
# Query all items
query {
  inventoryItems {
    id
    name
    quantity
    createdAt
  }
}

# Query by id
query ($id: ID!) {
  inventoryItemById(id: $id) {
    id
    name
    quantity
  }
}

# Add an item (persists in memory, publishes to Kafka)
mutation {
  addInventoryItem(name: "Widget", quantity: 10) {
    id
    name
    quantity
  }
}

# Subscription (receive new items as they are added)
subscription {
  inventoryItemAdded {
    id
    name
    quantity
  }
}
```

## Example GraphQL — Users (via /graphql)
```graphql
# Query all users
query {
  users {
    id
    name
    email
    createdAt
  }
}

# Query by id
query ($id: ID!) {
  userById(id: $id) {
    id
    name
    email
  }
}

# Add user (logs + traces with correlation)
mutation {
  addUser(name: "Ada", email: "ada@example.com") {
    id
    name
    email
  }
}
```

## Observability
- Grafana: http://localhost:3000 (admin/admin, anonymous enabled)
- Tempo (traces), Prometheus (metrics), Loki (logs) pre-provisioned.
- OpenTelemetry Collector: `4317` (gRPC) / `4318` (HTTP) OTLP, Prometheus scrape `9464`.
- Kafka: inside compose `kafka:9092`; host listener `localhost:29092`.
- GraphQL operation logs in Loki: filter `{service.name="test-opentelemetry"} |= "GraphQL operation completed"`; for errors add `|= "success=False"`; introspection logs are Debug.
- Repository Debug logs in Loki: filter `{service.name="test-opentelemetry"} |= "module=inventory"` or `|= "module=users"`; look for `durationMs>` patterns to spot slower calls.
- Logs are exported to the OTel Collector via the Serilog OpenTelemetry sink (OTLP to `http://localhost:4317`) and forwarded to Loki. Ensure the stack is up (`docker compose up -d`) before running the service so logs appear in Grafana.

Verification tips:
- **Traces**: Grafana → Tempo, filter `service.name="test-opentelemetry"`; spans include GraphQL + Kafka producer/consumer + ASP.NET Core.
- **Metrics**: Grafana → Prometheus (e.g., `http_server_request_duration_seconds_count`).
- **Logs**: Grafana → Loki, query `{service.name="test-opentelemetry"}`; logs carry `trace_id`/`span_id` for correlation.
- **Correlation**: Copy a `trace_id` from logs (or Tempo) and use it to filter in Loki or Tempo.

## Project layout
- `src/TestOpenTelemetry/` – ASP.NET Core host (Serilog, OTel, multi-schema GraphQL endpoints, health).
- `src/InventoryService/` – class library for inventory domain + GraphQL (Queries/Mutations) + Kafka.
- `src/UserService/` – class library for user domain + GraphQL (Queries/Mutations).
- `src/Shared/Logging/` – Serilog helpers and Activity enricher.
- `docker-compose.yml`, `otel-collector-config.yaml`, `prometheus.yml`, `tempo.yaml`, `loki-config.yaml`, `grafana/provisioning/datasources/` – infra stack.
- Solution file: `TestOpenTelemetry.sln` (startup: TestOpenTelemetry).

---
Changes summary
- Projects/files added or changed: InventoryService (library), UserService (library), new host `TestOpenTelemetry`, shared Serilog helpers, README updates, solution updated.
- Run: `docker compose up -d` then `dotnet run --project src/TestOpenTelemetry`.
- Example GraphQL calls: see Inventory and Users sections above.
- Confirm observability: Grafana → Loki for logs (`{service.name="test-opentelemetry"}`), Tempo for traces (`service.name="test-opentelemetry"`), Prometheus for metrics; correlate via `trace_id`.
