# Fraud Detection API — Architecture

> This document describes the architecture of the PRODUCTION REWORK implementing the real technical challenge
> (see DECISIONS.md ADR-051). The system validates every created financial transaction **asynchronously**:
> API → Kafka → anti-fraud Worker → database/API.

## Why Hexagonal Architecture

Hexagonal Architecture (Ports & Adapters) provides:

| Benefit | Description |
|---------|-------------|
| Testability | Business logic can be tested without HTTP, Kafka, or DB |
| Replaceability | Adapters can be swapped (e.g., Kafka publisher → in-memory, EF Core → other store) |
| Evolution | New adapters added without changing core logic |
| Simplicity | Clear separation between "what" (Domain) and "how" (Adapters) |

## Why Vertical Slice

Vertical Slice organizes code by use case, not by technical layer:

| Benefit | Description |
|---------|-------------|
| Cohesion | Each slice contains everything needed for one feature |
| Isolation | Changes to one slice don't affect others |
| Clarity | Easy to understand the flow of a specific feature |

The system has exactly three slices: **CreateTransaction** (API side), **EvaluateTransaction** (worker side), and **GetTransaction** (query side).

## Why Explicit CQRS

CQRS is implemented **explicitly** — no MediatR, no `IRequest<T>`:

| Benefit | Description |
|---------|-------------|
| Simplicity | No framework overhead for three use cases |
| Visibility | Dependency graph is visible in the Handler constructor |
| Debuggability | No mediator indirection — call chain is linear |
| Control | No assembly scanning, no pipeline behaviors to configure |

This is an own architectural decision, retained although the challenge client does not require it (ADR-052).

## Dependency Direction

```
Api ──→ Application ──→ Domain
 │                       │
 └──→ Infrastructure ───┘
      (implements ports)

Worker ──→ Application ──→ Domain
   │            │
   └──→ Infrastructure
```

**Rules:**
- Dependencies always point **inward** toward Domain
- **Domain** has zero dependencies — not on ASP.NET, EF Core, Kafka, or any external library
- **Application** depends only on Domain (and FluentValidation — a library, not framework)
- **Infrastructure** depends on Application + Domain
- **Api** and **Worker** depend on everything (each is a composition root)

## Projects

| Project | Responsibility |
|---------|----------------|
| **Domain** | `Transaction` entity, `TransactionStatus`/`RejectionReason` enums, 2 specifications, `FraudRuleEngine`, Guard/Result patterns |
| **Application** | Use cases (Create/Evaluate/Get), ports (`ITransactionRepository`, `IEventPublisher`), integration events, validators |
| **Infrastructure** | Adapters: EF Core DbContext/repository, Kafka producer (`KafkaEventPublisher`), `KafkaOptions` |
| **Worker** (new) | Anti-fraud microservice: Kafka consumer `BackgroundService` + its own composition root |
| **Api** | HTTP adapter (Minimal API), DI composition root, middleware, Swagger |

