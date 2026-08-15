# Validation

## Definición

La **validación** es el proceso de verificar que los datos de entrada cumplan con las reglas de formato antes de llegar al Domain.

Validar ≠ Ejecutar reglas de negocio.

La validación asegura que los datos estén bien formados.

Las reglas de negocio aseguran que los datos sean válidos en contexto.

---

## Diferencia entre Validación y Business Rules

| Tipo | Pregunta | Capa | Ejemplo |
|------|----------|------|---------|
| Validation | "¿Los datos están bien formados?" | Application | Currency tiene 3 caracteres |
| Business Rule | "¿Esta transacción está permitida?" | Domain | Cliente tiene saldo suficiente |

```csharp
// Validation (Application Layer)
RuleFor(x => x.Currency).Length(3);

// Business Rule (Domain Layer)
if (amount > customer.Balance)
    throw new InsufficientBalanceException();
```

---

## ¿Por qué FluentValidation?

FluentValidation es una librería que permite definir reglas de validación de forma expresiva.

Ventajas:

- Sintaxis limpia y legible
- Separación de reglas de validación
- Fácil de testear
- Integración con .NET

```csharp
public class AnalyzeTransactionValidator : AbstractValidator<AnalyzeTransactionCommand>
{
    public AnalyzeTransactionValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).Length(3);
    }
}
```

---

## ¿Por qué en Application Layer?

La validación de entrada pertenece a la Application Layer porque:

1. Es la primera capa que recibe datos externos
2. Rechaza datos inválidos antes de cruzar capas
3. Mantiene el Domain puro (sin dependencias de validación)
4. Sigue el principio de "fail fast"

```
HTTP Request → Validation → Domain → Response
                 ↓
            Rechazar si inválido
```

---

## Reglas de validación en el proyecto

### TransactionId

```csharp
RuleFor(x => x.TransactionId)
    .NotEmpty()
    .WithMessage("Transaction ID is required.");
```

### CustomerId

```csharp
RuleFor(x => x.CustomerId)
    .NotEmpty()
    .WithMessage("Customer ID is required.");
```

### Amount

```csharp
RuleFor(x => x.Amount)
    .GreaterThanOrEqualTo(0)
    .WithMessage("Amount must be greater than or equal to zero.");
```

**Nota:** Se cambió de `GreaterThan(0)` a `GreaterThanOrEqualTo(0)` en Phase 2/5 para alinear con la validación del Domain (`Money` permite `Amount >= 0`). El Domain es la fuente de verdad — la validación de entrada no debe ser más restrictiva que las reglas del dominio (ver ADR-017 para contexto).

### Timestamp

```csharp
RuleFor(x => x.Timestamp)
    .NotEmpty()
    .WithMessage("Timestamp is required.");
```

El timestamp representa la fecha y hora en que ocurrió la transacción (UTC). El cliente lo provee y se almacena como `CreatedAt` en la base de datos.

### Country

```csharp
RuleFor(x => x.Country)
    .Must(country => country is null || CountryCodeRegex.IsMatch(country))
    .WithMessage("Country must be a valid ISO 3166-1 alpha-2 code (2 uppercase letters).");
```

El país es opcional (nullable). Cuando se provee, debe ser un código ISO 3166-1 alpha-2 de dos letras mayúsculas. Se usa `Regex.IsMatch("^[A-Z]{2}$")` para validación.

### Currency

```csharp
RuleFor(x => x.Currency)
    .NotEmpty()
    .WithMessage("Currency is required.")
    .Length(3)
    .WithMessage("Currency must be exactly 3 characters.")
    .Must(currency => currency == currency.ToUpperInvariant())
    .WithMessage("Currency must be uppercase.");
```

### Metadata

El diccionario `Metadata` es opcional (nullable). Cuando se provee, está limitado por `MetadataLimitsOptions` (sección `MetadataLimits` en appsettings): `MaxEntries` (10), `MaxKeyLength` (50), `MaxValueLength` (200) y `MaxTotalBytes` (2048 bytes UTF-8). Exceder cualquier límite devuelve `400` (ver ADR-047).

