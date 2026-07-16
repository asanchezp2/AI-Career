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
