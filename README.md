# Trade Order Service

A production-style trade order processing system built as a portfolio project. Demonstrates end-to-end engineering across API design, messaging, data quality, containerisation, and observability.

---

## Quick Start

```bash
# Start infrastructure
docker compose up -d

# Run database migrations
.\setup.ps1
```

---

## Architecture

```
Dashboard (browser)
    ↓
Auth.Service  ←──────────────────────────────┐
    ↓                                         │ OAuth2 / OIDC
Orders API (/v1)                              │
    ↓              ↓                          │
PostgreSQL      RabbitMQ ──→ Order Worker ───→┘
    ↑               ↑
Outbox (Sync)   Dead-Letter Queue
    ↑
Ingestion Framework
    ↑
Python Data-Quality CLI  ←──→  S3 Bucket
    ↑
MCP Server (4 tools)

Consul  ←── Service Discovery
OpenTelemetry → Prometheus → Grafana
```

---

## Project Structure

The solution follows a layered architecture. Each project has a single responsibility and dependencies only flow inward toward the domain.

```
src/
├── Portfolio.TradeOrderService.Api               # Orders REST API host
├── Portfolio.TradeOrderService.McpServer         # MCP tool server (4 tools)
├── Portfolio.TradeOrderService.ServiceInterfaces # Service contracts
├── Portfolio.TradeOrderService.Services          # Business logic
├── Portfolio.TradeOrderService.Sync              # Outbox → RabbitMQ publisher
├── Portfolio.TradeOrderService.Models            # Domain models and DTOs
├── Portfolio.TradeOrderService.Database          # Migration runner + SQL schema
├── Portfolio.TradeOrderService.DataAccess        # Repositories
├── Portfolio.TradeOrderService.Configuration     # Strongly-typed config classes
└── Portfolio.TradeOrderService.Client            # Typed HTTP client SDK
```

### Project responsibilities

| Project | Responsibility |
|---|---|
| **Api** | ASP.NET Core host. Controllers, middleware, DI registration, health endpoints, rate limiting. Entry point for all HTTP traffic. |
| **McpServer** | Standalone MCP tool server exposing 4 tools: `get_order_status`, `list_recent_orders`, `get_data_quality_report`, `replay_dead_letter_message`. Read-only by default with approval gate and audit log. |
| **ServiceInterfaces** | Contracts only — `IOrderService`, `IIngestionService`, etc. No implementations. Allows the Api to depend on abstractions. |
| **Services** | Implementations of the service interfaces. Contains all business logic: order validation, idempotency checks, status transitions. |
| **Sync** | Background service that reads the outbox table and publishes `OrderPlaced` events to RabbitMQ. Decouples DB writes from message publishing. |
| **Models** | Shared domain models (`Order`, `OutboxMessage`, `IngestionJob`), DTOs, enums, and request/response contracts. No dependencies. |
| **Database** | Migration runner and embedded SQL schema files. Runs all unapplied migrations on startup. Tracks applied migrations in `__migrations` table. |
| **DataAccess** | Repository implementations (`OrderRepository`, `OutboxRepository`, etc.). All SQL lives here. Depends on Models and Database. |
| **Configuration** | Strongly-typed option classes (`DatabaseOptions`, `RabbitMqOptions`, `S3Options`). Bound from `appsettings.json` at startup. |
| **Client** | Typed `HttpClient` wrapper for external consumers of the Orders API. Shares Models so request/response types are consistent. |

### Dependency flow

Dependencies only flow inward — inner layers never reference outer layers.

```
Models          Configuration
    ↓                ↓
Database → DataAccess
              ↓
ServiceInterfaces → Services
                       ↓
    Sync    Client    Api    McpServer
```

---

## Database Migrations

Plain SQL migration files live inside the Database project as embedded resources and run automatically on API startup:

```
src/Portfolio.TradeOrderService.Database/Migrations/
├── 001_create_orders.sql
├── 002_create_outbox.sql
├── 003_create_ingestion_jobs.sql
└── 004_create_audit_log.sql
```

The `MigrationRunner` applies each file in order and records it in a `__migrations` table — already-applied migrations are skipped on subsequent startups.

To start Postgres and run the API (which triggers migrations):

```powershell
.\setup.ps1
dotnet run --project src/Portfolio.TradeOrderService.Api
```

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/v1/orders` | Submit a trade order (requires `orders.write`) |
| `GET` | `/v1/orders/{id}` | Get order by ID (requires `orders.read`) |
| `GET` | `/v1/orders` | List orders with filters and paging (requires `orders.read`) |
| `GET` | `/v1/data-quality/reports` | Data quality report results |
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

Full spec: [`openapi.yaml`](openapi.yaml)

---

## Resume Bullets

| Bullet | Sections |
|---|---|
| Versioned OpenAPI-first REST API with OAuth2, rate limiting, MCP server, dashboard | API + Auth + Dashboard |
| Reusable ingestion framework, pluggable adapters, idempotent loads, job tracking | Ingestion Framework |
| Python data-quality library (3 checks), MCP server (4 tools, read-only, approvals, audit) | Data Quality + MCP |
| Docker, Kubernetes + Helm, S3, EC2, GitHub Actions, PowerShell | Infrastructure |
| OpenTelemetry telemetry, Grafana dashboard, runbook, tested recovery scenarios | Observability |