```csharp
// Ejemplo: 12 entradas excede MaxEntries (10)
RuleFor(x => x.Metadata)
    .Must(metadata => metadata is null || metadata.Count <= options.MaxEntries)
    .WithMessage($"Metadata must not contain more than {options.MaxEntries} entries.");
```

---

## Tests de validación

El validador `AnalyzeTransactionValidator` tiene **22 tests unitarios** en el proyecto `FraudDetection.UnitTests`.

Los tests cubren:

Cada regla individual se testea con dos casos (válido e inválido):

| Test | Verifica |
|------|----------|
| `Valid_Command_PassesValidation` | Comando completo válido |
| `Timestamp_Required_ReturnsValidationError` | Timestamp requerido |
| `Empty_TransactionId_FailsValidation` | TransactionId requerido |
| `Empty_CustomerId_FailsValidation` | CustomerId requerido |
| `Amount_Zero_PassesValidation` | Amount >= 0 es válido (Domain es fuente de verdad) |
| `Amount_Negative_FailsValidation` | Amount negativo inválido |
| `Empty_Currency_FailsValidation` | Currency requerido |
| `Currency_LengthLessThanThree_FailsValidation` | Currency debe tener 3 caracteres |
| `Currency_LengthMoreThanThree_FailsValidation` | Currency no debe superar 3 caracteres |
| `Currency_Lowercase_FailsValidation` | Currency debe estar en mayúsculas |
| `Country_Null_PassesValidation` | Country opcional (null válido) |
| `Country_EmptyString_PassesValidation` | Country vacío válido |
| `Country_Lowercase_FailsValidation` | Country debe ser ISO 3166-1 alpha-2 |
| `Country_ThreeCharacters_FailsValidation` | Country debe tener 2 letras |
| `Country_ValidUppercase_PassesValidation` | Country válido |
| `Metadata_Null_PassesValidation` | Metadata opcional (null válido) |
| `Metadata_WithinLimits_PassesValidation` | Metadata dentro de límites |
| `Metadata_TooManyEntries_FailsValidation` | Metadata respeta `MaxEntries` (ADR-047) |
| `Metadata_KeyTooLong_FailsValidation` | Keys respetan `MaxKeyLength` |
| `Metadata_ValueTooLong_FailsValidation` | Values respetan `MaxValueLength` |
| `Metadata_TotalBytesExceeded_FailsValidation` | Metadata respeta `MaxTotalBytes` |
| `Metadata_ExactlyAtLimits_PassesValidation` | Metadata en el límite exacto es válido |

```csharp
[Fact]
public void EmptyTransactionId_ReturnsValidationError()
{
    var command = new AnalyzeTransactionCommand
    {
        TransactionId = Guid.Empty,
        CustomerId = Guid.NewGuid(),
        Amount = 100,
        Currency = "USD"
    };

    var validator = new AnalyzeTransactionValidator();
    var result = validator.TestValidate(command);

    result.ShouldHaveValidationErrorFor(x => x.TransactionId);
}
```

Estos tests son **independientes**, **rápidos** y no requieren mocking porque FluentValidation trabaja directamente con el comando.

---

## Validation vs Domain Validation

| Application Validation | Domain Validation |
|------------------------|-------------------|
| Formato de entrada | Reglas de negocio |
| Campos requeridos | Invariantes |
| Longitud, rango | Transiciones de estado |
| Independiente del contexto | Dependiente del contexto |
| Rechaza datos mal formados | Rechaza operaciones inválidas |

---

## Best practices

1. Validar entrada en Application Layer
2. No transformar valores en la validación
3. Mensajes de error claros y específicos
4. No validar reglas de negocio en FluentValidation
5. Mantener validadores separados por feature
6. Testear cada regla de validación
7. Usar `NotEmpty()` para campos requeridos
8. Usar `GreaterThanOrEqualTo()` para valores numéricos (alineado con Domain)
9. Usar `Length()` para strings de longitud fija
10. Usar `Must()` para validaciones personalizadas

---

