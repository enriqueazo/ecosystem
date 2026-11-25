# Feature Specification: Base del servidor TypeScript, CI y contratos JSON

**Feature Branch**: `feat/002-servidor-typescript-ci`

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description: "Base del servidor TypeScript, CI y contratos JSON (spec 002), sobre la base fusionada de la spec 001. Registra e implementa D-01 (runtime TypeScript para la VM JavaScript de Nakama, pruebas en Node más integración contra Nakama), D-04 (CI con GitHub Actions sin Unity) y D-05 (contratos en JSON versionado). Incluye un módulo técnico mínimo, sin lógica de juego, que demuestre la cadena completa."

> **Nivel de detalle técnico**: igual que en la spec 001, esta funcionalidad es infraestructura y
> las tecnologías (TypeScript, Nakama, GitHub Actions, JSON) son decisiones ya tomadas por el
> responsable, que la spec registra. Las versiones exactas, las herramientas auxiliares, la
> estructura de archivos y los comandos se fijan en `/speckit-plan`, tras verificar su
> compatibilidad.

## Punto de partida

Estado de `main` en `438428c`, con la spec 001 fusionada:

- `server-nakama/` solo tiene un README. Ese README exige que el ADR del runtime llegue **en el
  mismo cambio** que el código, las pruebas, las comprobaciones, las reglas de `.gitignore`, el
  montaje en el entorno local y la documentación.
- `deploy/local/compose.yaml` no monta módulos.
- `shared/README.md` fija el versionado, los errores y la compatibilidad, pero deja pendientes
  el formato (D-05), la codificación de la versión y la comprobación automática de
  compatibilidad.
- No hay CI y el README raíz dice "no configurada (D-04)".
- Las comprobaciones existentes son `tools/tests/run.sh` (21 pruebas), `tools/check-repo.sh` y
  `tools/check-backend.sh`.
- Los números de ADR libres empiezan en **0006**. La propuesta del responsable usaba
  0004–0007, pero 0004 y 0005 ya están asignados y los números no se reutilizan. Por eso:
  D-01 → ADR-0006, D-04 → ADR-0007 y D-05 → ADR-0008.

## User Scenarios & Testing *(mandatory)*

Actores:
- **desarrollador/a de servidor**: escribe módulos, pruebas y contratos;
- **desarrollador/a de cliente**: consume contratos;
- **revisor/a**: integra cambios basándose en la CI.

### User Story 1 - Desarrollar y probar módulos de servidor de forma reproducible (Priority: P1)

Una persona desarrolladora instala las dependencias del servidor con la versión de herramientas
fijada. Ejecuta formato, análisis, comprobación de tipos y pruebas unitarias con comandos
documentados, y genera el artefacto que carga Nakama. Todo sin depender de servicios externos.

**Why this priority**: es la condición para escribir cualquier lógica de servidor autoritativa
(principio I), y la decisión D-01 no se considera implementada sin ella.

**Independent Test**: en un clon limpio, seguir solo `server-nakama/README.md` para instalar,
comprobar, probar y construir. Después, introducir un error de tipos y otro de formato y ver que
las comprobaciones fallan.

**Acceptance Scenarios**:

1. **Given** un clon limpio con la versión de herramientas documentada, **When** se instalan las
   dependencias, **Then** se resuelven exactamente las versiones del archivo de bloqueo
   versionado y la instalación termina sin errores.
2. **Given** las dependencias instaladas, **When** se ejecutan formato, análisis, tipos y pruebas
   unitarias, **Then** todo pasa y cada comando termina con código 0.
3. **Given** un error de tipos, de formato o de análisis introducido a propósito, **When** se
   ejecuta la comprobación correspondiente, **Then** falla con código distinto de 0 e indica el
   archivo y la línea.
4. **Given** las dependencias instaladas, **When** se ejecuta la construcción, **Then** se
   produce un único artefacto en una ubicación ignorada por Git, apto para la VM de JavaScript
   de Nakama.
5. **Given** una versión de herramienta distinta de la fijada, **When** se intenta instalar o
   construir, **Then** el proceso avisa o falla de forma explícita en lugar de continuar en
   silencio.

