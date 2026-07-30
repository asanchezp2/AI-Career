# Value Objects

# Objetivo

Comprender cuándo un concepto del dominio debe modelarse como un **Value Object** y no como una Entity.

Al finalizar este tema seré capaz de:

- identificar Value Objects en un dominio
- implementarlos correctamente en .NET
- explicar por qué existen en DDD
- utilizarlos dentro de una Arquitectura Hexagonal
- responder preguntas de entrevista sobre este patrón

---

# ¿Qué problema resuelven?

Muchos proyectos utilizan tipos primitivos para representar conceptos del negocio.

Ejemplo:

```csharp
Guid transactionId
Guid customerId
decimal amount
string currency
```

El compilador no conoce el significado de esos datos.

Esto provoca errores como:

- intercambiar parámetros
- validaciones repetidas
- reglas repartidas por toda la aplicación
- código difícil de mantener

Los Value Objects encapsulan esas reglas en un único lugar.

---

# Definición

Un **Value Object** representa un concepto del negocio cuya identidad depende únicamente de sus valores.

Dos Value Objects con el mismo contenido son iguales.

No importa cuándo fueron creados.

No importa dónde fueron creados.

Solo importan sus valores.

---

# Características

Un Value Object debe ser:

- Inmutable (Immutable)
- Sin identidad propia (No Identity)
- Comparable por valor (Value Equality)
- Auto validado (Self Validating)
- Representar un concepto del dominio (Business Concept)

---

# ¿Por qué usamos `record`?

En .NET los `record` implementan automáticamente:

- Value Equality
- Inmutabilidad sencilla
- Código reducido
- Mejor legibilidad

Ejemplo:

```csharp
var money1 = new Money(100, "USD");
var money2 = new Money(100, "USD");

money1 == money2; // true
```

No necesitamos implementar `Equals()` manualmente.

---

# ¿Por qué no usamos `record struct`?

Podría utilizarse.

Sin embargo, para este proyecto decidimos usar `record` porque:

- es más simple
- es el enfoque más común en DDD
- evita complejidad innecesaria
- es suficiente para nuestro dominio

Si en el futuro aparecen problemas reales de rendimiento podremos reevaluar esta decisión.

---

# Value Objects del proyecto

## Money

Representa una cantidad monetaria.

Responsabilidades:

- validar monto
- validar moneda
- almacenar ambos valores como una única unidad

Ejemplo:

```csharp
var amount = new Money(150.75m, "USD");
```

---

## TransactionId

Representa el identificador de una transacción.

En lugar de utilizar:

```csharp
Guid
```

utilizamos:

```csharp
TransactionId
```

Esto aporta seguridad de tipos (Type Safety).

---

## CustomerId

Representa el identificador del cliente.

Aunque internamente contiene un Guid, expresa un concepto distinto del dominio.

El compilador impide intercambiarlo accidentalmente con TransactionId.

---

## FraudRuleId

Representa el identificador de una regla de fraude.

Sigue el mismo patrón que TransactionId y CustomerId:

```csharp
public record FraudRuleId
{
    public Guid Value { get; }

    public static FraudRuleId New() => new(Guid.NewGuid());
    public static FraudRuleId From(Guid value) => new(value);

    private FraudRuleId(Guid value)
    {
        Guard.AgainstEmptyGuid(value, nameof(value));
        Value = value;
    }
}
```

Se agregó cuando se implementó la entidad `FraudRule` en Sprint 2.

**Nota:** En Phase 2/5, los tres constructores de Value Objects que validaban `Guid.Empty` manualmente (`TransactionId`, `CustomerId`, `FraudRuleId`) fueron refactorizados para usar `Guard.AgainstEmptyGuid()`. El constructor de `Money` ahora usa `Guard.AgainstNegative()` y `Guard.AgainstNullOrWhiteSpace()`. Esto centraliza la validación, elimina código duplicado y estandariza los mensajes de error.

---

# Strongly Typed IDs

Los Strongly Typed IDs son un caso particular de Value Object.

Su objetivo es encapsular identificadores primitivos.

Ejemplo incorrecto:

```csharp
Process(Guid transactionId, Guid customerId);
```

Nada impide hacer:

```csharp
Process(customerId, transactionId);
```

Ejemplo correcto:

```csharp
Process(TransactionId transactionId,
        CustomerId customerId);
```

Ahora el error se detecta durante la compilación.

---

# Primitive Obsession

Uno de los anti-patrones más comunes.

Consiste en modelar el dominio usando únicamente:

- string
- Guid
- int
- decimal
- bool

En lugar de crear conceptos propios del negocio.

Ejemplo:

❌

```csharp
decimal amount
string currency
```

