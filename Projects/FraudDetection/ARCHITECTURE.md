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
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── SecurityHeadersMiddleware.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── FraudDetection.Application/
│   │   ├── Abstractions/
│   │   │   ├── IFraudRuleProvider.cs
│   │   │   ├── IBlacklistProvider.cs
│   │   │   └── ITransactionRepository.cs
│   │   └── Features/
│   │       └── Transactions/
│   │           ├── AnalyzeTransaction/
│   │           │   ├── AnalyzeTransactionCommand.cs
│   │           │   ├── AnalyzeTransactionValidator.cs
│   │           │   ├── AnalyzeTransactionHandler.cs
│   │           │   └── AnalyzeTransactionResult.cs
│   │           └── GetTransaction/
│   │               └── GetTransactionResponse.cs
│   │
│   ├── FraudDetection.Domain/
│   │   ├── Entities/
│   │   │   ├── Transaction.cs
│   │   │   ├── FraudRule.cs
│   │   │   └── BlacklistedCustomer.cs
│   │   ├── Enums/
│   │   │   ├── TransactionStatus.cs
│   │   │   └── FraudRuleAction.cs
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
│       ├── Configuration/
│       │   └── FraudRuleOptions.cs
│       ├── Persistence/
│       │   ├── Configurations/
│       │   │   ├── TransactionConfiguration.cs
│       │   │   ├── FraudRuleConfiguration.cs
│       │   │   └── BlacklistedCustomerConfiguration.cs
│       │   ├── Converters/
│       │   │   ├── TransactionIdConverter.cs
│       │   │   ├── CustomerIdConverter.cs
│       │   │   ├── FraudRuleIdConverter.cs
│       │   │   └── TransactionStatusConverter.cs
│       │   ├── Migrations/
│       │   │   ├── 20260730151852_InitialCreate.cs
│       │   │   ├── 20260730183216_AddActionCountryMetadata.cs
│       │   │   ├── 20260730192656_AddCustomerIdCreatedAtIndex.cs
│       │   │   └── 20260803232420_AddBlacklistedCustomer.cs
│       │   ├── Repositories/
│       │   │   └── EfTransactionRepository.cs
│       │   └── FraudDetectionDbContext.cs
│       └── Providers/
│           ├── InMemoryFraudRuleProvider.cs
│           ├── DbFraudRuleProvider.cs
│           └── DbBlacklistProvider.cs
│
└── tests/
    ├── FraudDetection.UnitTests/          (165 tests)
    └── FraudDetection.IntegrationTests/   (44 tests)
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
| `IFraudRuleProvider` | Provides fraud rules and specification mappings | `DbFraudRuleProvider` (active — reads rules from DB, thresholds from `FraudRuleOptions`), `InMemoryFraudRuleProvider` (testing fallback) |
| `IBlacklistProvider` | Provides blacklisted customer data | `DbBlacklistProvider` (active — reads from the `BlacklistedCustomers` table) |
| `ITransactionRepository` | Persists transactions and provides history queries | `EfTransactionRepository` |

Transactions are created, evaluated, and then persisted to the database. The blacklist is reloaded on every analysis request so recently added/removed customers are honored immediately.

### Port Location

```
Application/Abstractions/IFraudRuleProvider.cs
Application/Abstractions/IBlacklistProvider.cs
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
| `BlacklistedCustomer` | `CustomerId` | Created with a non-empty reason; used as the source for `BlacklistCustomerSpecification` |

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

### Blacklist Flow (Dynamic Specification Layering)

The blacklist is **database-backed and dynamic**:

1. `IBlacklistProvider` (port, Application) exposes `IsBlacklistedAsync`, `GetAllAsync`, `AddAsync`, `RemoveAsync`
2. `DbBlacklistProvider` (adapter, Infrastructure) implements it against the `BlacklistedCustomers` table via EF Core
3. The handler loads the full blacklist **on every analysis request** and constructs a fresh `BlacklistCustomerSpecification` from the current customer IDs
4. That specification is layered over the provider's static specifications in a copy of the dictionary — the rule engine needs no changes

```
AnalyzeTransactionHandler
   │  _blacklistProvider.GetAllAsync()          ← DB-backed, per request
   ▼
   new Dictionary<string, ISpecification>(staticSpecs)
   { ["Blacklist"] = new BlacklistCustomerSpecification(currentIds) }
   ▼