## Real Folder Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/
│   │   ├── Endpoints/
│   │   │   └── TransactionsEndpoint.cs          # POST /api/v1/transactions + GET /{id}
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs   # RFC 7807 ProblemDetails
│   │   │   └── SecurityHeadersMiddleware.cs     # Security headers + HSTS
│   │   ├── Program.cs                           # Composition root (producer side)
│   │   └── appsettings.json
│   │
│   ├── FraudDetection.Worker/                   # ← NEW anti-fraud microservice
│   │   ├── Workers/
│   │   │   └── TransactionEvaluationWorker.cs   # Kafka consumer (BackgroundService)
│   │   ├── Program.cs                           # Composition root (evaluation side)
│   │   └── appsettings.json
│   │
│   ├── FraudDetection.Application/
│   │   ├── Abstractions/
│   │   │   ├── ITransactionRepository.cs        # Add/GetById/GetDailyAccumulated/Update
│   │   │   └── IEventPublisher.cs               # Publish TransactionCreated/Evaluated
│   │   ├── Events/
│   │   │   ├── TransactionCreatedEvent.cs
│   │   │   └── TransactionEvaluatedEvent.cs
│   │   ├── Configuration/
│   │   │   ├── RateLimitOptions.cs
│   │   │   └── RateLimitOptionsValidator.cs
│   │   ├── Exceptions/
│   │   │   └── TransactionConflictException.cs  # Defensive duplicate-key signal
│   │   └── Features/
│   │       └── Transactions/
│   │           ├── CreateTransaction/           # Command, Validator, Handler, Result
│   │           ├── EvaluateTransaction/         # Command, Handler, Result (worker side)
│   │           └── GetTransaction/              # GetTransactionResponse
│   │
│   ├── FraudDetection.Domain/                   # Pure domain logic
│   │   ├── Entities/
│   │   │   └── Transaction.cs                   # Approve()/Reject(reason) invariants
│   │   ├── Enums/
│   │   │   ├── TransactionStatus.cs             # Pending | Approved | Rejected (only 3)
│   │   │   └── RejectionReason.cs               # HighValue | DailyAccumulated
│   │   ├── Guard.cs                             # Centralized precondition checks
│   │   ├── Result.cs                            # Result pattern (non-generic)
│   │   ├── Services/
│   │   │   ├── FraudRuleEngine.cs               # Deterministic 2-rule evaluation
│   │   │   └── FraudRuleEngineResult.cs
│   │   └── Specifications/
│   │       ├── ISpecification.cs
│   │       └── Transactions/
│   │           ├── HighValueSpecification.cs
│   │           └── DailyAccumulatedSpecification.cs
│   │
│   └── FraudDetection.Infrastructure/
│       ├── Configuration/
│       │   ├── KafkaOptions.cs                  # Kafka: section + KafkaOptionsValidator
│       │   └── KafkaOptionsValidator.cs
│       ├── Messaging/
│       │   ├── KafkaEventPublisher.cs           # Confluent.Kafka producer (IEventPublisher)
│       │   └── KafkaJsonSerializerOptions.cs    # Shared camelCase/lowercase-enum JSON
│       └── Persistence/
│           ├── Configurations/
│           │   └── TransactionConfiguration.cs
│           ├── Converters/
│           │   ├── TransactionStatusConverter.cs    # → lowercase string
│           │   └── RejectionReasonConverter.cs      # → lowercase string
│           ├── Migrations/
│           │   └── 20260813020511_InitialCreate.cs  # ← single fresh migration
│           ├── Repositories/
│           │   └── EfTransactionRepository.cs
│           ├── DesignTimeDbContextFactory.cs    # dotnet ef without booting the host
│           └── FraudDetectionDbContext.cs
│
└── tests/
    ├── FraudDetection.UnitTests/               # Domain + Application tests (unit — 99 tests)
    └── FraudDetection.IntegrationTests/        # API + persistence tests (SQLite file-based — 38 tests)
