# Vertical Slice Architecture

## Definición

**Vertical Slice Architecture** organiza el código por caso de uso, no por capa técnica.

Cada "slice" (rebanada) contiene todo lo necesario para una feature: comando, validador, handler, y pruebas.

---

## Diferencia con Arquitectura por Capas

| Arquitectura por Capas | Vertical Slice |
|------------------------|----------------|
| Organiza por técnica | Organiza por feature |
| Controllers/ en una carpeta | Cada feature tiene su carpeta |
| Services/ en otra carpeta | Todo junto por caso de uso |
| Alta acoplamiento entre features | Bajo acoplamiento entre features |

### Arquitectura por Capas

```
├── Controllers/
│   └── TransactionController.cs
├── Services/
│   └── TransactionService.cs
├── Repositories/
│   └── TransactionRepository.cs
└── Models/
    └── TransactionDto.cs
```

### Vertical Slice

```
├── Features/
│   └── Transactions/
│       └── AnalyzeTransaction/
│           ├── AnalyzeTransactionCommand.cs
│           ├── AnalyzeTransactionValidator.cs
│           └── AnalyzeTransactionHandler.cs
```

---

## ¿Por qué Vertical Slice?

### Cohesión

Todo lo relacionado con una feature está en un mismo lugar.

No necesitas buscar en múltiples carpetas.

### Isolation

Cambios en una feature no afectan otras features.

Puedes modificar AnalyzeTransaction sin tocar GetTransaction.

### Clarity

El flujo de una feature es fácil de entender.

Desde el comando hasta el handler, todo está claro.

---

## Componentes de un Slice

Cada slice típicamente contiene:

| Componente | Responsabilidad |
|------------|-----------------|
| Command | DTO de entrada |
| Validator | Validación de input |
| Handler | Lógica de negocio |
| Response | DTO de salida (opcional) |

```csharp
// Command
public class AnalyzeTransactionCommand
{
    public Guid TransactionId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
}

// Validator
public class AnalyzeTransactionValidator : AbstractValidator<AnalyzeTransactionCommand>
{
    public AnalyzeTransactionValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

// Handler
public class AnalyzeTransactionHandler
{
    // Lógica de negocio aquí
}
```

---

## Vertical Slice + Hexagonal Architecture

Vertical Slice funciona bien con Hexagonal Architecture:

- Cada slice es un **Port** (puerto de entrada)
- El **Handler** implementa la lógica del caso de uso
- Los **Adapters** conectan el slice con el mundo exterior

```
HTTP Request → Controller → Handler → Domain → Repository
                    ↓
              AnalyzeTransaction Slice
```

---

## Relation with Fraud Detection API

En nuestro proyecto:

```
Features/
└── Transactions/
    └── AnalyzeTransaction/
        ├── AnalyzeTransactionCommand.cs      # Command
        ├── AnalyzeTransactionValidator.cs    # Validator
        └── AnalyzeTransactionHandler.cs      # Handler
```

Cada feature futura tendrá su propia carpeta:

```
Features/
├── Transactions/
│   ├── AnalyzeTransaction/
│   ├── GetTransaction/
│   └── ListTransactions/
└── FraudRules/
    ├── CreateFraudRule/
    └── GetFraudRules/
```

---

## Best practices

1. Organizar por feature, no por técnica
2. Cada slice debe ser independiente
3. No compartir código entre slices (salvo Domain)
4. Mantener el Handler enfocado en una sola responsabilidad
5. Validar input en el Validator, no en el Handler
6. El Domain no conoce los Slices
7. Los Slices pueden evolucionar independientemente
8. Facilita el testing por feature
9. Reduce el merge conflict entre equipos
10. Facilita el refactoring incremental

---

## Common mistakes

1. Organizar por técnica en lugar de por feature
2. Compartir lógica entre slices
3. Poner lógica de negocio en el Controller
4. No separar Command de Handler
5. Crear slices demasiado grandes
6. No mantener la independencia de slices
7. Poner infraestructura en el Domain
8. No testear cada slice independientemente
9. Crear dependencias circulares entre slices
10. No documentar la estructura de slices

---

## Interview questions

1. ¿Cuál es la diferencia entre Vertical Slice y arquitectura por capas?
2. ¿Por qué organizar por feature en lugar de por técnica?
3. ¿Cómo se integra Vertical Slice con Hexagonal Architecture?
4. ¿Qué componentes tiene un slice típico?
5. ¿Cómo afecta Vertical Slice al testing?
6. ¿Cuándo NO deberías usar Vertical Slice?
7. ¿Cómo manejas la compartición de código entre slices?
8. ¿Cómo escala Vertical Slice con múltiples equipos?
9. ¿Qué relación tiene Vertical Slice con CQRS?
10. ¿Cómo facilita Vertical Slice el refactoring?

---

## Technical English

| English | Español | Explicación |
|---------|---------|-------------|
| Vertical Slice | Rebanada Vertical | Organización por caso de uso |
| Feature | Funcionalidad | Capacidad del sistema |
| Command | Comando | DTO de entrada para una operación |
| Handler | Manejador | Clase que procesa un comando |
| Cohesión | Cohesión | Grado de relación entre componentes |
| Isolation | Aislamiento | Independencia entre features |
| Layer Architecture | Arquitectura por Capas | Organización por técnica |
| Slice | Rebanada | Componente vertical de una feature |
| Use Case | Caso de Uso | Operación que realiza el sistema |
| Port | Puerto | Interfaz de entrada/salida |
| Adapter | Adaptador | Implementación concreta de un puerto |
| Bounded Context | Contexto Límite | Área del dominio con reglas propias |
| Business Logic | Lógica de Negocio | Reglas del dominio |
| Input Validation | Validación de Entrada | Verificar formato de datos |
| Dependency Injection | Inyección de Dependencias | Proporcionar dependencias externas |
| Separation of Concerns | Separación de Responsabilidades | Cada componente una tarea |
| Single Responsibility | Responsabilidad Única | Una clase, un propósito |
| Loose Coupling | Bajo Acoplamiento | Poca dependencia entre componentes |
| High Cohesion | Alta Cohesión | Mucha relación dentro del componente |
| Refactoring | Refactorización | Mejorar código sin cambiar comportamiento |

---

## Quick Review

• Vertical Slice organizes by feature, not by layer.
• Each slice contains Command, Validator, and Handler.
• Slices are independent and isolated.
• Changes to one slice don't affect others.
• Vertical Slice pairs well with Hexagonal Architecture.
• The Domain layer is shared across all slices.
• Each feature is easy to understand and test.
• Reduce merge conflicts with isolated slices.
• Facilitates incremental refactoring.
• Organize by business capability, not technical role.