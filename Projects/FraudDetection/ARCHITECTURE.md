# Fraud Detection API — Architecture

## Why Hexagonal Architecture

Hexagonal Architecture (Ports & Adapters) provides:

| Benefit | Description |
|---------|-------------|
| Testability | Business logic can be tested without HTTP, DB, or external systems |
| Replaceability | Adapters can be swapped (e.g., InMemory → Db provider) |
| Evolution | New adapters added without changing core logic |
| Simplicity | Clear separation between "what" (Domain) and "how" (Adapters) |

## Why Vertical Slice

Vertical Slice organizes code by use case, not by technical layer:

| Benefit | Description |
|---------|-------------|
| Cohesion | Each slice contains everything needed for one feature |
| Isolation | Changes to one slice don't affect others |
| Clarity | Easy to understand the flow of a specific feature |

## Why Explicit CQRS

CQRS is implemented **explicitly** — no MediatR, no `IRequest<T>`:

| Benefit | Description |
|---------|-------------|
| Simplicity | No framework overhead for a single use case |
| Visibility | Dependency graph is visible in the Handler constructor |
| Debuggability | No mediator indirection — call chain is linear |
| Control | No assembly scanning, no pipeline behaviors to configure |

## Dependency Direction

```
Api ──→ Application ──→ Domain
 │                       │
 └──→ Infrastructure ───┘
      (implements ports)
```

**Rules:**
- Dependencies always point **inward** toward Domain
- **Domain** has zero dependencies — not on ASP.NET, EF Core, or any external library
- **Application** depends only on Domain (and FluentValidation — a library, not framework)
- **Infrastructure** depends on Application + Domain
- **Api** depends on everything (composition root)

## Projects

| Project | Responsibility |
|---------|----------------|
| **Domain** | Business rules, entities, value objects, specifications, domain services |
| **Application** | Use cases, CQRS commands/handlers, ports (interfaces), FluentValidation |
| **Infrastructure** | Adapter implementations: EF Core DbContext, providers, value converters, entity configs |
| **Api** | HTTP adapter (Minimal API), DI composition root, Swagger |

## Real Folder Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/
│   │   ├── Endpoints/
│   │   │   └── AnalyzeTransactionEndpoint.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── FraudDetection.Application/
│   │   ├── Abstractions/
│   │   │   └── IFraudRuleProvider.cs
│   │   └── Features/
│   │       └── Transactions/
│   │           └── AnalyzeTransaction/
│   │               ├── AnalyzeTransactionCommand.cs
│   │               ├── AnalyzeTransactionValidator.cs
│   │               ├── AnalyzeTransactionHandler.cs
│   │               └── AnalyzeTransactionResult.cs
│   │
│   ├── FraudDetection.Domain/
│   │   ├── Entities/
│   │   │   ├── Transaction.cs
│   │   │   └── FraudRule.cs
│   │   ├── Enums/
│   │   │   └── TransactionStatus.cs
│   │   ├── Services/
│   │   │   ├── FraudRuleEngine.cs
│   │   │   └── FraudRuleEngineResult.cs
│   │   ├── Specifications/
│   │   │   ├── ISpecification.cs
│   │   │   └── Transactions/
│   │   │       ├── BlacklistCustomerSpecification.cs
│   │   │       ├── HighAmountTransactionSpecification.cs
│   │   │       ├── HighRiskCountrySpecification.cs
│   │   │       └── VelocityTransactionSpecification.cs
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── TransactionId.cs
│   │       ├── CustomerId.cs
│   │       └── FraudRuleId.cs
│   │
│   └── FraudDetection.Infrastructure/
│       ├── Persistence/
│       │   ├── Configurations/
│       │   │   ├── TransactionConfiguration.cs
│       │   │   └── FraudRuleConfiguration.cs
│       │   ├── Converters/
│       │   │   ├── TransactionIdConverter.cs
│       │   │   ├── CustomerIdConverter.cs
│       │   │   ├── FraudRuleIdConverter.cs
│       │   │   └── TransactionStatusConverter.cs
│       │   ├── Migrations/
│       │   │   └── 20260730151852_InitialCreate.cs
│       │   └── FraudDetectionDbContext.cs
│       └── Providers/
│           ├── InMemoryFraudRuleProvider.cs
│           └── DbFraudRuleProvider.cs
│
└── tests/
    ├── FraudDetection.UnitTests/          (158 tests)
    └── FraudDetection.IntegrationTests/   (33 tests)
