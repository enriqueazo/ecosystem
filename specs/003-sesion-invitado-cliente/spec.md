# Feature Specification: Sesión de invitado y conexión del cliente

**Feature Branch**: `feat/003-sesion-invitado-cliente`

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description: "Conectar por primera vez el cliente Unity con el servidor Nakama mediante una sesión de jugador invitado (autenticación por dispositivo), persistente y renovable, con detección de versiones de protocolo incompatibles, validación de respuestas con los contratos compartidos, resiliencia ante fallos de red y una escena mínima de conexión. Solo invitado, pero sin impedir un futuro sistema de cuentas. Pruebas de Unity en local, preparadas para la CI."

> **Nivel de detalle técnico**: igual que en las specs 001 y 002, la pila (Unity, Nakama, contratos
> JSON) está fijada por decisiones registradas (ADR-0005, 0006 y 0008). La spec habla de *qué*
> debe ocurrir. Las versiones, bibliotecas, estructura de código y valores exactos se deciden en
> `/speckit-plan`.

## Clarifications

### Session 2026-11-25

- Q: ¿Debe esta spec permitir probar en un emulador o dispositivo físico contra el backend local, lo que exigiría exponer Nakama fuera de `127.0.0.1`? → A: No. Solo en el editor de Unity contra el Nakama local en `127.0.0.1`; la regla de seguridad de la spec 001 (FR-016) no cambia. Las pruebas en dispositivos reales llegarán con un entorno de pruebas remoto o con su propia spec.

## Punto de partida

Estado de `main` en `4f338cf`, con las specs 001 y 002 fusionadas:

- **Cliente Unity** (6000.3.29f1, URP): solo una escena `Bootstrap` con una cámara y pruebas de
  configuración. No tiene SDK de Nakama ni configuración de conexión (ADR-0004, que esta spec
  sustituye).
- **Servidor**:
  - el RPC `protocol_version` (contrato v1) exige sesión y devuelve
    `{protocolVersion, moduleVersion}`;
  - la versión de protocolo vigente es `1`, en `shared/contracts/protocol.json`;
  - los errores llegan como `{code, message}` y el cliente debe ignorar el campo `error` de
    Nakama (ADR-0006).
- **Entorno local**: Nakama solo escucha en `127.0.0.1`.
  - La clave de servidor de cada persona está en su `deploy/local/.env`, que no se versiona.
  - `compose.yaml` fija la caducidad de la sesión (`--session.token_expiry_sec 7200`) pero no la
    del token de refresco, que queda en el valor por defecto de Nakama. Esta spec lo revisa
    (FR-009).
- **CI**: GitHub Actions sin Unity (ADR-0007). La protección de `main` exige `repo`, `server` y
  `backend`.

## User Scenarios & Testing *(mandatory)*

Actores:
- **jugador** (invitado, sin cuenta registrada);
- **desarrollador/a** (ejecuta y prueba el cliente contra el entorno local);
- **revisor/a**.

### User Story 1 - Entrar como invitado sin registrarse (Priority: P1)

Un jugador abre el juego por primera vez y queda conectado al servidor como invitado, sin rellenar
nada. El servidor le asigna una identidad propia y estable (su cuenta de jugador).

**Why this priority**: sin identidad en el servidor no puede existir nada autoritativo (principio
I): ni progreso, ni partidas, ni economía. Es la base de todas las funcionalidades siguientes.

**Independent Test**: con el entorno local arrancado, abrir la escena de conexión en el editor desde
un estado limpio. El jugador queda conectado y el servidor muestra una cuenta nueva.

**Acceptance Scenarios**:

1. **Given** una instalación sin datos previos y el servidor disponible, **When** el jugador abre
   el juego, **Then** en ≤ 5 s la escena muestra el estado «conectado» y existe una cuenta nueva
   en el servidor.
2. **Given** dos instalaciones distintas, **When** cada una se conecta por primera vez, **Then**
   el servidor les asigna cuentas distintas.
3. **Given** un jugador conectado, **When** se consulta su identidad en el cliente, **Then** es el
   identificador de cuenta que asigna el servidor, no un identificador del dispositivo.

---

### User Story 2 - Volver a entrar con la misma cuenta (Priority: P1)

