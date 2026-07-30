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

### HighRiskCountrySpecification

Evalúa: `_highRiskCountryCodes.Contains(transaction.Country)`

Decisiones de diseño:
- Verifica el campo `Country` de la Transaction (ISO 3166-1 alpha-2), no la moneda
- Códigos de alto riesgo: IR, KP, SY, VE
- Si `transaction.Country` es null (país no especificado), retorna false (no aplica)
- Los códigos se normalizan a mayúsculas en el constructor
- Recibe `IEnumerable<string>` en el constructor (configurable, no hardcodeado)
- Transaction null es rechazado (`ArgumentNullException`)
- No depende de Application, Infrastructure ni librerías externas

Corrección de diseño:
- Originalmente utilizaba `transaction.Amount.Currency` como proxy geográfico (códigos de moneda IRR, KPW, SYP, VEF)
- En Phase 4/5 se corrigió para usar `transaction.Country` con códigos de país (IR, KP, SY, VE)
- La moneda es un proxy imperfecto porque no refleja el origen geográfico real de la transacción

---

## ¿Cómo se relaciona con DDD?

| Concepto DDD | Specification Pattern |
|---|---|
| **Ubiquitous Language** | La especificación tiene nombre de negocio: `HighAmountTransactionSpecification` |
| **Bounded Context** | Pertenece al Domain del contexto FraudDetection |
| **Domain Model** | Es un ciudadano de primera clase del Domain |
| **Entity** | La entidad (`Transaction`) se mantiene limpia de reglas externas |
| **Value Object** | La especificación usa `Money` correctamente sin exponer su implementación |
| **Domain Service** | Las Specifications son utilizadas por el Domain Service `FraudRuleEngine` |

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

## FraudRuleEngine: Domain Service que usa Specifications

El `FraudRuleEngine` ya está **implementado** (no es futuro). Es un **Domain Service** stateless que:

1. Recibe una `Transaction`, una colección de `FraudRule`, y un diccionario de `ISpecification`
2. Itera las reglas habilitadas, busca su specification correspondiente, y la evalúa
3. Acumula el RiskScore de las reglas que aplican
4. Retorna un `FraudRuleEngineResult` con el score total y el status recomendado
5. Evalúa el `Action` de las reglas que aplicaron — si alguna tiene `FraudRuleAction.Reject`, el status es `Rejected`

```csharp
// Domain/Services/FraudRuleEngine.cs (updated with Rejected logic)
public class FraudRuleEngine
{
    public FraudRuleEngineResult Evaluate(
        Transaction transaction,
        IEnumerable<FraudRule> fraudRules,
        IReadOnlyDictionary<string, ISpecification> specifications)
    {
        var matchedRules = new List<FraudRule>();
        var totalRiskScore = 0;

        foreach (var rule in fraudRules)
        {
            if (!rule.IsEnabled)
                continue;

            if (!specifications.TryGetValue(rule.RuleName, out var specification))
                continue;

            if (specification.IsSatisfiedBy(transaction))
            {
                matchedRules.Add(rule);
                totalRiskScore += rule.RiskScore;
            }
        }

        var recommendedStatus = matchedRules.Any(r => r.Action == FraudRuleAction.Reject)
            ? TransactionStatus.Rejected
            : totalRiskScore > 0
                ? TransactionStatus.UnderReview
                : TransactionStatus.Approved;

        return new FraudRuleEngineResult(
            totalRiskScore,
            recommendedStatus,
            matchedRules.AsReadOnly());
    }
}
```

**Flujo de uso en el Handler de Application:**

```csharp
// AnalyzeTransactionHandler.Handle()
var rules = _ruleProvider.GetAllRules();
var specifications = _ruleProvider.GetSpecifications();
var evaluation = _engine.Evaluate(transaction, rules, specifications);

// Aplicar el status recomendado usando behavior de la Entity
transaction.Approve();  // o MarkForReview() / Reject()
```

**Cómo se conectan las Specifications con las reglas:**

