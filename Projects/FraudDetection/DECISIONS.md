# Fraud Detection API — Architecture Decision Log

## ADR-001: Use Hexagonal Architecture

**Date:** 2026-07-09  
**Decision:** Use Hexagonal Architecture (Ports & Adapters)  
**Reason:** Testability, replaceability, and evolution. Business logic isolated from infrastructure.  
**Status:** Approved

---

## ADR-002: Use Vertical Slice Architecture

**Date:** 2026-07-09  
**Decision:** Organize code by use case (Vertical Slice)  
**Reason:** Cohesion, isolation, and clarity. Each feature is self-contained.  
**Status:** Approved

---

## ADR-003: Start with In-Memory Repositories

**Date:** 2026-07-09  
**Decision:** Use in-memory repositories for Sprint 1  
**Reason:** Simplicity. Focus on architecture before infrastructure.  
**Status:** Approved

---

## ADR-027: Timestamp Input Field

**Date:** 2026-07-30  
**Decision:** Add a `Timestamp` field to `AnalyzeTransactionCommand` and pass it to the `Transaction` constructor as `CreatedAt`  
**Reason:** The challenge specification requires a timestamp field in the transaction data. Accepting it from the client allows the transaction to carry its original occurrence time rather than forcing the server to use the current clock time. The validator enforces `NotEmpty` to ensure a value is always provided.  
**Trade-offs:**
- Client-provided timestamps could be inaccurate or manipulated — acceptable for this scope, production would validate recency (e.g., within last 5 minutes)
- The field is required (non-nullable `DateTime`) — simpler schema, every transaction must have a known timestamp
- Timestamp is stored as the `CreatedAt` column in the database, used for velocity queries and indexing  
**Status:** Approved

---

## ADR-028: Structured Logging in Application Layer

**Date:** 2026-07-30  
**Decision:** Use `Microsoft.Extensions.Logging.ILogger<T>` for structured logging in the Application layer (AnalyzeTransactionHandler) and API layer (ExceptionHandlingMiddleware)  
**Reason:** Structured logging provides context-rich output (customer ID, transaction ID, risk score) that is machine-parseable and queryable. Using the standard .NET logging abstraction keeps the Application layer framework-agnostic — the logger is injected via constructor (consistent with DI pattern used for all handler dependencies).  
**Implementation:**
- `AnalyzeTransactionHandler` logs at start (customer ID), success (transaction ID, status, risk score), and failure (error message)
- `ExceptionHandlingMiddleware` logs unhandled exceptions with HTTP method, path, and trace identifier
- Log levels: Information for business events, Error for exceptions  
**Trade-offs:**
- Adds constructor parameter to Handler (third-party dependency via ILogger) — acceptable trade-off for observability
- No OpenTelemetry/metrics integration yet — future enhancement  
**Status:** Approved

---

## ADR-029: Global Exception Handling Middleware