```

## Ports and Adapters

### Primary (Driving) Ports

| Port | Purpose | Adapter |
|------|---------|---------|
| `POST /api/v1/transactions` | Create a transaction (201 + pending) | `TransactionsEndpoint` (Minimal API) |
| `GET /api/v1/transactions/{id}` | Query transaction state (200/404) | `TransactionsEndpoint` (Minimal API) |
| `GET /health/ready` | Readiness — SQL Server + Kafka via HealthChecks (200/503, per-dependency JSON detail) | `MapHealthChecks` + custom ResponseWriter, ADR-059 |
| `GET /health/live` | Liveness — no dependencies (predicate selects no checks), always 200 | `MapHealthChecks` (Predicate `_ => false`), ADR-059 |
| `GET /health` | Alias of `/health/ready` — backwards compatibility | `MapHealthChecks` (same options object) |
| `GET /api/v1/version` | Build version metadata (`version`, `informationalVersion`, `environment`, optional `commit`) | `VersionEndpoint` (Minimal API, composition root only) |

There are no interface-based primary ports — the Minimal API delegate invokes the Handler directly. In hexagonal terms, the HTTP endpoint IS the inbound adapter.

### Secondary (Driven) Ports

| Port | Purpose | Implementations |
|------|---------|-----------------|
| `ITransactionRepository` | Persists transactions; reads by ID; computes the day's accumulated value; persists status transitions | `EfTransactionRepository` (SQL Server; duplicate-key translation; `AsNoTracking` reads) |
| `IEventPublisher` | Publishes integration events to Kafka | `KafkaEventPublisher` (Confluent.Kafka producer, JSON, keyed by transaction ID) |

### Port Location

```
Application/Abstractions/ITransactionRepository.cs
Application/Abstractions/IEventPublisher.cs
```

Ports are defined in the **Application Layer** because they represent capabilities the application needs from the outside world. The Domain defines business rules; the Application defines what it needs from infrastructure. Kafka is fully hidden behind `IEventPublisher` (ADR-053).

## The Async Flow (Api → Kafka → Worker → DB)

```
┌──────────┐   POST /api/v1/transactions   ┌────────────────────────────────┐
│  Client  │ ────────────────────────────▶ │  FraudDetection.Api             │
└──────────┘ ◀──────────────────────────── │  CreateTransactionHandler       │
      201 { transactionExternalId,         │  1. new Transaction(...pending) │
            createdAt, status: "pending" } │  2. repository.AddAsync         │
                                           │  3. publish TransactionCreated  │
                                           └──────────────┬─────────────────┘
                                                          │ Kafka
                                                          ▼
                                            topic "transaction-created"
                                                          │
                                                          ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  FraudDetection.Worker (anti-fraud microservice)                             │
│  TransactionEvaluationWorker (BackgroundService)                             │
│    consume → EvaluateTransactionHandler:                                      │
│      1. repository.GetByIdAsync(id)                                           │
│      2. repository.GetDailyAccumulatedAsync(sourceAccountId, day)             │
│         (includes this transaction — it is already persisted as Pending)      │
│      3. FraudRuleEngine.Evaluate(tx, accumulated)                             │
│         HighValue (>2000)?        → Rejected(HighValue)                       │
│         DailyAccumulated (>20000)?→ Rejected(DailyAccumulated)                │
│         else                      → Approved                                  │
│      4. tx.Approve() / tx.Reject(reason)   ← domain invariants                │
│      5. repository.UpdateAsync(tx)                                            │
│      6. publish TransactionEvaluated → topic "transaction-evaluated"          │
│      7. consumer.Commit(offset)          ← at-least-once (ADR-058)            │
└───────┬──────────────────────────────────────────────────────────────────────┘
        │ SQL Server (shared DB — pragmatic choice, ADR-054)
        ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  FraudDetection.Api — GET /api/v1/transactions/{id}                          │
│  reads the CURRENT state: { transactionExternalId, createdAt, status,        │
│  rejectionReason? }  — 400/404/429 RFC 7807 ProblemDetails everywhere        │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Key design points:**
- The create endpoint NEVER evaluates fraud — the challenge mandates async messaging with no synchronous evaluation in the request (ADR-058)
- Delivery is **at-least-once** with an idempotent consumer: offsets are committed only after persist+publish; redelivery replays the current state instead of re-evaluating, and duplicate `TransactionEvaluated` messages are tolerated downstream (ADR-058)
- `TransactionCreatedEvent` carries the full creation snapshot; the worker only needs the ID (it re-reads the row for the state transition and day computation)

## Domain Layer

### Guard Pattern

The `Guard` class centralizes precondition validation, replacing repetitive inline checks across the Domain:

