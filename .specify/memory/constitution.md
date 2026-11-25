# Ecosystem Constitution

Juego móvil multiplataforma (Android e iOS) de estrategia con mundo y partidas en directo,
unidades y edificios animados, progresión persistente y posibles modos PvP.

Convención: **MUST** = obligatorio y verificable en revisión; **SHOULD** = recomendación que
puede omitirse con justificación escrita en la spec, el plan o el PR.

## Core Principles

### I. Servidor autoritativo (NON-NEGOTIABLE)
- El servidor Nakama MUST ser la autoridad sobre identidad, progreso, economía, recompensas,
  resultados y estado de partidas competitivas.
- El cliente MAY gestionar controles, presentación, animación y predicción visual, pero MUST
  reconciliarse con el estado confirmado por el servidor.
- El servidor MUST validar formato, tamaño, límites, permisos y estado actual de cada operación
  y MUST NOT confiar en valores críticos calculados por el cliente.
- Las operaciones que conceden recursos, progreso o recompensas MUST ser idempotentes ante
  reintentos y seguras ante concurrencia (sin duplicados).
- Los valores de balance usados por el cliente solo para presentación MUST validarse igualmente
  en servidor.

*Rationale*: la integridad de la economía y del PvP no puede depender de un cliente manipulable.

### II. Contratos cliente-servidor versionados
- Todo mensaje, RPC y evento MUST tener una definición compartida en el proyecto de contratos,
  con versión, campos obligatorios, límites, errores, estados y reglas de compatibilidad.
- Un cambio de contrato MUST actualizar en el mismo cambio: definición, cliente, servidor,
  pruebas y documentación.
- Receptores MUST tolerar campos desconocidos, rechazar mensajes inválidos y responder de forma
  definida a clientes con versión antigua o incompatible.
- Los mensajes MUST incluir solo los datos que el receptor necesita; no se envían objetos
  internos completos.
- No se envían actualizaciones de red por fotograma; mensajes y frecuencias se definen según
  el diseño y necesidades reales.

### III. Resiliencia en tiempo real
Todo diseño de partida o sesión MUST tratar explícitamente: desconexión, reintento, mensajes
duplicados o retrasados, reingreso a partidas, concurrencia y versiones anteriores del
protocolo. La spec o plan de cada funcionalidad en directo MUST indicar el comportamiento
esperado en cada caso y tener pruebas para los casos límite aplicables.

### IV. Arquitectura desacoplada y simple
- Cliente Unity y backend MUST comunicarse solo mediante contratos; el servidor MUST NOT
  depender de clases internas de Unity.
- Se SHOULD separar reglas de juego, casos de uso, infraestructura y presentación, con
  dependencias en una única dirección y sin ciclos.
- MUST evitarse: módulos monolíticos, estado global innecesario, lógica crítica duplicada y
  abstracciones sin valor demostrable.
- En Unity, escenas, prefabs y singletons MUST NOT actuar como dependencias globales implícitas.
- No se añaden paquetes, servicios, frameworks ni infraestructura sin documentar la necesidad
  que cubren y su coste de mantenimiento.
- No se crean APIs públicas sin consumidores claros.

### V. Calidad de código
- Se aplican responsabilidad única, alta cohesión, bajo acoplamiento y composición antes que
  herencia profunda; funciones acotadas, nombres descriptivos y tipos adecuados.
- Manejo de errores consistente: MUST NOT ocultarse errores ni usarse excepciones como flujo
  normal.
- Cancelación, concurrencia y ciclo de vida de operaciones asíncronas, objetos Unity y partidas
  MUST gestionarse explícitamente.
- Se siguen las convenciones oficiales de cada lenguaje/herramienta y las ya establecidas en el
  repositorio.
- Antes de usar una API o paquete, MUST confirmarse que existe y es compatible con las versiones
  fijadas; no se inventan métodos ni se suponen comportamientos.
- Los cambios MUST ser acotados y revisables; no se mezclan refactorizaciones amplias con
  cambios funcionales no relacionados.

### VI. Desarrollo guiado por pruebas (TDD)
- Para lógica nueva comprobable, MUST seguirse Red–Green–Refactor: prueba fallida primero,
  cambio mínimo para pasarla, refactor con pruebas verdes.