---

### User Story 2 - Contrato técnico de versión de protocolo de extremo a extremo (Priority: P1)

Una persona desarrolladora arranca el entorno local y el módulo de servidor queda cargado en
Nakama. Un cliente autenticado puede consultar, mediante un RPC técnico definido como contrato
JSON versionado en `shared/`, qué versión de protocolo y qué versión del módulo sirve el
servidor. Las pruebas de integración contra Nakama confirman que el comportamiento en el runtime
real coincide con el de las pruebas en Node.

**Why this priority**: demuestra la cadena completa (contrato → servidor → runtime real → entorno
local) sin lógica de juego. Además deja la consulta que los clientes usarán para detectar
incompatibilidades de versión (principio II).

**Independent Test**: arrancar el entorno local, autenticar un cliente de prueba, llamar al RPC y
comparar la respuesta con el contrato. Repetir con peticiones inválidas y sin sesión.

**Acceptance Scenarios**:

1. **Given** el entorno local arrancado con el módulo construido, **When** termina el arranque,
   **Then** los registros de Nakama muestran que el módulo se ha cargado y el RPC registrado, y
   la verificación del backend lo confirma.
2. **Given** un cliente con sesión válida, **When** llama al RPC de versión de protocolo,
   **Then** recibe una respuesta que cumple el contrato: versión de protocolo y versión del
   módulo, sin ningún otro dato interno.
3. **Given** una llamada sin sesión válida ni clave autorizada, **When** se invoca el RPC,
   **Then** se rechaza con el error de autenticación de Nakama y no se ejecuta lógica del módulo.
4. **Given** una petición con campos desconocidos, **When** se invoca el RPC, **Then** los campos
   desconocidos se ignoran y la respuesta es válida (tolerancia, principio II).
5. **Given** una petición que no es JSON válido o que supera el tamaño máximo del contrato,
   **When** se invoca el RPC, **Then** se devuelve un error con la estructura estándar de
   `shared/README.md` (`code` estable, sin detalles internos).
6. **Given** las mismas entradas, **When** se ejecutan las pruebas unitarias en Node y las de
   integración contra Nakama, **Then** ambas obtienen los mismos resultados observables.

---

### User Story 3 - Integración continua que bloquea cambios defectuosos (Priority: P2)

Cuando se abre o actualiza un PR hacia `main`, o se integra un cambio en `main`, la CI ejecuta
automáticamente:
- las comprobaciones del repositorio;
- las del servidor (formato, análisis, tipos, pruebas unitarias y construcción);
- las del backend (arranque del entorno local con el módulo y pruebas de integración).

Informa del resultado en el PR. No ejecuta Unity ni necesita secretos del repositorio.

**Why this priority**: la constitución exige que la CI bloquee la integración si fallan las
comprobaciones. Depende de que las historias 1 y 2 existan para tener algo que comprobar.

**Independent Test**: abrir un PR de prueba con un fallo deliberado en cada grupo de
comprobaciones y ver que la CI lo marca como fallido; con el fallo corregido, la CI pasa.

**Acceptance Scenarios**:

1. **Given** un PR hacia `main` sin errores, **When** se ejecuta la CI, **Then** todos los
   trabajos terminan con éxito y el PR lo muestra.
2. **Given** un PR con un fallo en las comprobaciones del repositorio, del servidor o del
   backend, **When** se ejecuta la CI, **Then** el trabajo correspondiente falla y el registro
   permite identificar la causa sin reproducirlo en local.
3. **Given** la definición de la CI, **When** se revisa, **Then** todas las acciones y
   herramientas externas están fijadas a versiones inmutables y revisables, y no se usa ningún
   secreto del repositorio. Los valores del entorno local en CI se generan en cada ejecución y
   no se imprimen.
4. **Given** un PR que solo cambia `client-unity/`, **When** se ejecuta la CI, **Then** no se
   arranca Unity. Las comprobaciones del repositorio se ejecutan igualmente.
5. **Given** la CI en funcionamiento, **When** se consulta el README raíz, **Then** el estado
   descrito coincide con la realidad: qué ejecuta, cuándo, qué no cubre (Unity) y cómo
   reproducir cada trabajo en local.

