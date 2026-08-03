# Fraud Detection API

A production-grade Fraud Detection API built with **.NET 8** using **Hexagonal Architecture** + **Vertical Slice** + **Explicit CQRS**.

## Goal

Analyze financial transactions in real time using a rules engine backed by the **Specification Pattern**. Returns risk decisions (Approved, UnderReview, Rejected) based on configurable fraud rules.

## Architecture

| Layer | Pattern | Responsibility |
|-------|---------|----------------|
| **Domain** | Rich Domain Model | Entities, Value Objects, Specifications, Domain Services, Guard Pattern, Result Pattern |
| **Application** | CQRS (explicit, no MediatR) | Commands, Validators, Handlers, Port interfaces |
| **Infrastructure** | Adapters | EF Core + SQL Server persistence, Fraud Rule + Blacklist Providers |
| **Api** | Minimal API | HTTP endpoints, DI composition root, middleware, Swagger |

- **Hexagonal Architecture** (Ports & Adapters) — domain is pure; infrastructure plugs in via interfaces
- **Vertical Slices** — code organized by feature, not by layer
- **CQRS** — explicit Command/Handler pattern without MediatR
- **Specification Pattern** — business rules extracted into composable classes
- **Guard Pattern** — centralized precondition checks replacing scattered null/range/empty validation
- **Result Pattern** — state transitions return `Result` instead of throwing exceptions for expected failures

## Technologies

| Category | Technology |
|----------|------------|
| Runtime | .NET 8, ASP.NET Core |
| Persistence | Entity Framework Core 8 + SQL Server |
| Validation | FluentValidation |
| Testing | xUnit (165 unit + 44 integration) |
| API Docs | OpenAPI / Swagger (development only) |
| Containerization | Docker + docker-compose (SQL Server 2022 + API) |
| CI/CD | GitHub Actions (path-filtered workflow) |

## Project Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/                    # HTTP Adapter (Minimal API)
│   │   ├── Endpoints/
│   │   │   └── AnalyzeTransactionEndpoint.cs  # POST analyze + GET by id
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs # RFC 7807 ProblemDetails
│   │   │   └── SecurityHeadersMiddleware.cs   # Security headers + HSTS
│   │   ├── Program.cs                         # Composition root
│   │   └── appsettings.json
│   ├── FraudDetection.Application/            # Use cases & ports
│   │   ├── Abstractions/
│   │   │   ├── IFraudRuleProvider.cs          # Port interface
│   │   │   ├── IBlacklistProvider.cs          # Port interface
│   │   │   └── ITransactionRepository.cs      # Port interface
│   │   └── Features/
│   │       └── Transactions/
│   │           ├── AnalyzeTransaction/        # Command, Validator, Handler, Result
│   │           └── GetTransaction/
│   │               └── GetTransactionResponse.cs
│   ├── FraudDetection.Domain/                 # Pure domain logic
│   │   ├── Entities/
│   │   │   ├── Transaction.cs
│   │   │   ├── FraudRule.cs
│   │   │   └── BlacklistedCustomer.cs
│   │   ├── Enums/
│   │   │   ├── TransactionStatus.cs
│   │   │   └── FraudRuleAction.cs
│   │   ├── Guard.cs                           # Centralized precondition checks
│   │   ├── Result.cs / Result{T}.cs           # Result pattern
│   │   ├── Services/
│   │   │   ├── FraudRuleEngine.cs             # Domain service
│   │   │   └── FraudRuleEngineResult.cs
│   │   ├── Specifications/
│   │   │   ├── ISpecification.cs
│   │   │   └── Transactions/                  # HighAmount, Velocity, Blacklist, HighRiskCountry
│   │   └── ValueObjects/                      # Money, TransactionId, CustomerId, FraudRuleId
│   └── FraudDetection.Infrastructure/         # Adapter implementations
│       ├── Configuration/
│       │   └── FraudRuleOptions.cs            # Config-driven rule parameters
│       ├── Persistence/
│       │   ├── Configurations/                # Per-entity EF Core configs
│       │   ├── Converters/                    # Value converters for strongly-typed IDs
│       │   ├── Migrations/                    # 4 migrations incl. AddBlacklistedCustomer
│       │   ├── Repositories/
│       │   │   └── EfTransactionRepository.cs
│       │   └── FraudDetectionDbContext.cs
│       └── Providers/
│           ├── DbFraudRuleProvider.cs         # Active (reads rules from DB)
│           ├── InMemoryFraudRuleProvider.cs   # Testing fallback
│           └── DbBlacklistProvider.cs         # Active (reads blacklist from DB)
└── tests/
    ├── FraudDetection.UnitTests/              # 165 tests
    └── FraudDetection.IntegrationTests/       # 44 tests