| Method | Used In |
|--------|---------|
| `AgainstNull<T>(T?, string)` | Entity constructors, FraudRuleEngine, handlers |
| `AgainstNullOrWhiteSpace(string, string)` | String inputs (legacy — no current string fields on Transaction) |
| `AgainstOutOfRange(int, int, int, string)` | `TransferTypeId` (> 0) |
| `AgainstOutOfRange(decimal, decimal, decimal, string)` | Reserved |
| `AgainstEmptyGuid(Guid, string)` | `TransactionExternalId`, `SourceAccountId`, `TargetAccountId` |
| `AgainstNegative(decimal, string)` | `DailyAccumulatedSpecification` constructor |
| `AgainstNonPositive(decimal, string)` | `Value` (> 0) |
| `AgainstUndefinedEnum<T>(T, string)` | `Reject(reason)` — reason must be a defined enum value |

**Why centralized:** consistent precondition vocabulary, single-place semantics, testable in one file.

### Result Pattern

State transitions in `Transaction` (`Approve`, `Reject(reason)`) return `Result` instead of throwing for expected failures:

- `Approve()` — only from `Pending`; clears `RejectionReason`
- `Reject(RejectionReason reason)` — only from `Pending`; reason is mandatory (compile-time signature + `AgainstUndefinedEnum` guard)
- Any other transition returns `Result.Failure("Only transactions in Pending status can change state...")`

**Why only for state transitions:** state transitions represent expected domain flows where a caller (Handler) should handle the outcome explicitly. Other preconditions (null checks, range validation) remain exception-based because they represent programming errors, not expected business outcomes.

### Entity: Transaction

| Field | Type | Notes |
|-------|------|-------|
| `TransactionExternalId` | `Guid` | Primary key, **server-generated** (`Guid.NewGuid()`), contract name `transactionExternalId` |
| `SourceAccountId` | `Guid` | External account identifier; must not be `Guid.Empty` |
| `TargetAccountId` | `Guid` | External account identifier; must not be `Guid.Empty` |
| `TransferTypeId` | `int` | > 0 |
| `Value` | `decimal` | > 0, `decimal(18,2)` in DB |
| `CreatedAt` | `DateTime` | UTC, server-generated; day boundary for the daily rule (ADR-057) |
| `Status` | `TransactionStatus` | `Pending` \| `Approved` \| `Rejected` — exactly three states |
| `RejectionReason` | `RejectionReason?` | `HighValue` \| `DailyAccumulated`; set only when rejected (ADR-056) |

There are no value objects anymore: the real contract uses plain Guids/decimal and the IDs carry no domain behavior (ADR-051 supersedes ADR-009 for this context).

### Specification Pattern

```csharp
// Domain/Specifications/ISpecification.cs
public interface ISpecification
{
    bool IsSatisfiedBy(Transaction transaction);
}
```

The interface is **non-generic** (YAGNI — only `Transaction` is evaluated). Exactly two specifications exist — the complete set of rejection criteria from the challenge:

| Specification | Criterion | Threshold |
|---------------|-----------|-----------|
| `HighValueSpecification` | `Value > HighValueLimit` | `2000m` (constant in the spec) |
| `DailyAccumulatedSpecification` | `accumulatedToday > DailyAccumulatedLimit` | `20000m` (constant in the spec) |

Thresholds are **constants of the Domain specs** — the real challenge fixes them; there is no rules table and no `FraudRuleOptions` (ADR-051). `DailyAccumulatedSpecification` receives the pre-computed day sum via its constructor (same pattern as the legacy velocity spec): the repository computes `SUM(Value)` and the spec stays pure. The accumulated sum INCLUDES the transaction being evaluated (ADR-057).

### Domain Service: FraudRuleEngine

The `FraudRuleEngine` is a **stateless, deterministic** domain service:

```csharp
public FraudRuleEngineResult Evaluate(Transaction transaction, decimal dailyAccumulatedAmount)
```

Rules run in **fixed precedence order** — both rules reject, so the first satisfied rule determines the rejection reason:

1. `HighValueSpecification` satisfied → `Rejected(HighValue)`
2. `DailyAccumulatedSpecification` satisfied → `Rejected(DailyAccumulated)`
3. neither → `Approved`