---

### User Story 4 - Contratos JSON versionados y documentación de decisiones (Priority: P2)

Una persona desarrolladora que vaya a crear un contrato encuentra en `shared/README.md` el formato
JSON elegido, cómo se codifica la versión, cómo se valida un mensaje y cómo se comprueba la
compatibilidad. Las decisiones D-01, D-04 y D-05 constan como resueltas, con su ADR.

**Why this priority**: cierra decisiones abiertas y fija las reglas antes del primer contrato de
producto. El único contrato que existe en esta fase es el técnico de la historia 2.

**Independent Test**: revisar `shared/README.md`, el contrato de versión de protocolo, los ADR
0006–0008 y `docs/decisions/pendientes.md`.

**Acceptance Scenarios**:

1. **Given** `shared/README.md`, **When** se revisa, **Then** las secciones que estaban
   pendientes de D-05 están completas: formato, codificación de la versión, validación y
   comprobación de compatibilidad.
2. **Given** el contrato de versión de protocolo, **When** se revisa, **Then** tiene versión,
   campos obligatorios, límites, errores y ejemplos válidos e inválidos, y las pruebas lo usan
   como fuente de verdad.
3. **Given** `docs/decisions/pendientes.md` y el índice de ADR, **When** se revisan, **Then**
   D-01, D-04 y D-05 aparecen como resueltas con enlace a ADR-0006, ADR-0007 y ADR-0008, y no
   queda ninguna definición duplicada.

---

### Edge Cases

- **Artefacto del módulo no construido o desactualizado**: al arrancar el entorno local sin
  construir antes, el problema se detecta. La verificación del backend o el propio arranque
  indica que el módulo no está cargado; Nakama no puede arrancar en silencio sin módulo
  mientras la documentación dice lo contrario.
- **Error de sintaxis o de carga en el módulo**: Nakama no queda sano, o la verificación falla
  mostrando el error de carga del módulo en los registros.
- **Código que funciona en Node pero no en la VM de Nakama** (APIs de Node, módulos del sistema,
  sintaxis no admitida): las pruebas de integración lo detectan y la construcción o el análisis
  lo impiden cuando sea posible.
- **Dependencias de ejecución**: el módulo no puede depender de paquetes de Node que no
  funcionen en la VM de Nakama. Las dependencias de desarrollo no forman parte del artefacto.
- **Versión de herramientas distinta** (Node o gestor de paquetes): aviso o fallo explícito,
  nunca una instalación que difiera del archivo de bloqueo.
- **Petición del RPC malformada, demasiado grande o con tipos incorrectos**: error estándar sin
  detalles internos y sin trazas en la respuesta. El registro de servidor no incluye la carga
  completa.
- **Llamada sin autenticación**: rechazada antes de ejecutar lógica del módulo.
- **CI sin Docker disponible o con el arranque del backend lento**: el trabajo del backend tiene
  un tiempo límite y falla con un mensaje claro; no queda colgado ni pasa en falso.
- **PR desde un fork**: la CI funciona sin secretos y sin permisos de escritura.
- **Cambio de versión de un contrato sin actualizar servidor, pruebas o documentación**: las
  pruebas de contrato o la revisión lo detectan (lista de comprobación de `shared/README.md`).
- **Regla de protección de rama no configurada**: la CI informa del resultado, pero para que
  *bloquee* la integración el responsable tiene que configurarlo en GitHub. Queda documentado
  como paso manual.

## Requirements *(mandatory)*

Convención (constitución): **MUST** es obligatorio; **SHOULD** es una recomendación que se puede
omitir con justificación escrita.

### Functional Requirements

#### Servidor TypeScript (D-01)

- **FR-001**: `server-nakama/` MUST contener un proyecto TypeScript con:
  - la versión de herramientas (Node y gestor de paquetes) fijada y documentada;
  - dependencias fijadas a versión exacta;
  - un archivo de bloqueo versionado.
- **FR-002**: MUST existir comandos reproducibles y documentados para: instalar dependencias desde
  el archivo de bloqueo, formatear y comprobar el formato, análisis estático, comprobación de
  tipos, pruebas unitarias, pruebas de integración y construcción.
