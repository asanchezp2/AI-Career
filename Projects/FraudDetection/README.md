# Fraud Detection API

A production-grade Fraud Detection API built with **.NET 8** using **Hexagonal Architecture** + **Vertical Slice** + **Explicit CQRS**.

## Goal

Analyze financial transactions in real time using a rules engine backed by the **Specification Pattern**. Returns risk decisions (Approved, UnderReview, Rejected) based on configurable fraud rules.

## Architecture

| Layer | Pattern | Responsibility |
|-------|---------|----------------|
| **Domain** | Rich Domain Model | Entities, Value Objects, Specifications, Domain Services, Guard Pattern, Result Pattern |
| **Application** | CQRS (explicit, no MediatR) | Commands, Validators, Handlers, Port interfaces |
| **Infrastructure** | Adapters | EF Core + SQL Server persistence, Fraud Rule Providers |
| **Api** | Minimal API | HTTP endpoints, DI composition root, Swagger |

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
| Testing | xUnit (158 unit + 33 integration) |
| API Docs | OpenAPI / Swagger |

## Project Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/                    # HTTP Adapter (Minimal API)
│   │   ├── Endpoints/
│   │   │   └── AnalyzeTransactionEndpoint.cs
│   │   ├── Program.cs                         # Composition root
│   │   └── appsettings.json
│   ├── FraudDetection.Application/            # Use cases & ports
│   │   ├── Abstractions/
│   │   │   └── IFraudRuleProvider.cs           # Port interface
│   │   └── Features/
│   │       └── Transactions/
│   │           └── AnalyzeTransaction/
│   │               ├── AnalyzeTransactionCommand.cs
│   │               ├── AnalyzeTransactionValidator.cs
│   │               ├── AnalyzeTransactionHandler.cs
│   │               └── AnalyzeTransactionResult.cs
│   ├── FraudDetection.Domain/                 # Pure domain logic
│   │   ├── Entities/
│   │   │   ├── Transaction.cs
│   │   │   └── FraudRule.cs
│   │   ├── Enums/
│   │   │   └── TransactionStatus.cs
│   │   ├── Guard.cs                          # Centralized precondition checks
│   │   ├── Result.cs                         # Result pattern (non-generic)
│   │   ├── Result{T}.cs                      # Result pattern (generic)
│   │   ├── Services/
│   │   │   ├── FraudRuleEngine.cs             # Domain service
│   │   │   └── FraudRuleEngineResult.cs
│   │   ├── Specifications/
│   │   │   ├── ISpecification.cs
│   │   │   └── Transactions/
│   │   │       └── HighAmountTransactionSpecification.cs
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── TransactionId.cs
│   │       ├── CustomerId.cs
│   │       └── FraudRuleId.cs
│   └── FraudDetection.Infrastructure/         # Adapter implementations
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Converters/
│       │   ├── Migrations/
│       │   └── FraudDetectionDbContext.cs
│   └── Providers/
│       ├── InMemoryFraudRuleProvider.cs    # Testing fallback
│       └── DbFraudRuleProvider.cs          # Active (reads from DB)
└── tests/
    ├── FraudDetection.UnitTests/              # 158 tests
    └── FraudDetection.IntegrationTests/        # 33 tests
```

## Current Status

**191 tests** — 158 unit, 33 integration. All passing.

### Implemented

- ✅ Domain entities: `Transaction`, `FraudRule`
- ✅ Value Objects: `Money`, `TransactionId`, `CustomerId`, `FraudRuleId`
- ✅ Specification Pattern: `ISpecification`, `HighAmountTransactionSpecification`, `VelocityTransactionSpecification`, `BlacklistCustomerSpecification`, `HighRiskCountrySpecification`
- ✅ Domain Service: `FraudRuleEngine` — stateless, evaluates rules via specifications
- ✅ Explicit CQRS: `AnalyzeTransactionCommand` → `Validator` → `Handler` → result
- ✅ Minimal API: `POST /api/v1/transactions/analyze`, `GET /api/v1/transactions/{id}`, `GET /health`
- ✅ FluentValidation with validation rules for all 7 command fields
- ✅ Guard Pattern: `Guard` static class (7 methods — null, empty, range, negative, whitespace)
- ✅ Result Pattern: `Result` / `Result<T>` for state transitions (Approve, Reject, MarkForReview)
- ✅ EF Core 8 + SQL Server: context, configurations, value converters, migrations
- ✅ `FraudRuleAction` enum (`Review`, `Reject`) — rules can now trigger rejection
- ✅ `FraudRuleEngine` produces `Rejected` status when any matched rule has `Action == Reject`
- ✅ Transaction persistence: transactions persisted to DB after analysis via `ITransactionRepository`
- ✅ Real velocity detection: `GetTransactionCountSinceAsync()` queries DB for recent transactions per customer
- ✅ Country field on `Transaction` — replaces Currency proxy in `HighRiskCountrySpecification`
- ✅ Metadata dictionary on `Transaction` — stored as JSON column in DB
- ✅ Timestamp field on `Transaction` — client-provided timestamp used as `CreatedAt`
- ✅ GET endpoint: `GET /api/v1/transactions/{id}` returns persisted transaction
- ✅ DbFraudRuleProvider active — reads rules from DB, auto-migration + seeding on dev startup
- ✅ InMemoryFraudRuleProvider preserved for testing
- ✅ Global exception handling middleware — replaces per-endpoint try/catch with sanitized JSON responses
- ✅ Structured logging in handler and middleware via `ILogger<T>`
- ✅ Health check endpoint: `GET /health` with DB connectivity check
- ✅ Performance benchmark tests: Stopwatch-based with < 100ms assertions
- ✅ CustomerId + CreatedAt composite index for velocity query performance
- ✅ AsNoTracking() for read queries (GetTransactionCountSinceAsync)

### Intentionally Deferred

- ⏳ Composite specifications (AND/OR/NOT) — current rules are independently evaluated
- ⏳ Production SQL Server provisioning (auto-migration+seeding works in dev)
- ⏳ Docker containerization
- ⏳ CI/CD pipeline
- ⏳ Authentication / Authorization
- ⏳ Kafka event streaming
- ⏳ AI-powered analysis

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](ARCHITECTURE.md) | Full architecture deep-dive with diagrams |
| [Challenge](CHALLENGE.md) | Technical challenge description |
| [Implementation Plan](IMPLEMENTATION_PLAN.md) | Sprint-based progress tracking |
| [Decisions](DECISIONS.md) | Architecture Decision Log (ADR-001 through ADR-033) |
| [KnowledgeBase](../KnowledgeBase/Architecture/) | Educational reference for patterns used |

## Quick Start

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the API (requires SQL Server connection string in appsettings.json)
dotnet run --project src/FraudDetection.Api
```

The API runs at `https://localhost:5001` with Swagger at `/swagger`. On development startup, pending migrations are auto-applied and four seed fraud rules are created.

### API Examples

```bash
# Analyze a transaction
curl -X POST "https://localhost:5001/api/v1/transactions/analyze" \
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
curl "https://localhost:5001/api/v1/transactions/3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b"

# Health check
curl "https://localhost:5001/health"
```

### Known Limitations

- Integration tests use SQLite in-memory (not SQL Server) — results are indicative for performance tests
- No authentication or authorization
- Blacklist IDs are seeded (no dedicated table with CRUD API)
- No Docker or CI/CD pipeline
- Production performance not guaranteed from SQLite-based tests