The result (`FraudRuleEngineResult`) is a record of `(RecommendedStatus, RejectionReason?)`. No risk scoring exists (ADR-056).

## Application Layer Slices

### CreateTransaction (API side)

| File | Responsibility |
|------|----------------|
| `CreateTransactionCommand` | `SourceAccountId`, `TargetAccountId`, `TransferTypeId`, `Value` — maps 1:1 to the challenge's Resource 1 payload |
| `CreateTransactionValidator` | FluentValidation: Guids non-empty, `TransferTypeId > 0`, `Value > 0` |
| `CreateTransactionHandler` | Creates the domain Transaction (Pending) → `AddAsync` → publishes `TransactionCreated` → returns `{ transactionExternalId, createdAt, status: "pending" }` |
| `CreateTransactionResult` | Response DTO (201 body) |

Persist-then-publish is deliberate and documented: a publish failure surfaces as 500 while the row stays pending (outbox pattern is the documented production path — ADR-058).

### EvaluateTransaction (worker side)

| File | Responsibility |
|------|----------------|
| `EvaluateTransactionCommand` | `TransactionExternalId` |
| `EvaluateTransactionHandler` | Load → (replay if not Pending) → compute day sum → `FraudRuleEngine.Evaluate` → `Approve()`/`Reject(reason)` → `UpdateAsync` → result |
| `EvaluateTransactionResult` | `(TransactionExternalId, Status, RejectionReason?)` |

Lives in **Application** (not in the Worker project) so the whole evaluation logic is unit-testable without Kafka/hosting concerns. The Worker is a thin orchestrator: consume → call handler → publish → commit.

### GetTransaction (query side)

`GetTransactionResponse(TransactionExternalId, CreatedAt, Status, RejectionReason?)` — the challenge's Resource 2 contract base (transactionExternalId + createdAt) extended with `status` and the audit `rejectionReason`.

## Infrastructure Layer

### Persistence (EF Core 8 + SQL Server)

- `FraudDetectionDbContext` — single aggregate: `Transactions`
- `TransactionConfiguration` — table `Transactions`, PK `TransactionExternalId`, `decimal(18,2)` Value, lowercase-string `Status`/`RejectionReason` (max 20), composite index **`IX_Transactions_SourceAccountId_CreatedAt`** covering the daily-accumulated query (equality on account + range on UTC day)
- `EfTransactionRepository`:
  - `AddAsync` — insert; duplicates translate to `TransactionConflictException` (defensive — IDs are server-generated)
  - `GetByIdAsync(Guid)` — `AsNoTracking`
  - `GetDailyAccumulatedAsync(Guid, DateOnly)` — `SUM(Value)` over `[midnight UTC, midnight UTC + 1 day)` (ADR-057)
  - `UpdateAsync` — attach + save (worker is the only status writer; no concurrency token — documented in ADR-054)
- One fresh `InitialCreate` migration (`20260813020511`); `FraudDetectionDbContextFactory` (design-time) keeps `dotnet ef` independent of the API host (ADR-055)
- Status/reason converters store LOWERCASE strings, matching the JSON wire format

### Messaging (Kafka via Confluent.Kafka — ADR-053)

- `KafkaEventPublisher : IEventPublisher` — producer with `Acks.All` + idempotence; JSON via `KafkaJsonSerializerOptions` (camelCase, lowercase enums); **message key = transaction external ID** → per-transaction partitioning/ordering; `MessageTimeoutMs = 10s` dev fail-fast
- Topics: `transaction-created`, `transaction-evaluated` (configurable `Kafka:Topics:*`)
- `KafkaOptions` + `KafkaOptionsValidator` (Infrastructure) — validated at startup in both hosts
- The consumer lives in the **Worker** project (the only consumer today): `Confluent.Kafka` consumer with `GroupId`, `AutoOffsetReset`, manual commits, poison-message skip, and per-message DI scopes (a `BackgroundService` is a singleton and must not hold a scoped `DbContext`)

