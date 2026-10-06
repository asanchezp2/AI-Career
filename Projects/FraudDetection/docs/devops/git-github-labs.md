# Git y GitHub: checkpoint de portafolio

Este laboratorio documenta trabajo demostrable sobre este repositorio. No reemplaza la experiencia con un repositorio público real ni autoriza a publicar cambios automáticamente.

## Flujo practicado

1. Crear una rama corta `feature/<tema>` desde `main` actualizado.
2. Hacer cambios acotados, revisar `git diff` y `git status`, y ejecutar las comprobaciones locales pertinentes.
3. Crear un commit con mensaje convencional y descriptivo (`feat:`, `fix:`, `docs:`, `ci:`).
4. Abrir un pull request que explique contexto, decisión, validación y limitaciones; pedir/revisar feedback antes de integrar.
5. Consultar el resultado de GitHub Actions y corregir el fallo en la rama, preservando la evidencia del error.
6. Tras merge autorizado, verificar el estado de `main` y eliminar solo la rama ya integrada.

## Evidencia para entrevista

- `git log --oneline --decorate -n 10` y `git status --short` para explicar historia y estado.
- Un PR con descripción, revisión y CI verde (cuando exista un cambio que el usuario decida publicar).
- [Workflow de GitHub Actions](../../../../.github/workflows/ci.yml) como ejemplo del pipeline que valida restore/build/test, Compose e imágenes locales.
- Explicar una decisión basada en requisitos, como separar la evaluación de la persistencia del resultado por Kafka.

## Ejercicio seguro

Practicar `rebase`, resolución de conflictos y revert en un repositorio descartable local. No practicar `reset --hard`, force-push ni eliminación de ramas sobre el repositorio del portafolio. Antes de integrar, inspeccionar el diff completo y confirmar que no hay secretos ni datos reales.

## Checkpoint

- [ ] Explicar branch, commit, PR, review, merge y revert con un ejemplo propio.
- [ ] Leer un fallo de Actions y localizar el paso, log y cambio que lo introdujo.
- [ ] Mantener rama limpia y describir cómo volver a un estado conocido sin reescribir historia compartida.
- [ ] Presentar este repositorio sin afirmar despliegues o servicios que no se hayan ejecutado.