- Prioridad de pruebas: reglas de dominio, validaciones, transiciones de estado, economía,
  recompensas, protocolo y casos límite.
- TDD NO se fuerza en arte, configuración visual, escenas o trabajo exploratorio; en esos casos
  MUST documentarse cómo se valida manualmente.
- Las pruebas MUST verificar comportamiento observable, ser deterministas y aisladas, y las
  unitarias MUST NOT depender de servicios externos. Las de integración usan dependencias
  locales controladas o dobles y documentan sus requisitos.
- MUST NOT reducirse cobertura ni eliminarse pruebas para hacer pasar una implementación sin
  justificación documentada.
- MUST NOT afirmarse que una tarea está verificada si no se ejecutaron las pruebas
  correspondientes; si no pudieron ejecutarse, se indica qué falta y por qué.

Equivalentes por tecnología:
- **C#/Unity**: NUnit vía Unity Test Framework; EditMode para lógica independiente, PlayMode
  para ciclo de vida de Unity, escenas, prefabs o interacción entre componentes.
- **Nakama en Go**: paquete `testing` estándar, más pruebas de integración de módulos/APIs
  cuando sean necesarias.
- **Nakama en TypeScript/JavaScript**: framework de pruebas fijado y compatible con el runtime;
  un módulo probado en Node no se asume equivalente en el runtime de Nakama.
- **Nakama en Lua**: framework compatible fijado o pruebas ejecutables documentadas; reglas
  separadas del código dependiente del host.
- **PostgreSQL**: restricciones, consultas y migraciones probadas en una instancia desechable
  o aislada; nunca con datos de producción.
- **Contratos compartidos**: pruebas de serialización, validación y compatibilidad que
  demuestren que cliente y servidor interpretan igual los mensajes.

### VII. Seguridad y privacidad
- MUST NOT guardarse secretos en Git, cliente, builds, logs, documentación ni ejemplos; se usan
  variables de entorno o un gestor de secretos.
- El repositorio MUST incluir `.env.example` con nombres y valores ficticios.
- MUST NOT registrarse contraseñas, tokens, datos personales innecesarios ni cargas completas
  potencialmente sensibles.
- Configuración de desarrollo, pruebas y producción MUST estar separada.
- No se accede directamente a tablas internas de Nakama; se usan interfaces soportadas y las
  migraciones propias de la aplicación se documentan.
- Los logs y métricas SHOULD ser útiles, con niveles apropiados y sin ruido. No se añaden
  sistemas de monitorización ni servicios externos hasta que una necesidad concreta lo
  justifique.

### VIII. Reproducibilidad y versiones fijadas
- Versiones de Unity, paquetes, Nakama, PostgreSQL, runtime, herramientas, dependencias y
  acciones de CI MUST fijarse y documentarse. No se actualizan de forma incidental; una
  actualización es un cambio propio y explícito.
- Arranque local, dependencias, pruebas, configuración y despliegue MUST estar documentados de
  forma reproducible, y README/instrucciones MUST actualizarse cuando cambien los pasos.
- Configuración de balance y contenido SHOULD separarse del código cuando aporte valor.

## Pila Tecnológica y Repositorio

- **Cliente**: Unity y C# para Android e iOS. No se asumen plataformas adicionales.
- **Backend**: Nakama con lógica autoritativa; persistencia en PostgreSQL según prácticas
  compatibles con la versión de Nakama elegida.
- **Runtime de Nakama** (Go, TypeScript/JavaScript o Lua): MUST elegirse y documentarse en un
  registro de decisión antes de implementar lógica sustancial. No se mezclan runtimes sin una
  decisión de arquitectura que lo justifique.
- **Monorepo Git** con carpetas diferenciadas para cliente Unity, servidor Nakama, contratos
  compartidos, despliegue y documentación.
- MUST versionarse los `.meta` de Unity y los archivos de bloqueo de dependencias (incluido
  `go.sum` si se elige Go).
- `.gitignore` MUST excluir cachés, compilaciones, datos locales, preferencias personales y
  secretos, sin ignorar archivos necesarios para reconstruir el proyecto. MUST NOT incluirse
  datos internos de PostgreSQL.
- Las decisiones técnicas importantes MUST registrarse con contexto, decisión y consecuencias.

## Flujo de Desarrollo y Calidad