## Common mistakes

1. Mezclar validación con reglas de negocio
2. Transformar valores durante la validación
3. Validar en el Domain cosas que deberían validarse en Application
4. No testear los validadores
5. Usar mensajes de error genéricos
6. Olvidar validar campos requeridos
7. No usar FluentValidation y hacer validación manual
8. Poner lógica de infraestructura en el validador
9. No separar validadores por feature
10. Validar demasiado temprano o demasiado tarde

---

## Relation with Fraud Detection API

En nuestro proyecto:

- `AnalyzeTransactionValidator` valida el input en Application Layer (22 tests de validación)
- `Transaction.ChangeStatus()` valida reglas de negocio en Domain Layer
- La validación rechaza datos inválidos antes de llegar al Domain
- Las reglas de negocio garantizan invariantes del dominio
- Amount se valida con `GreaterThanOrEqualTo(0)` para alinear con el Domain (Phase 2/5)
- Timestamp y Country se agregaron como campos opcionales en Phase 4/5

```csharp
// Application: valida formato
var validator = new AnalyzeTransactionValidator();
var result = await validator.ValidateAsync(command);

// Domain: valida reglas de negocio
transaction.Approve();  // Valida que esté Pending
```

---

## Interview questions

1. ¿Cuál es la diferencia entre validación y reglas de negocio?
2. ¿Por qué FluentValidation pertenece a Application Layer?
3. ¿Qué validías en Application vs Domain?
4. ¿Cómo testear un validador con FluentValidation?
5. ¿Por qué no transformar valores durante la validación?
6. ¿Qué es "fail fast" y cómo se aplica?
7. ¿Cuándo usar `Must()` vs reglas predefinidas?
8. ¿Cómo manejar errores de validación en una API?
9. ¿Por qué no validar reglas de negocio en FluentValidation?
10. ¿Cómo se relaciona la validación con la arquitectura hexagonal?

---

## Technical English

| English | Español | Explicación |
|---------|---------|-------------|
| Validation | Validación | Verificar que datos cumplan reglas de formato |
| Business Rule | Regla de Negocio | Lógica que define si una operación es permitida |
| FluentValidation | FluentValidation | Librería para definir reglas de validación |
| Input Validation | Validación de Entrada | Verificar datos que llegan desde fuera |
| Domain Validation | Validación de Dominio | Verificar reglas de negocio dentro del Domain |
| AbstractValidator | Validador Abstracto | Clase base de FluentValidation |
| RuleFor | Regla Para | Método que define una regla de validación |
| NotEmpty | No Vacío | Regla que verifica que un valor no esté vacío |
| GreaterThan | Mayor Que | Regla que verifica que un valor sea mayor |
| Length | Longitud | Regla que verifica la longitud de un string |
| Must | Debe | Regla personalizada con lambda |
| TestValidate | Validar Prueba | Método para testear validadores |
| ShouldHaveValidationErrorFor | Debe Tener Error Para | Verifica que exista un error de validación |
| ShouldNotHaveAnyValidationErrors | No Debe Tener Errores | Verifica que no haya errores |
| Fail Fast | Fallar Rápido | Rechazar datos inválidos lo antes posible |
| Invariant | Invariante | Regla que siempre debe ser verdadera |
| Boundary | Límite | Punto donde se valida la entrada |
| DTO | DTO | Data Transfer Object para transportar datos |
| Command | Comando | DTO que representa una operación a ejecutar |
| Handler | Manejador | Clase que procesa un comando |

---

## Quick Review

• Validation asks "Is the input well-formed?"
• Business Rules ask "Is this operation allowed?"
• FluentValidation belongs to Application Layer.
• Domain Validation belongs to Domain Layer.
• Never transform values during validation.
• Fail fast: reject bad input at the boundary.
• Use `NotEmpty()` for required fields.
• Use `Must()` for custom validation logic.
• Use `GreaterThanOrEqualTo()` to align with Domain invariants.
• Test every validation rule independently (22 tests for AnalyzeTransactionValidator).
• Validation and Business Rules serve different purposes.