## Configuration

Both hosts (Api `appsettings.json` + Worker `appsettings.json`) bind the same sections; environment override uses the double-underscore convention (`KAFKA__BOOTSTRAPSERVERS`, `ConnectionStrings__DefaultConnection`):

| Section | Used by | Purpose |
|---------|---------|---------|
| `ConnectionStrings:DefaultConnection` | Api + Worker | SQL Server connection string (`(localdb)` in dev; the `sqlserver` service in compose) |
| `Kafka` | Api + Worker | `BootstrapServers`, `GroupId`, `AutoOffsetReset`, `Topics:{TransactionCreated,TransactionEvaluated}` — bound to `KafkaOptions`, validated at startup by `KafkaOptionsValidator` (fail-fast, ADR-053) |
| `RateLimit` | Api | `PermitLimit` (30), `WindowSeconds` (60) for the fixed-window policy `create-transaction` (ADR-046) |
| `AutoMigrate` | Api + Worker | When `true` (or in Development), pending migrations are applied at startup — compose dev/portfolio choice (ADR-054) |

## DI Wiring

- **Api** (`Program.cs`): `FraudDetectionDbContext` (scoped, SQL Server) → `ITransactionRepository` (scoped `EfTransactionRepository`) → `IEventPublisher` (singleton `KafkaEventPublisher`) → `CreateTransactionValidator` + `CreateTransactionHandler` (scoped). `KafkaOptions` and `RateLimitOptions` use `Configure<>` + `ValidateOnStart`; the fixed-window limiter policy is registered via `AddRateLimiter`.
- **Worker** (`Program.cs`): same persistence + publisher registrations and Kafka validation; `FraudRuleEngine` (singleton, stateless), `ITransactionRepository` + `EvaluateTransactionHandler` (scoped), hosted `TransactionEvaluationWorker`. Because a `BackgroundService` is a singleton, scoped dependencies are resolved **per message** via `IServiceScopeFactory` — the worker must never hold a scoped `DbContext`.
- Both are independent composition roots with the same dependency direction (Api/Worker → Application → Domain; Infrastructure implements the ports). The Api is the producer side only; the Worker is the only consumer.

## Request Lifecycle (API)

```
HTTP POST /api/v1/transactions
   │
   ▼ SecurityHeadersMiddleware (global) — security headers on the response
   ▼ ExceptionHandlingMiddleware (global) — 500 → RFC 7807 ProblemDetails + requestId
   ▼ RateLimitingMiddleware (global) — policy "create-transaction" (fixed window)
   │     exhausted → 429 ProblemDetails + Retry-After (ADR-046)
   ▼ TransactionsEndpoint
   │     validator.ValidateAsync(command)
   │       invalid → 400 ValidationProblem  (FluentValidation)
   ▼ CreateTransactionHandler.Handle(command)
   │     new Transaction(Guid.NewGuid(), source, target, transferTypeId, value)
   │     repository.AddAsync(transaction)                 ← persisted as Pending
   │     eventPublisher.PublishAsync(TransactionCreated)  ← Kafka (persist-then-publish)
   ▼ HTTP 201 Created
         Location: /api/v1/transactions/{id}
         Body: { transactionExternalId, createdAt, status: "pending" }
```

`GET /api/v1/transactions/{id:guid}` reads the current row → `200` with state (lowercase `status`, optional `rejectionReason`) or `404` ProblemDetails with a `transactionExternalId` extension.

## EF Core Mapping Strategy

All EF Core concerns are isolated in **Infrastructure**.

### Value Converters

| Converter | Converts |
|-----------|----------|
| `TransactionStatusConverter` | `TransactionStatus` ↔ lowercase string (`'approved'`) |
| `RejectionReasonConverter` | `RejectionReason` ↔ lowercase string (`'highvalue'`) |

No strongly-typed ID converters remain — identity is a plain `Guid` (ADR-051).

### Migrations