```

## Ports and Adapters

### Primary (Driving) Ports

| Port | Purpose | Adapter |
|------|---------|---------|
| `POST /api/v1/transactions/analyze` | Analyze a transaction | `AnalyzeTransactionEndpoint` (Minimal API) |
| `GET /api/v1/transactions/{id}` | Retrieve a persisted transaction | `AnalyzeTransactionEndpoint` (Minimal API) |
| `GET /health` | Liveness/health check | Inline Minimal API delegate in Program.cs |

There are no interface-based primary ports — the Minimal API delegate invokes the Handler directly. In hexagonal terms, the HTTP endpoint IS the inbound adapter.

### Secondary (Driven) Ports

| Port | Purpose | Implementations |
|------|---------|-----------------|
| `IFraudRuleProvider` | Provides fraud rules and specification mappings | `DbFraudRuleProvider` (active — reads from DB), `InMemoryFraudRuleProvider` (testing fallback) |

`ITransactionRepository` port provides transaction persistence and history queries. Transactions are created, evaluated, and then persisted to the database.

### Port Location

```
Application/Abstractions/IFraudRuleProvider.cs
Application/Abstractions/ITransactionRepository.cs
```

Ports are defined in the **Application Layer** because they represent capabilities the application needs from the outside world. The Domain defines business rules; the Application defines what it needs from infrastructure.

## Domain Layer

### Guard Pattern

The `Guard` class centralizes precondition validation, replacing repetitive inline checks across the Domain:

| Method | Replaces | Used In |
|--------|----------|---------|
| `AgainstNull<T>(T?, string)` | `ArgumentNullException.ThrowIfNull` + nullables | Entity/VO constructors, FraudRuleEngine, Handler |
| `AgainstNullOrWhiteSpace(string, string)` | Manual `string.IsNullOrWhiteSpace` checks | `FraudRule.Rename`, `Money` constructor |
| `AgainstOutOfRange(int, int, int, string)` | `ArgumentOutOfRangeException` | `FraudRule.ChangeRiskScore` (0–100) |
| `AgainstOutOfRange(decimal, decimal, decimal, string)` | Manual range checks | Reserved for future decimal-range VOs |
| `AgainstEmptyGuid(Guid, string)` | 3 identical `Guid.Empty` checks | `TransactionId`, `CustomerId`, `FraudRuleId` constructors |
| `AgainstNegative(decimal, string)` | Negative value checks | `Money.Amount`, `HighAmountTransactionSpecification` threshold |

**Why centralized:** Eliminates 11 scattered null checks and 3 identical `Guid.Empty` checks. The Guard lives in Domain (not SharedKernel) because all consumers are Domain classes — no external project needs it.

### Result Pattern

State transitions in `Transaction` (`Approve`, `Reject`, `MarkForReview`) return `Result` instead of throwing `InvalidOperationException`:

- **`Result`** (non-generic) — success/failure with error message; returned by `Transaction.ChangeStatus()`
- **`Result<T>`** (generic) — success with value or failure with error; available for future use

**Why only for state transitions:** State transitions represent expected domain flows where a caller (Handler) should handle the outcome explicitly. Other preconditions (null checks, range validation) remain exception-based because they represent programming errors, not expected business outcomes.

The `ApplyRecommendedStatus` method in the handler checks `result.IsFailure` and throws only if the programmatic contract is violated (transaction not in Pending state), which would indicate a bug.

### Entities

| Entity | Identity | Key Behavior |
|--------|----------|-------------|
| `Transaction` | `TransactionId` | `Approve()`, `Reject()`, `MarkForReview()` — return `Result`, protected via `ChangeStatus()` |
| `FraudRule` | `FraudRuleId` | `Enable()`, `Disable()`, `Rename()`, `ChangeRiskScore()` |

### Value Objects

| VO | Wraps | Validation | Guard Methods Used |
|----|-------|------------|-------------------|
| `Money` | `decimal Amount` + `string Currency` | Amount ≥ 0, Currency 3-letter ISO | `AgainstNegative`, `AgainstNullOrWhiteSpace` |
| `TransactionId` | `Guid` | Not `Guid.Empty` | `AgainstEmptyGuid` |
| `CustomerId` | `Guid` | Not `Guid.Empty` | `AgainstEmptyGuid` |
| `FraudRuleId` | `Guid` | Not `Guid.Empty` | `AgainstEmptyGuid` |

### Specification Pattern

```csharp
// Domain/Specifications/ISpecification.cs
public interface ISpecification
{
    bool IsSatisfiedBy(Transaction transaction);
}
```

The interface is **non-generic** (YAGNI — only `Transaction` is evaluated). Implementations live in `Domain/Specifications/Transactions/`.

Four specifications currently exist:
- `HighAmountTransactionSpecification` — evaluates `transaction.Amount.Amount >= threshold`
- `VelocityTransactionSpecification` — evaluates `transaction.RecentTransactionCount >= max` (velocity check)
- `BlacklistCustomerSpecification` — evaluates whether `transaction.CustomerId` is in a blacklist set
- `HighRiskCountrySpecification` — evaluates whether `transaction.Country` is a high-risk country code (ISO 3166-1 alpha-2)

### Domain Service: FraudRuleEngine

The `FraudRuleEngine` is a **stateless domain service** that:

1. Receives a `Transaction`, a list of `FraudRule`, and a specification dictionary
2. Iterates enabled rules and evaluates their specifications
3. Accumulates risk scores from matched rules
4. Returns a `FraudRuleEngineResult` with total risk score and recommended status

```csharp
public FraudRuleEngineResult Evaluate(
    Transaction transaction,
    IEnumerable<FraudRule> fraudRules,
    IReadOnlyDictionary<string, ISpecification> specifications)
