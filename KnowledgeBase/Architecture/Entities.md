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
| Ejemplo | Transaction | Money |

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

**Behavior:**

- `Approve()` — transiciona a Approved
- `Reject()` — transiciona a Rejected
- `MarkForReview()` — transiciona a UnderReview

```csharp
public void Approve() => ChangeStatus(TransactionStatus.Approved);
```

El method privado `ChangeStatus()` centraliza la validación.

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

El `ChangeStatus()` method garantiza esta regla:

```csharp
private void ChangeStatus(TransactionStatus newStatus)
{
    if (Status != TransactionStatus.Pending)
        throw new InvalidOperationException(...);

    Status = newStatus;
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
- Las transiciones de estado deben ser validadas
- El estado final (Approved/Rejected) no debe cambiar