FraudRuleEngine.Evaluate(transaction, rules, effectiveSpecifications)
```

The `FraudRules` table stores the Blacklist rule metadata (risk score, Reject action); only the customer set itself is dynamic.

### Config-Driven Rule Parameters

Fraud rule thresholds are bound from the `FraudRules` appsettings section into `FraudRuleOptions` (Infrastructure/Configuration) and injected into `DbFraudRuleProvider`, which uses them to construct the static specifications:

| Setting | Default | Drives |
|---------|---------|--------|
| `HighAmountThreshold` | 10000 | `HighAmountTransactionSpecification` |
| `VelocityMaxTransactions` | 5 | `VelocityTransactionSpecification` |
| `VelocityWindowMinutes` | 60 | `VelocityTransactionSpecification` (time window) |
| `HighRiskCountries` | IR, KP, SY, VE | `HighRiskCountrySpecification` |

No business numbers are hardcoded in providers anymore — `InMemoryFraudRuleProvider` (testing fallback) still uses its own constants, which is intentional.

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
  SecurityHeadersMiddleware (global)
         │   Adds security headers to the response:
         │   X-Content-Type-Options, X-Frame-Options, Referrer-Policy,
         │   X-Permitted-Cross-Domain-Policies, Content-Security-Policy
         │   (HSTS applied via UseHsts() outside Development)
         ▼
  ExceptionHandlingMiddleware (global)
         │   Catches all unhandled exceptions
         │   Logs structured error with Method, Path, TraceIdentifier
         │   Returns 500 ProblemDetails (RFC 7807, application/problem+json)
         │   with requestId = context.TraceIdentifier
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
          │    new Transaction(id, customerId, amount, timestamp, country, metadata)
          │    transaction.RecentTransactionCount = await _transactionRepository.GetTransactionCountSinceAsync(customerId, since)  (real velocity query)
          │
          │  Gets rules + specs:
          │    _ruleProvider.GetAllRules()
          │    _ruleProvider.GetSpecifications()
          │
          │  Loads dynamic blacklist:
          │    var blacklisted = await _blacklistProvider.GetAllAsync()
          │    effectiveSpecs = specs + ["Blacklist" = new BlacklistCustomerSpecification(blacklisted ids)]
          │
          ▼
   FraudRuleEngine.Evaluate(transaction, rules, effectiveSpecs)
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
  Transaction persisted via ITransactionRepository.AddAsync
         │
         ▼
  AnalyzeTransactionResult { TransactionId, Status, TotalRiskScore, MatchedRules }
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
- `TransactionConfiguration` — configures ID, owned Money, required properties, `IX_Transactions_CustomerId_CreatedAt` index
- `FraudRuleConfiguration` — configures ID, required properties, Action column
- `BlacklistedCustomerConfiguration` — configures `CustomerId` as key, `Reason` (max 200), `CreatedAt`

### Migrations

Four migrations have been generated and are applied automatically on development startup (or in any environment with `AutoMigrate=true`):
- `InitialCreate` — Transactions + FraudRules tables
- `AddActionCountryMetadata` — Action column, Country, Metadata JSON
- `AddCustomerIdCreatedAtIndex` — velocity query index
- `AddBlacklistedCustomer` — BlacklistedCustomers table

### Integration Tests

Tests use **SQLite in-memory** (not SQL Server) to avoid external dependencies during CI:
```csharp
options.UseSqlite("DataSource=:memory:")
```

## Error Handling (ProblemDetails / RFC 7807)

- `AddProblemDetails()` registers the standard ProblemDetails service
- `ExceptionHandlingMiddleware` catches unhandled exceptions, logs them (Method, Path, TraceIdentifier), and returns `500 Internal Server Error` with an RFC 7807 `application/problem+json` body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred",
  "status": 500,
  "detail": "The server encountered an unexpected error. Please try again later.",
  "requestId": "0HN3V9FL7O3S7:00000001"
}
```

No stack traces or internal details are exposed. The `requestId` matches the structured log entry for correlation.

## Security Headers

`SecurityHeadersMiddleware` applies to every response:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `Referrer-Policy` | `no-referrer` | No referrer leakage |
| `X-Permitted-Cross-Domain-Policies` | `none` | Restricts Adobe/PDF cross-domain access |
| `Content-Security-Policy` | `default-src 'self'` | Restricts content sources |

`UseHsts()` is applied in non-development environments, enforcing HTTPS at the edge (terminated by a reverse proxy or load balancer in production).

## Docker Deployment