El jugador cierra el juego y lo vuelve a abrir, al rato o días después, y sigue siendo el mismo
jugador sin hacer nada.

**Why this priority**: una cuenta de invitado que cambia en cada apertura perdería todo lo que el
jugador consiga en el futuro.

**Independent Test**: conectar, cerrar y volver a abrir; después, simular la caducidad de la
sesión y volver a abrir. En todos los casos la identidad es la misma.

**Acceptance Scenarios**:

1. **Given** un jugador que ya se conectó, **When** vuelve a abrir el juego con la sesión vigente,
   **Then** queda conectado con la misma cuenta, sin crear otra.
2. **Given** una sesión caducada pero renovable, **When** el jugador abre el juego, **Then** la
   sesión se renueva sin intervención y la cuenta es la misma.
3. **Given** una sesión que ya no se puede renovar (caducada del todo o revocada), **When** el
   jugador abre el juego, **Then** el cliente vuelve a autenticarse como invitado con la misma
   instalación y recupera **la misma cuenta**.
4. **Given** una cuenta deshabilitada en el servidor, **When** el jugador abre el juego, **Then**
   ve un estado de error definido y no se crea otra cuenta en silencio.

---

### User Story 3 - Saber que hay que actualizar el juego (Priority: P1)

Al conectar, el cliente comprueba que habla la misma versión de protocolo que el servidor. Si no es
así, el jugador ve que debe actualizar el juego y no puede seguir con una versión incompatible.

**Why this priority**: es la primera aplicación real del principio II (compatibilidad de
contratos). Sin ella, un cliente antiguo podría enviar mensajes que el servidor rechazaría de
formas confusas.

**Independent Test**: conectar con un cliente cuya versión de protocolo difiere de la del
servidor, en los dos sentidos, y comprobar el bloqueo.

**Acceptance Scenarios**:

1. **Given** cliente y servidor con la misma versión de protocolo, **When** el jugador se conecta,
   **Then** queda conectado.
2. **Given** un servidor con una versión de protocolo **mayor** que la del cliente, **When** el
   jugador se conecta, **Then** la escena muestra «versión incompatible: actualiza el juego» y no
   permite continuar.
3. **Given** un servidor con una versión de protocolo **menor** que la del cliente, **When** el
   jugador se conecta, **Then** la escena muestra un estado de incompatibilidad distinto («el
   servidor no admite esta versión, inténtalo más tarde») y no permite continuar.
4. **Given** una respuesta del servidor que no cumple el contrato (campos que faltan, tipos
   incorrectos o campos extra no previstos), **When** el cliente la recibe, **Then** la trata como
   error de protocolo definido y no la usa.

---

### User Story 4 - Comportamiento predecible sin conexión o con el servidor caído (Priority: P2)

Si no hay red, el servidor no responde o tarda demasiado, el jugador ve el estado «sin conexión»,
el cliente reintenta con límites y el jugador puede reintentar a mano. Nunca queda bloqueado en
«conectando» indefinidamente.

**Why this priority**: es la experiencia habitual en móvil, y el principio III exige definir el
comportamiento ante desconexiones y reintentos.

**Independent Test**: con el entorno local detenido, abrir la escena y observar los reintentos y
el estado final. Arrancar después el entorno y reintentar a mano.

**Acceptance Scenarios**:

1. **Given** el servidor no disponible, **When** el jugador abre el juego, **Then** el cliente hace
   un número limitado de reintentos automáticos con espera creciente y termina en «sin conexión»
   en ≤ 60 s desde la apertura.
2. **Given** el estado «sin conexión», **When** el servidor vuelve a estar disponible y el jugador
   pulsa «Reintentar», **Then** queda conectado con su cuenta de siempre.
3. **Given** una petición que no responde, **When** pasa el tiempo límite de la petición, **Then**
   se cancela y cuenta como intento fallido; no se queda esperando.
4. **Given** una conexión en curso, **When** el jugador cierra el juego o sale de la escena,
   **Then** las operaciones pendientes se cancelan sin errores ni avisos en el registro.
5. **Given** un reintento que coincide con una respuesta tardía del intento anterior, **When**
   llegan ambas, **Then** el resultado es una sola sesión y una sola cuenta.

---