- **FR-003**: La construcción MUST producir un único artefacto JavaScript compatible con la VM de
  Nakama 3.42.0, en una ruta ignorada por Git. Su compatibilidad (nivel de lenguaje, ausencia de
  APIs de Node, punto de entrada) MUST estar justificada en el plan y verificada por las pruebas
  de integración.
- **FR-004**: Las dependencias de ejecución del módulo MUST limitarse a los tipos y definiciones del
  runtime de Nakama y a código propio; cualquier dependencia de ejecución adicional MUST
  justificarse en un ADR.
- **FR-005**: La lógica MUST separarse del código dependiente del host de Nakama: las funciones de
  validación y de construcción de respuestas se prueban en Node sin Nakama, y el registro en el
  runtime es una capa fina (principio IV).
- **FR-006**: Las pruebas unitarias MUST ser deterministas, no depender de servicios externos y
  escribirse antes que el código que prueban (principio VI).
- **FR-007**: `.gitignore` MUST incluir las reglas del runtime TypeScript (dependencias
  instaladas, artefactos de construcción, cachés de herramientas) sin ignorar el archivo de
  bloqueo ni la configuración de las herramientas.
- **FR-008**: `server-nakama/README.md` MUST sustituir la sección "Qué se añadirá al resolver D-01"
  por el procedimiento real: requisitos, instalación, comprobaciones, pruebas, construcción,
  carga en el entorno local y estructura del código.

#### Módulo técnico y contrato de versión de protocolo

- **FR-009**: El módulo MUST registrarse en Nakama y exponer **un único** RPC técnico, "versión de
  protocolo", sin lógica de juego.
- **FR-010**: La respuesta del RPC MUST contener solo la versión de protocolo vigente y la
  versión del módulo. Ningún otro dato del servidor (configuración, entorno, rutas, nodos,
  dependencias).
- **FR-011**: El RPC MUST exigir una sesión de cliente válida. MUST rechazar llamadas sin
  autenticación mediante el mecanismo de Nakama, antes de ejecutar lógica propia.
- **FR-012**: El RPC MUST aceptar una petición JSON vacía o con campos desconocidos (que ignora).
  MUST rechazar una carga que no sea JSON válido o que supere el tamaño máximo del contrato, con
  la estructura de error estándar de `shared/README.md`.
- **FR-013**: El contrato del RPC MUST definirse en `shared/` en el formato elegido (FR-018),
  con: identificador, versión de contrato, campos y tipos, límites, errores posibles y ejemplos
  válidos e inválidos. Las pruebas en Node y las de integración MUST validar contra esa
  definición.
- **FR-014**: Las pruebas de integración MUST ejecutarse contra Nakama del entorno local y cubrir,
  como mínimo, los escenarios 1–5 de la historia 2. La comparación con las pruebas en Node del
  escenario 6 MUST cubrir los mismos casos de entrada.

#### Entorno local

- **FR-015**: `deploy/local/compose.yaml` MUST cargar en Nakama el artefacto construido, montado
  en solo lectura, sin montar otras rutas del repositorio.
- **FR-016**: Si el artefacto no existe, el arranque o la verificación del backend MUST fallar
  con un mensaje que indique cómo construirlo.
- **FR-017**: `tools/check-backend.sh` (o una comprobación equivalente documentada) MUST poder
  verificar, además de la salud, que el módulo está cargado. Se mantienen sus códigos de salida
  existentes y se documentan los nuevos en su contrato. Las pruebas de `tools/tests/run.sh` MUST
  cubrir el cambio.

#### Contratos JSON (D-05)

- **FR-018**: `shared/README.md` MUST completar las partes pendientes de D-05:
  - formato de definición de contratos en JSON;
  - cómo se valida un mensaje contra su definición;
  - cómo se codifican la versión de protocolo y la de contrato en los mensajes;
  - cómo se comprueba automáticamente la compatibilidad entre versiones;
  - organización de carpetas.