- **Multi-stage Dockerfile**: SDK 8.0 build stage (layer-cached restore via csproj copies, Release publish) → `aspnet:8.0` runtime stage
- Runtime stage installs `curl` (for the healthcheck), switches to the non-root `$APP_UID` user, sets `ASPNETCORE_URLS=http://+:8080`, and defines a `HEALTHCHECK` hitting `GET /health`
- **docker-compose.yml**: SQL Server 2022 (`mssql/server:2022-latest`) with a named volume and health gate, plus the API container that waits for SQL Server (`depends_on: condition: service_healthy`)
- `AutoMigrate=true` env var makes the API apply pending migrations and seed default rules + demo blacklisted customer on container startup
- The SA password is passed via `MSSQL_SA_PASSWORD` (default demo value; override via `.env`)
- `.dockerignore` excludes bin/, obj/, .git, test results, development appsettings, and `.env`

## Current Limitations

### Implemented (What Works)

- Full fraud analysis flow: HTTP → Handler → Domain Service → Result → Response
- 209 tests passing (165 unit + 44 integration), build with 0 errors and 0 warnings
- DbFraudRuleProvider active — reads fraud rules from database with auto-migration + seeding on dev startup
- Transaction persistence via ITransactionRepository (transactions persisted after analysis)
- Real velocity detection via GetTransactionCountSinceAsync() querying the database
- Country field on Transaction (ISO 3166-1 alpha-2) replacing currency proxy in HighRiskCountrySpecification
- Metadata dictionary on Transaction (stored as JSON column)
- Timestamp field on Transaction — client-provided timestamp used as CreatedAt
- GET /api/v1/transactions/{id} endpoint returning the typed GetTransactionResponse DTO
- Health check endpoint at GET /health with DB connectivity check
- Guard Pattern centralizing all precondition checks
- Result Pattern for Transaction state transitions
- EF Core mappings, value converters, and 4 migrations ready
- Global ExceptionHandlingMiddleware returning RFC 7807 ProblemDetails with requestId
- SecurityHeadersMiddleware + HSTS (non-development)
- Structured logging with ILogger<T> in handlers and middleware
- Performance benchmark tests (Stopwatch-based, < 1000ms budget with documented methodology)
- CustomerId + CreatedAt composite index for velocity query performance
- AsNoTracking() for read-only queries (GetTransactionCountSinceAsync)
- Blacklist persistence: BlacklistedCustomer entity, IBlacklistProvider port, DbBlacklistProvider, per-request dynamic specification layering
- Config-driven rule parameters (FraudRuleOptions bound from the FraudRules section)
- Docker containerization (multi-stage, non-root, HEALTHCHECK, AutoMigrate)
- GitHub Actions CI (path-filtered to Projects/FraudDetection)

### Amount Boundary Alignment

The `Money` Value Object validates `amount >= 0` (via `Guard.AgainstNegative`). The FluentValidation validator previously used `GreaterThan(0)`, which was stricter than the Domain. This mismatch meant that `Amount = 0` passed Domain validation but was rejected by the API.

**Fix:** Changed the FluentValidation rule to `GreaterThanOrEqualTo(0)` to match the Domain invariant. Domain is the source of truth — input validation must not be stricter than domain rules.

### Intentionally Deferred

| Feature | Rationale |
|---------|-----------|
| Composite specifications (AND/OR/NOT) | Current rules are independently evaluated — composition adds complexity without current benefit |
| Additional specifications | Only 4 rules required by the challenge — domain can be extended later |
| Blacklist CRUD API | Provider supports add/remove programmatically; HTTP endpoints deferred |
| Authentication / Authorization | Not scoped for this phase — documented in README |
| OpenTelemetry metrics and tracing | Structured logs + health endpoint cover current observability needs |
| Rate limiting | No protection against API abuse — documented |

### Engine Status Logic

| Status | Produced by Engine? | Condition |
|--------|---------------------|-----------|
| `Approved` | ✅ Yes | Returned when no rules match (risk score 0) |
| `UnderReview` | ✅ Yes | Returned when at least one rule matches and none have `Action == Reject` |
| `Rejected` | ✅ Yes | Returned when at least one matched rule has `Action == Reject` (takes precedence over review) |

### Future Extension

| Feature | Expected Sprint |
|---------|----------------|
| Blacklist CRUD API (HTTP) | Future |
| Composite specifications (AND/OR/NOT) | Future |
| OpenTelemetry metrics and tracing | Future |
| Authentication / authorization | Future |
| Rate limiting | Future |
| Load testing against SQL Server | Future |
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
