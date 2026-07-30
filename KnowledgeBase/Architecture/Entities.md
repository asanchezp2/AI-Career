# Entity

## Definición

Una **Entity** es un objeto del dominio que tiene identidad propia y cambia a lo largo del tiempo.

A diferencia de un Value Object, dos Entities con los mismos atributos son diferentes si tienen distinto ID.

La igualdad se define por identidad, no por valores.

---

## Diferencia entre Entity y Value Object

| Característica | Entity | Value Object |
|----------------|--------|--------------|
| Identidad | ✅ Tiene ID propio | ❌ No tiene ID |
| Igualdad | Por identidad (ID) | Por valores |
| Mutabilidad | ✅ Puede cambiar estado | ❌ Inmutable |
| Lifecycle | ✅ Tiene ciclo de vida | ❌ Se reemplaza |
| Ejemplo | Transaction, FraudRule | Money |

---

## Características

### Identity

Cada Entity tiene un identificador único.

Dos objetos con el mismo ID son la misma Entity, sin importar sus atributos.

```csharp
var t1 = new Transaction(TransactionId.From(id), ...);
var t2 = new Transaction(TransactionId.From(id), ...);

t1 == t2; // true (mismo ID)
```

---

### Lifecycle

Una Entity existe a lo largo del tiempo.

Puede ser creada, modificada y eventualmente eliminada.

Su estado cambia pero su identidad permanece.

---

### Mutable state

Las Entities pueden cambiar su estado.

En Transaction, el `Status` cambia de `Pending` a `Approved`, `Rejected` o `UnderReview`.

La mutabilidad es controlada through behavior.

---

### Behavior

Las Entities contienen comportamiento que valida y ejecuta cambios de estado.

No exponen setters públicos.

Los cambios se realizan through methods que encapsulan las reglas.

```csharp
transaction.Approve();  // ✅ Behavior controlado
transaction.Status = ...;  // ❌ Setter público
```

---

### Encapsulation

Las Entities ocultan su estado interno.

Solo exponen lo necesario through propiedades de solo lectura y methods públicos.

El estado mutable es `private set`.

---

## Why not an Anemic Model?

### Rich Domain Model

Un **Rich Domain Model** coloca el comportamiento dentro de la Entity.

Un **Anemic Model** coloca el comportamiento en servicios externos.

```csharp
// ❌ Anemic Model
public class Transaction
{
    public TransactionStatus Status { get; set; }  // Público, mutable
}

public class TransactionService
{
    public void Approve(Transaction t) { t.Status = Approved; }  // Lógica fuera
}

// ✅ Rich Domain Model
public class Transaction
{
    public TransactionStatus Status { get; private set; }  // Privado

    public void Approve()  // Lógica dentro
    {
        ChangeStatus(TransactionStatus.Approved);
    }
}
```

---

### Why business rules belong inside the Entity

La Entity es la dueña de su estado.

Si la lógica está en un servicio externo:

- múltiples servicios pueden contradict las reglas
- la Entity se vuelve un DTO
- no hay encapsulación
- la validación se dispersa

---

### Why setters should not be public

Un setter público permite cualquier cambio de estado.

```csharp
// ❌ Cualquiera puede poner un estado inválido
transaction.Status = TransactionStatus.Approved;

// ✅ Solo la Entity controla sus transiciones
transaction.Approve();  // Valida que esté Pending
```

---

## Transaction example

Transaction representa una transacción financiera.

**Propiedades:**

- `TransactionId Id` — identificador único (Strongly Typed ID)
- `CustomerId CustomerId` — cliente que inició la transacción
- `Money Amount` — monto monetario (Value Object)
- `DateTime CreatedAt` — fecha de creación (UTC)
- `TransactionStatus Status` — estado actual
- `string? Country` — código ISO 3166-1 alpha-2 del país de origen de la transacción (opcional, nullable)
- `Dictionary<string, string> Metadata` — metadatos opcionales en formato clave-valor
- `int RecentTransactionCount` — número de transacciones recientes del cliente, usado por `VelocityTransactionSpecification`. Actualmente se consulta desde la base de datos mediante `ITransactionRepository.GetTransactionCountSinceAsync()` en lugar de usar un valor hardcodeado.

