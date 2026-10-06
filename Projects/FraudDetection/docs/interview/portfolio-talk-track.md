# Guion breve para entrevista

## Presentación de 90 segundos

“Construí Fraud Detection Challenge como un sistema .NET 8 con una API y un worker desacoplados por Kafka. La API crea la transacción en `pending`; el worker aplica dos reglas deterministas y publica su decisión; la API consume esa respuesta y persiste el estado terminal. La regla acumulada agrupa por cuenta de origen y día UTC, una interpretación que documenté porque el reto no fija agrupación ni zona horaria. SQL Server y Kafka KRaft corren en Docker Compose. El repositorio incluye pruebas, CI de build y construcción local de imágenes, trazabilidad del reto y decisiones de arquitectura. La entrega es at-least-once; tolero respuestas idénticas duplicadas y dejo explícito que una solución productiva necesitaría outbox y DLQ.”

## Demostración local

1. Mostrar el README y la matriz `CHALLENGE_TRACEABILITY.md`.
2. Levantar Compose siguiendo el README y enseñar `/swagger`, `/health/live` y `/health/ready`.
3. Crear una transacción válida y consultar hasta ver `approved`.
4. Crear una transacción por encima de 2000 y mostrar `rejected` con la razón.
5. Crear una transacción que, junto con el acumulado previo de la misma cuenta UTC, supere 20000; explicar el supuesto documentado.
6. Mostrar los logs API/Worker y el pipeline CI; explicar que no se publica imagen ni se usa infraestructura facturable.

La demo depende de Docker disponible y de ejecutar los comandos actuales del README; no afirmar que fue ejecutada si no se validó en el entorno.

## Preguntas que conviene preparar

- ¿Por qué responder `201 pending` en vez de decidir en el POST? Para respetar el procesamiento asíncrono pedido y separar disponibilidad HTTP de la evaluación.
- ¿Qué ocurre si el worker publica y cae antes de confirmar el offset? Kafka reentrega; la API acepta el resultado terminal idéntico de forma idempotente.
- ¿Qué ocurre si la base falla al aplicar la respuesta? No se confirma el offset; el consumidor vuelve al registro y reintenta.
- ¿Cómo mejoraría garantías productivas? Outbox para persistir y publicar atómicamente, DLQ/reintentos limitados, métricas/trazas y estrategia de concurrencia.
- ¿Qué no se debe atribuir al reto? La agrupación por `sourceAccountId` y UTC son supuestos de implementación, no requisitos literales.
- ¿Qué infraestructura se creó? Docker local; no cloud, registro pago ni recursos con costo.

## English practice version

“I built this .NET 8 fraud-detection challenge as an API and a separate Kafka worker. The API creates each transaction as pending. The worker evaluates two deterministic rules and publishes a response event; the API consumes that event and persists the terminal status. The daily aggregate uses the source account and UTC day as an explicit assumption because the prompt leaves both undefined. The whole stack is designed to run locally with Docker Compose. I tested the application and persistence paths, documented at-least-once delivery and duplicate handling, and kept the production gaps visible: an outbox, a dead-letter strategy, and end-to-end broker tests.”

Practice follow-up: “I did not deploy to cloud or publish an image. The current demo uses free local tools, and the Docker daemon must be available to run the full stack.”

## Criterios de preparación

- [ ] Explicar el flujo y las decisiones sin leer el README.
- [ ] Ejecutar los tres casos funcionales en menos de cinco minutos.
- [ ] Señalar en el código dónde se evalúa, publica, consume y persiste.
- [ ] Distinguir funcionalidades validadas, limitaciones conocidas y trabajo futuro.