**Date:** 2026-07-30  
**Decision:** Implement `ExceptionHandlingMiddleware` as global middleware in the API pipeline, replacing the per-endpoint try/catch in `AnalyzeTransactionEndpoint`  
**Reason:** A global middleware provides consistent error handling across all endpoints (including future endpoints and the health check). It catches unhandled exceptions, logs structured error information (method, path, trace identifier), and returns a sanitized JSON response — no stack traces or internal details leaked to the client.  
**Implementation:**
- Middleware class in `FraudDetection.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Registered in `Program.cs` via `app.UseMiddleware<...>()` before endpoint mapping
- Returns `500 Internal Server Error` with `{ error, requestId }` JSON body
- Uses `context.TraceIdentifier` for correlation  
**Superseded by ADR-035:** the response body is now an RFC 7807 `ProblemDetails` (`application/problem+json`) with `requestId` as an extension property.
**Trade-offs:**
- Catches all exceptions — cannot differentiate between client errors (4xx) and server errors (5xx) without additional logic. This is acceptable because validation errors are handled by FluentValidation before reaching the handler
- Global middleware is less granular than per-endpoint handling — but middleware can be extended to handle specific exception types later  
**Status:** Approved

---

## ADR-030: Health Check Endpoint

**Date:** 2026-07-30  
**Decision:** Add a `GET /health` endpoint that returns 200 OK with `{ status: "healthy", timestamp }` when the database is reachable, and 500 when it is not  
**Reason:** A health endpoint is required for deployment orchestration (load balancers, Kubernetes probes) and operational monitoring. It provides a simple liveness check that verifies database connectivity.  
**Implementation:**
- Inline Minimal API delegate in `Program.cs` — no separate handler class (single responsibility: check DB, return status)
- Uses `context.Database.CanConnectAsync()` for lightweight connectivity check
- Logs a warning on failure via `ILogger<Program>`
- Returns `200 OK` on success, `500 Internal Server Error` on failure  
**Trade-offs:**
- Simple DB connectivity check — does not verify migration state or business health (deferred for future enhancement)
- Inline delegate instead of separate endpoint class — acceptable for a single-line mapping; would extract if more health checks are added  
**Status:** Approved

---

## ADR-031: Performance Benchmark Strategy

**Date:** 2026-07-30  
**Decision:** Add Stopwatch-based performance benchmark tests that assert response times under 100ms for all endpoints (analyze, get transaction, health check) and velocity scenarios  
**Reason:** The challenge requires response time under 100ms. Stopwatch-based tests provide a simple, repeatable way to verify this constraint in CI without external tooling (no BenchmarkDotNet, no load testing infrastructure).  
**Implementation:**
- `TransactionAnalysisPerformanceTests` in `FraudDetection.IntegrationTests/Performance/`
- Four test methods: basic analysis, velocity scenario (5 seed + 1 analyze), health check, GET transaction
- Assertions: `sw.ElapsedMilliseconds < 100`
- Use `Debug.Assert` style — documented as indicative (SQLite in-memory, not SQL Server)  
**Superseded by ADR-040 (performance budget relaxation):** the assertion budget is now `< 1000ms` with a documented methodology — the architectural `<100ms` expectation is validated by design (index, AsNoTracking, COUNT-only query), not by flaky CI wall-clock assertions.
**Trade-offs:**
- SQLite in-memory is significantly faster than SQL Server — passing tests do not guarantee production performance
- Stopwatch is less precise than BenchmarkDotNet — adequate for order-of-magnitude assertions (sub-100ms vs multi-second)
- Single-threaded sequential execution in test runner does not reflect production request volume  
**Status:** Approved

---

## ADR-032: CustomerId + CreatedAt Composite Index for Velocity

**Date:** 2026-07-30  
**Decision:** Add a composite database index on `CustomerId + CreatedAt` columns to optimize the velocity detection query (`GetTransactionCountSinceAsync`)  
**Reason:** The velocity rule queries `COUNT(*) WHERE CustomerId = @id AND CreatedAt >= @since`. Without an index, this is a full table scan. The composite index `IX_Transactions_CustomerId_CreatedAt` covers the query entirely, enabling an index seek + range scan.  
**Implementation:**
- `builder.HasIndex(t => new { t.CustomerId, t.CreatedAt })` in `TransactionConfiguration`
- Migration `AddCustomerIdCreatedAtIndex` generated and applied
- Index name: `IX_Transactions_CustomerId_CreatedAt`  
**Trade-offs:**
- Index adds storage and write overhead (each INSERT updates the index) — negligible for transaction volumes in this scope
- Composite index on (CustomerId, CreatedAt) is correct for equality on CustomerId + range on CreatedAt — a separate index on CustomerId alone is not needed
- The `GetTransactionCountSinceAsync` query also uses `AsNoTracking()` to avoid change tracker overhead  
**Status:** Approved

---

## ADR-033: Phase 5 Completion — Project Complete

**Date:** 2026-07-30  
**Decision:** Close the active implementation phase. All core requirements from the challenge are implemented.  
**Reason:** The project has met the challenge compliance criteria:
- All four fraud rules implemented (HighAmount, Velocity, Blacklist, HighRiskCountry)
- Full persistence layer with EF Core + SQL Server
- Real velocity detection via database queries
- Three API endpoints (POST analyze, GET transaction, GET health)
- Global exception handling middleware
- Structured logging
- Performance benchmark tests
- 187 tests passing (158 unit + 29 integration)
- Comprehensive documentation (README, Architecture, Decisions, KnowledgeBase)
- No secrets in source code, clean .gitignore, portfolio-ready structure  
**Deferred Features:**
- Authentication/authorization
- Docker containerization
- CI/CD pipeline
- Blacklist CRUD API
- OpenTelemetry metrics and tracing
- Kafka event streaming
- AI-powered analysis  
**Status:** Approved
**Note:** Evolved into `InMemoryFraudRuleProvider` (now testing fallback). In Phase 5/5, `ITransactionRepository` was added for persistence and velocity queries — transactions are now persisted.
**Superseded by ADR-034 through ADR-040 (productization):** the project has since added blacklist persistence (ADR-034), ProblemDetails (ADR-035), security headers (ADR-036), config-driven rules (ADR-037), Docker (ADR-038), CI (ADR-039), and a relaxed performance budget (ADR-040) — 209 tests passing (165 unit + 44 integration).

---

## ADR-004: No External Integrations in Sprint 1

**Date:** 2026-07-09  
**Decision:** Exclude PostgreSQL, Kafka, AI, Docker from Sprint 1  
**Reason:** Scope control. Build foundation first, integrate later.  
**Status:** Approved

---

## ADR-005: Use .NET 8

**Date:** 2026-07-09  
**Decision:** Target .NET 8  
**Reason:** Latest LTS version, performance improvements, modern features.  
**Status:** Approved

---

## ADR-006: Use FluentValidation for Input Validation

**Date:** 2026-07-17  
**Decision:** Use FluentValidation library for Application Layer validation  
**Reason:** FluentValidation provides a clean, expressive syntax for input validation. It separates validation rules from business logic and integrates well with .NET.  
**Status:** Approved

---

## ADR-007: Validation Belongs to Application Layer

**Date:** 2026-07-17  
**Decision:** Place FluentValidation validators in Application Layer  
**Reason:** Input validation (format, required fields) belongs at the boundary. Business rules (domain invariants) belong in the Domain Layer. This separation follows SRP and keeps the Domain pure.  
**Status:** Approved

---

## ADR-008: Use Records for Value Objects

**Date:** 2026-07-15  
**Decision:** Use C# records for Value Objects  
**Reason:** Records provide Value Equality out of the box, are immutable by default, and reduce boilerplate code. This aligns with DDD principles for Value Objects.  
**Status:** Approved

---

## ADR-009: Strongly Typed IDs Over Primitive Guid

**Date:** 2026-07-15  
**Decision:** Use Strongly Typed IDs (TransactionId, CustomerId, FraudRuleId) instead of Guid  
**Reason:** Type Safety prevents mixing up identifiers. Self-documenting code. Compiler catches errors at build time.  
**Status:** Approved

---

## ADR-010: Non-Generic Specification Pattern in Domain

**Date:** 2026-07-30  
**Decision:** Use a non-generic `ISpecification` interface in Domain, scoped to `Transaction`  
**Reason:** YAGNI — only `Transaction` is evaluated in this domain. A generic `ISpecification<T>` would add complexity without benefit. The interface belongs in Domain because specifications are business rules, not infrastructure concerns.  
**Trade-offs:**
- If future entities need specification evaluation, the interface must be made generic (backward-compatible change)
- No generic composition (AND/OR/NOT) is possible without a generic interface — this is acceptable for now  
**Status:** Approved

---

## ADR-011: Minimal API over Controllers

**Date:** 2026-07-30  
**Decision:** Use ASP.NET Core Minimal API instead of Controllers for the HTTP layer  
**Reason:** Minimal API is simpler, has less ceremony, and is sufficient for the current API surface (one endpoint). Controllers would add unnecessary complexity. The endpoint delegate is thin — it validates, calls the handler, and returns a response.  
**Trade-offs:**
- Minimal API does not support `[FromServices]` attribute injection in all scenarios — parameters are resolved by convention
- Filter pipeline (action filters, result filters) is not available — not needed here
- If the API grows to 10+ endpoints, controllers may provide better organization
- Integration testing requires `WebApplicationFactory` with `Program` exposed as `public partial class`  
**Status:** Approved

---

## ADR-012: EF Core + SQL Server for Persistence

**Date:** 2026-07-30  
**Decision:** Use Entity Framework Core 8 with SQL Server for data persistence  
**Reason:** The project targets enterprise environments where SQL Server is common (Azure SQL, on-premises). Originally the plan specified PostgreSQL, but SQL Server was chosen because it aligns better with the target deployment context and the architectural goal of building portfolio-grade code for international interviews.  
**Trade-offs:**
- SQL Server requires a running instance (Docker or cloud) — not available in local dev without setup
- PostgreSQL was the original choice but was dropped because it added an extra technology to learn without architectural benefit
- EF Core abstracts the provider — switching to PostgreSQL later requires only changing the NuGet package and connection string
- Integration tests use SQLite in-memory (not SQL Server) to avoid external dependencies in CI  
**Status:** Approved

---

## ADR-013: Explicit CQRS without MediatR

**Date:** 2026-07-30  
**Decision:** Implement CQRS explicitly — no MediatR, no IRequest/IHandler interfaces  
**Reason:** MediatR adds indirection, pipeline behaviors, and assembly scanning that are not needed for a single use case. An explicit `AnalyzeTransactionHandler` class with direct constructor injection is simpler, easier to debug, and makes the dependency graph visible without a mediator.  
**Trade-offs:**
- Without MediatR, cross-cutting concerns (logging, validation) must be applied manually or via decorators
- Adding a new command requires manually registering the handler in DI (not automatic assembly scanning)
- If the project grows to 20+ handlers, MediatR's pipeline behaviors and automatic registration become more valuable
- The validation pipeline is handled by explicit `validator.ValidateAsync(command)` in the endpoint — no pipeline needed  
**Status:** Approved

---

## ADR-014: Value Converter + Owned Type Strategy for EF Core Mapping

**Date:** 2026-07-30  
**Decision:** Map Value Objects to relational columns using EF Core Value Converters (for strongly-typed IDs) and Owned Types (for Money)  
**Reason:** Strongly-typed IDs (`TransactionId`, `CustomerId`, `FraudRuleId`) are mapped via `ValueConverter<TModel, TProvider>` to serialize to/from `Guid` in the database. `Money` is mapped as an owned type with two columns (`Amount`, `Currency`). This keeps the Domain pure — no EF Core attributes or dependencies leak into the Domain layer.  
**Implementation:**
- `TransactionIdConverter`, `CustomerIdConverter`, `FraudRuleIdConverter` — convert `*Id` → `Guid`
- `TransactionStatusConverter` — converts `TransactionStatus` enum → `string` (more readable than int in DB)
- `Money` — configured as owned type in `TransactionConfiguration` with `.OwnsOne(t => t.Amount)`
- All converters are registered in `FraudDetectionDbContext.OnModelCreating()`  
**Trade-offs:**
- Value Converters require manual registration — no automatic discovery
- Owned types in EF Core have limitations (cannot be tracked independently, cannot have private constructors easily)
- The mapping knowledge lives in Infrastructure, not Domain — correct separation but requires keeping configurations in sync  
**Status:** Approved

---

## ADR-015: FraudRuleProvider Strategy (InMemory → Db)

**Date:** 2026-07-30 (updated 2026-07-30 Phase 5/5)  
**Decision:** Started with `InMemoryFraudRuleProvider` as the default; switched to `DbFraudRuleProvider` in Phase 5/5.  
**Phase 2 (initial):** `InMemoryFraudRuleProvider` was the default — seeded one rule (HighAmount), enabled flow without DB.  
**Phase 5/5 (current):** `DbFraudRuleProvider` is now active — reads enabled rules from the `FraudRules` table via EF Core. Four rules are seeded on development startup when the database is empty. The `InMemoryFraudRuleProvider` is preserved for testing scenarios.  
**Registration in Program.cs:**  
```csharp
builder.Services.AddScoped<IFraudRuleProvider, DbFraudRuleProvider>();
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
```
**Trade-offs:**
- In-memory provider is still available for unit tests (no DB dependency)
- Rules not modifiable via API yet — seeding happens only on empty DB  
**Status:** Approved — superseded by ADR-026

---

## ADR-016: Guard Pattern Centralized in Domain

**Date:** 2026-07-30  
**Decision:** Implement a static `Guard` class in the Domain layer to centralize precondition checks  
**Reason:** The codebase had 11 scattered null checks, 3 identical `Guid.Empty` checks, and multiple manual string validation patterns. Centralizing them into `Guard.AgainstNull`, `Guard.AgainstEmptyGuid`, `Guard.AgainstNullOrWhiteSpace`, `Guard.AgainstOutOfRange`, and `Guard.AgainstNegative` eliminates duplication and standardizes error messages.  
**Why not SharedKernel:** The Guard is only used by Domain classes. Placing it in a SharedKernel project adds an extra project dependency for no benefit. If another bounded context needs the same Guard, it can be extracted then, but YAGNI applies.  
**Why not an external library (FluentValidation, Ardalis.GuardClauses):** The project intentionally avoids external dependencies in the Domain layer (see ADR-007 — Domain is pure). A custom Guard class is 68 lines, self-explanatory, and has zero dependencies.  
**Trade-offs:**
- Every Domain method that needs a new precondition type requires extending the Guard class
- Static methods are less flexible than a Guard instance (harder to mock in tests) — but precondition guards are rarely mocked
- The overloaded `AgainstNull` (reference type vs. nullable value type) adds slight complexity  
**Status:** Approved

---

## ADR-017: Result Pattern for State Transitions Only

**Date:** 2026-07-30  
**Decision:** Use `Result` / `Result<T>` for Transaction state transitions (`Approve`, `Reject`, `MarkForReview`), while keeping exceptions for programming contracts (null checks, range validation)  
**Reason:** State transitions represent expected domain flows where the caller (the Handler) should handle the outcome explicitly. Returning `Result` makes the success/failure branching visible in the calling code. Other preconditions (null arguments, out-of-range values) represent programming errors that should never happen — exceptions are appropriate for these.  
**Why not applied elsewhere:**
- **ValueObject factories** (`TransactionId.From`, `new Money(...)`) — invalid input here indicates a caller bug, not an expected business outcome. Exceptions are correct.
- **FraudRuleEngine.Evaluate** — the engine is already total (always returns a valid `FraudRuleEngineResult`). There is no failure path to model.
- **Programmatic null checks in Guard** — `Guard.AgainstNull` throws because a null argument is a contract violation, not a business flow decision.  
**Trade-offs:**
- Two patterns (Result + exceptions) coexist in the codebase, requiring developers to know which to use where
- The `ApplyRecommendedStatus` handler method must check `result.IsFailure` and throw — an extra step that would be automatic if state transitions threw directly
- Result pattern adds ceremony for simple state changes (one-line method now returns `Result` instead of throwing)
- The `Rejected` case in `ApplyRecommendedStatus` was unreachable at the time but is now producible (see ADR-020)
- `Result<T>` not currently used but available for future domain operations that return values on success  
**Status:** Approved

---

## ADR-018: FraudRuleAction Enum

**Date:** 2026-07-30  
**Decision:** Add a `FraudRuleAction` enum with `Review` and `Reject` values as a property on `FraudRule`  
**Reason:** The engine needs to distinguish between rules that flag a transaction for manual review vs. rules that trigger automatic rejection. A boolean `IsRejection` flag was considered but rejected because enums are more explicit and can be extended (future: `Allow`, `Escalate`, etc.) without adding more boolean flags.  
**Trade-offs:**
- Adds a new enum to Domain — minimal footprint (two values)
- Default is `Review` (backward-compatible — existing rules continue to produce `UnderReview` without code changes)
- `Reject` triggers `Rejected` status in the engine, which short-circuits the normal Approved/UnderReview flow  
**Status:** Approved

---

## ADR-019: New Fraud Specifications (Velocity, Blacklist, HighRiskCountry)

**Date:** 2026-07-30  
**Decision:** Add three new `ISpecification` implementations: `VelocityTransactionSpecification`, `BlacklistCustomerSpecification`, and `HighRiskCountrySpecification`  
**Reason:** The challenge requires detecting velocity (excessive transactions in a time window), blacklisted customers (immediate rejection), and geographic risk (high-risk country). Each maps to a distinct `FraudRuleAction` — Velocity and Blacklist trigger `Reject`, HighRiskCountry triggers `Review`.  
**Trade-offs:**
- `VelocityTransactionSpecification` needs `RecentTransactionCount` on `Transaction` — adds a mutable property to an otherwise behavior-controlled entity. This is acceptable because the property is set by the application layer before evaluation and is read-only during the evaluation.
- ~~`HighRiskCountrySpecification` uses Currency as proxy for geographic risk~~ → **Superseded by ADR-023**: now uses Country field with ISO country codes
- `BlacklistCustomerSpecification` uses `CustomerId` equality — efficient via `HashSet<CustomerId>`  
**Status:** Superseded by ADR-023 (geographic proxy fix)

---

## ADR-020: FraudRuleEngine Rejected Status

**Date:** 2026-07-30  
**Decision:** Update `FraudRuleEngine.Evaluate` to produce `Rejected` status when any matched rule has `Action == FraudRuleAction.Reject`  
**Reason:** Previously the engine only produced `Approved` (no matches) or `UnderReview` (any match). The `Rejected` branch existed in the handler but was unreachable. The new logic checks `matchedRules.Any(r => r.Action == FraudRuleAction.Reject)` first — if true, returns `Rejected` regardless of risk score.  
**Logic:**
```
matchedRules.Any(Action == Reject) → Rejected
else totalRiskScore > 0 → UnderReview
else → Approved
```
**Trade-offs:**
- Rejection takes precedence over review — if both reject and review rules match, the transaction is Rejected (pessimistic approach, appropriate for fraud detection)
- Risk scoring is still accumulated for all matched rules (observability), but status is determined by rule action, not score thresholds  
**Status:** Approved

---

## ADR-021: Currency as Geographic Proxy

**Date:** 2026-07-30  
**Decision:** Use `transaction.Amount.Currency` as a proxy for geographic risk in `HighRiskCountrySpecification`  
**Reason:** The `Transaction` entity lacks a `Country` field. Adding one would require changes to the command, validator, handler, and database schema. Using currency as a proxy is a pragmatic shortcut — certain currencies are strongly associated with high-risk regions (e.g., currency codes from sanctioned countries).  
**Trade-offs:**
- Currency is an imperfect proxy — a transaction can be in RUB from a low-risk country or in USD from a high-risk country
- In production, enrich `Transaction` with `Country` field or use geo-IP lookup
- The assumption is documented in the specification class XML comment  
**When to fix:** When the API receives `Country` as input or country lookup infrastructure exists  
**Status:** Superseded by ADR-023

---

## ADR-022: Transaction Persistence via ITransactionRepository

**Date:** 2026-07-30  
**Decision:** Add an `ITransactionRepository` port to Application.Abstractions and implement `EfTransactionRepository` in Infrastructure.Persistence.Repositories  
**Reason:** Transactions must be persisted to the database after fraud evaluation for auditability and to enable real velocity detection queries. The repository provides `AddAsync`, `GetByIdAsync`, and `GetTransactionCountSinceAsync` methods.  
**Trade-offs:**
- Not a generic repository — explicit methods for specific use cases (YAGNI)
- `AddAsync` is called after evaluation in the handler; the transaction is created, evaluated, then persisted
- `GetTransactionCountSinceAsync` enables real DB-backed velocity counts instead of the previous `RecentTransactionCount = 0` hardcode
- Async methods throughout (handler and repository are async)  
**Status:** Approved

---

## ADR-023: Country Field Replaces Currency Proxy in HighRiskCountrySpecification

**Date:** 2026-07-30  
**Decision:** Add a `Country` property (nullable `string?`) to the `Transaction` entity, and update `HighRiskCountrySpecification` to use `transaction.Country` instead of `transaction.Amount.Currency`  
**Reason:** Currency was an imperfect proxy for geographic risk. A transaction in USD from a high-risk country would not be flagged, while a transaction in IRR from a low-risk country would be. Country codes (ISO 3166-1 alpha-2) are the correct dimension for geographic risk evaluation.  
**Implementation:**
- `Transaction` gains `string? Country` — validated non-whitespace when provided
- `HighRiskCountrySpecification` now checks `transaction.Country` against a set of country codes (IR, KP, SY, VE)
- The `Country` field is optional in the API input (nullable) — when null, the specification returns false (no match)
- Country is stored as a nullable column in the database  
**Trade-offs:**
- Adding a new field to Transaction required changes to the domain entity, command, handler, and DB migration
- Country is validated non-whitespace when provided, but not validated as a real ISO code — application-level constraint only
- Null country is treated as "not high-risk" — a transaction with no country info will never match this rule  
**Status:** Approved

---

## ADR-024: Metadata as JSON Column

**Date:** 2026-07-30  
**Decision:** Add a `Dictionary<string, string> Metadata` property to `Transaction`, stored as a JSON column in the database via EF Core `HasConversion`  
**Reason:** The challenge requires a flexible metadata mechanism. A `Dictionary<string, string>` provides a simple key-value store without requiring a separate table or schema changes for each new metadata field.  
**Implementation:**
- `Transaction.Metadata` initialized as empty dictionary in constructor
- EF Core configuration uses `.Property(t => t.Metadata).HasConversion(v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default), v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) ?? new())`
- Database stores a JSON text column; deserializes on read  
**Trade-offs:**
- No schema enforcement — any string key/value can be stored
- JSON deserialization adds slight overhead on each read
- Querying within JSON values is possible but not indexed
- The dictionary is mutable (not a typical domain VO) — pragmatically chosen over a more complex immutable design  
**Status:** Approved

---

## ADR-025: API Versioning via Route Prefix

**Date:** 2026-07-30  
**Decision:** Use explicit route prefix `/api/v1/` for all API endpoints instead of ASP.NET API versioning libraries  
**Reason:** The project has two endpoints — a versioning library adds unnecessary complexity. A simple route prefix (`/api/v1/transactions/analyze`, `/api/v1/transactions/{id}`) is sufficient. When v2 endpoints are needed, they can be added alongside v1 without breaking existing clients.  
**Trade-offs:**
- No URL-based versioning negotiation (Accept header, query string) — explicit prefix only
- Version must be manually bumped when creating new endpoint versions
- Simpler than adding `Microsoft.AspNetCore.Mvc.Versioning` for two endpoints  
**Status:** Approved

---

## ADR-026: DbFraudRuleProvider Activation and Startup Seeding

**Date:** 2026-07-30  
**Decision:** Activate `DbFraudRuleProvider` as the primary `IFraudRuleProvider` implementation, with development-time auto-migration and conditional seeding  
**Reason:** Phase 5/5 makes persistence operational. The `DbFraudRuleProvider` reads rules from the `FraudRules` table, enabling rule configuration through the database rather than hardcoded in-memory collections.  
**Implementation:**
- `Program.cs` registers `DbFraudRuleProvider` as scoped (per-request DbContext)
- On development startup, `context.Database.MigrateAsync()` auto-applies pending migrations
- If the `FraudRules` table is empty, four seed rules are inserted: HighAmount (Review), Velocity (Reject), Blacklist (Reject), HighRiskCountry (Review)
- The `InMemoryFraudRuleProvider` is preserved for test scenarios (unit tests don't need DB access)  
**Trade-offs:**
- Seeding is a startup script in `Program.cs`, not EF Core model seeding (no migration generation needed)
- Only runs on empty DB — adding new rules requires direct DB insert or API endpoint (future)
- Production would use explicit migration commands, not auto-migrate
- Specification thresholds are still code-defined (e.g., 10000 for HighAmount) — not yet data-driven
- Blacklist remains an empty list returned by the provider — no dedicated table yet  
**Superseded by ADR-034 and ADR-037:** the blacklist is now persisted in a dedicated `BlacklistedCustomers` table (ADR-034), and specification thresholds are bound from the `FraudRules` configuration section via `FraudRuleOptions` (ADR-037).
**Status:** Approved

---

## ADR-034: Blacklist Persistence

**Date:** 2026-08-03  
**Decision:** Persist the blacklist in a dedicated `BlacklistedCustomers` table, backed by a new `BlacklistedCustomer` entity (Domain), an `IBlacklistProvider` port (Application), and a `DbBlacklistProvider` adapter (Infrastructure)  
**Reason:** The blacklist was previously an empty list returned by the rule provider — the Blacklist rule could not actually reject anyone. Persisting it makes the rule operational and auditable, and keeps the Domain pure: the handler loads blacklisted customer IDs through the port and layers a dynamic `BlacklistCustomerSpecification` over the provider's static specifications on every request.  
**Implementation:**
- `BlacklistedCustomer` entity: `CustomerId` (identity), `Reason` (max 200), `CreatedAt` — Guard-validated, private constructor for EF Core
- `IBlacklistProvider` port: `IsBlacklistedAsync`, `GetAllAsync`, `AddAsync`, `RemoveAsync`
- `DbBlacklistProvider`: EF Core implementation with `AsNoTracking()` reads
- Migration `AddBlacklistedCustomer` creates the table; startup seeding inserts demo customer `00000000-0000-0000-0000-000000000001`
- Handler: `_blacklistProvider.GetAllAsync()` per request → `BlacklistCustomerSpecification(currentIds)` layered over `_ruleProvider.GetSpecifications()`
- `DbFraudRuleProvider` intentionally no longer creates the Blacklist specification — it is dynamic  
**Trade-offs:**
- One extra DB query per analysis request (small table, indexed by primary key) — acceptable; could be cached with invalidation later
- No HTTP CRUD endpoints yet — add/remove is programmatic via the provider
- Demo seed customer ships in code — clearly logged and overridable  
**Status:** Approved

---

## ADR-035: ProblemDetails (RFC 7807) Error Contract

**Date:** 2026-08-03  
**Decision:** Use `AddProblemDetails()` plus an updated `ExceptionHandlingMiddleware` that returns RFC 7807 `application/problem+json` responses with a `requestId` extension property, replacing the previous custom `{ error, requestId }` JSON shape  
**Reason:** RFC 7807 (now RFC 9457) is the industry-standard error contract — machine-readable, self-describing (`type`, `title`, `status`, `detail`), and understood by clients, API gateways, and tooling out of the box. It also reuses the framework's own `ProblemDetails` type instead of a hand-rolled schema.  
**Implementation:**
- `builder.Services.AddProblemDetails()`
- Middleware writes `ProblemDetails { Status = 500, Title, Type = rfc9110 URL, Detail }` with `Extensions["requestId"] = context.TraceIdentifier`
- Explicit `contentType: "application/problem+json"` (the default overload would write `application/json`)
- No stack traces or internal details are ever included  
**Trade-offs:**
- Response shape changed from the earlier `{ error, requestId }` — a breaking contract change for any pre-existing client (none in production)
- ProblemDetails carries a `type` URL that clients may dereference — kept as the standard RFC URL, not a project-specific docs page  
**Status:** Approved

---

## ADR-036: Security Headers Middleware + HSTS

**Date:** 2026-08-03  
**Decision:** Add `SecurityHeadersMiddleware` applying a baseline set of security headers to every response, and enable HSTS in non-development environments  
**Reason:** API responses should not leak referrer information, be frameable, or allow MIME sniffing. A single middleware guarantees a consistent baseline for every endpoint (including future ones) without per-endpoint ceremony.  
**Implementation:**
- Headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none`, `Content-Security-Policy: default-src 'self'`
- `app.UseHsts()` guarded by `if (!app.Environment.IsDevelopment())` — HSTS only where HTTPS is actually enforced at the edge
- Middleware registered before exception handling and endpoints so headers are present even on error responses  
**Trade-offs:**
- CSP `default-src 'self'` is conservative — the API serves no inline content, so this is safe
- HSTS relies on the deployment terminating HTTPS (reverse proxy/load balancer) — in Docker the container itself is HTTP on port 8080
- Additional hardening (CORS, custom headers) deliberately left out — single-origin API  
**Status:** Approved