- **FR-019**: El formato MUST permitir que cliente (C#) y servidor (TypeScript) validen los mismos
  mensajes a partir de la misma definición, sin duplicar la definición a mano en cada lado.
- **FR-020**: Debe existir al menos una comprobación automática que falle si un contrato cambia de
  forma incompatible sin subir su versión, o si un ejemplo válido deja de validar. Se ejecuta en
  local y en CI.

#### CI (D-04)

- **FR-021**: MUST existir un flujo de GitHub Actions que se ejecute en cada PR hacia `main` y
  en cada integración en `main`, con trabajos separados para: comprobaciones del repositorio,
  comprobaciones y pruebas del servidor, y backend con pruebas de integración.
- **FR-022**: Todas las acciones de terceros MUST fijarse a una referencia inmutable (commit) con
  la versión legible en un comentario. Las herramientas (Node, imágenes de contenedor) MUST usar
  las mismas versiones fijadas que en local.
- **FR-023**: La CI MUST NOT usar secretos del repositorio. El trabajo del backend MUST generar
  valores aleatorios para la configuración local en cada ejecución, sin imprimirlos, y MUST
  limpiar el entorno al terminar.
- **FR-024**: Los permisos del token de la CI MUST limitarse a lectura del contenido.
- **FR-025**: Cada trabajo MUST tener un tiempo límite. El backend MUST mostrar los registros de
  los servicios cuando falla.
- **FR-026**: La CI MUST NOT arrancar Unity ni ejecutar pruebas de Unity. La documentación MUST
  indicar que las pruebas del cliente se ejecutan en local (`client-unity/README.md`) hasta que
  una decisión posterior las incorpore.
- **FR-027**: El README raíz MUST describir el estado real de la CI: qué ejecuta, cuándo, cómo
  reproducir cada trabajo en local y qué no cubre. MUST indicar el paso manual que necesita el
  responsable para que los trabajos sean obligatorios antes de integrar (protección de la rama
  `main`).
- **FR-028**: La CI MUST NOT declararse configurada hasta que un PR real haya mostrado los
  trabajos ejecutándose: uno en verde y al menos un fallo deliberado detectado (historia 3,
  escenarios 1–2).

#### Decisiones y documentación

- **FR-029**: MUST crearse ADR-0006 (runtime TypeScript), ADR-0007 (CI con GitHub Actions) y
  ADR-0008 (contratos en JSON versionado) con estado `aceptada`. En el mismo cambio,
  `docs/decisions/pendientes.md` mueve D-01, D-04 y D-05 a *Resueltas* y se actualiza el índice.
- **FR-030**: Cada procedimiento MUST seguir describiéndose en un solo documento:
  - servidor: `server-nakama/README.md`;
  - entorno local: `deploy/local/README.md`;
  - contratos: `shared/README.md`;
  - CI: README raíz o un documento enlazado desde él.
- **FR-031**: `tools/check-repo.sh` MUST comprobar también que el archivo de bloqueo del servidor
  no está ignorado, con su prueba en `tools/tests/run.sh`.
- **FR-032**: El trabajo MUST entregarse en commits atómicos con Conventional Commits en la rama
  `feat/002-servidor-typescript-ci`, y ninguna verificación se declara superada sin haberse
  ejecutado (principio VI).

### Recomendaciones (SHOULD)

- **R-01**: Un único comando de servidor que ejecute todas las comprobaciones en el mismo orden
  que la CI.
- **R-02**: Que la CI almacene en caché las dependencias del servidor por el archivo de bloqueo,
  sin afectar a la reproducibilidad.
- **R-03**: Mostrar las versiones de las herramientas al principio de cada trabajo de CI.

### Decisiones pendientes

| ID | Decisión | Opciones | Qué bloquea | Cómo se resuelve |
|----|----------|----------|-------------|------------------|
| P-01 | Versión exacta de Node y del gestor de paquetes | La LTS vigente (en la máquina de trabajo hay Node 26.13.1) u otra que fije el equipo | FR-001 y FR-022 | **Resuelta en el plan** (research.md R-1): Node 26.13.1 (LTS) con npm 11.20.0. |
| P-02 | Herramientas de formato, análisis, pruebas, construcción y validación de JSON | Se eligen en el plan entre opciones mantenidas y compatibles con la versión de Node fijada | FR-002, FR-003 y FR-019 | **Resuelta en el plan** (research.md R-2 a R-6): Biome 2.6.9, `node:test`, TypeScript 7.0.2 solo para tipos, Rollup 4 + Babel 7 a ES5 y Ajv 8 en modo standalone. Se registra en ADR-0006 y ADR-0008. |
| P-03 | Perfil de las partidas en directo: exigentes en rendimiento o por turnos | — | Nada en esta spec (no hay lógica de partida). Influye en la estructura futura de módulos y en la prioridad de las pruebas de rendimiento | Indicado por el responsable antes de la spec de partidas. ADR-0006 lo recoge como pregunta abierta. |

### Key Entities

- **Módulo de servidor**: artefacto construido que Nakama carga. Tiene una versión y registra
  RPC.
- **Contrato**: definición JSON versionada de un mensaje o RPC. Tiene identificador, versión,
  campos, límites, errores y ejemplos.
- **Versión de protocolo**: entero global que identifica el conjunto compatible de contratos
  (`shared/README.md` → *Versionado*).
- **Trabajo de CI**: unidad de comprobación con su disparador, tiempo límite, permisos y
  resultado visible en el PR.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Desde un clon limpio, con las herramientas fijadas, una persona instala, comprueba,
  prueba y construye el servidor siguiendo solo su README en **≤ 10 minutos** (sin contar las
  descargas).
- **SC-002**: El **100 %** de los escenarios de aceptación de la historia 2 tienen una prueba
  automatizada que pasa contra el runtime real de Nakama.
- **SC-003**: Cada uno de los 3 tipos de fallo deliberado (repositorio, servidor y backend) hace
  fallar la CI en el **100 %** de los intentos, y la causa se identifica leyendo solo el registro
  del trabajo.
- **SC-004**: Una ejecución completa de la CI en un PR termina en **≤ 15 minutos**.
- **SC-005**: **0** secretos en el repositorio y **0** valores sensibles impresos en los
  registros de CI.
- **SC-006**: **0** acciones de terceros sin fijar a una referencia inmutable.
- **SC-007**: Las decisiones D-01, D-04 y D-05 aparecen como resueltas en **un único** documento
  de pendientes, con su ADR, y **0** documentos contradicen el estado de la CI.
- **SC-008**: La respuesta del RPC técnico contiene **exactamente** los campos del contrato,
  verificado en el 100 % de las pruebas.

## Assumptions

- La spec 001 está fusionada en `main`: entorno local, `tools/`, Unity 6000.3.29f1 y el registro
  de decisiones funcionan como quedaron documentados.
- El repositorio sigue alojado en GitHub (`enriqueazo/ecosystem`) y GitHub Actions está
  disponible con ejecutores alojados que tienen Docker.
- La protección de rama de `main` (trabajos obligatorios) la configura el responsable en GitHub.
  Esta spec la documenta, pero no la aplica (es una acción sobre la configuración del
  repositorio).
- El RPC técnico usa la autenticación de cliente de Nakama (sesión). La autenticación de
  servidor a servidor por clave HTTP queda fuera de esta fase.
- La versión de protocolo inicial es `1` y la del primer contrato también es `1`.
- "Comprobaciones del backend" en CI significa arrancar el entorno local definido en
  `deploy/local/` con el módulo cargado y ejecutar la verificación de conectividad y las pruebas
  de integración. No es un despliegue.
- El perfil de rendimiento de las partidas (P-03) no afecta a esta spec, porque no hay lógica de
  partida.

## Out of Scope

- Mecánicas, reglas o contenido del juego; cuentas, matchmaking, partidas, chat, clasificaciones
  y economía.
- El SDK de Nakama en el cliente Unity y la validación de contratos en C# dentro de Unity. El
  formato debe permitirlo (FR-019), pero se implementa con la primera funcionalidad de cliente
  que consuma contratos.
- Pruebas o builds de Unity en CI y gestión de licencias de Unity en CI.
- Despliegue de producción, publicación de imágenes, entornos remotos y monitorización.
- Configurar la protección de rama en GitHub (paso manual del responsable).