### User Story 5 - Probar el cliente en local y estar listos para la CI (Priority: P2)

Una persona desarrolladora configura el cliente para su entorno local sin tocar archivos
versionados y ejecuta dos grupos de pruebas, en el editor o en modo batch: las que no necesitan
servidor y las que necesitan el Nakama local.

**Why this priority**: es lo que hace reproducible el trabajo del cliente. Además, el responsable
quiere poder llevar estas pruebas a la CI más adelante sin rehacerlas.

**Independent Test**: desde un clon limpio, configurar el cliente siguiendo solo
`client-unity/README.md` y ejecutar los dos grupos de pruebas en modo batch.

**Acceptance Scenarios**:

1. **Given** un clon limpio, **When** la persona sigue el README del cliente, **Then** configura la
   conexión local (host, puerto y clave de su `.env`) en un archivo no versionado, a partir de un
   ejemplo versionado.
2. **Given** el cliente sin configuración local, **When** se ejecuta la escena o las pruebas que
   necesitan servidor, **Then** fallan con un mensaje que indica qué archivo crear. Las pruebas
   que no necesitan servidor pasan igualmente.
3. **Given** el entorno local apagado, **When** se ejecutan en modo batch las pruebas que no
   necesitan servidor, **Then** pasan todas.
4. **Given** el entorno local arrancado con el módulo cargado, **When** se ejecutan en modo batch
   las pruebas que necesitan servidor, **Then** pasan todas.

---

### Edge Cases

- **Reinstalación o borrado de datos de la app**: se pierde el identificador de instalación y el
  jugador entra con una **cuenta nueva**. Es la limitación conocida de las cuentas de invitado
  hasta que exista la vinculación de cuentas (fuera de alcance). Se documenta y no se intenta
  evitar con identificadores de hardware (privacidad y políticas de las tiendas).
- **Varios dispositivos**: cada instalación es una cuenta de invitado distinta. Unificarlas
  requiere el futuro sistema de cuentas.
- **Datos locales corruptos o incompletos** (sesión ilegible o identificador de instalación
  inválido): se descartan los datos de sesión y se vuelve a autenticar. El identificador de
  instalación solo se regenera si es inválido, nunca por un fallo de red.
- **Reloj del dispositivo desajustado**: la caducidad de la sesión no depende solo del reloj
  local; si el servidor rechaza la sesión, se sigue el flujo de renovación o reautenticación.
- **Respuestas lentas o tardías**: un intento superado por otro no debe aplicar su resultado
  (sin carreras entre reintentos).
- **Clave de servidor incorrecta en la configuración local**: estado de error de configuración
  diferenciado de «sin conexión», con un mensaje para el desarrollador; sin reintentos infinitos.
- **El servidor responde, pero el módulo no está cargado** (el RPC no existe): se trata como
  incompatibilidad o error de servidor definido, no como «conectado».
- **Errores con `error.stackTrace` de Nakama**: el cliente ignora ese campo y nunca lo muestra al
  jugador ni lo registra completo.
- **Registros del cliente**: nunca contienen tokens de sesión, el token de refresco, la clave de
  servidor ni el identificador de instalación completo.

## Requirements *(mandatory)*

Convención (constitución): **MUST** es obligatorio; **SHOULD** es una recomendación que se puede
omitir con justificación escrita.

### Functional Requirements

#### Identidad y preparación para cuentas

- **FR-001**: El cliente MUST autenticar al jugador como invitado con la autenticación por
  dispositivo estándar de Nakama, sin RPC propio de autenticación. La cuenta se crea en la primera
  conexión.
- **FR-002**: El identificador que se envía como «dispositivo» MUST ser un **identificador de
  instalación** aleatorio generado por el cliente la primera vez y guardado localmente. MUST NOT
  derivarse de identificadores de hardware ni de publicidad.
- **FR-003**: La identidad del jugador en todo el cliente MUST ser el identificador de cuenta que
  asigna el servidor (userId). Ningún dato del jugador se asocia en el cliente ni en el servidor al
  identificador de instalación.
- **FR-004**: El diseño MUST permitir añadir más adelante otros métodos de autenticación y la
  vinculación de credenciales (correo, Google, Apple, Game Center) a la **misma** cuenta, sin migrar
  datos.
  - Verificable en revisión: la autenticación está detrás de un punto de extensión con un único
    método implementado (invitado), y nada asume que una cuenta equivale a una instalación.
  - Lo documenta el ADR del SDK (FR-021).

