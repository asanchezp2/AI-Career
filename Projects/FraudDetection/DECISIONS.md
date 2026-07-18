# Fraud Detection API - Architecture Decision Log

## ADR-001: Use Hexagonal Architecture

**Date:** 2026-07-09  
**Decision:** Use Hexagonal Architecture (Ports & Adapters)  
**Reason:** Testability, replaceability, and evolution. Business logic isolated from infrastructure.  
**Status:** Approved

## ADR-002: Use Vertical Slice Architecture

**Date:** 2026-07-09  
**Decision:** Organize code by use case (Vertical Slice)  
**Reason:** Cohesion, isolation, and clarity. Each feature is self-contained.  
**Status:** Approved

## ADR-003: Start with In-Memory Repositories

**Date:** 2026-07-09  
**Decision:** Use in-memory repositories for Sprint 1  
**Reason:** Simplicity. Focus on architecture before infrastructure.  
**Status:** Approved

## ADR-004: No External Integrations in Sprint 1

**Date:** 2026-07-09  
**Decision:** Exclude PostgreSQL, Kafka, AI, Docker from Sprint 1  
**Reason:** Scope control. Build foundation first, integrate later.  
**Status:** Approved

## ADR-005: Use .NET 8

**Date:** 2026-07-09  
**Decision:** Target .NET 8  
**Reason:** Latest LTS version, performance improvements, modern features.  
**Status:** Approved

## ADR-006: Use FluentValidation for Input Validation

**Date:** 2026-07-17  
**Decision:** Use FluentValidation library for Application Layer validation  
**Reason:** FluentValidation provides a clean, expressive syntax for input validation. It separates validation rules from business logic and integrates well with .NET.  
**Status:** Approved

## ADR-007: Validation Belongs to Application Layer

**Date:** 2026-07-17  
**Decision:** Place FluentValidation validators in Application Layer  
**Reason:** Input validation (format, required fields) belongs at the boundary. Business rules (domain invariants) belong in the Domain Layer. This separation follows SRP and keeps the Domain pure.  
**Status:** Approved

## ADR-008: Use Records for Value Objects

**Date:** 2026-07-15  
**Decision:** Use C# records for Value Objects  
**Reason:** Records provide Value Equality out of the box, are immutable by default, and reduce boilerplate code. This aligns with DDD principles for Value Objects.  
**Status:** Approved

## ADR-009: Strongly Typed IDs Over Primitive Guid

**Date:** 2026-07-15  
**Decision:** Use Strongly Typed IDs (TransactionId, CustomerId) instead of Guid  
**Reason:** Type Safety prevents mixing up identifiers. Self-documenting code. Compiler catches errors at build time.  
**Status:** Approved
