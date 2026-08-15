# Fraud Detection API

A production-grade transaction anti-fraud system built with **.NET 8** using **Hexagonal Architecture** + **Vertical Slice** + **Explicit CQRS**, with **Kafka**-based asynchronous evaluation.

Every financial transaction is created through the API (state: `pending`), validated **asynchronously** by an anti-fraud microservice (a dedicated Kafka worker), and ends in `approved` or `rejected`.

## Goal

Implements the real technical challenge: every created transaction must be validated by an anti-fraud microservice that sends a message back to update the transaction state. There are exactly **three states** — `pending`, `approved`, `rejected` — and exactly **two rejection criteria**:

| # | Rule | Rejection threshold |
|---|------|---------------------|
| 1 | High value | `value` > **2000** |
| 2 | Daily accumulated (same `sourceAccountId`, UTC day) | accumulated > **20000** |

The API never evaluates rules synchronously — evaluation happens via Kafka (`TransactionCreated` → worker → `TransactionEvaluated`).

## Architecture

| Layer | Pattern | Responsibility |
|-------|---------|----------------|
| **Domain** | Rich Domain Model | `Transaction` entity, `TransactionStatus`/`RejectionReason` enums, 2 Specifications, `FraudRuleEngine` (deterministic), Guard Pattern, Result Pattern |
| **Application** | CQRS (explicit, no MediatR) | Commands, Validators, Handlers, Ports (`ITransactionRepository`, `IEventPublisher`), integration Events |
| **Infrastructure** | Adapters | EF Core + SQL Server persistence, `EfTransactionRepository`, `KafkaEventPublisher` (Confluent.Kafka), `KafkaOptions` |
| **Worker** | `FraudDetection.Worker` (new) | Anti-fraud microservice: `BackgroundService` consuming `TransactionCreated`, evaluating, persisting the new status, publishing `TransactionEvaluated` |
| **Api** | Minimal API | `POST`/`GET` endpoints, DI composition root, middleware, Swagger |

- **Hexagonal Architecture** (Ports & Adapters) — domain is pure; Kafka and EF Core plug in via interfaces
- **Vertical Slices** — code organized by feature, not by layer
- **CQRS** — explicit Command/Handler pattern without MediatR
- **Specification Pattern** — the two rejection rules are composable specifications with fixed constants
- **Guard Pattern** — centralized precondition checks replacing scattered null/range/empty validation
- **Result Pattern** — state transitions return `Result` instead of throwing exceptions for expected failures

Data flow (all async):

```
POST /api/v1/transactions  →  Transaction persisted (pending)  →  Kafka: transaction-created
                                                                          │
                                                                          ▼
                                          FraudDetection.Worker  ──  evaluates (specifications)
                                                                          │
      GET /api/v1/transactions/{id}  ←  SQL Server  ←  status persisted (approved/rejected)
                                                                          │
                                                          Kafka: transaction-evaluated (audit)
```

## Technologies

| Category | Technology |
|----------|------------|
| Runtime | .NET 8, ASP.NET Core, Worker (console host) |
| Persistence | Entity Framework Core 8 + SQL Server (migrations) |
| Messaging | Kafka (KRaft, single node) via Confluent.Kafka |
| Validation | FluentValidation |
| Testing | xUnit (unit + integration) |
| API Docs | OpenAPI / Swagger (all environments — public portfolio choice, ADR-059) |
| Containerization | Docker + docker-compose (SQL Server 2022 + Kafka + API + Worker) |
| CI/CD | GitHub Actions (path-filtered workflow) |

