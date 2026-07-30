# Fraud Detection API — Implementation Plan

## Sprint 1: Solution Structure + Domain + Application Foundation

**Status:** ✅ Complete

**Objective:** Create solution structure, implement domain layer, and set up application layer foundation.

**Tasks Completed:**
- ✅ Create solution file and all projects
- ✅ Implement `Transaction` Entity with behavior
- ✅ Implement Value Objects (`Money`, `TransactionId`, `CustomerId`)
- ✅ Implement `TransactionStatus` Enum
- ✅ Add FluentValidation for `AnalyzeTransactionCommand`
- ✅ Set up Vertical Slice folder structure
- ✅ Create 40 unit tests

**Deliverables:**
- `FraudDetection.sln` with all projects (Domain, Application, Api, Infrastructure, UnitTests, IntegrationTests)
- Domain layer with Entity, Value Objects, Enums
- Application layer with Command, Validator, Handler placeholder
- 40 passing unit tests

---

## Sprint 2: Domain Layer Completion

**Status:** ✅ Complete

**Objective:** Complete domain layer with `FraudRule` entity and `FraudRuleEngine` domain service.

**Tasks Completed:**
- ✅ Implement `FraudRule` entity with `FraudRuleId` value object
- ✅ Implement `FraudRuleEngine` stateless domain service
- ✅ Implement `FraudRuleEngineResult` record
- ✅ Implement `ISpecification` contract
- ✅ Implement `HighAmountTransactionSpecification`
- ✅ Create unit tests

**Deliverables:**
- `FraudRule` entity (rich model with Enable/Disable/Rename/ChangeRiskScore behavior)
- `FraudRuleEngine` domain service
- Specification Pattern infrastructure
- Unit tests

**Notes:**
- SharedKernel was intentionally not created — the project had one but it remained empty and was deleted
- No generic `ISpecification<T>` was needed (YAGNI — only `Transaction` is evaluated)
- No composite specifications (AND/OR/NOT) — deferred for future sprints

---

## Sprint 3: Application Layer

**Status:** ✅ Complete

**Objective:** Implement the AnalyzeTransaction use case with explicit CQRS.

**Tasks Completed:**
- ✅ Define `IFraudRuleProvider` port interface in Application.Abstractions
- ✅ Implement `AnalyzeTransactionHandler` (explicit Handler, no MediatR)
- ✅ Implement `AnalyzeTransactionResult` DTO
- ✅ Enhance `AnalyzeTransactionValidator` with 6 rules
- ✅ Wire handler to `FraudRuleEngine` and `IFraudRuleProvider` via DI
- ✅ Create unit tests

**Deliverables:**
- Port interface `IFraudRuleProvider`
- Use case implementation
- DTOs (Command, Result)
- Unit tests

**Design Decision:**
- CQRS is implemented **explicitly** — no MediatR, no `IRequest<T>` interfaces.
- The Handler receives dependencies directly via constructor injection (FraudRuleEngine, IFraudRuleProvider).
- This avoids framework overhead and keeps the dependency graph visible.

---

## Sprint 4: API Layer

**Status:** ✅ Complete

**Objective:** Implement HTTP adapter using Minimal API.

**Tasks Completed:**
- ✅ Create `POST /api/transactions/analyze` endpoint
- ✅ Implement validation error handling (FluentValidation → `Results.ValidationProblem`)
- ✅ Configure DI container in `Program.cs`
- ✅ Add OpenAPI/Swagger with XML documentation
- ✅ Add HTTPS redirection

**Deliverables:**
- Minimal API endpoint (not controllers — see ADR-011)
- DI composition root
- Swagger UI documentation

**Notes:**
- Controllers were explicitly **not** used — Minimal API is sufficient for the current surface
- No middleware pipeline customization was needed

---

## Sprint 5: In-Memory Adapters

**Status:** ✅ Complete (Partially)

**Objective:** Implement outbound adapters for fraud rule provisioning.

**Tasks Completed:**
- ✅ Implement `InMemoryFraudRuleProvider` (active by default)
- ✅ Seed initial fraud rules (HighAmount rule with risk score 50)
- ✅ Create integration tests

**Deferred (resolved in Phase 4/5):**
- Transaction persistence was not yet implemented — transactions were transient
- `ITransactionRepository` was added in Phase 4/5 with `EfTransactionRepository`

