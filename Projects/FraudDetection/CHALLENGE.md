# Fraud Detection API — Reto técnico real

> **Nota histórica:** este archivo fue originalmente una transcripción errónea del
> reto (risk score, estado *Under Review*, reglas de blacklist/velocity/geographic y
> el endpoint `/api/v1/transactions/analyze`). El reto REAL está verificado contra
> `Challenge_BE-LT.docx` y se documenta abajo; la reconstrucción completa y sus
> decisiones están en `DECISIONS.md` (ADR-051 → ADR-058) y `OPENCODE_RETURN.md`.

## Objetivo del negocio

Construir un sistema anti-fraude para transacciones financieras: cada transacción
creada debe ser validada por un **microservicio anti-fraude asíncrono** (vía Kafka)
que envía un mensaje de vuelta para actualizar el estado de la transacción.

## Requerimientos funcionales

### States (exactamente 3)

| Estado | Descripción |
|--------|-------------|
| `pending` | Creada y encolada para evaluación asíncrona |
| `approved` | Pasó la evaluación de fraude |
| `rejected` | Rechazada por una de las dos reglas |

No existe estado *Under Review*.

### Reglas de fraude (exactamente 2)

| # | Regla | Umbral de rechazo |
|---|-------|-------------------|
| 1 | High value | `value` > **2000** |
| 2 | Daily accumulated (mismo `sourceAccountId`, día UTC) | acumulado > **20000** |

Ambas reglas rechazan (no hay reglas "de review"). Precedencia documentada:
`HighValue` se evalúa primero (ADR-057).

### Flujo asíncrono

```
POST /api/v1/transactions → row persistido (pending) → Kafka: transaction-created
                                                                │
                                                                ▼
                                FraudDetection.Worker (evaluación) → persiste estado
                                                                │
        GET /api/v1/transactions/{id} ← SQL Server ←             │
                                        Kafka: transaction-evaluated (audit)
```

No hay evaluación en el request: el API nunca aplica las reglas de forma síncrona.

### Endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/v1/transactions` | Crea la transacción `pending`, publica `TransactionCreated` → `201 Created` + `Location` |
| GET | `/api/v1/transactions/{id}` | Consulta el estado actual (`pending`/`approved`/`rejected`, con `rejectionReason` si está rechazada); `404` si no existe |

### Payload de creación real (incluye `tranferTypeId`)

```json
{
  "sourceAccountId": "3f4e2a1b-8c7d-6e5f-0a1b-2c3d4e5f6a7b",
  "targetAccountId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "tranferTypeId": 1,
  "value": 120
}
```

> `tranferTypeId` es la grafía literal del documento del reto (con su errata, sin la
> 's'). El servicio la acepta como wire name canónico y también acepta `transferTypeId`
> (case-insensitive).

## Alcance y no-funcionales

- .NET 8, ASP.NET Core Web API + Worker (host de consola), Kafka (Confluent.Kafka), EF Core 8 + SQL Server
- Hexagonal Architecture + Vertical Slice + CQRS explícito (sin MediatR), Specification/Guard/Result
- Entrega asíncrona sobre Kafka: at-least-once, consumer idempotente (ADR-058)

## Fuente de verdad

- `Challenge_BE-LT.docx` — documento original del reto (referenciado en `OPENCODE_RETURN.md`)
- `DECISIONS.md` — ADR-051 → ADR-058 (reconstrucción y decisiones técnicas)