```
FraudRule (RuleName="HighAmount", RiskScore=50)
    │
    └──→ Busca en spec dictionary: specifications["HighAmount"]
                │
                ▼
       HighAmountTransactionSpecification(threshold: 10000m)
                │
                ├── Amount >= 10000 → rule applies (+50 risk score)
                └── Amount < 10000  → rule does not apply
```

La conexión entre `FraudRule` y `ISpecification` se hace por nombre (`RuleName`). Esto permite que nuevas reglas se agreguen simplemente creando una Specification y registrándola en el provider.

---

### Specifications actuales

Actualmente existen **cuatro** Specifications concretas en la carpeta `Transactions/`:

- `HighAmountTransactionSpecification` — evalúa si el monto supera un threshold
- `VelocityTransactionSpecification` — evalúa si `transaction.RecentTransactionCount >= maxTransactionCount` (control de velocidad)
- `BlacklistCustomerSpecification` — evalúa si `transaction.CustomerId` está en un conjunto de clientes blacklisteados
- `HighRiskCountrySpecification` — evalúa si `transaction.Country` es un código de país de alto riesgo (ISO 3166-1 alpha-2)

Cada Specification se asocia a una `FraudRule` por nombre (`RuleName`) en el provider:

```
FraudRule("HighAmount", RiskScore=50, Action=Review)
  → HighAmountTransactionSpecification(threshold: 10000)

FraudRule("Velocity", RiskScore=70, Action=Reject)
  → VelocityTransactionSpecification(maxTransactionCount: 5, timeWindow: 1h)

FraudRule("Blacklist", RiskScore=100, Action=Reject)
  → BlacklistCustomerSpecification(blacklistedCustomers: {...})

FraudRule("HighRiskCountry", RiskScore=30, Action=Review)
  → HighRiskCountrySpecification(highRiskCountryCodes: {"IR", "KP", "SY", "VE"})
```

**Composición de Specifications** (AND/OR/NOT) no está implementada. Si en el futuro se necesita combinar reglas, se pueden crear Specifications compuestas:

```csharp
// Futuro: composición AND
public class AndSpecification : ISpecification
{
    private readonly ISpecification _left;
    private readonly ISpecification _right;

    public bool IsSatisfiedBy(Transaction transaction) =>
        _left.IsSatisfiedBy(transaction) && _right.IsSatisfiedBy(transaction);
}
```

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

El `FraudRuleEngine` también se testea de forma aislada:

```csharp
[Fact]
public void HighAmountRule_TriggersUnderReview()
{
    var transaction = CreateTransaction(20000);
    var rule = new FraudRule(FraudRuleId.New(), "HighAmount", 50);
    var specs = new Dictionary<string, ISpecification>
    {
        ["HighAmount"] = new HighAmountTransactionSpecification(10000)
    };
    var engine = new FraudRuleEngine();

    var result = engine.Evaluate(transaction, [rule], specs);

    Assert.Equal(TransactionStatus.UnderReview, result.RecommendedStatus);
    Assert.Equal(50, result.TotalRiskScore);
}
```

---

## Resumen

| Aspecto | Dentro de Entity | Specification Pattern |
|---------|-----------------|---------------------|
| Reglas internas del ciclo de vida | ✅ `Approve()`, `ChangeStatus()` | ❌ No aplica |
| Reglas de evaluación externas | ❌ Satura la entidad | ✅ `HighAmountTransactionSpecification` |
| Threshold configurable | ❌ Hardcodeado | ✅ Constructor parameter |
| Combinación de reglas | ❌ Métodos separados | ✅ AND / OR composition (futuro) |
| Testing aislado | ❌ Requiere crear Entity | ✅ Clase independiente |
| Reutilización entre entidades | ❌ Acoplado a Transaction | ✅ Interface genérica posible |
| Domain Service | ❌ No aplica | ✅ `FraudRuleEngine` usa Specifications |

---

## Próximo paso

Composición de Specifications (AND, OR, NOT) para combinar reglas complejas sin modificar el engine ni las specifications existentes.