#### Sesión

- **FR-005**: El cliente MUST guardar localmente la sesión (token de sesión y token de refresco)
  y el identificador de instalación, protegidos por el almacenamiento de la propia app y sin
  exponerlos en registros (principio VII).
- **FR-006**: Al abrir el juego, el cliente MUST:
  1. usar la sesión guardada si sigue vigente;
  2. si ha caducado, renovarla con el token de refresco;
  3. si no se puede renovar, volver a autenticarse como invitado con el mismo identificador de
     instalación, recuperando la misma cuenta.
- **FR-007**: Si el servidor rechaza la sesión durante el uso (caducada o revocada), el cliente
  MUST aplicar el mismo orden que FR-006 una sola vez antes de pasar a error. Nunca crea una cuenta
  nueva salvo que el identificador de instalación sea nuevo.
- **FR-008**: Si la cuenta está deshabilitada o la autenticación se rechaza por un motivo
  definitivo, el cliente MUST mostrar un estado de error definido y no reintentar en bucle.
- **FR-009**: Las caducidades de la sesión y del token de refresco del entorno local MUST quedar
  fijadas de forma explícita y coherente (refresco más largo que la sesión) en
  `deploy/local/compose.yaml`, documentadas, y elegidas para que un jugador que vuelve en los
  siguientes días no tenga que reautenticarse. Los valores se deciden en el plan.

#### Compatibilidad de protocolo y contratos

- **FR-010**: Tras autenticarse, el cliente MUST llamar al RPC `protocol_version` y comparar el
  `protocolVersion` recibido con el suyo, que sale de `shared/contracts/protocol.json` en la
  construcción, sin copia manual.
  - Igual: compatible.
  - Servidor mayor: «actualiza el juego».
  - Servidor menor: «el servidor no admite esta versión».

  Los dos estados de incompatibilidad bloquean el avance.
- **FR-011**: El cliente MUST validar la respuesta de `protocol_version` contra el esquema
  `response` del contrato de `shared/contracts/`, usando la definición compartida y no una copia
  escrita a mano. Una respuesta que no valida es un error de protocolo (US3, escenario 4).
- **FR-012**: El cliente MUST interpretar los errores del RPC según `shared/README.md` → *Errores*:
  lee `code` y el error estándar de `message`, e ignora el campo `error`.
- **FR-013**: Añadir o cambiar un contrato MUST seguir generando automáticamente lo que el cliente
  necesita para validarlo, en el mismo flujo que el servidor (`shared/README.md` → *g*), o fallar
  de forma explícita.

#### Resiliencia

- **FR-014**: Cada petición al servidor MUST tener un tiempo límite y ser cancelable.
- **FR-015**: Los fallos de red o de disponibilidad MUST reintentarse automáticamente un número
  limitado de veces con espera creciente, y terminar en «sin conexión» en ≤ 60 s desde el inicio.
  Los rechazos definitivos (autenticación denegada, incompatibilidad o error de configuración)
  MUST NOT reintentarse.
- **FR-016**: Desde «sin conexión», el jugador MUST poder reintentar a mano, y el reintento sigue
  el mismo flujo completo.
- **FR-017**: Al cerrar el juego o salir de la escena, todas las operaciones pendientes MUST
  cancelarse sin dejar errores ni avisos en el registro. Un resultado tardío de un intento ya
  superado MUST NOT aplicarse.

#### Escena de conexión

- **FR-018**: MUST existir una escena mínima de conexión, sin arte final, que muestre los estados:
  - conectando;
  - conectado (con un identificador corto de la cuenta, para depurar);
  - sin conexión (con «Reintentar»);
  - actualiza el juego;
  - servidor incompatible;
  - error de cuenta;
  - error de configuración.

  Los textos son provisionales y no muestran detalles técnicos al jugador.
- **FR-019**: La lógica de conexión y sesión MUST estar separada de la escena y de los componentes
  de Unity, para poder probarla sin ella (principios IV y VI). La escena solo refleja estados y
  envía la acción «Reintentar».