✔

```csharp
Money amount
```

---

# Arquitectura

En Arquitectura Hexagonal los Value Objects pertenecen al **Domain**.

Nunca dependen de:

- Entity Framework
- ASP.NET
- Bases de datos
- APIs
- Infraestructura

Su única responsabilidad es representar conceptos del negocio.

---

# EF Core Mapping

Los Value Objects fuertemente tipados (`TransactionId`, `CustomerId`, `FraudRuleId`) se mapean a la base de datos mediante **Value Converters** en la capa de Infrastructure.

Los converters convierten entre el VO del Domain y el tipo primitivo (`Guid`) que entiende EF Core:

```csharp
// Infrastructure/Persistence/Converters/TransactionIdConverter.cs
public sealed class TransactionIdConverter : ValueConverter<TransactionId, Guid>
{
    public TransactionIdConverter()
        : base(id => id.Value, value => TransactionId.From(value))
    {
    }
}
```

El Domain NO tiene conocimiento de estos converters. No necesita atributos de EF Core, ni implementar interfaces de persistencia. Es el Infrastructure quien se adapta al Domain (no al revés).

Para `Money`, se utiliza un **Owned Type** en lugar de un converter, produciendo dos columnas en la tabla:
- `Amount_Amount` (decimal)
- `Amount_Currency` (string)

---

# Decisiones Arquitectónicas

Durante este proyecto se tomaron las siguientes decisiones.

| Decisión | Motivo |
|----------|--------|
| Usar `record` | Simplicidad y Value Equality |
| No usar `record struct` | Evitar optimización prematura |
| Encapsular Guid | Type Safety |
| Validar en el constructor | Garantizar objetos válidos |
| No usar librerías externas | Aprender la implementación desde cero |
| ValueConverter para IDs | Mapear a DB sin modificar el Domain |
| Owned Type para Money | Dos columnas sin modificar el Domain |
| Guard.AgainstEmptyGuid | Centralizar validación de Guid.Empty (tres VOs idénticos) |
| Guard.AgainstNegative | Validar Amount no negativo en Money |
| Guard.AgainstNullOrWhiteSpace | Validar Currency no vacío en Money |

---

# Errores comunes

❌ Crear un Value Object mutable.

❌ Usar setters públicos.

❌ Permitir estados inválidos.

❌ Colocar lógica de infraestructura.

❌ Crear Value Objects para cualquier cosa.

---

# Preguntas de entrevista

- ¿Cuál es la diferencia entre Entity y Value Object?
- ¿Por qué usar `record`?
- ¿Qué problema resuelven los Strongly Typed IDs?
- ¿Qué es Primitive Obsession?
- ¿Por qué los Value Objects deben ser inmutables?
- ¿Cómo se mapean Value Objects a la base de datos sin contaminar el Domain?

---

# Inglés Técnico

| English | Español |
|----------|----------|
| Value Object | Objeto de Valor |
| Entity | Entidad |
| Identity | Identidad |
| Immutable | Inmutable |
| Value Equality | Igualdad por Valor |
| Reference Equality | Igualdad por Referencia |
| Strongly Typed ID | Identificador Fuertemente Tipado |
| Primitive Obsession | Obsesión por Primitivos |
| Domain Model | Modelo de Dominio |
| Business Rule | Regla de Negocio |
| Value Converter | Convertidor de Valor |
| Owned Type | Tipo Propietario |

---

# Resumen

Los Value Objects representan conceptos del negocio definidos únicamente por sus valores.

Permiten:

- centralizar validaciones
- expresar mejor el dominio
- aumentar la seguridad de tipos
- evitar Primitive Obsession
- escribir código más mantenible

En este proyecto ya existen cuatro ejemplos:

- Money
- TransactionId
- CustomerId
- FraudRuleId

Todos los Value Objects del proyecto centralizan su validación de precondiciones (null, empty GUID, negative) a través de la clase `Guard` en el Domain, en lugar de realizar comprobaciones manuales. Esto reduce la duplicación y mantiene los constructores enfocados en la lógica del dominio.

---

# Summary (English)

Value Objects model business concepts whose identity is defined only by their values.

Benefits:

- Better Domain Model
- Type Safety
- Encapsulated Validation
- Immutable Design
- Easier Maintenance

Current implementation:

- Money
- TransactionId
- CustomerId
- FraudRuleId

EF Core mapping is done via ValueConverters and Owned Types in Infrastructure — the Domain remains pure.

---

# Próximo tema

**Model Validation**

Aprenderemos cómo validar los datos que llegan desde la API utilizando:

- Data Annotations
- FluentValidation

y veremos por qué la validación de entrada no sustituye las reglas del dominio.