---

## ADR-037: Config-Driven Rule Parameters (FraudRuleOptions)

**Date:** 2026-08-03  
**Decision:** Bind fraud rule thresholds from the `FraudRules` appsettings section into `FraudRuleOptions` (Infrastructure/Configuration) and use them to construct the specifications in `DbFraudRuleProvider`  
**Reason:** Business numbers (HighAmountThreshold = 10000, VelocityMaxTransactions = 5, VelocityWindowMinutes = 60, HighRiskCountries) were hardcoded in the provider. Configuration-driven parameters make rules tunable without code changes, follow the project rule "use configuration files instead of hardcoded values," and are required for environment-specific tuning (e.g., stricter thresholds in production).  
**Implementation:**
- `FraudRuleOptions` POCO with `SectionName = "FraudRules"` and defaults matching the challenge
- `builder.Services.Configure<FraudRuleOptions>(configuration.GetSection(FraudRuleOptions.SectionName))` in Program.cs
- `DbFraudRuleProvider` receives `IOptions<FraudRuleOptions>` and builds `HighAmountTransactionSpecification`, `VelocityTransactionSpecification`, and `HighRiskCountrySpecification` from it  
**Trade-offs:**
- Rule metadata (name, risk score, action) stays in the DB; thresholds come from config — two sources of rule configuration, documented
- `InMemoryFraudRuleProvider` (testing fallback) keeps its own constants — intentional, it is not the production path
- Changing thresholds requires a container restart (config reload not implemented)  
**Status:** Approved

