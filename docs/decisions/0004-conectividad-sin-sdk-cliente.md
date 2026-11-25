# 0004 · Verificar la conectividad del backend sin SDK en el cliente

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La base del proyecto necesita confirmar que el backend local (Nakama) está disponible. Esto
podía comprobarse desde el cliente Unity con el SDK de Nakama o desde el equipo anfitrión. La
constitución prohíbe añadir paquetes sin una necesidad documentada, y la conexión real del
cliente llegará con la primera funcionalidad de producto (p. ej. inicio de sesión).

## Opciones consideradas

- **Solo desde el anfitrión**: un script consulta la comprobación de salud de Nakama. No añade
  dependencias al cliente ni configuración de conexión.
- **SDK de Nakama en Unity con una prueba de conexión mínima**: valida el camino real, pero
  obliga a fijar ya la versión del SDK y a configurar endpoints y claves en el cliente, y
  amplía el alcance.

## Decisión

La conectividad se verifica **solo desde el anfitrión** con `tools/check-backend.sh`. El cliente
no incluye el SDK de Nakama ni configuración de conexión en esta fase.

## Consecuencias

- El SDK se añade, con versión fijada y su propio ADR, en la primera funcionalidad que lo
  necesite.
- Hasta entonces no se valida la conexión cliente↔servidor de extremo a extremo.
- La clave de servidor solo existe en el `.env` local; no hay claves en el cliente.