#### Configuración

- **FR-020**: La configuración de conexión del cliente (host, puerto, clave de servidor y si usa
  conexión segura) MUST separarse por entorno.
  - Para desarrollo local: un archivo de ejemplo versionado y un archivo real no versionado
    (cubierto por `.gitignore` y comprobado por `tools/check-repo.sh`).
  - MUST NOT versionarse claves de servidor reales ni configuración de producción. La forma de
    inyectarla en builds de publicación queda fuera de alcance.
  - Si falta la configuración local, el error dice qué archivo crear y a partir de cuál.

#### Dependencias y decisiones

- **FR-021**: El SDK de Nakama para Unity MUST añadirse con versión exacta, compatible con Unity
  6000.3.29f1 y con IL2CPP en Android e iOS, con un ADR que **sustituye** a ADR-0004 (estado de
  ADR-0004 → «sustituida por NNNN»). El ADR incluye el punto de extensión de FR-004.
- **FR-022**: La validación de contratos en C# MUST basarse en la definición compartida, ser
  compatible con Unity e IL2CPP (sin generar código en ejecución), fijar la versión de toda
  dependencia y justificarse en un ADR. **Decisión confirmada (2026-11-25)**: validadores C#
  generados desde `shared/contracts/` con el subconjunto declarado de JSON Schema 2020-12, usando
  el Newtonsoft oficial de Unity para leer el JSON (ADR-0010, research.md R-2).
- **FR-023**: Cada paquete nuevo MUST documentarse en la tabla de paquetes de
  `client-unity/README.md` con su necesidad (principio IV).

#### Pruebas

- **FR-024**: Las pruebas MUST separarse en dos grupos ejecutables por separado en el editor y en
  modo batch:
  - **sin servidor** (EditMode y PlayMode con un doble del servidor): sesión, renovación,
    reautenticación, reintentos, tiempos límite, cancelación, compatibilidad de versiones,
    validación de contratos y errores;
  - **con servidor** (contra el Nakama local): flujo completo de US1–US3 y renovación real.
- **FR-025**: Las pruebas con servidor MUST tomar la configuración del mismo archivo local de
  FR-020 o de variables de entorno, y fallar de inmediato con un mensaje claro si el entorno no está
  disponible, sin colgarse.
- **FR-026**: `client-unity/README.md` MUST documentar los comandos en modo batch de cada grupo,
  sus requisitos y cómo se incorporarían a la CI (sin configurarla), para que añadirlos a
  GitHub Actions no obligue a rehacer las pruebas.
- **FR-027**: Las pruebas de configuración existentes (spec 001) MUST seguir pasando. Las nuevas
  escenas se añaden a la build sin romper sus invariantes, o las pruebas se actualizan de forma
  justificada.
- **FR-028**: Todo cambio MUST entregarse en commits atómicos con Conventional Commits en la rama
  `feat/003-sesion-invitado-cliente`. Ninguna verificación se declara superada sin haberse
  ejecutado.

### Recomendaciones (SHOULD)

- **R-01**: Mostrar en la escena la versión del cliente y del protocolo, para depurar.
- **R-02**: Registrar en el cliente las transiciones de estado con un nivel adecuado, sin datos
  sensibles (útil para depurar en dispositivo).
- **R-03**: Aislar el SDK detrás de una interfaz propia, para que los dobles de prueba no dependan
  de sus clases concretas.

### Decisiones pendientes (se resuelven en `/speckit-plan`)

| ID | Decisión | Qué bloquea | Cómo se resuelve |
|----|----------|-------------|------------------|
| P-01 | Versión exacta del SDK de Nakama para Unity y su forma de instalación (paquete UPM por URL de Git, tarball u otra) | FR-021 | Verificar la compatibilidad con Unity 6000.3, IL2CPP y Nakama 3.42.0; ADR |
| P-02 | Biblioteca de JSON Schema 2020-12 para C# y cómo llegan los esquemas al cliente (copia generada en la construcción o lectura en el editor) | FR-011, FR-013, FR-022 | **Resuelta**: validadores generados (research.md R-2, ADR-0010) |
| P-03 | Almacenamiento local de la sesión (preferencias de la app frente a almacenamiento seguro del sistema) | FR-005 | Valorar el riesgo (tokens de invitado) frente a la complejidad en Android e iOS |
| P-04 | Valores de caducidad de la sesión y del token de refresco, y política de reintentos (número, espera y tiempo límite por petición) | FR-009, FR-014, FR-015 | Valores coherentes con SC-001 y SC-004 |
| P-05 | Perfil de las partidas (rendimiento frente a turnos), heredado de la spec 002 | Nada en esta spec | El responsable, antes de la spec de partidas |