## Project Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/                    # HTTP Adapter (Minimal API)
│   │   ├── Endpoints/TransactionsEndpoint.cs  # POST /api/v1/transactions + GET /{id}
│   │   ├── Middleware/                        # ExceptionHandling (RFC 7807) + SecurityHeaders
│   │   ├── Program.cs                         # Composition root (producer side only)
│   │   └── appsettings.json                   # Kafka, RateLimit, ConnectionStrings
│   ├── FraudDetection.Worker/                 # Anti-fraud microservice (NEW, .NET 8 Worker)
│   │   ├── Workers/TransactionEvaluationWorker.cs  # Kafka consumer BackgroundService
│   │   ├── Program.cs                         # Composition root (evaluation side)
│   │   └── appsettings.json
│   ├── FraudDetection.Application/            # Use cases & ports
│   │   ├── Abstractions/                      # ITransactionRepository + IEventPublisher
│   │   ├── Events/                            # TransactionCreatedEvent, TransactionEvaluatedEvent
│   │   ├── Configuration/                     # RateLimitOptions (+ validator) — rules are constants now
│   │   └── Features/Transactions/
│   │       ├── CreateTransaction/             # Command, Validator, Handler, Result (201 + pending)
│   │       ├── EvaluateTransaction/           # Command, Handler, Result (worker side)
│   │       └── GetTransaction/                # GetTransactionResponse
│   ├── FraudDetection.Domain/                 # Pure domain logic
│   │   ├── Entities/Transaction.cs            # Approve() / Reject(reason) invariants
│   │   ├── Enums/                             # TransactionStatus (3 states), RejectionReason
│   │   ├── Guard.cs / Result.cs
│   │   ├── Services/FraudRuleEngine.cs        # Deterministic: 2 specs → Approved/Rejected+reason
│   │   └── Specifications/Transactions/       # HighValueSpecification, DailyAccumulatedSpecification
│   └── FraudDetection.Infrastructure/         # Adapter implementations
│       ├── Configuration/                     # KafkaOptions (+ validator)
│       ├── Messaging/                         # KafkaEventPublisher (producer) + JSON serializer options
│       ├── Persistence/                       # DbContext, configurations, converters, 1 migration, repository
│       └── ...
└── tests/
    ├── FraudDetection.UnitTests/              # Domain + Application tests
    └── FraudDetection.IntegrationTests/       # API + persistence tests (SQLite file-based)
```

## Current Status

Build: **0 errors, 0 warnings**. Tests: **152 total, all passing** — 111 unit + 41 integration (verified with `dotnet build` / `dotnet test`).

### Implemented

- Domain: `Transaction` with `TransactionExternalId` (server-generated Guid PK), `SourceAccountId`, `TargetAccountId`, `TransferTypeId`, `Value` (> 0), `CreatedAt` (UTC, server-generated), `Status` (pending/approved/rejected), `RejectionReason?` (audit)
- State transition invariants: only `Pending` can transition; `Reject` requires a reason; invalid transitions return `Result.Failure`
- Specification Pattern: `HighValueSpecification` (value > 2000) and `DailyAccumulatedSpecification` (accumulated > 20000) — thresholds are constants in the Domain specs
- `FraudRuleEngine`: deterministic — HighValue first, then DailyAccumulated, else Approved; returns the rejection reason
- Endpoints: `POST /api/v1/transactions` (201 + Location + pending), `GET /api/v1/transactions/{id}` (200/404 ProblemDetails), `GET /api/v1/version`, `GET /health` (alias of readiness), `GET /health/live` (liveness), `GET /health/ready` (readiness — SQL Server + Kafka, 200/503 with per-dependency detail)
- Kafka: `TransactionCreated` / `TransactionEvaluated` topics, JSON serialization (camelCase, lowercase enums), message key = transaction external ID, at-least-once consumer with idempotent replay, poison-message handling
- Anti-fraud worker: `FraudDetection.Worker` — subscribes, evaluates via the Application layer, persists the new status, publishes `TransactionEvaluated`, commits offsets explicitly
- EF Core 8 + SQL Server: single fresh `InitialCreate` migration; `(SourceAccountId, CreatedAt)` index for the daily-accumulated aggregation; status/reason stored as lowercase strings
- ProblemDetails (RFC 7807) error contract everywhere (`ExceptionHandlingMiddleware` + 404 responses)
- Rate limiting (fixed window, config-driven `RateLimit`) on the create endpoint, `429` ProblemDetails + `Retry-After`
- Security headers + HSTS, structured logging, health probes via the HealthChecks framework (liveness vs readiness, ADR-059), Swagger (all environments), Docker + docker-compose (4 services), GitHub Actions CI, Architecture Decision Log (ADR-001 → ADR-059)

### Intentionally Deferred

- Authentication / Authorization — documented in ADR-041 (portfolio scope)
- Transactional outbox for exactly-once publishing — documented production path in ADR-058
- Dead-letter topic for poison messages — poison messages are logged/committed/skipped (ADR-058)
- Explicit Kafka topic management (compose uses `AUTO_CREATE_TOPICS_ENABLE=true`) — ADR-053
- OpenTelemetry metrics and tracing — structured logs + health endpoints cover current needs

## Configuration

### Kafka (`Kafka` section — API and Worker)

```json
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "GroupId": "fraud-detection-worker",
  "AutoOffsetReset": "Earliest",
  "Topics": {
    "TransactionCreated": "transaction-created",
    "TransactionEvaluated": "transaction-evaluated"
  }
}
```

Bound to `KafkaOptions` (Infrastructure/Configuration) and validated at startup — a misconfigured deployment **fails fast** instead of silently producing/consuming nothing.

### Rate limit (`RateLimit` section — API)

```json
"RateLimit": {
  "PermitLimit": 30,
  "WindowSeconds": 60
}
```

Applies to `POST /api/v1/transactions` only (policy `create-transaction`). Exceeded requests receive `429` `application/problem+json` with a `Retry-After` header.

## API

### POST /api/v1/transactions — create (Resource 1)

Creates the transaction in `pending` status and queues it for asynchronous anti-fraud evaluation. Returns `201 Created` with a `Location` header. The fraud decision is never evaluated synchronously.

Request:

```json
{
  "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
  "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "tranferTypeId": 1,
  "value": 120
}
```

`tranferTypeId` is the challenge document's literal field name (it spells it with the typo, no 's' after "tran"). The API accepts **both** that spelling and the correctly-spelled `transferTypeId` (case-insensitive) — see "Known Limitations".

- `400` ProblemDetails on validation errors (Guids required, `tranferTypeId` > 0, `value` > 0)
- `429` when the rate limit is exceeded

Response (`201 Created`):

```json
{
  "transactionExternalId": "9c8b7a6f-5e4d-4c3b-8a9f-0e1d2c3b4a59",
  "createdAt": "2026-08-12T20:15:30.1234567Z",
  "status": "pending"
}
```

### GET /api/v1/transactions/{id} — query state (Resource 2)

Returns the current transaction state (poll this to observe the async evaluation result):

```json
{
  "transactionExternalId": "9c8b7a6f-5e4d-4c3b-8a9f-0e1d2c3b4a59",
  "createdAt": "2026-08-12T20:15:30.1234567Z",
  "status": "rejected",
  "rejectionReason": "highvalue"
}
```

- `status` is `pending` | `approved` | `rejected` (lowercase)
- `rejectionReason` (`highvalue` | `dailyaccumulated`) is present only when rejected
- `404` RFC 7807 ProblemDetails when the transaction does not exist

## Quick Start

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the API (requires SQL Server + Kafka; adjust connection strings/appsettings)
dotnet run --project src/FraudDetection.Api
# and the anti-fraud worker:
dotnet run --project src/FraudDetection.Worker
```