```

## Current Status

**209 tests** — 165 unit, 44 integration. All passing. Build: 0 errors, 0 warnings.

### Implemented

- Domain entities: `Transaction`, `FraudRule`, `BlacklistedCustomer`
- Value Objects: `Money`, `TransactionId`, `CustomerId`, `FraudRuleId`
- Specification Pattern: `ISpecification` + 4 specifications (HighAmount, Velocity, Blacklist, HighRiskCountry)
- Domain Service: `FraudRuleEngine` — stateless, evaluates rules via specifications
- Explicit CQRS: `AnalyzeTransactionCommand` → `Validator` → `Handler` → result
- Endpoints: `POST /api/v1/transactions/analyze`, `GET /api/v1/transactions/{id}`, `GET /health`
- Typed GET response: `GetTransactionResponse` record (replaces anonymous object)
- FluentValidation with validation rules for all command fields
- Guard Pattern + Result Pattern for domain invariants and state transitions
- EF Core 8 + SQL Server: context, configurations, value converters, 4 migrations
- Transaction persistence after analysis (`ITransactionRepository` / `EfTransactionRepository`)
- Real velocity detection: `GetTransactionCountSinceAsync()` with `CustomerId + CreatedAt` index and `AsNoTracking()`
- **Blacklist persistence**: `BlacklistedCustomer` table (migration `AddBlacklistedCustomer`), `IBlacklistProvider` port, `DbBlacklistProvider` implementation; the handler reloads the blacklist on every request and layers a dynamic `BlacklistCustomerSpecification` over the rule provider's static specifications
- Config-driven rules: `FraudRuleOptions` bound from the `FraudRules` appsettings section (HighAmountThreshold, VelocityMaxTransactions, VelocityWindowMinutes, HighRiskCountries) — no hardcoded business numbers
- **ProblemDetails (RFC 7807)**: `AddProblemDetails()` + `ExceptionHandlingMiddleware` returns `application/problem+json` with a `requestId` — no stack traces leaked
- **Security headers**: `SecurityHeadersMiddleware` (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, X-Permitted-Cross-Domain-Policies, CSP) + HSTS in non-development environments
- Structured logging via `ILogger<T>` in handler and middleware
- Health check endpoint with DB connectivity check
- Performance benchmark tests: wall-clock Stopwatch assertions with `< 1000ms` budget and documented methodology
- Docker: multi-stage Dockerfile (non-root user, HEALTHCHECK), docker-compose (SQL Server 2022 + API), `AutoMigrate` env var for containerized startup
- CI/CD: GitHub Actions workflow (`Projects/FraudDetection` path-filtered) — restore, build, test, upload test results

### Intentionally Deferred

- Composite specifications (AND/OR/NOT) — current rules are independently evaluated
- Blacklist CRUD API — the provider supports add/remove programmatically; no HTTP endpoints yet
- Authentication / Authorization
- OpenTelemetry metrics and tracing
- Rate limiting
- Kafka event streaming
- AI-powered analysis

## Security

- No authentication or authorization — out of scope for the challenge and documented as a known limitation (see Known Limitations)
- Security headers applied to all responses: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none`, `Content-Security-Policy: default-src 'self'`
- HSTS enabled in non-development environments
- Errors return RFC 7807 `ProblemDetails` with a `requestId` — internal details and stack traces are never exposed
- No secrets in source code; the Docker SA password is provided via the `MSSQL_SA_PASSWORD` environment variable
- Swagger UI is enabled in Development only

## Observability

- Structured logging (`ILogger<T>` with named properties) for analysis events and unhandled exceptions
- `GET /health` liveness endpoint with database connectivity check
- `requestId` correlation on every 500 response (`TraceIdentifier`)