**Deliverables:**
- `InMemoryFraudRuleProvider` — seed data with 1 rule and 1 specification mapping
- Integration tests

---

## Sprint 6: EF Core + SQL Server Integration

**Status:** ✅ Complete (Partially)

**Objective:** Configure EF Core with SQL Server for persistence.

**Tasks Completed:**
- ✅ Add EF Core 8 with SQL Server provider
- ✅ Implement `FraudDetectionDbContext` with entity configurations
- ✅ Create Value Converters for all strongly-typed IDs (`TransactionIdConverter`, `CustomerIdConverter`, `FraudRuleIdConverter`)
- ✅ Create `TransactionStatusConverter` (enum → string)
- ✅ Configure `Money` as owned type
- ✅ Create `IEntityTypeConfiguration<T>` for `Transaction` and `FraudRule`
- ✅ Create initial migration (`InitialCreate`)
- ✅ Implement `DbFraudRuleProvider` (ready but inactive)
- ✅ Create persistence integration tests (SQLite in-memory)

**Pending / Deferred:**
- SQL Server database provisioning — the EF Core setup is ready, but no database is running
- The `DbFraudRuleProvider` is registered in DI but commented out — switch when database is available
- Integration tests use SQLite in-memory, not SQL Server

**Deliverables:**
- Full EF Core infrastructure (Context, Configurations, Converters, Migrations)
- `DbFraudRuleProvider` implementation
- 11 persistence integration tests

---

## Phase 2/5: Guard Pattern + Result Pattern + Amount Boundary Fix

**Status:** ✅ Complete

**Objective:** Improve code quality by centralizing precondition checks, introducing the Result pattern for state transitions, and aligning validation boundaries.

**Tasks Completed:**
- ✅ Create `Guard` class with 7 methods (AgainstNull, AgainstNullOrWhiteSpace, AgainstOutOfRange, AgainstEmptyGuid, AgainstNegative)
- ✅ Replace 11 scattered null checks + 3 Guid.Empty checks + manual string checks with Guard calls
- ✅ Create `Result` / `Result<T>` classes for domain operation outcomes
- ✅ Change Transaction state transitions (Approve, Reject, MarkForReview) to return `Result` instead of throwing
- ✅ Update `ApplyRecommendedStatus` handler to check `result.IsFailure`
- ✅ Fix Amount boundary: change FluentValidation `GreaterThan(0)` → `GreaterThanOrEqualTo(0)` to match Domain
- ✅ Add try/catch in API endpoint for sanitized 500 responses
- ✅ Write 16 Guard tests + 4 Result tests
- ✅ Update TransactionTests to assert Result instead of exception
- ✅ Update ValidatorTests for Amount=0 being valid

**Files Changed:**
- `Domain/Guard.cs` — CREATED
- `Domain/Result.cs` — CREATED
- `Domain/Result{T}.cs` — CREATED
- `tests/.../GuardTests.cs` — CREATED
- `tests/.../ResultTests.cs` — CREATED
- Modified: Transaction.cs, FraudRule.cs, Money.cs, TransactionId.cs, CustomerId.cs, FraudRuleId.cs
- Modified: FraudRuleEngine.cs, HighAmountTransactionSpecification.cs
- Modified: AnalyzeTransactionHandler.cs, AnalyzeTransactionValidator.cs
- Modified: AnalyzeTransactionEndpoint.cs, DbFraudRuleProvider.cs

**Deliverables:**
- Guard pattern (centralized precondition validation)
- Result pattern (explicit state transition outcomes)
- Aligned Amount validation (Domain as source of truth)
- Sanitized API error handling
- 120 unit tests + 19 integration tests = 139 total

---

## Phase 3/5: Complete Fraud Detection Business Logic

**Status:** ✅ Complete

**Objective:** Implement all business rules required by the challenge — velocity, blacklist, and geographic checks — and make the Rejected status producible.

**Tasks Completed:**
- ✅ Add `FraudRuleAction` enum (`Review`, `Reject`)
- ✅ Add `Action` property to `FraudRule` (defaults to `Review` for backward compatibility)
- ✅ Add `RecentTransactionCount` property to `Transaction` (for velocity evaluation)
- ✅ Implement `VelocityTransactionSpecification` (check `RecentTransactionCount >= max`)
- ✅ Implement `BlacklistCustomerSpecification` (check `CustomerId` in blacklist)
- ✅ Implement `HighRiskCountrySpecification` (check `Currency` as geographic proxy)
- ✅ Update `FraudRuleEngine.Evaluate` — if any matched rule has `Action == Reject`, recommended status is `Rejected`
- ✅ Update `InMemoryFraudRuleProvider` — seed all 4 rules with specifications
- ✅ Update `DbFraudRuleProvider` — same 4 rules and specifications
- ✅ Update `AnalyzeTransactionHandler` — set `RecentTransactionCount` before evaluation
- ✅ Write 28 new unit tests + 1 new integration test
- ✅ Build: 0 errors, 0 warnings