### Local Kafka (without the full compose stack)

For local `dotnet run` development, start a single-node KRaft broker (mirrors the compose service — topics auto-create on first produce):

```bash
docker run -d --name frauddetection-kafka \
  -e KAFKA_NODE_ID=1 \
  -e KAFKA_PROCESS_ROLES=broker,controller \
  -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@localhost:29093 \
  -e KAFKA_LISTENERS=PLAINTEXT://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093,PLAINTEXT_HOST://0.0.0.0:9092 \
  -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:9092 \
  -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT \
  -e KAFKA_INTER_BROKER_LISTENER_NAME=PLAINTEXT \
  -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER \
  -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
  -e KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1 \
  -e KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1 \
  -e KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS=0 \
  -e KAFKA_AUTO_CREATE_TOPICS_ENABLE=true \
  -e CLUSTER_ID=MkU3OEVBNTcwNTJENDM2Qk \
  -p 9092:9092 \
  confluentinc/cp-kafka:7.7.1

docker rm -f frauddetection-kafka   # stop / remove
```

The API and Worker `appsettings.json` already point at `localhost:9092`.

### Docker (recommended)

Run the whole system — SQL Server, Kafka (KRaft), API, and the anti-fraud Worker — with one command:

```bash
# Start all services (builds the images on first run)
docker compose up --build

# Stop (add -v to also remove the SQL Server data volume)
docker compose down
```

- API on `http://localhost:8080` — Swagger under `/swagger` (enabled in ALL environments — public portfolio choice, ADR-059); health probes at `http://localhost:8080/health/live` (liveness) and `/health/ready` (readiness)
- SQL Server 2022 on port `1433` (named volume `sqlserver-data`); Kafka on `localhost:9092`
- The API and the Worker both auto-apply migrations on startup (`AutoMigrate=true`), so no manual `dotnet ef database update` is required
- The Worker starts only after Kafka and the API are healthy (the API applies the shared schema first)
- The SA password defaults to `Your_Strong_Passw0rd!` (demo only) — **override it** with `MSSQL_SA_PASSWORD` in a `.env` file in this directory (git-ignored)

> **Upgrade note (pre-existing SQL Server volume):** if this machine previously ran the
> OLD pre-rework version, its `sqlserver-data` volume still holds the old schema (old
> tables + the 4 old migrations), so `docker compose up` crashes during migration with
> `SqlException 2714: object 'Transactions' already exists` (API container exit). Fix:
> reset the volume once with `docker compose down -v` **before** `docker compose up`.
> `-v` is destructive — it wipes the SQL Server data volume; this portfolio challenge
> has no precious data, and a fresh clone starts clean anyway.

