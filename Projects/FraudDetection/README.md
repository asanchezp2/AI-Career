# Fraud Detection API

A production-ready Fraud Detection API built with .NET 8, Hexagonal Architecture, and Vertical Slice Architecture.

## Goal

Build a real-time fraud detection system that analyzes financial transactions and returns risk decisions using a rules engine.

## Architecture

- **Pattern:** Hexagonal Architecture (Ports & Adapters)
- **Organization:** Vertical Slice by use case
- **Framework:** .NET 8, ASP.NET Core Web API

## Technologies

| Category | Technology |
|----------|------------|
| Runtime | .NET 8 |
| Architecture | Hexagonal + Vertical Slice |
| Validation | FluentValidation |
| Testing | xUnit |
| Documentation | OpenAPI/Swagger |

## Current Status

### Implemented

- ✅ Solution structure with all projects
- ✅ Domain Layer: Transaction Entity, Value Objects (Money, TransactionId, CustomerId)
- ✅ Application Layer: AnalyzeTransactionCommand, FluentValidation, Vertical Slice structure
- ✅ 40 unit tests passing

### Pending

- ⏳ Application Layer: Use cases, ports, handlers
- ⏳ API Layer: Controllers, middleware, DI
- ⏳ Infrastructure: In-memory adapters
- ⏳ PostgreSQL integration
- ⏳ Kafka integration
- ⏳ AI integration

## Project Structure

```
FraudDetection/
├── src/
│   ├── FraudDetection.Api/                        # HTTP Adapter
│   ├── FraudDetection.Application/                # Application Layer
│   │   └── Features/
│   │       └── Transactions/
│   │           └── AnalyzeTransaction/
│   │               ├── AnalyzeTransactionCommand.cs
│   │               ├── AnalyzeTransactionValidator.cs
│   │               └── AnalyzeTransactionHandler.cs
│   ├── FraudDetection.Domain/                     # Domain Layer
│   │   ├── Entities/
│   │   │   └── Transaction.cs
│   │   ├── Enums/
│   │   │   └── TransactionStatus.cs
│   │   └── ValueObjects/
│   │       ├── Money.cs
│   │       ├── TransactionId.cs
│   │       └── CustomerId.cs
│   ├── FraudDetection.Infrastructure/             # Outbound Adapters
│   └── FraudDetection.SharedKernel/               # Shared Utilities
└── tests/
    ├── FraudDetection.UnitTests/
    └── FraudDetection.IntegrationTests/
```

## Documentation

- [Challenge](CHALLENGE.md) - Technical challenge details
- [Architecture](ARCHITECTURE.md) - Architectural decisions
- [Implementation Plan](IMPLEMENTATION_PLAN.md) - Sprint-based plan
- [Decisions](DECISIONS.md) - Architecture Decision Log