Exactly one migration (ADR-055): `InitialCreate` — `Transactions` table + `IX_Transactions_SourceAccountId_CreatedAt` index. Applied automatically on development startup or with `AutoMigrate=true` (API and Worker share the schema — ADR-054).

### Integration Tests

Tests use **SQLite file-based** (temporary `.db` file, not a shared `:memory:` connection) so each `DbContext` opens its own connection and SQLite's locking/busy-timeout semantics apply for concurrency tests. The ephemeral schema is built with `EnsureCreated` (migrations are SQL Server-targeted — ADR-049). Suite status: **152 tests passing (111 unit + 41 integration), 0 warnings** (verified with `dotnet build` / `dotnet test`). The daily-accumulated `SUM` projects to `double` for SQLite portability (cast back to `decimal` — see the repository remarks). A full Api → Kafka → Worker → DB round trip is NOT automated via Testcontainers in CI: the worker evaluation is covered at handler level with fakes, and the end-to-end flow is validated manually against the compose stack (see README).

## Error Handling (ProblemDetails / RFC 7807)

- `AddProblemDetails()` registers the standard ProblemDetails service
- `ExceptionHandlingMiddleware` catches unhandled exceptions, logs them (Method, Path, TraceIdentifier), and returns `500 Internal Server Error` with an RFC 7807 `application/problem+json` body — no stack traces or internal details; `requestId` correlates with the structured log entry
- `GET /api/v1/transactions/{id}` not found → `404 ProblemDetails` with a `transactionExternalId` extension
- Validation errors → `400 ValidationProblem` (FluentValidation `ToDictionary()`)
- Rate-limit rejection → `429 ProblemDetails` + `Retry-After` header (ADR-046)

## Security Headers

`SecurityHeadersMiddleware` applies `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none`, `Content-Security-Policy: default-src 'self'` to every response. `UseHsts()` is applied in non-development environments. No secrets in source code; Kafka and DB configuration are environment-driven.

## Docker Deployment

- **Multi-stage Dockerfile**: one SDK 8.0 build stage publishes both the API and the Worker; two runtime stages — `final-api` (default; curl + HEALTHCHECK on `/health/live`, non-root user) and `final-worker` (no HTTP surface, so no HEALTHCHECK)
- **docker-compose.yml** (4 services):
  - `sqlserver` — SQL Server 2022, named volume, health gate
  - `kafka` — Confluent cp-kafka **KRaft single node** (no ZooKeeper), PLAINTEXT internal listener (`kafka:29092`) + `localhost:9092` for host tools, `AUTO_CREATE_TOPICS_ENABLE=true` dev convenience, health check via `kafka-topics --list`
  - `api` — build `target: final-api`; Kafka + SQL env; waits for `sqlserver` AND `kafka` healthy
  - `worker` — build `target: final-worker`; same Kafka + SQL env; `AutoMigrate=true`; waits for `kafka` AND `api` healthy (the API applies the shared schema first)
- `AutoMigrate=true` makes both the API and the Worker apply pending migrations on startup (dev/portfolio choice — ADR-054)
- The SA password is passed via `MSSQL_SA_PASSWORD` (default demo value; override via `.env`)

## Current Limitations (Real Challenge Scope)

### Implemented (What Works)

- Async anti-fraud flow: API (201 + pending) → Kafka `transaction-created` → Worker evaluates → persists approved/rejected → publishes `transaction-evaluated`
- Exactly three states; exactly two rejection rules with constants in Domain specs
- GET returns current state + rejection reason audit (decision audit = `RejectionReason`, ADR-056)
- At-least-once delivery with idempotent consumer replay and poison-message handling (ADR-058)
- Rate limiting, ProblemDetails everywhere, security headers, health probes, structured logging
- Production build 0 errors / 0 warnings; **152 tests passing (111 unit + 41 integration)**; fresh single migration; docker-compose with Kafka

### Intentionally Deferred

