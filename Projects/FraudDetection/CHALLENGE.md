# Fraud Detection API — Reto técnico real

> Especificación reconstruida del documento original `Challenge BE-LT.docx`.
> La matriz de trazabilidad está en `CHALLENGE_TRACEABILITY.md`; el documento
> original se mantiene fuera del repositorio.

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
| 2 | Daily accumulated | acumulado > **20000** |

Ambas reglas rechazan. El documento original no define la clave de agregación ni
la zona horaria. Para esta implementación se eligió explícitamente agrupar por
`sourceAccountId` y día UTC, incluyendo la transacción evaluada. Es una
interpretación documentada, no un detalle textual del reto.

### Flujo asíncrono

```
POST /api/v1/transactions → persistida (pending) → Kafka: transaction-created
                                                       │
                                                       ▼
                                      FraudDetection.Worker evalúa
                                                       │
                       Kafka: transaction-evaluated ───┘
                                                       │
                                                       ▼
                                  API consume y actualiza el estado
                                                       │
GET /api/v1/transactions/{id} ← SQL Server ← approved/rejected
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
- Respuesta asíncrona sobre Kafka: el worker publica la evaluación y la API la aplica; entrega at-least-once e idempotencia en la transición
- Base de datos: elección libre según el reto; esta implementación usa SQL Server. Kafka es obligatorio.

## Fuente de verdad

- `Challenge BE-LT.docx` — documento original proporcionado por el usuario, fuera del repositorio
- `CHALLENGE_TRACEABILITY.md` — requisito → implementación → prueba
- `ARCHITECTURE.md` — diseño actual, decisiones y limitaciones
