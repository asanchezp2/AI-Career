# Fraud Detection API - Technical Challenge

## Business Objective

Build a Fraud Detection API that analyzes financial transactions in real-time and returns risk decisions using a rules engine.

## Functional Requirements

### Transaction Analysis

- System must accept transaction data via HTTP POST
- System must validate transaction fields
- System must apply fraud rules to each transaction
- System must calculate a risk score
- System must return a decision (Approve, Reject, Under Review)

### Transaction Data

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| TransactionId | Guid | Yes | Unique identifier |
| Amount | decimal | Yes | Transaction amount |
| Currency | string | Yes | ISO 4217 currency code |
| CustomerId | Guid | Yes | Customer identifier |
| Timestamp | DateTime | Yes | Transaction timestamp |
| Metadata | Dictionary | No | Additional data |

### Fraud Rules (Initial)

| Rule | Description | Action |
|------|-------------|--------|
| High Amount | Amount > 10,000 | Flag for review |
| Velocity | > 5 transactions in 1 hour | Reject |
| Blacklist | Customer in blacklist | Reject |
| Geographic | Transaction from high-risk country | Flag for review |

## Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Response Time | < 100ms |
| Availability | 99.9% |
| Scalability | Horizontal |
| Auditability | Full transaction log |

## API Endpoints (Sprint 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/v1/transactions/analyze | Analyze a transaction |
| GET | /api/v1/transactions/{id} | Get transaction by ID |

## Constraints

- .NET 8
- ASP.NET Core Web API
- Hexagonal Architecture
- Vertical Slice Architecture
- No external integrations in Sprint 1

## Acceptance Criteria

- [ ] API accepts transaction data
- [ ] API validates input
- [ ] API applies fraud rules
- [ ] API returns risk decision
- [ ] API returns transaction details
- [ ] Unit tests pass
- [ ] API documentation available

## Open Questions

> **Note:** The following questions need clarification before proceeding.

1. What are the exact fraud rules to implement?
2. What is the expected transaction volume?
3. What authentication method should be used?
4. What is the data retention policy?
5. Should the system support multiple currencies?
