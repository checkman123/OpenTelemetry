# Inventory Service (GraphQL + Kafka + OpenTelemetry)

Minimal ASP.NET Core 8.0 microservice that exposes a Hot Chocolate GraphQL API, publishes/consumes Kafka events, and ships traces/metrics/logs via OpenTelemetry to a local Grafana stack.

## Prerequisites
- .NET 8 SDK
- Docker & Docker Compose

## Quick start
1) Start observability + Kafka stack:
   ```bash
   docker compose up -d
   ```
2) Run the service locally:
   ```bash
   cd src/InventoryService
   dotnet restore
   dotnet run
   ```
   - Kafka configuration via env vars: `Kafka__BootstrapServers` (default `localhost:9092`), `Kafka__Topic` (default `inventory-events`), `Kafka__GroupId` (default `inventory-service`).
   - OpenTelemetry OTLP endpoint override: `OpenTelemetry__Endpoint` (default `http://localhost:4317`).
3) Health check: `GET http://localhost:5000/healthz` (or https on 5001).

## GraphQL
- Endpoint: `http://localhost:5000/graphql`
- Subscriptions (WebSocket): `ws://localhost:5000/graphql`
- Banana Cake Pop UI: `http://localhost:5000/graphql/ui`

Example operations:
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

## Observability & Grafana
- Grafana: http://localhost:3000 (admin/admin, anonymous enabled)
- Tempo (traces): pre-provisioned Grafana datasource; OTLP collector exports traces here.
- Prometheus (metrics): http://localhost:9090, also wired into Grafana.
- Loki (logs): http://localhost:3100, pre-provisioned in Grafana.
- OpenTelemetry Collector: exposed on `4317` (gRPC) / `4318` (HTTP) for OTLP, Prometheus scrape endpoint on `9464`.

Suggested Grafana views:
- **Explore → Tempo**: search service `inventory-service` to see spans (ASP.NET Core, GraphQL, Kafka producer/consumer).
- **Explore → Prometheus**: query metrics (e.g., `http_server_request_duration_seconds_count`).
- **Explore → Loki**: filter by `{service_name="inventory-service"}` for structured logs.

## Project layout
- `src/InventoryService/Program.cs` – minimal hosting, GraphQL + health endpoint wiring.
- `src/InventoryService/GraphQL/` – Query, Mutation, Subscription.
- `src/InventoryService/Kafka/` – producer and background consumer with tracing.
- `src/InventoryService/Observability/` – OpenTelemetry setup and ActivitySource constants.
- `src/InventoryService/Services/` – in-memory repository.
- `docker-compose.yml` – Kafka, Zookeeper, OpenTelemetry Collector, Prometheus, Tempo, Loki, Grafana.
- `otel-collector-config.yaml`, `prometheus.yml`, `tempo.yaml`, `loki-config.yaml`, `grafana/provisioning/datasources/` – infra configs.

## Running in containers (optional)
The compose file only starts infra. To containerize the service, add a Dockerfile and a service entry to `docker-compose.yml` pointing at the built image, reusing the same env vars shown above.