```

**Risk logic:**
- `matchedRules.Any(Action == Reject)` → `Rejected`
- `totalRiskScore > 0` → `UnderReview`
- `totalRiskScore == 0` → `Approved`
- Rejection takes precedence over review — if both rejection and review rules match, the transaction is `Rejected`

## Request Lifecycle

```
HTTP POST /api/v1/transactions/analyze
         │
         ▼
  ExceptionHandlingMiddleware (global)
         │   Catches all unhandled exceptions
         │   Logs structured error with Method, Path, TraceIdentifier
         │   Returns 500 sanitized JSON response
         │
         ▼
  AnalyzeTransactionEndpoint
         │  Creates: AnalyzeTransactionCommand (deserialized from JSON body)
         │  Calls:   validator.ValidateAsync(command)
         │
         ▼
   AnalyzeTransactionValidator
          │
          │  Validates: TransactionId != empty, Amount >= 0,
          │  Currency.Length == 3, Currency is uppercase, CustomerId != empty
         │
         ▼  (if invalid → return 400 ValidationProblem)
         │
  AnalyzeTransactionHandler.Handle(command)
         │
          │  Creates domain objects:
          │    TransactionId.From(), CustomerId.From(), new Money()
          │    new Transaction(id, customerId, amount)
          │    transaction.RecentTransactionCount = await _transactionRepository.GetTransactionCountSinceAsync(customerId, since)  (real velocity query)
          │
          │  Gets rules + specs:
          │    _ruleProvider.GetAllRules()
          │    _ruleProvider.GetSpecifications()
          │
          ▼
   FraudRuleEngine.Evaluate(transaction, rules, specifications)
          │
          │  For each enabled rule with matching spec:
          │    if spec.IsSatisfiedBy(transaction) → add risk score
          │    if matched rule has Action == Reject → status = Rejected
          │
          ▼
   FraudRuleEngineResult { TotalRiskScore, RecommendedStatus, MatchedRules }
         │
         ▼
  Handler applies status via Result pattern:
    Approved → transaction.Approve()  → Result
    UnderReview → transaction.MarkForReview() → Result
    Rejected → transaction.Reject() → Result
    If result.IsFailure → throw (programming contract violation)
         │
         ▼
  AnalyzeTransactionResult { TransactionId, Status, TotalRiskScore }
         │
         ▼
  HTTP 200 OK ← JSON response