**Behavior:**

- `Approve()` — transiciona a Approved, retorna `Result`
- `Reject()` — transiciona a Rejected, retorna `Result`
- `MarkForReview()` — transiciona a UnderReview, retorna `Result`

```csharp
public Result Approve() => ChangeStatus(TransactionStatus.Approved);
```

El method privado `ChangeStatus()` centraliza la validación. A diferencia de la implementación inicial (que lanzaba `InvalidOperationException`), ahora retorna `Result.Failure(...)` con un mensaje de error cuando la transición no es válida, y `Result.Success()` cuando es exitosa.

Esto permite que el llamante (el Handler) maneje el resultado explícitamente:

```csharp
var result = transaction.Approve();
if (result.IsFailure)
    throw new InvalidOperationException(result.Error); // Error de programación
```

**¿Por qué Result en lugar de excepción?** Las transiciones de estado representan flujos esperados del dominio. Retornar `Result` hace visible en el código que la operación puede fallar por razones de negocio (transición inválida). Las excepciones se reservan para errores de programación (null, argumentos inválidos).

---

## FraudRule example

FraudRule representa una regla configurable de detección de fraude.

Se agregó en Sprint 2 junto con FraudRuleEngine y Specification Pattern.

**Propiedades:**

- `FraudRuleId Id` — identificador único (Strongly Typed ID)
- `string RuleName` — nombre descriptivo (ej: "HighAmount", "CrossBorder")
- `int RiskScore` — puntaje de riesgo (0–100)
- `FraudRuleAction Action` — acción a tomar cuando la regla aplica (`Review` o `Reject`)
- `bool IsEnabled` — si la regla está activa

**Behavior:**

- `Enable()` / `Disable()` — activar/desactivar la regla
- `Rename(string newName)` — cambiar el nombre (valida no vacío)
- `ChangeRiskScore(int newRiskScore)` — cambiar puntaje (valida 0–100)
- `Action (FraudRuleAction)` — comportamiento incluido en el constructor, default `Review`

```csharp
public class FraudRule
{
    public FraudRuleId Id { get; private set; }
    public string RuleName { get; private set; }
    public int RiskScore { get; private set; }
    public FraudRuleAction Action { get; private set; }
    public bool IsEnabled { get; private set; }

    public FraudRule(FraudRuleId id, string ruleName, int riskScore, FraudRuleAction action = FraudRuleAction.Review)
    {
        Guard.AgainstNull(id, nameof(id));
        Guard.AgainstNullOrWhiteSpace(ruleName, nameof(ruleName));
        Guard.AgainstOutOfRange(riskScore, 0, 100, nameof(riskScore));
        Id = id;
        RuleName = ruleName;
        RiskScore = riskScore;
        Action = action;
        IsEnabled = true;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    public void ChangeRiskScore(int newRiskScore)
    {
        Guard.AgainstOutOfRange(newRiskScore, 0, 100, nameof(newRiskScore));
        RiskScore = newRiskScore;
    }

    public void Rename(string newName)
    {
        Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));
        RuleName = newName;
    }
}
```

**Decisiones de diseño:**
- RiskScore validado en rango 0–100 mediante `Guard.AgainstOutOfRange` (invariante del dominio)
- RuleName validado no vacío mediante `Guard.AgainstNullOrWhiteSpace` (invariante del dominio)
- Identity basada en FraudRuleId (Value Object, no Guid primitivo)
- Equality implementada por ID (`Equals(FraudRuleId)`)
- Los métodos privados `ValidateRiskScore` y `ValidateRuleName` fueron reemplazados por llamadas directas a `Guard` en Phase 2/5 — menos código, mismos invariantes

---

## EF Core Materialization