### Key Entities

- **Identificador de instalación**: valor aleatorio generado en la primera apertura y guardado
  localmente. Solo sirve para la autenticación de invitado, no es la identidad del jugador y no se
  muestra ni se registra completo.
- **Cuenta de jugador**: identidad asignada por el servidor (userId). Es la única identidad del
  jugador y, en el futuro, recibirá credenciales vinculadas.
- **Sesión**: token de sesión, token de refresco y sus caducidades. Se guarda localmente, se
  renueva y se descarta si es inválida.
- **Configuración de conexión**: host, puerto, clave de servidor y uso de conexión segura, por
  entorno. La de desarrollo local no se versiona.
- **Estado de conexión**: conectando, conectado, sin conexión, actualiza el juego, servidor
  incompatible, error de cuenta y error de configuración, con transiciones definidas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Con el servidor disponible, el jugador llega a «conectado» en **≤ 5 s** desde que se
  abre la escena de conexión, tanto la primera vez como en aperturas posteriores (entorno local).
- **SC-002**: En **10 de 10** reaperturas consecutivas, también tras forzar la caducidad de la
  sesión, el jugador recupera **la misma cuenta** y no se crea ninguna cuenta adicional.
- **SC-003**: Con el servidor no disponible, el jugador ve «sin conexión» en **≤ 60 s** y **nunca**
  queda indefinidamente en «conectando».
- **SC-004**: Las dos combinaciones de versiones incompatibles (servidor mayor y menor) bloquean el
  avance con su mensaje en el **100 %** de las pruebas.
- **SC-005**: **0** tokens, claves ni identificadores de instalación completos en los registros
  del cliente y en Git.
- **SC-006**: Las pruebas sin servidor pasan con el entorno local apagado, y las pruebas con
  servidor pasan con él arrancado, las dos en modo batch.
- **SC-007**: Una persona configura y ejecuta el cliente contra su entorno local siguiendo solo la
  documentación en **≤ 15 minutos**, sin contar instalaciones ni descargas.

## Assumptions

- Las specs 001 y 002 están fusionadas: entorno local, RPC `protocol_version` v1, contratos JSON y
  CI funcionan como quedaron documentados.
- La verificación se hace **solo en el editor de Unity** contra el Nakama local en `127.0.0.1`
  (Clarifications). La regla de seguridad de la spec 001 (FR-016) no cambia. Las builds de
  Android e iOS deben seguir compilando, pero conectarlas a un servidor queda fuera de alcance.
- El jugador es siempre invitado. Perfil, nombre visible y vinculación de cuentas llegan en specs
  posteriores (el diseño las permite, FR-004).
- Perder la cuenta de invitado al reinstalar es aceptable hasta que exista la vinculación de
  cuentas. No se muestra ningún aviso al jugador en esta spec.
- Los textos de la escena son provisionales, en español, sin localización.
- Unity no se ejecuta en la CI (ADR-0007). Las pruebas del cliente se preparan para incorporarse
  más adelante (FR-026).
- La configuración de builds de publicación (host, clave y TLS de producción) queda fuera de
  alcance; solo se exige que no se versione.

## Out of Scope

- Cuentas con correo o redes sociales, vinculación y desvinculación de credenciales, y
  recuperación de cuentas (solo se prepara el diseño, FR-004).
- Perfil, nombre visible, avatar y ajustes del jugador.
- Matchmaking, partidas, chat, clasificaciones, economía y progresión.
- Conexión en tiempo real (socket) de Nakama: no se necesita sin partidas ni chat.
- Unity en la CI y publicación en tiendas.
- Conectar un emulador o un dispositivo físico al backend local, o exponer Nakama fuera de
  `127.0.0.1` (Clarifications).
- Arte, diseño visual final y localización de la escena de conexión.
