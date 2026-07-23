# Specification Pattern

## Objetivo

Comprender el **Specification Pattern**, cuándo utilizarlo y cómo implementarlo dentro de una arquitectura DDD y Hexagonal.

Conceptos clave:
- separar reglas de negocio reutilizables
- mantener las entidades del Domain enfocadas en su comportamiento principal
- componer reglas complejas a partir de reglas simples

---

## ¿Qué problema resuelve?

Frecuentemente, las entidades del Domain acumulan métodos que verifican condiciones del negocio.

Ejemplo:

```csharp
public class Transaction
{
    public bool IsHighAmount(decimal threshold) =>
        Amount.Amount >= threshold;

    public bool IsCrossBorder() => ...

    public bool IsNewCustomer() => ...
}
```

Cada nueva regla añade más responsabilidad a la entidad.

Esto provoca:
- **Entity Bloat** — la entidad crece con reglas que no forman parte de su comportamiento esencial
- **Baja reusabilidad** — las reglas están acopladas a la entidad y no pueden combinarse libremente
- **Dificultad de testing** — cada regla requiere crear una entidad completa
- **Mezcla de conceptos** — reglas de negocio mezcladas con comportamiento de la entidad

El **Specification Pattern** resuelve esto extrayendo cada regla a su propia clase.

---

## ¿Qué es Specification Pattern?

Es un patrón de diseño de Domain-Driven Design que encapsula una regla de negocio en un objeto independiente.

Una **Specification** es un objeto que evalúa si un candidato cumple un criterio específico.

```csharp
// Define el contrato
public interface ISpecification
{
    bool IsSatisfiedBy(Transaction transaction);
}

// Implementa una regla concreta
public class HighAmountTransactionSpecification : ISpecification
{
    private readonly decimal _threshold;

    public HighAmountTransactionSpecification(decimal threshold)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold cannot be negative.");
        _threshold = threshold;
    }

    public bool IsSatisfiedBy(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return transaction.Amount.Amount >= _threshold;
    }
}
```

Uso:

```csharp
var specification = new HighAmountTransactionSpecification(10000m);
bool isHighAmount = specification.IsSatisfiedBy(transaction);
```

---

## Implementación en el proyecto

### ISpecification

```csharp
// Domain/Specifications/ISpecification.cs
namespace FraudDetection.Domain.Specifications;

public interface ISpecification
{
    bool IsSatisfiedBy(Transaction transaction);
}
```

El contrato pertenece al **Domain Layer**. Recibe un `Transaction` porque en nuestro dominio la unidad de evaluación es la transacción.

No utiliza genéricos porque en esta fase no son necesarios (YAGNI).

### HighAmountTransactionSpecification

```csharp
// Domain/Specifications/Transactions/HighAmountTransactionSpecification.cs
namespace FraudDetection.Domain.Specifications.Transactions;

public class HighAmountTransactionSpecification : ISpecification
```

Evalúa: `transaction.Amount.Amount >= threshold`

Decisiones de diseño:
- El threshold se recibe en el constructor (configurable, no hardcodeado)
- Threshold negativo es rechazado como invariante (`ArgumentOutOfRangeException`)
- Transaction null es rechazado (`ArgumentNullException`)
- Solo opera con conceptos del Domain (`Transaction`, `Money`)
- No depende de Application, Infrastructure ni librerías externas

---

## ¿Cómo se relaciona con DDD?

| Concepto DDD | Specification Pattern |
|---|---|
| **Ubiquitous Language** | La especificación tiene nombre de negocio: `HighAmountTransactionSpecification` |
| **Bounded Context** | Pertenece al Domain del contexto FraudDetection |
| **Domain Model** | Es un ciudadano de primera clase del Domain |
| **Entity** | La entidad (`Transaction`) se mantiene limpia de reglas externas |
| **Value Object** | La especificación usa `Money` correctamente sin exponer su implementación |
| **Domain Service** | Las Specifications pueden ser usadas por un Domain Service (futuro `FraudRuleEngine`) |

---

## ¿Por qué no poner la regla directamente en Transaction?

Opción 1 — dentro de Transaction:

```csharp
public class Transaction
{
    public bool IsHighAmount() => Amount.Amount >= 10000;
}
```

Problemas:
- La entidad necesita conocer el threshold, que es un detalle de configuración
- El threshold queda hardcodeado (10000)
- Si la regla cambia (threshold configurable por cliente), la entidad debe cambiar
- No se puede combinar con otras reglas sin añadir más métodos

Opción 2 — Specification Pattern:

```csharp
var highAmount = new HighAmountTransactionSpecification(10000m);
var crossBorder = new CrossBorderTransactionSpecification();

bool isSuspicious = highAmount.IsSatisfiedBy(tx)
                 && crossBorder.IsSatisfiedBy(tx);
```

Ventajas:
- El threshold es configurable
- Cada regla es independiente y testeable
- Las reglas pueden combinarse sin modificar la entidad
- La entidad se mantiene enfocada en su comportamiento esencial
- Las reglas pueden persistirse, configurarse por cliente, etc.

**Cuándo usar cada una:**
- **Dentro de la Entity**: invariantes del ciclo de vida (ej: `Approve()`, `ChangeStatus()`)
- **Specification**: reglas de evaluación externas (ej: umbrales de fraude, criterios de riesgo)

---

## Preparación para FraudRuleEngine

Actualmente el proyecto tiene:
- `Transaction` — la entidad a evaluar
- `FraudRule` — reglas configurables con nombre, risk score y estado
- `ISpecification` — contrato para evaluar condiciones
- `HighAmountTransactionSpecification` — especificación concreta

El siguiente paso natural será:

1. Crear Specifications adicionales (`CrossBorderTransactionSpecification`, `NewCustomerTransactionSpecification`, etc.)
2. Implementar **AND / OR / NOT** composition para combinar Specifications
3. Implementar `FraudRuleEngine` como Domain Service que:
   - Recibe una `Transaction`
   - Evalúa todas las `FraudRule` activas
   - Usa Specifications para determinar si cada regla aplica
   - Calcula un risk score total
   - Decide el status recomendado

El `FraudRuleEngine` no reemplaza las Specifications. Las **compone y orquesta**.

---

## Testabilidad

Cada Specification es una clase independiente que puede testearse de forma aislada:

```csharp
[Fact]
public void AmountBelowThreshold_ReturnsFalse()
{
    var transaction = CreateTransaction(5000);
    var specification = new HighAmountTransactionSpecification(10000);

    var result = specification.IsSatisfiedBy(transaction);

    Assert.False(result);
}
```

Ventajas:
- No necesita mocking
- No necesita infraestructura
- Tests rápidos y deterministas
- Cobertura clara de cada regla de negocio

---

## Resumen

| Aspecto | Dentro de Entity | Specification Pattern |
|---------|-----------------|---------------------|
| Reglas internas del ciclo de vida | ✅ `Approve()`, `ChangeStatus()` | ❌ No aplica |
| Reglas de evaluación externas | ❌ Satura la entidad | ✅ `HighAmountTransactionSpecification` |
| Threshold configurable | ❌ Hardcodeado | ✅ Constructor parameter |
| Combinación de reglas | ❌ Métodos separados | ✅ AND / OR composition |
| Testing aislado | ❌ Requiere crear Entity | ✅ Clase independiente |
| Reutilización entre entidades | ❌ Acoplado a Transaction | ✅ Interface genérica posible |

---

## Próximo paso

Composición de Specifications (AND, OR, NOT) e implementación de `FraudRuleEngine` como Domain Service que evalúa una `Transaction` contra múltiples `FraudRule` usando Specifications.