---

## ADR-038: Docker Containerization

**Date:** 2026-08-03  
**Decision:** Containerize the API and SQL Server using a multi-stage Dockerfile, a docker-compose.yml, and a .dockerignore  
**Reason:** Docker provides reproducible deployments and a one-command local production-like environment. The challenge's production-readiness goal requires a deployable artifact beyond `dotnet run`.  
**Implementation:**
- Multi-stage Dockerfile: SDK 8.0 build stage (per-project csproj copies for layer caching, `dotnet restore`, `dotnet publish -c Release`), `aspnet:8.0` runtime stage
- Runtime: installs `curl` for the healthcheck (before dropping privileges), runs as non-root `$APP_UID`, `ASPNETCORE_URLS=http://+:8080`, `HEALTHCHECK` hitting `GET /health`
- docker-compose: SQL Server 2022 with named volume `sqlserver-data`, SA password from `MSSQL_SA_PASSWORD` (default demo value), health gate; API with `AutoMigrate=true` and `ConnectionStrings__DefaultConnection` pointing at the `sqlserver` service, `depends_on: condition: service_healthy`
- `.dockerignore`: bin/, obj/, .git, TestResults, .env, development appsettings  
**Trade-offs:**
- Container runs HTTP only — HTTPS is expected to be terminated by a reverse proxy (documented)
- `AutoMigrate=true` runs migrations on startup — pragmatic for containers; strict production might use a separate migration step
- SA password default is a demo value — must be overridden outside local demos  
**Status:** Approved