Las Entities de este proyecto utilizan **private constructors** y **private setters** para permitir que EF Core materialice objetos desde la base de datos sin exponer setters públicos:

```csharp
// EF Core parameterless constructor (used for materialization only)
private FraudRule() { Id = null!; RuleName = null!; Action = FraudRuleAction.Review; }

// Public constructor (used by application code)
public FraudRule(FraudRuleId id, string ruleName, int riskScore) { ... }
```

Esto es una **práctica estándar** en DDD con EF Core:

- El constructor privado solo es usado por EF Core via reflection
- No debilita la encapsulación porque el código de aplicación nunca lo invoca
- Los `private set` en propiedades permiten que EF Core establezca valores sin exponer mutabilidad pública
- Las propiedades navegables (`null!`) se inicializan con el null-forgiving operator porque EF Core las asigna inmediatamente después de construir el objeto

Esta técnica aparece en la documentación oficial de EF Core y es ampliamente utilizada en proyectos DDD.

---

## State transitions

### Transitions válidas

```
Pending → Approved
Pending → Rejected
Pending → UnderReview
```

Solo transiciones desde `Pending` son válidas.

### Transitions inválidas

```
Approved → (cualquier estado)
Rejected → (cualquier estado)
UnderReview → (cualquier estado)
```

Una vez que una transacción fue procesada, su estado es **final**.

### ¿Por qué?

En fraud detection, una transacción aprobada o rechazada ya fue evaluada.

Cambiar ese estado crearía inconsistencia en el audit trail.

El `ChangeStatus()` method garantiza esta regla retornando `Result` en lugar de lanzar una excepción:

```csharp
private Result ChangeStatus(TransactionStatus newStatus)
{
    if (Status != TransactionStatus.Pending)
        return Result.Failure(
            $"Only transactions in Pending status can change state. Current status: {Status}.");

    Status = newStatus;
    return Result.Success();
}
```

---

## Best practices

1. La Entity debe ser dueña de su estado
2. No exponer setters públicos
3. Encapsular cambios de estado through methods
4. Validar invariantes dentro de la Entity
5. Usar identity basado en Strongly Typed IDs
6. Mantener propiedades inmutables excepto estado mutable
7. Implementar equality basado en ID
8. Evitar dependencias de infraestructura
9. Crear methods que expresen la intención del negocio
10. Documentar transiciones de estado permitidas
11. Usar private constructors + private setters para EF Core (estándar en DDD)
12. Centralizar validaciones de precondiciones con `Guard` (AgainstNull, AgainstOutOfRange, etc.)
13. Usar `Result` para transiciones de estado que pueden fallar por reglas de negocio, reservar excepciones para errores de programación

---

## Common mistakes

1. Exponer setters públicos en propiedades
2. Colocar lógica de negocio en servicios externos
3. No validar invariantes dentro de la Entity
4. Hacer la Entity un DTO sin behavior
5. Usar Guid directamente en lugar de Strongly Typed IDs
6. Crear methods como `SetStatus()` que rompen encapsulación
7. No implementar equality basado en ID
8. Permitir transiciones de estado inválidas
9. Agregar dependencias de infraestructura
10. No documentar las transiciones de estado
11. Lanzar excepciones para transiciones de estado esperadas (usar `Result` en su lugar)
12. Repetir validaciones de null/range/empty en cada Entity en lugar de usar `Guard`

---

## Interview questions

1. ¿Cuál es la diferencia entre Entity y Value Object?
2. ¿Por qué las Entities no deben tener setters públicos?
3. ¿Qué es un Rich Domain Model vs Anemic Model?
4. ¿Por qué la lógica de negocio debe estar dentro de la Entity?
5. ¿Cómo implementas equality en una Entity?
6. ¿Qué son las invariantes y por qué son importantes?
7. ¿Cómo controlas los cambios de estado en una Entity?
8. ¿Por qué usar Strongly Typed IDs en lugar de Guid?
9. ¿Cuándo deberías crear una nueva Entity vs un Value Object?
10. ¿Cómo afecta el diseño de Entities a la testabilidad?
11. ¿Cómo maneja EF Core la materialización de Entities con constructores privados?
12. ¿Por qué usar `Result` en lugar de excepciones para transiciones de estado?
13. ¿Cuándo deberías usar una excepción vs un `Result` en el Domain?
14. ¿Cuándo tiene sentido centralizar validaciones con una clase `Guard` vs mantenerlas inline?

