# Deploy lab roadmap

La demostración soportada hoy es local con Docker Compose (SQL Server + Kafka KRaft + API + Worker). Este directorio reserva el espacio para las fases DevOps posteriores al checkpoint de candidatura; no implica que esas fases estén implementadas.

## Orden posterior al checkpoint

1. Docker: optimizar/revisar imágenes, redes, volúmenes, healthchecks y logs; reproducir localmente.
2. Kubernetes local: manifiestos mínimos y probes en kind/minikube; eliminar el entorno al terminar.
3. Observabilidad: métricas, logs y trazas en stack local.
4. Asistencia de IA para incidentes: solo evidencia anonimizada, resumen e hipótesis; nunca ejecución de operaciones.
5. Cloud y Terraform: primero conceptos y demos gratuitas; no crear recursos facturables.
6. Scripting: automatizar comprobaciones repetibles con límites y mensajes claros.

No hay despliegue remoto ni publicación automática de contenedores. CI construye las imágenes como validación, sin login ni push a un registro. La política del proyecto es gasto cero.