```

## EF Core Mapping Strategy

All EF Core concerns are isolated in **Infrastructure**.

### Value Converters

Strongly-typed IDs are mapped via `ValueConverter<TModel, TProvider>`:

| Converter | Converts |
|-----------|----------|
| `TransactionIdConverter` | `TransactionId` ↔ `Guid` |
| `CustomerIdConverter` | `CustomerId` ↔ `Guid` |
| `FraudRuleIdConverter` | `FraudRuleId` ↔ `Guid` |
| `TransactionStatusConverter` | `TransactionStatus` ↔ `string` |

### Owned Types

`Money` is mapped as an **owned type** (not a converter), producing two columns:
- `Amount_Amount` (decimal)
- `Amount_Currency` (string)

### Entity Configurations

Separate `IEntityTypeConfiguration<T>` classes per entity:
- `TransactionConfiguration` — configures ID, owned Money, required properties
- `FraudRuleConfiguration` — configures ID, required properties

### Migrations

The `InitialCreate` migration is already generated and ready. The `FraudDetectionDbContext` is configured for SQL Server but no database is running.

### Integration Tests

Tests use **SQLite in-memory** (not SQL Server) to avoid external dependencies during CI:
```csharp
options.UseSqlite("DataSource=:memory:")
```

## Current Limitations

### Implemented (What Works)

- Full fraud analysis flow: HTTP → Handler → Domain Service → Result → Response
- 191 tests passing (158 unit + 33 integration)
- DbFraudRuleProvider active — reads fraud rules from database with auto-migration + seeding on dev startup
- Transaction persistence via ITransactionRepository (transactions persisted after analysis)
- Real velocity detection via GetTransactionCountSinceAsync() querying the database
- Country field on Transaction (ISO 3166-1 alpha-2) replacing currency proxy in HighRiskCountrySpecification
- Metadata dictionary on Transaction (stored as JSON column)
- Timestamp field on Transaction — client-provided timestamp used as CreatedAt
- GET /api/v1/transactions/{id} endpoint for retrieving persisted transactions
- Health check endpoint at GET /health with DB connectivity check
- Guard Pattern centralizing all precondition checks
- Result Pattern for Transaction state transitions
- EF Core mappings, value converters, and migration ready
- Global ExceptionHandlingMiddleware replacing per-endpoint try/catch
- Structured logging with ILogger<T> in handlers and middleware
- Performance benchmark tests (Stopwatch-based, < 100ms assertions)
- CustomerId + CreatedAt composite index for velocity query performance
- AsNoTracking() for read-only queries (GetTransactionCountSinceAsync)

### Amount Boundary Alignment

The `Money` Value Object validates `amount >= 0` (via `Guard.AgainstNegative`). The FluentValidation validator previously used `GreaterThan(0)`, which was stricter than the Domain. This mismatch meant that `Amount = 0` passed Domain validation but was rejected by the API.

**Fix:** Changed the FluentValidation rule to `GreaterThanOrEqualTo(0)` to match the Domain invariant. Domain is the source of truth — input validation must not be stricter than domain rules.

### Intentionally Deferred

| Feature | Rationale |
|---------|-----------|
| Composite specifications (AND/OR/NOT) | Current rules are independently evaluated — composition adds complexity without current benefit |
| Additional specifications | Only 4 rules required by the challenge — domain can be extended later |
| Production SQL Server provisioning | Dev auto-migration+seeding works; production DB not provisioned |
| Authentication / Authorization | Not scoped for this phase |

### Engine Status Logic

| Status | Produced by Engine? | Condition |
|--------|---------------------|-----------|
| `Approved` | ✅ Yes | Returned when no rules match (risk score 0) |
| `UnderReview` | ✅ Yes | Returned when at least one rule matches and none have `Action == Reject` |
| `Rejected` | ✅ Yes | Returned when at least one matched rule has `Action == Reject` (takes precedence over review) |

### Future Extension

| Feature | Expected Sprint |
|---------|----------------|
| Production SQL Server provisioning | Future |
| Composite specifications (AND/OR/NOT) | Future |
| Docker containerization | Future |
| CI/CD pipeline | Future |
| Blacklist persistence (dedicated table) | Future |
| OpenTelemetry metrics and tracing | Future |
| Kafka event streaming | Sprint 7 |
| AI-powered analysis | Sprint 8 |
| n8n automation | Future |

## Architecture Diagrams

### Hexagonal View

```
                    ┌──────────────────────────────────────────┐
                    │              Domain Layer                │
                    │                                          │
                    │  ┌──────────┐  ┌──────────────────────┐  │
                    │  │ Entities │  │   Specifications     │  │
                    │  │ Transaction  │  ISpecification       │  │
                    │  │ FraudRule │  │ HighAmountSpec      │  │
                     │  │              │  │ VelocitySpec        │  │
                     │  │              │  │ BlacklistSpec       │  │
                     │  │              │  │ HighRiskCountrySpec │  │
                    │  └──────────┘  └──────────────────────┘  │
                    │  ┌──────────────────────────────────┐    │
                    │  │   FraudRuleEngine (Domain Svc)   │    │
                    │  └──────────────────────────────────┘    │
                    └──────────────────────────────────────────┘
                              ▲            ▲
                              │            │
                    ┌─────────┴────────────┴──────────────────┐
                    │          Application Layer               │
                    │                                          │
                    │  ┌────────────────────────────────────┐  │
                    │  │    AnalyzeTransaction Slice        │  │
                    │  │  Command → Validator → Handler    │  │
                    │  └────────────────────────────────────┘  │
                    │  ┌────────────────────────────────────┐  │
                    │  │   IFraudRuleProvider (Port)        │  │
                    │  └────────────────────────────────────┘  │
                    └──────────────────────────────────────────┘
                              ▲            ▲
               ┌──────────────┘            └──────────────┐
               │                                           │
    ┌──────────┴──────────┐                  ┌────────────┴──────────┐
    │    Api Layer         │                  │  Infrastructure      │
    │  (Inbound Adapter)   │                  │  (Outbound Adapters) │
    │                      │                  │                      │
    │  AnalyzeTransaction  │                  │  InMemoryRuleProv.   │
    │  Endpoint (Minimal)  │                  │  DbFraudRuleProvider │
    │                      │                  │  EF Core DbContext   │
    │  Program.cs (DI)     │                  │  ValueConverters     │
    └──────────────────────┘                  └──────────────────────┘
```

### Dependency Graph (Layer Diagram)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Api                                                                 │
│  Depends on: Application, Infrastructure, Domain                     │
│  Contains: Endpoints, Program.cs (composition root)                  │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Application                                                         │
│  Depends on: Domain (+ FluentValidation)                              │
│  Contains: Commands, Handlers, Validators, Port interfaces            │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Domain                                                              │
│  Depends on: nothing (pure)                                          │
│  Contains: Entities, VOs, Specifications, Domain Services            │
└─────────────────────────────────────────────────────────────────────┘
                            ▲
                            │
┌─────────────────────────────────────────────────────────────────────┐
│  Infrastructure                                                      │
│  Depends on: Domain, Application (implements ports)                  │
│  Contains: EF Core, Providers, Converters, Configurations            │
└─────────────────────────────────────────────────────────────────────┘
```