---

## Technical English

| English | Español | Explicación |
|---------|---------|-------------|
| Entity | Entidad | Objeto con identidad propia |
| Identity | Identidad | Propiedad que distingue una instancia de otra |
| Lifecycle | Ciclo de vida | Fases por las que pasa una Entity |
| Mutable | Mutable | Que puede cambiar después de crearse |
| Inmutable | Inmutable | Que no puede cambiar después de crearse |
| State | Estado | Valores actuales de las propiedades |
| Behavior | Comportamiento | Methods que ejecutan lógica de negocio |
| Encapsulation | Encapsulación | Ocultar estado interno, exponer solo lo necesario |
| Invariant | Invariante | Regla que siempre debe ser verdadera |
| Rich Domain Model | Modelo de Dominio Rico | Entity con comportamiento propio |
| Anemic Model | Modelo Anémico | Entity sin comportamiento, lógica en servicios |
| Setter | Setter | Method que establece un valor |
| State Transition | Transición de Estado | Cambio de un estado a otro |
| Audit Trail | Registro de Auditoría | Historial de cambios de estado |
| Strongly Typed ID | ID Fuertemente Tipado | Wrapper sobre un tipo primitivo |
| Record | Record | Tipo de referencia con Value Equality |
| Class | Class | Tipo de referencia tradicional |
| Property | Propiedad | Miembro de datos con get/set |
| Private set | Setter privado | Setter accesible solo dentro de la clase |
| Override | Sobrescribir | Reimplementar un method de la clase base |
| Materialization | Materialización | Creación de objetos desde datos persistidos |
| Guard | Guardián | Clase estática que centraliza validaciones de precondiciones |
| Result Pattern | Patrón Resultado | Retornar éxito/fracaso en lugar de lanzar excepciones |
| Precondition | Precondición | Condición que debe cumplirse antes de ejecutar lógica |
| State Transition | Transición de Estado | Cambio de un estado a otro controlado por métodos |

---

## Resumen

- Entity tiene identidad propia definida por su ID
- Entity puede cambiar estado (mutable)
- Entity encapsula su estado through behavior
- Rich Domain Model > Anemic Model
- Validar invariantes dentro de la Entity
- No exponer setters públicos
- Controlar transiciones de estado through methods
- Transaction es un ejemplo de Entity con identity, lifecycle y behavior
- FraudRule es otro ejemplo con su propio comportamiento y validaciones
- Las transiciones de estado deben ser validadas
- El estado final (Approved/Rejected) no debe cambiar
- Private constructors + private setters para EF Core es una práctica estándar
- `Guard` centraliza validaciones de precondiciones (null, range, empty GUID, negative)
- `Result` reemplaza excepciones en transiciones de estado esperadas (Approve, Reject, MarkForReview)

---

## Quick Review

• Entity owns identity defined by its ID.
• Value Objects own values, not identity.
• Rich Models protect invariants inside the Entity.
• Anemic Models put logic in external services (avoid).
• Public setters break encapsulation.
• State transitions must be validated.
• TransactionId determines equality, not attributes.
• Behavior belongs inside the Entity, not in services.
• Strongly Typed IDs prevent mixing up identifiers.
• Business rules belong in Domain, not Application.
• FraudRule is a second entity example with Enable/Disable/Rename behavior.
• EF Core private constructors are standard practice, not a design smell.
• Guard class centralizes precondition validation (AgainstNull, AgainstEmptyGuid, etc.).
• Result pattern makes state transition outcomes explicit (Success / Failure with error).
• Use Result for expected business flows, exceptions for programming contract violations.