**Key Decisions:**
- `FraudRuleAction` enum instead of boolean — more explicit, extensible
- Rejection takes precedence over review (pessimistic approach)
- ~~Currency as proxy for geographic risk (documented limitation — see ADR-021)~~ → **Fixed in Phase 4/5**: now uses Country field with ISO country codes (see ADR-023)
- ~~Velocity count defaults to 0 in handler~~ → **Fixed in Phase 4/5**: now queries real DB-backed counts via `GetTransactionCountSinceAsync()`

**Deliverables:**
- 4 specified business rules (HighAmount, Velocity, Blacklist, HighRiskCountry)
- `FraudRuleAction` enum in Domain
- `RecentTransactionCount` property for velocity context
- Rejected status now producible by the engine
- 148 unit tests + 20 integration tests = 168 total

---

## Phase 4/5: Persistence, Real Velocity Detection and End-to-End Integration

**Status:** ✅ Complete

**Objective:** Persist transactions to the database, replace hardcoded velocity context with real DB queries, add Country and Metadata to Transaction, activate DbFraudRuleProvider, add GET endpoint, and version the API.

**Tasks Completed:**
- ✅ Add `ITransactionRepository` port with `AddAsync`, `GetByIdAsync`, `GetTransactionCountSinceAsync`
- ✅ Implement `EfTransactionRepository` in Infrastructure.Persistence.Repositories
- ✅ Update `AnalyzeTransactionHandler` to query real velocity via `GetTransactionCountSinceAsync()`
- ✅ Persist transaction after analysis in handler
- ✅ Add `Country` field (nullable `string?`) to Transaction entity
- ✅ Update `HighRiskCountrySpecification` to use `transaction.Country` (ISO country codes) instead of currency proxy
- ✅ Add `Metadata` dictionary (`Dictionary<string, string>`) to Transaction entity
- ✅ Configure EF Core JSON conversion for Metadata column
- ✅ Configure Action column mapping in `FraudRuleConfiguration`
- ✅ Add `FraudRuleAction` mapping fix migration
- ✅ Activate `DbFraudRuleProvider` — reads rules from DB, auto-migration + seeding on dev startup
- ✅ Add API versioning: routes now `/api/v1/transactions/analyze`
- ✅ Add GET endpoint: `GET /api/v1/transactions/{id}`
- ✅ Update integration tests for new endpoint and persistence flow
- ✅ Expand unit tests for Country, Metadata, and velocity changes
- ✅ Build: 0 errors, 0 warnings
- ✅ **182 tests passing** (155 unit + 27 integration)

**Key Decisions:**
- `ITransactionRepository` is not generic — explicit methods for specific use cases (YAGNI)
- Velocity counts ALL transaction statuses (approved, under review, rejected) — challenge doesn't specify filtering
- Current transaction is excluded from velocity count (queried before persistence)
- Country is validated non-whitespace when provided, but not validated as ISO code
- Metadata stored as JSON column via EF Core `HasConversion` — schema-less and flexible
- API versioning via route prefix (`/api/v1/`) — no versioning library needed
- Startup seeding is a simple script in `Program.cs`, not EF Core model seeding — only runs when DB is empty
- `InMemoryFraudRuleProvider` preserved for testing

**Deliverables:**
- Full persistence flow (Transaction → DB)
- Real velocity detection (DB-backed query)
- Country field replacing Currency proxy
- Metadata JSON column
- DbFraudRuleProvider active with auto-seeding
- API versioning (v1 prefix)
- GET endpoint for persisted transactions
- 155 unit tests + 27 integration tests = 182 total

---

## Phase 5/5: Documentation Polish, Health Check, Exception Middleware, Performance Tests

**Status:** ✅ Complete

**Objective:** Finalize the project with documentation audit, global exception handling, health endpoint, performance benchmarks, index optimization, and portfolio-ready polish.