### API Examples

```bash
# Create a transaction (201 → pending; evaluated asynchronously by the worker)
curl -i -X POST "http://localhost:8080/api/v1/transactions" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
    "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "tranferTypeId": 1,
    "value": 120
  }'

# Query the transaction state (poll until approved/rejected; 404 ProblemDetails if unknown)
curl "http://localhost:8080/api/v1/transactions/9c8b7a6f-5e4d-4c3b-8a9f-0e1d2c3b4a59"

# Rejection examples: value 2500 → highvalue; or 11 transactions of 2000 for the same
# sourceAccountId in one UTC day → the 11th (accumulated 22000 > 20000) is rejected
# with dailyaccumulated (each single value ≤ 2000, so high-value does not mask it)

# Health checks
curl "http://localhost:8080/health/live"  # liveness — no dependencies, always 200 while the process is up
curl "http://localhost:8080/health/ready" # readiness — SQL Server + Kafka; 200 only when both are up,
                                          # 503 otherwise with per-dependency detail
curl "http://localhost:8080/health"       # alias of /health/ready (backwards compatible)
curl "http://localhost:8080/api/v1/version" # build version metadata (commit when a SourceRevisionId build)
```

### Test the async flow end to end

1. `docker compose up --build`
2. POST a transaction with `value` > 2000 → `201` with `status: "pending"`
3. GET the transaction → within seconds it should report `status: "rejected"`, `rejectionReason: "highvalue"`
4. Repeat with `value: 120` → `status: "approved"`

## Security

- No authentication or authorization — out of scope for the challenge (ADR-041)
- Security headers on all responses (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies`, CSP); HSTS in non-development environments
- Errors return RFC 7807 `ProblemDetails` — internal details and stack traces are never exposed
- Rate limiting on the create endpoint (`429` + `Retry-After`)
- No secrets in source code; the Docker SA password and Kafka config are environment-driven
- Swagger UI enabled in all environments (public portfolio repo — see ADR-059; real systems with sensitive data would gate it)

## Known Limitations

- **At-least-once delivery**: duplicate `TransactionEvaluated` messages are possible after crash-redelivery; the worker is idempotent, downstream consumers must tolerate duplicates (ADR-058)
- **Persist-then-publish** in the create flow: a publish failure surfaces as a 500 while the row stays pending; transactional outbox is the documented production path (ADR-058)
- Shared database between API and Worker — pragmatic single-deployment choice, documented in ADR-054 (production would split)
- Integration tests use SQLite (file-based), not SQL Server — performance numbers are indicative only
- **No automated Kafka E2E test**: CI runs unit + integration tests with fake publisher/repository — a full Api → Kafka → Worker → DB round trip is not exercised via Testcontainers and must be validated manually against the running compose stack (see "Test the async flow end to end" below)
- **SQLite decimal `SUM`**: the integration test provider has no native decimal type, so the daily-accumulated aggregate projects to `double` and casts back to `decimal` — exact to the cent for realistic daily amounts; SQL Server translates it to `SUM(CAST(Value AS float))` (see `EfTransactionRepository.GetDailyAccumulatedAsync`)
- **Challenge field spelling**: the challenge document (Challenge_BE-LT.docx) writes `tranferTypeId`; a custom `JsonConverter<CreateTransactionCommand>` (see `CreateTransactionCommandConverter.cs`) binds the **literal challenge spelling** to `TransferTypeId`, and also accepts the correctly-spelled `transferTypeId` as an alias — both case-insensitively (`tranferTypeId` wins when both are present). Posting the challenge's exact payload returns `201`.
- No OpenTelemetry/metrics — observability is structured logs + health endpoints

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](ARCHITECTURE.md) | Full architecture deep-dive with the async flow |
| [Challenge](CHALLENGE.md) | The real challenge requirements (3 states, 2 fraud rules, Kafka async flow) |
| [Decisions](DECISIONS.md) | Architecture Decision Log (ADR-001 through ADR-058) |
| [KnowledgeBase](../../KnowledgeBase/Architecture/) | Educational reference for patterns used |

## CI/CD

The repository-level GitHub Actions workflow (`.github/workflows/ci.yml`) runs on push and pull requests to `main`, path-filtered to `Projects/FraudDetection/**` (covers the Worker): restore, Release build of the solution (all 5 src projects + 2 test projects), full test suite, and test-result artifact upload.