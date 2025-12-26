# TestOpenTelemetry (GraphQL Gateway + Serilog + OpenTelemetry)

Architecture
- **Gateway** (`TestOpenTelemetry`): ASP.NET Core 8 GraphQL gateway exposing a single schema at `/graphql`, composing downstream Inventory and User services. Banana Cake Pop UI at `/graphql/ui` (Development).
- **InventoryService.Host**: independent ASP.NET Core 8 GraphQL service (`/graphql`), in-memory inventory repo, Kafka producer/consumer (`inventory-events`).
- **UserService.Host**: independent ASP.NET Core 8 GraphQL service (`/graphql`), in-memory user repo.
- **Observability stack**: Kafka + Zookeeper, OpenTelemetry Collector, Prometheus, Tempo, Loki, Grafana (via docker-compose).

## Logging (Serilog)
- Serilog is the primary logger (structured JSON to console + rolling file logs, OTLP via Serilog sink).
- Enrichment: `service.name`, `service.version`, `environment`, `RequestId`, `ConnectionId`, Activity `trace_id` / `span_id` for log/trace correlation.
- Request logging middleware adjusts level: 4xx→Warning, 5xx/exception→Error; includes method/path/status/elapsed ms.
- Tuning: `Serilog:MinimumLevel` overrides (e.g., `Microsoft`, `HotChocolate`) in `appsettings*.json` or env vars.
- GraphQL operation logging (gateway): one log per operation (query/mutation) with operationType, operationName, success/failure, errorCount, trace/span IDs; introspection logged at Debug only; document hash logged instead of full query (no custom duration timing).
- Repository Debug logging: all repository methods emit Debug logs with module tags (`module=inventory`, `module=users`) and durationMs. Enable by setting Debug levels (e.g., `appsettings.Development.json` already raises default to Debug).

## Running the system
1) Start infra (Kafka + OTel + Grafana):
   ```bash
   docker compose up -d
   ```
2) Run services locally (optional; compose also builds/runs them):
   ```bash
   # Gateway
   dotnet run --project src/TestOpenTelemetry
   # Inventory service
   dotnet run --project src/InventoryService.Host --urls http://localhost:6001
   # User service
   dotnet run --project src/UserService.Host --urls http://localhost:6002
   ```
   - Gateway downstream endpoints: `Downstream__InventoryGraphQLEndpoint` (default `http://localhost:6001/graphql`), `Downstream__UserGraphQLEndpoint` (default `http://localhost:6002/graphql`).
   - OTel endpoint override: `OpenTelemetry__Endpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT` (default `http://localhost:4317`).
   - Kafka config for inventory: `Kafka__BootstrapServers` (default `localhost:29092`), `Kafka__Topic` (`inventory-events`), `Kafka__GroupId` (`inventory-service`).
   - Environment/URLs: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`.
3) Health checks:
   - Gateway: `GET http://localhost:5000/healthz`
   - Inventory: `GET http://localhost:6001/healthz`
   - Users: `GET http://localhost:6002/healthz`

## Endpoints
- Gateway GraphQL: `http://localhost:5000/graphql`
- Gateway Banana Cake Pop (Development): `http://localhost:5000/graphql/ui`
- Inventory GraphQL (direct, optional): `http://localhost:6001/graphql`
- Users GraphQL (direct, optional): `http://localhost:6002/graphql`
- Health: see above

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
- Loki 3.x: running `grafana/loki:3.6.3` (single binary, filesystem); requires tsdb storage with schema v13 and removes `shared_store`. Restart Loki after config changes with `docker compose up -d loki`.
- Logs to Loki: OpenTelemetry Collector exports logs to Loki via OTLP HTTP (`/otlp`) using the modern ingestion path; low-cardinality labels promoted for filtering are `service_name`, `environment`, `module`, `operationType`, and `level`. All other properties stay in structured metadata when you expand a log line.
- Log readability: Console sink now writes plain text (`[{Timestamp:HH:mm:ss} {Level:u3}] {Message}`) so Loki shows short lines by default; structured metadata (trace/span IDs, module, operationType/Name, environment, service.name) remains available in Loki’s “structured metadata”/labels for filtering.
- Sample LogQL filters:
  - By module: `{module="inventory"}` or `{module="users"}`
  - By operation type: `{operationType="Query"}` `|= "operationName=inventoryItems"`
  - By level or service: `{level="Warning"}` or `{service_name="test-opentelemetry"}`
- Tempo Drilldown readiness: Tempo (v2.9.0) runs metrics-generator with service-graphs and span-metrics enabled, memberlist ring configured, and generator WAL under `/var/tempo/generator`; this is required for Grafana Traces Drilldown rate()/RED views to work.
- Tempo TraceQL tip: Equality queries should quote strings, e.g., `{resource.service.name="inventory-service"} | rate()` (quoted value is required; unquoted values will parse error).
- GraphQL operation logs in Loki: filter `{service.name="test-opentelemetry"} |= "GraphQL operation completed"`; for errors add `|= "success=False"`; introspection logs are Debug.
- Repository Debug logs in Loki: filter `{service.name="inventory-service"} |= "module=inventory"` or `{service.name="user-service"} |= "module=users"`; look for `durationMs>` patterns to spot slower calls.
- Logs are exported to the OTel Collector via the Serilog OpenTelemetry sink (OTLP to `http://localhost:4317`) and forwarded to Loki. Ensure the stack is up (`docker compose up -d`) before running the service so logs appear in Grafana.
- Distributed tracing verification:
  1. Start compose, run gateway.
  2. Execute a gateway query (e.g., `inventoryItems` or `users`).
  3. In Grafana → Tempo, filter `service.name="test-opentelemetry"` and find the trace; spans from `inventory-service`/`user-service` should appear in the same trace (traceparent propagated).
  4. In Loki, filter by `trace_id="<id>"` to correlate logs across services.

## Project layout
- `src/TestOpenTelemetry/` – ASP.NET Core gateway (Serilog, OTel, downstream GraphQL calls, Banana Cake Pop UI).
- `src/InventoryService.Host/` – inventory GraphQL service host.
- `src/UserService.Host/` – user GraphQL service host.
- `src/InventoryService/` – class library for inventory domain + GraphQL (Queries/Mutations) + Kafka.
- `src/UserService/` – class library for user domain + GraphQL (Queries/Mutations).
- `src/Shared/Logging/` – Serilog helpers and Activity enricher.
- `docker-compose.yml`, `otel-collector-config.yaml`, `prometheus.yml`, `tempo.yaml`, `loki-config.yaml`, `grafana/provisioning/datasources/` – infra stack.