**Tasks Completed:**
- ✅ Add `ExceptionHandlingMiddleware` — global middleware replacing per-endpoint try/catch with structured logging and sanitized JSON error responses
- ✅ Add `GET /health` endpoint — liveness check with DB connectivity verification via `context.Database.CanConnectAsync()`
- ✅ Add performance benchmark tests — Stopwatch-based assertions (< 100ms) for all endpoints and velocity scenarios
- ✅ Add `CustomerId + CreatedAt` composite index — optimizes velocity query performance (index seek + range scan)
- ✅ Use `AsNoTracking()` in read query (`GetTransactionCountSinceAsync`) to avoid change tracker overhead
- ✅ Structured logging — add `ILogger<T>` to `AnalyzeTransactionHandler` and `ExceptionHandlingMiddleware`
- ✅ Documentation audit — update all docs for accuracy (test counts, features, patterns)
- ✅ Remove try/catch from endpoint — replaced by global middleware
- ✅ Add 6 new integration tests (health check + 4 performance + 1 index migration)
- ✅ `.gitignore` audit — verify coverage for bin/, obj/, appsettings*.Development.json, .env, etc.
- ✅ Final report generated (`OPENCODE_RETURN.md`)
- ✅ Build: 0 errors, 0 warnings
- ✅ **191 tests passing** (158 unit + 33 integration)

**Key Decisions:**
- Global exception middleware over per-endpoint try/catch for consistency across all endpoints (see ADR-029)
- Health check uses simple DB connectivity check — no migration state verification (see ADR-030)
- Performance tests use Stopwatch (not BenchmarkDotNet) — adequate for < 100ms assertions, indicative only with SQLite (see ADR-031)
- Composite index on (CustomerId, CreatedAt) is correct for equality + range query pattern (see ADR-032)

**Deliverables:**
- ExceptionHandlingMiddleware (global)
- GET /health endpoint
- 4 performance benchmark tests
- CustomerId + CreatedAt composite index
- Structured logging throughout
- Comprehensive documentation (ADR-001 through ADR-033)
- Portfolio-ready project structure
- 158 unit tests + 33 integration tests = 191 total

---

## Sprint 7: Kafka Integration

**Status:** 🔲 Deferred (post-completion)

**Objective:** Add event streaming with Kafka.

**Tasks:**
- Configure Kafka producer
- Configure Kafka consumer
- Implement event handlers
- Create integration tests

---

## Sprint 8: AI Integration

**Status:** 🔲 Deferred (post-completion)

**Objective:** Add AI-powered fraud analysis.

**Tasks:**
- Create AI provider abstraction
- Implement OpenAI provider
- Implement Anthropic provider
- Create integration tests

---

## Summary

| Phase/Sprint | Status | Deliverables |
|--------------|--------|-------------|
| 1 — Solution + Domain Foundation | ✅ Complete | 6 projects, Transaction, VOs, 40 tests |
| 2 — Domain Completion | ✅ Complete | FraudRule, FraudRuleEngine, Specification Pattern |
| 3 — Application Layer | ✅ Complete | CQRS Handler, Validator, Ports, 100 unit tests |
| 4 — API Layer | ✅ Complete | Minimal API endpoint, Swagger, DI |
| 5 — In-Memory Adapters | ✅ Partial | InMemoryFraudRuleProvider; ITransactionRepository not yet needed |
| 6 — SQL Server | ✅ Complete | EF Core setup, DbFraudRuleProvider active, 20 integration tests |
| 2/5 — Guard + Result + Fixes | ✅ Complete | Guard, Result, Amount boundary fix, try/catch, 120 unit + 19 integration tests |
| 3/5 — Fraud Business Logic | ✅ Complete | FraudRuleAction, 3 new specs, Rejected status, velocity context, 148 unit + 20 integration tests |
| 4/5 — Persistence + Velocity + E2E | ✅ Complete | ITransactionRepository, real velocity, Country+Metadata, DbFraudRuleProvider active, API v1, GET endpoint, 155 unit + 27 integration tests |
| **5/5 — Documentation + Health + Middleware + Performance** | **✅ Complete** | **ExceptionHandlingMiddleware, GET /health, performance benchmarks, CustomerId+CreatedAt index, structured logging, doc polish, 158 unit + 33 integration tests** |
| 7 — Kafka | 🔲 Deferred | — |
| 8 — AI | 🔲 Deferred | — |