| Feature | Rationale |
|---------|-----------|
| Transactional outbox | Documented production path for persist-then-publish gap (ADR-058) |
| Dead-letter topic / retry policy | Poison messages are logged/committed/skipped; processing failures retried by redelivery (ADR-058) |
| Explicit Kafka topic management | `AUTO_CREATE_TOPICS_ENABLE=true` for dev; production manages topics (ADR-053) |
| Split databases (API vs Worker) | Shared DB is a pragmatic single-deployment choice (ADR-054) |
| Authentication / Authorization | Deliberate portfolio decision (ADR-041) |
| OpenTelemetry metrics and tracing | Structured logs + health endpoints cover current needs |

### Known Accepted Risks

| Risk | Note |
|------|------|
| At-least-once duplicates | Duplicate `TransactionEvaluated` possible after crash-redelivery; workers are idempotent, consumers must tolerate duplicates (ADR-058) |
| Lost message window (persist-then-publish) | Publish failure → 500 + Pending row; outbox is the production fix (ADR-058) |
| No concurrency token on Transaction | Worker is the only status writer; last-write-wins (ADR-054) |
| UTC day boundaries | Server-side rule semantics; documented (ADR-057) |
| Kafka E2E round trip not automated | No Testcontainers/broker test in CI — the real Api → Kafka → Worker → DB flow is validated manually via docker compose (README); worker logic is unit/integration-tested at handler level |

## Architecture Diagrams

### Hexagonal View

```
                    ┌──────────────────────────────────────────┐
                    │              Domain Layer                │
                    │                                          │
                    │  ┌──────────┐   ┌─────────────────────┐  │
                    │  │ Entity   │   │ Specifications      │  │
                    │  │Transaction│  │ HighValueSpec       │  │
                    │  │          │   │ DailyAccumulatedSpec│  │
                    │  └──────────┘   └─────────────────────┘  │
                    │  ┌──────────────────────────────────┐    │
                    │  │   FraudRuleEngine (Domain Svc)   │    │
                    │  │   deterministic, 2 fixed rules   │    │
                    │  └──────────────────────────────────┘    │
                    └──────────────────────────────────────────┘
                              ▲            ▲
                              │            │
                    ┌─────────┴────────────┴──────────────────┐
                    │          Application Layer               │
                    │  CreateTransaction | EvaluateTransaction │
                    │  GetTransaction (vertical slices)        │
                    │  Ports: ITransactionRepository,          │
                    │         IEventPublisher                  │
                    └─────────▲──────────────▲─────────────────┘
                              │              │
        ┌─────────────────────┴───┐    ┌─────┴──────────────────┐
        │  Api (Inbound Adapter)  │    │  Infrastructure        │
        │  TransactionsEndpoint   │    │  (Outbound Adapters)   │
        │  Program.cs (DI)        │    │  EfTransactionRepository│
        ├─────────────────────────┤    │  KafkaEventPublisher   │
        │  Worker (Inbound from   │    │  EF Core DbContext     │
        │  Kafka — consumer loop) │    └────────────────────────┘
        └─────────────────────────┘
```

### Dependency Graph (Layer Diagram)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Api  /  Worker  (composition roots)                                 │
│  Depends on: Application, Infrastructure, Domain                     │
│  Api: endpoints, middleware    Worker: Kafka consumer loop           │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Application                                                         │
│  Depends on: Domain (+ FluentValidation)                              │
│  Contains: Commands, Handlers, Validators, Ports, Events              │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Domain                                                              │
│  Depends on: nothing (pure)                                          │
│  Contains: Transaction, enums, Specifications, FraudRuleEngine,      │
│            Guard, Result                                             │
└─────────────────────────────────────────────────────────────────────┘
                            ▲
                            │
┌─────────────────────────────────────────────────────────────────────┐
│  Infrastructure                                                      │
│  Depends on: Domain, Application (implements ports)                  │
│  Contains: EF Core + repository, Kafka producer, Kafka options       │
└─────────────────────────────────────────────────────────────────────┘
```