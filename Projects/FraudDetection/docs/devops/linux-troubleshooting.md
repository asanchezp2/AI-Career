# Linux troubleshooting: escenarios de práctica

Los casos se ejecutan en una VM Linux local o contenedor de práctica. No requieren cloud ni recursos de pago. Primero se recoge evidencia, luego se forma una hipótesis y se cambia una cosa por vez.

| Síntoma | Evidencia/lectura inicial | Hipótesis a comprobar | Recuperación segura |
|---|---|---|---|
| API no responde | `docker compose ps`, `docker compose logs --tail=100 api`, `curl -i http://localhost:8080/health/live` | proceso detenido, puerto no publicado, healthcheck o configuración | corregir causa, reconstruir/reiniciar solo el servicio afectado y volver a probar liveness/readiness |
| API viva pero no lista | `/health/ready`, logs de `api`, `docker compose ps` | SQL Server o Kafka no disponible, configuración de conexión errónea | inspeccionar servicio dependiente y DNS/config; recuperar la dependencia y comprobar readiness |
| Kafka parece sano pero mensajes no fluyen | logs `worker` y `api`, nombres de tópicos y grupos en config | tópico/grupo incorrecto, evento inválido, offset detenido o consumer desconectado | corregir config, reiniciar consumidor si corresponde, verificar un mensaje de prueba y estado final |
| Puerto en uso | `ss -ltnp` (permisos según entorno), `docker compose ps` | otro proceso o contenedor ocupa el puerto | identificar propietario y elegir un puerto libre; no matar procesos desconocidos |
| Fallo de DNS entre servicios | `getent hosts kafka sqlserver`, `docker network inspect <red>` | servicio conectado a otra red o nombre distinto | corregir nombre/red en Compose y repetir resolución |
| Disco/volumen lleno | `df -h`, `df -i`, `du -sh <ruta>` | logs, capas o volumen consumen espacio | localizar consumo; rotar o limpiar solo artefactos identificados, preservando volúmenes de datos |
| Servicio systemd no inicia | `systemctl status <servicio>`, `journalctl -u <servicio> -n 100 --no-pager` | unidad/configuración inválida o dependencia fallida | validar el archivo de unidad, corregir, `systemctl daemon-reload` y reiniciar servicio autorizado |

## Formato para comunicar un incidente

1. Impacto y hora de inicio.
2. Evidencia observada (comandos y fragmentos de log redactados).
3. Hipótesis ordenadas y cómo se validaron.
4. Acción reversible aplicada y resultado.
5. Prevención/alerta o documentación que evitaría recurrencia.

Redactar tokens, contraseñas, direcciones de clientes y datos financieros antes de compartir logs. Una herramienta de IA, si se usa en la fase avanzada, puede resumir evidencia anonimizada y sugerir hipótesis; no ejecuta comandos ni cambios.

## Checkpoint

- [ ] Resolver al menos los escenarios de disponibilidad, DNS/puerto y disco en entorno local.
- [ ] Documentar una línea temporal con evidencia, causa y verificación de recuperación.
- [ ] Distinguir liveness de readiness y explicar por qué una dependencia caída no debe reiniciar en bucle a una API viva.
