# Guion breve para entrevista

## Presentación de 90 segundos

“Construí Fraud Detection Challenge como un sistema .NET 8 con una API y un worker desacoplados por Kafka. La API crea la transacción en pending; el worker aplica dos reglas deterministas y publica su decisión; la API consume esa respuesta y persiste el estado terminal. El acumulado agrupa por cuenta de origen y día UTC e incluye las transacciones rechazadas; el reto no define esa política. Ejecuté la demo local con Docker Compose y comprobé aprobación, rechazo por valor alto y rechazo por acumulado diario. La entrega es at-least-once; una solución productiva necesitaría outbox y DLQ.”

## Demostración local

1. Mostrar el README y la matriz `CHALLENGE_TRACEABILITY.md`.
2. Levantar Compose siguiendo el README y enseñar `/swagger`, `/health/live` y `/health/ready`.
3. Crear una transacción válida y consultar hasta ver `approved`.
4. Crear una transacción por encima de 2000 y mostrar `rejected` con la razón.
5. Crear una transacción que, junto con el acumulado previo de la misma cuenta UTC, supere 20000; explicar el supuesto documentado.
6. Mostrar los logs API/Worker y el pipeline CI; explicar que no se publica imagen ni se usa infraestructura facturable.

La demo local de Docker Compose se ejecutó y validó el 2026-10-06: aprobación, rechazo por valor alto y rechazo por acumulado diario. Para repetirla, sigue los comandos y la secuencia de `README.md`; no atribuyas al CI una prueba E2E con Kafka.

## Preguntas que conviene preparar

- ¿Por qué responder `201 pending` en vez de decidir en el POST? Para respetar el procesamiento asíncrono pedido y separar disponibilidad HTTP de la evaluación.
- ¿Qué ocurre si el worker publica y cae antes de confirmar el offset? Kafka reentrega; la API acepta el resultado terminal idéntico de forma idempotente.
- ¿Qué ocurre si la base falla al aplicar la respuesta? No se confirma el offset; el consumidor vuelve al registro y reintenta.
- ¿Cómo mejoraría garantías productivas? Outbox para persistir y publicar atómicamente, DLQ/reintentos limitados, métricas/trazas y estrategia de concurrencia.
- ¿Qué no se debe atribuir al reto? La agrupación por `sourceAccountId` y UTC son supuestos de implementación, no requisitos literales.
- ¿Qué infraestructura se creó? Docker local; no cloud, registro pago ni recursos con costo.

## English practice version

“I built this .NET 8 fraud-detection challenge as an API and a separate Kafka worker. The API creates each transaction as pending. The worker evaluates two deterministic rules and publishes a response event; the API consumes that event and persists the terminal status. The daily aggregate groups by source account and UTC day and includes previously rejected transactions, an explicit assumption because the prompt leaves that policy undefined. I ran the local Docker Compose demo and verified approval plus both rejection rules. Delivery is at least once; a production design would add an outbox, a dead-letter strategy, and automated broker-backed E2E coverage in CI.”

Practice follow-up: “I did not deploy to cloud or publish an image. The current demo uses free local tools, and the Docker daemon must be available to run the full stack.”

## Criterios de preparación

- [ ] Explicar el flujo y las decisiones sin leer el README.
- [ ] Ejecutar los tres casos funcionales en menos de cinco minutos.
- [ ] Señalar en el código dónde se evalúa, publica, consume y persiste.
- [ ] Distinguir funcionalidades validadas, limitaciones conocidas y trabajo futuro.
