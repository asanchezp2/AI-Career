# Sprint 1 - Transaction Entity - Report

**Date:** 2026-07-16  
**Status:** ✅ Complete

---

## Files Created

| File | Description |
|------|-------------|
| `src/FraudDetection.Domain/Enums/TransactionStatus.cs` | Enum: Pending, Approved, Rejected, UnderReview |
| `src/FraudDetection.Domain/Entities/Transaction.cs` | Transaction entity with identity-based equality |
| `tests/FraudDetection.UnitTests/Entities/TransactionTests.cs` | 5 unit tests |

---

## Build Result

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Test Result

```
Test Run Successful.
Total tests: 25
     Passed: 25
```

### Transaction Tests (5)

| Test | Status |
|------|--------|
| `Transaction_CreatedSuccessfully` | ✅ Passed |
| `Transaction_NullId_Throws` | ✅ Passed |
| `Transaction_NullCustomerId_Throws` | ✅ Passed |
| `Transaction_NullMoney_Throws` | ✅ Passed |
| `Transaction_DifferentIds_AreDifferentEntities` | ✅ Passed |

### All Domain Tests (25)

| Component | Tests | Status |
|-----------|-------|--------|
| Money Value Object | 10 | ✅ All passed |
| TransactionId Value Object | 5 | ✅ All passed |
| CustomerId Value Object | 5 | ✅ All passed |
| Transaction Entity | 5 | ✅ All passed |

---

## Why Transaction is an Entity (not a Value Object)

Transaction has **identity** — `TransactionId` determines uniqueness, not its attribute values. Two transactions with the same customer, amount, and status but different IDs are distinct transactions. Value Objects are equal by all their values (value equality). Entities maintain continuity through identity even as their state changes (identity equality). The `Equals` override compares by `TransactionId`, and `operator ==` / `operator !=` enforce identity-based equality throughout the codebase.