**Comprobaciones por cambio** (reproducibles): compilación, formato, análisis estático y
pruebas aplicables.
- Unity: documentar ejecución de compilación y pruebas en modo batch cuando el entorno de CI lo
  permita, sin depender de licencia o configuración local no documentada.
- Go: `gofmt`, `go test ./...` y análisis estático acordado.
- TypeScript/JavaScript: formato, lint, comprobación de tipos y pruebas mediante scripts
  reproducibles.
- Lua: comprobaciones equivalentes compatibles con el runtime elegido.
- Migraciones PostgreSQL: validadas en entorno aislado antes de aceptarse.

**CI**: MUST ejecutar las comprobaciones pertinentes a los archivos modificados y bloquear la
integración si fallan; configuración mínima, documentada, sin secretos y con versiones de
acciones/herramientas fijadas de forma revisable. MUST NOT declararse la CI configurada hasta
que exista un flujo funcional en el repositorio.

**Git**:
- Ramas breves y descriptivas: `feat/…`, `fix/…`, `docs/…`, `refactor/…`.
- **Una rama por spec**: cada nueva spec MUST desarrollarse en una rama propia, creada desde
  `main` actualizada antes de escribir la spec, y MUST NOT reutilizarse para otra spec.
  - Nombre normalizado: `<tipo>/<NNN>-<nombre-corto>`, donde `<tipo>` sigue la convención
    anterior (normalmente `feat`), y `<NNN>-<nombre-corto>` coincide exactamente con el
    directorio de la spec en `specs/` (p. ej. `feat/001-login-invitado` ↔
    `specs/001-login-invitado/`).
  - `<nombre-corto>`: 2–5 palabras en minúsculas, ASCII sin tildes ni `ñ`, separadas por
    guiones, sin espacios, guiones bajos, mayúsculas ni caracteres especiales; descriptivo y
    sin incluir nombres de personas o datos sensibles.
  - Spec, plan, tareas, implementación y pruebas de esa spec MUST vivir en su rama; la
    integración en `main` se hace mediante PR revisado y la rama se elimina tras el merge.
- Commits atómicos (coherentes, revisables y, cuando sea posible, compilables con pruebas
  pasando) con Conventional Commits y ámbito cuando ayude (`feat(server): …`, `fix(client): …`).
- MUST NOT mezclarse cambios sin relación ni incluirse archivos generados, secretos, cachés o
  configuraciones personales.
- MUST NOT reescribirse historia compartida ni hacerse push, merge, release o publicación
  salvo petición explícita.
- Contratos, documentación y pruebas se actualizan en el mismo cambio que los afecta.

## Decisiones Abiertas

No se inventan requisitos de producto, mecánicas, objetivos de rendimiento, monetización,
plataformas adicionales ni políticas de retención no decididos. Decisiones técnicas pendientes:

| Decisión | Debe resolverse antes de |
|----------|--------------------------|
| Runtime de Nakama (Go / TS-JS / Lua) | Implementar lógica de servidor sustancial |
| Enfoque exacto de sincronización | Implementar la lógica de partida correspondiente |
| Versiones fijas de Unity, Nakama, PostgreSQL y herramientas | Primera compilación/CI |

Una decisión abierta MUST presentarse con opciones, ventajas, costes y dependencias, y
registrarse al resolverse. Si una especificación contradice esta constitución, el conflicto MUST
identificarse y resolverse antes de planificar la implementación.

## Governance

- Esta constitución prevalece sobre otras prácticas del proyecto. Toda spec, plan y PR MUST
  verificar su cumplimiento; cualquier excepción a un MUST se justifica por escrito en el
  artefacto afectado.
- **Enmiendas**: se proponen por PR que modifique este archivo, con motivo y análisis de impacto
  en plantillas, specs y planes existentes; requieren aprobación del responsable del proyecto.
- **Versionado** (semver): MAJOR = eliminación o redefinición incompatible de principios;
  MINOR = nuevo principio/sección o ampliación material; PATCH = aclaraciones sin cambio
  semántico.
- **Revisión de cumplimiento**: en cada revisión de spec, plan y código se comprueban los
  principios I–VIII; la constitución se revisa al resolverse una decisión abierta que la afecte.

**Version**: 1.1.0 | **Ratified**: 2026-11-24 | **Last Amended**: 2026-11-25
