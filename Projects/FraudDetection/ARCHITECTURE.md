# Fraud Detection API - Architecture

## Why Hexagonal Architecture

Hexagonal Architecture (Ports & Adapters) provides:

| Benefit | Description |
|---------|-------------|
| Testability | Business logic can be tested without HTTP, DB, or external systems |
| Replaceability | Adapters can be swapped (e.g., REST → gRPC, PostgreSQL → MongoDB) |
| Evolution | New adapters added without changing core logic |
| Simplicity | Clear separation between "what" (Domain) and "how" (Adapters) |

## Why Vertical Slice

Vertical Slice organizes code by use case, not by technical layer:

| Benefit | Description |
|---------|-------------|
| Cohesion | Each slice contains everything needed for one feature |
| Isolation | Changes to one slice don't affect others |
| Clarity | Easy to understand the flow of a specific feature |

## Solution Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/                        # HTTP Adapter
│   ├── FraudDetection.Application/                # Application Layer
│   │   └── Features/
│   │       └── Transactions/
│   │           └── AnalyzeTransaction/
│   ├── FraudDetection.Domain/                     # Domain Layer
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   ├── FraudDetection.Infrastructure/             # Outbound Adapters
│   └── FraudDetection.SharedKernel/               # Shared Utilities
└── tests/
    ├── FraudDetection.UnitTests/
    └── FraudDetection.IntegrationTests/
```

## Dependency Direction

```
Api → Application → Domain ← SharedKernel
```

**Rule:** Dependencies always point inward. Domain never depends on anything outside.

## Projects

| Project | Responsibility |
|---------|----------------|
| Domain | Business rules, entities, value objects |
| Application | Use cases, ports (interfaces), DTOs, validation |
| Api | HTTP adapter, controllers, middleware |
| SharedKernel | Cross-cutting concerns (Result, Guard) |

## Current Implementation

### Domain Layer

| Component | Type | Description |
|-----------|------|-------------|
| Transaction | Entity | Financial transaction with identity and behavior |
| TransactionStatus | Enum | Pending, Approved, Rejected, UnderReview |
| Money | Value Object | Monetary amount with currency |
| TransactionId | Value Object | Strongly typed transaction identifier |
| CustomerId | Value Object | Strongly typed customer identifier |

### Application Layer

| Component | Type | Description |
|-----------|------|-------------|
| AnalyzeTransactionCommand | DTO | Input for transaction analysis |
| AnalyzeTransactionValidator | Validator | FluentValidation for command input |
| AnalyzeTransactionHandler | Handler | Placeholder for use case logic |

## Ports

### Primary Ports (Driving)

| Port | Purpose |
|------|---------|
| IAnalyzeTransactionUseCase | Analyze a transaction |

### Secondary Ports (Driven)

| Port | Purpose |
|------|---------|
| ITransactionRepository | Persist transactions |
| IFraudRuleRepository | Get active fraud rules |

## Adapters

### Inbound (Sprint 1)

| Adapter | Implements |
|---------|------------|
| TransactionsController | IAnalyzeTransactionUseCase |

### Outbound (Sprint 1: In-Memory)

| Adapter | Implements |
|---------|------------|
| InMemoryTransactionRepository | ITransactionRepository |
| InMemoryFraudRuleRepository | IFraudRuleRepository |

## Request Lifecycle

```
HTTP Request → Controller → Use Case → Domain → Repository → Response
```

## Future Evolution

| Sprint | Integration |
|--------|-------------|
| Sprint 4 | PostgreSQL |
| Sprint 5 | Kafka |
| Sprint 6 | AI (OpenAI/Anthropic) |
| Sprint 7 | n8n Automation |