## Configuration

Fraud rule parameters are config-driven via the `FraudRules` section in `appsettings.json`:

```json
"FraudRules": {
  "HighAmountThreshold": 10000,
  "VelocityMaxTransactions": 5,
  "VelocityWindowMinutes": 60,
  "HighRiskCountries": ["IR", "KP", "SY", "VE"]
}
```

These values are bound to `FraudRuleOptions` at startup and drive the specifications created by `DbFraudRuleProvider`. Changing a threshold requires no code change.

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](ARCHITECTURE.md) | Full architecture deep-dive with diagrams |
| [Challenge](CHALLENGE.md) | Technical challenge description |
| [Implementation Plan](IMPLEMENTATION_PLAN.md) | Sprint-based progress tracking |
| [Decisions](DECISIONS.md) | Architecture Decision Log (ADR-001 through ADR-040) |
| [KnowledgeBase](../KnowledgeBase/Architecture/) | Educational reference for patterns used |

## Quick Start

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the API (requires SQL Server; adjust the connection string in appsettings.json)
dotnet run --project src/FraudDetection.Api
```

The API runs at `https://localhost:7289` (HTTPS profile) or `http://localhost:5232` with Swagger at `/swagger`. On development startup, pending migrations are auto-applied, the four default fraud rules are seeded, and one demo blacklisted customer is created (`00000000-0000-0000-0000-000000000001`).

### Docker

Run the API and SQL Server in containers (requires Docker with the Compose plugin; first start pulls the .NET 8 and SQL Server images):

```bash
# Start SQL Server + API (builds the image on first run)
docker compose up --build

# Stop (add -v to also remove the SQL Server data volume)
docker compose down
```

- The API listens on `http://localhost:8080` — health check at `http://localhost:8080/health`.
- SQL Server 2022 runs in a container on port `1433` with a named volume (`sqlserver-data`) for persistence, so data survives `docker compose down`. The API waits for SQL Server's health check before starting.
- On startup the API **auto-applies migrations and seeds the default fraud rules and demo blacklisted customer** — the compose file sets `AutoMigrate=true` (Program.cs runs auto-migration when `Development` or `AutoMigrate=true`), so no manual `dotnet ef database update` is required.
- The container runs as a non-root user and exposes a `HEALTHCHECK` on `/health`.
- The SA password defaults to `Your_Strong_Passw0rd!` (demo only) — **override it** with the `MSSQL_SA_PASSWORD` environment variable, e.g. in a `.env` file in this directory (git-ignored):

```bash
# .env (optional, but recommended outside local demos)
MSSQL_SA_PASSWORD=Your_Strong_Passw0rd!
```

Test it:

```bash
curl http://localhost:8080/health
```

### API Examples

```bash
# Analyze a transaction
curl -X POST "https://localhost:7289/api/v1/transactions/analyze" \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
    "customerId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "amount": 15000.00,
    "currency": "USD",
    "timestamp": "2026-07-30T12:00:00Z",
    "country": "US",
    "metadata": {
      "channel": "web",
      "userAgent": "Mozilla/5.0"
    }
  }'

# Get a transaction by ID
curl "https://localhost:7289/api/v1/transactions/3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b"

# Health check
curl "https://localhost:7289/health"
```

### Known Limitations

- No authentication or authorization — any client can call the API (documented, out of scope)
- No blacklist CRUD API — blacklist entries are managed via the database or provider methods
- Integration tests run against SQLite in-memory, not SQL Server — performance numbers are indicative only
- No load/benchmark testing against SQL Server — production latency is an architectural expectation, not a measured guarantee
- EF Core logs a warning for the metadata `Dictionary<string, string>` value comparer (dictionary mutation is not tracked) — accepted trade-off for a JSON column
- No OpenTelemetry/metrics — observability is structured logs + health endpoint
- No rate limiting on the analyze endpoint

## CI/CD

The repository-level GitHub Actions workflow (`.github/workflows/ci.yml`) runs on push and pull requests to `main`, path-filtered to `Projects/FraudDetection/**`: restore, Release build, full test suite, and test-result artifact upload.