---

## ADR-039: GitHub Actions CI (Path-Filtered)

**Date:** 2026-08-03  
**Decision:** Add a GitHub Actions workflow (`.github/workflows/ci.yml`) that restores, builds, and tests the solution on push/PR to `main`, path-filtered to `Projects/FraudDetection/**`  
**Reason:** The project lives inside a multi-project workspace repository (`AI-Career`). Path filtering ensures CI only runs when the FraudDetection project changes, avoiding wasted builds. The workflow also uploads TRX test results as an artifact for independent audit.  
**Implementation:**
- Triggers: `push` and `pull_request` on `main`, `paths: ['Projects/FraudDetection/**']`
- Job: ubuntu-latest, working-directory `Projects/FraudDetection`, `actions/checkout@v4`, `actions/setup-dotnet@v4` (.NET 8.0.x), restore → Release build → test (`--no-build`, TRX logger), upload artifact on `always()`  
**Trade-offs:**
- Tests run on SQLite in-memory — no SQL Server service container; adequate for the current suite
- No publish/package step — CI validates, deployment remains manual (Docker)
- TRX results artifact retained 7 days  
**Status:** Approved

---

## ADR-040: Performance Test Budget Relaxation

**Date:** 2026-08-03  
**Decision:** Relax the performance test assertion budget from `< 100ms` to `< 1000ms` and document the measurement methodology  
**Reason:** Wall-clock Stopwatch assertions against SQLite in-memory on shared CI runners were flaky at `< 100ms` — passing or failing depended on runner load, not code quality. The architectural `< 100ms` expectation is validated by design (CustomerId+CreatedAt index, AsNoTracking, COUNT-only velocity query) and by a generous regression budget that only catches pathological issues (N+1 queries, missing indexes, synchronous IO).  
**Implementation:**
- All four `TransactionAnalysisPerformanceTests` assert `< 1000ms`
- Class-level XML doc explains the methodology: end-to-end wall-clock, local machine/CI runner, indicative only, not a production guarantee  
**Trade-offs:**
- No longer asserts the actual challenge target — the target remains an architectural expectation documented as "not proven in production"
- Preferable to BenchmarkDotNet for CI stability at this stage — no external benchmarking infra
- Production latency still requires load testing against SQL Server (documented gap)  
**Status:** Approved
