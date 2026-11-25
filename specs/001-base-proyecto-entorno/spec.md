# Feature Specification: Base del proyecto y entorno de desarrollo

**Feature Branch**: `feat/001-base-proyecto-entorno`

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description: "Crea la especificación de la funcionalidad “Base del proyecto y entorno de desarrollo” para preparar la estructura inicial de un juego móvil multiplataforma de estrategia (Unity/C# para Android e iOS, backend Nakama con PostgreSQL, monorepo). Sin mecánicas ni funcionalidades de producto; runtime de Nakama y proveedor de CI sin decidir."

> **Nota sobre el nivel de detalle técnico**: esta funcionalidad *es* la base técnica del
> repositorio, y la pila (Unity, Nakama, PostgreSQL, monorepo Git) ya está fijada por la
> constitución v1.1.0. Por eso la spec nombra esas tecnologías y las áreas del repositorio. Lo que
> **no** fija es el *cómo*: contenido exacto de archivos, comandos concretos, puertos numéricos,
> herramientas auxiliares y versiones se determinan en `/speckit-plan`, verificando compatibilidad.

## Clarifications

### Session 2026-11-25

- Q: ¿La base debe incluir conexión mínima del cliente Unity con Nakama o la conectividad se verifica solo desde el equipo anfitrión? → A: Solo desde el equipo anfitrión, mediante la comprobación de salud de Nakama; el SDK de Nakama se incorpora en la primera funcionalidad que lo necesite.
- Q: ¿Qué pipeline de render usa el proyecto Unity (D-07)? → A: URP (Universal Render Pipeline).

## Estado actual del repositorio y conflictos detectados

Revisión realizada el 2026-11-25 sobre `main` (`12e2040`):

| # | Hallazgo | Tipo | Resolución exigida por esta spec |
|---|----------|------|----------------------------------|
| C-1 | `.gitignore` incluye una sección "Go / Nakama" (`/server-nakama/bin/`, `*.so`, `coverage.out`…). Presupone Go como runtime, que la constitución mantiene como decisión abierta. | **Conflicto** con constitución (Pila, Decisiones abiertas) | FR-025: retirar las reglas específicas de runtime hasta que D-01 se resuelva. |
| C-2 | `.gitignore` ignora `*.csproj` y `*.sln` en **todo** el repositorio. Correcto para los generados por Unity, pero ocultaría cualquier proyecto .NET futuro fuera del cliente (p. ej. herramientas o contratos). | Riesgo | FR-026: limitar esas reglas al proyecto Unity. |
| C-3 | Las carpetas `client-unity/`, `server-nakama/`, `shared/`, `deploy/`, `docs/`, `tools/` existen solo en local y vacías; Git no las versiona, así que no existen para otras personas. | Hueco | FR-001–FR-003: cada área versionada debe tener contenido que la justifique. |
| C-4 | `.gitignore` incluye `/deploy/data/` y `/deploy/volumes/`, que suponen persistencia mediante carpetas dentro del repo. | Observación | FR-017: la persistencia local queda fuera de archivos versionados; si se usan carpetas, deben estar ignoradas. |
| C-5 | No hay README raíz, `.editorconfig`, `.gitattributes`, `.env.example` ni registro de decisiones. La constitución exige `.env.example` y registrar decisiones técnicas. | Hueco | FR-027–FR-033. |
| C-6 | En la máquina de trabajo no hay Unity Hub/Editor ni SDK de Android instalados; sí Docker 29.2 con Compose v5 (arm64) y Xcode. | Bloqueo parcial | La Historia 2 queda bloqueada hasta resolver D-02 e instalar el editor (ver *Decisiones pendientes*). |
| C-7 | El remoto es GitHub, pero la constitución no fija proveedor de CI. | Decisión abierta | D-04; no se asume GitHub Actions. |
| C-8 | La constitución exige fijar versiones de Unity, Nakama y PostgreSQL **antes de la primera compilación/CI**, y esta funcionalidad incluye la primera compilación. | Dependencia | D-02 y D-03 deben resolverse antes de implementar las historias afectadas. D-03 resuelta en el plan; D-02 pendiente de confirmación. |

No se han detectado contradicciones entre la petición de esta funcionalidad y la constitución.

## User Scenarios & Testing *(mandatory)*

Actores: **desarrollador/a del equipo** (clona el repositorio y trabaja en cliente o servidor) y
**revisor/a** (valida PRs y el cumplimiento de la constitución).

### User Story 1 - Levantar el backend local de forma reproducible (Priority: P1)

Una persona desarrolladora clona el repositorio, copia la configuración de ejemplo, define sus
valores locales y, con un único procedimiento documentado, arranca Nakama y PostgreSQL en su
máquina; puede comprobar que el backend responde, detenerlo conservando los datos y limpiarlo por
completo.

**Why this priority**: es el primer elemento que puede funcionar de extremo a extremo sin depender
de decisiones pendientes de cliente (versión de Unity) ni de runtime; desbloquea todo el trabajo
de servidor posterior.

**Independent Test**: en una copia limpia del repositorio, seguir solo la documentación para
arrancar, verificar, detener, rearrancar y limpiar el entorno.

**Acceptance Scenarios**:

1. **Given** un clon limpio y las herramientas previas instaladas, **When** se crea la
   configuración local a partir del ejemplo y se ejecuta el procedimiento de arranque, **Then**
   PostgreSQL queda listo antes de que Nakama arranque, Nakama aplica sus migraciones propias y
   la comprobación de salud documentada responde correctamente.
2. **Given** el backend en marcha, **When** se ejecuta la verificación de conectividad
   documentada, **Then** informa de éxito y termina con código 0.
3. **Given** el backend en marcha, **When** se detiene y se vuelve a arrancar, **Then** los datos
   creados en la sesión anterior siguen disponibles.
4. **Given** el backend detenido, **When** se ejecuta el procedimiento de limpieza, **Then** se
   eliminan contenedores y datos locales del entorno, y un nuevo arranque parte de una base de
   datos vacía.
5. **Given** el backend en marcha, **When** se listan los puertos abiertos en el equipo
   anfitrión, **Then** solo están expuestos la API de cliente de Nakama y su consola de
   administración, ambos ligados a la interfaz local (loopback); la base de datos no es accesible
   desde fuera de la red interna del entorno.

---

### User Story 2 - Abrir, compilar y probar el cliente Unity (Priority: P2)

Una persona desarrolladora abre el proyecto Unity con la versión fijada, el proyecto importa y
compila sin errores, la escena inicial se reproduce sin errores, las pruebas de EditMode y PlayMode
pasan, y se puede generar una build de desarrollo para Android y exportar el proyecto Xcode de iOS.

**Why this priority**: es imprescindible para el juego, pero está **bloqueada** por D-02 (versión
de Unity) y por la instalación del editor y sus módulos; puede desarrollarse en paralelo a la
Historia 1 en cuanto se resuelva.

**Independent Test**: abrir el proyecto desde un clon limpio con la versión fijada, ejecutar las
pruebas desde el Test Runner (o en modo batch) y generar ambas builds siguiendo la documentación.

**Acceptance Scenarios**:

1. **Given** un clon limpio y el editor de la versión fijada con módulos Android e iOS, **When**
   se abre el proyecto, **Then** la importación termina con 0 errores de compilación y la consola
   no muestra errores.
2. **Given** el proyecto abierto, **When** se reproduce la escena inicial, **Then** carga sin
   errores ni advertencias de scripts ausentes.
3. **Given** el proyecto abierto, **When** se ejecutan todas las pruebas EditMode y PlayMode,
   **Then** todas pasan.
4. **Given** la plataforma Android seleccionada, **When** se genera una build de desarrollo,
   **Then** se produce un paquete instalable sin errores.
5. **Given** la plataforma iOS seleccionada en macOS, **When** se exporta la build, **Then** se
   genera un proyecto Xcode sin errores (firma y publicación fuera de alcance).
6. **Given** el proyecto abierto y cerrado sin cambios intencionados, **When** se consulta el
   estado de Git, **Then** no aparecen archivos nuevos o modificados (todo lo generado está
   ignorado y lo versionado es estable).

---

### User Story 3 - Orientarse en el repositorio y conocer las decisiones pendientes (Priority: P2)

Una persona nueva lee el README raíz y entiende qué contiene cada área, qué herramientas necesita,
cómo empezar, qué decisiones siguen abiertas y dónde se registran las decisiones de arquitectura.

**Why this priority**: sin esta orientación las historias 1 y 2 no son reproducibles por terceros,
y las decisiones abiertas podrían resolverse de forma silenciosa.

**Independent Test**: una persona que no participó en la creación de la base sigue el README sin
ayuda y localiza, para cada decisión pendiente, qué bloquea y cómo se resuelve.

**Acceptance Scenarios**:

1. **Given** el README raíz, **When** se consulta, **Then** contiene requisitos previos con
   versiones (o referencia a la decisión pendiente correspondiente), mapa de áreas, primeros
   pasos para backend y cliente, comandos de uso y enlace al registro de decisiones.
2. **Given** cualquier área de primer nivel versionada, **When** se abre, **Then** contiene un
   README (o el contenido propio del área) que explica su propósito y límites.
3. **Given** una decisión pendiente, **When** se busca en la documentación, **Then** aparece en
   un único lugar con opciones, qué bloquea y qué información falta para resolverla.

---

### User Story 4 - Contratos y servidor preparados sin decisiones prematuras (Priority: P3)

Una persona desarrolladora que vaya a crear el primer RPC o mensaje encuentra la ubicación de los
contratos, sus reglas de versionado, errores y compatibilidad, y la ubicación de los módulos de
servidor, sin que exista código de juego ni un runtime elegido implícitamente.

**Why this priority**: evita acoplamientos y decisiones silenciosas, pero no aporta funcionamiento
ejecutable hasta la primera funcionalidad de producto.

**Independent Test**: revisar el área de contratos y la de servidor: la documentación cubre los
temas exigidos y no hay mensajes de juego, código de servidor ni configuración específica de un
runtime.

**Acceptance Scenarios**:

1. **Given** el área de contratos, **When** se revisa, **Then** documenta formato (o su decisión
   pendiente), versionado, estructura de errores, compatibilidad y el procedimiento para añadir
   un contrato, y no contiene definiciones de mensajes de juego.
2. **Given** el área de servidor, **When** se revisa, **Then** solo contiene documentación que
   explica su propósito, la decisión pendiente D-01 y qué se añadirá al resolverla.
3. **Given** el repositorio completo, **When** se busca código del cliente referenciado desde el
   servidor o viceversa, **Then** no existe ninguna dependencia directa entre ambos.

---

### User Story 5 - Comprobaciones de calidad y CI (Priority: P3)

Una persona revisora puede ejecutar localmente las comprobaciones disponibles (configuración del
entorno válida, ausencia de secretos versionados, pruebas del cliente) y, cuando se decida el
proveedor de CI, esas mismas comprobaciones bloquean la integración si fallan.

**Why this priority**: la constitución exige comprobaciones reproducibles, pero la parte de CI
está **bloqueada** por D-04 y, para Unity, por la licencia en CI.

**Independent Test**: ejecutar las comprobaciones locales documentadas; introducir
deliberadamente un fallo (p. ej. un `.env` añadido al índice o una configuración del entorno
inválida) y confirmar que la comprobación falla.

**Acceptance Scenarios**:

1. **Given** el repositorio sin cambios, **When** se ejecutan las comprobaciones locales
   documentadas, **Then** todas pasan.
2. **Given** un fallo introducido deliberadamente, **When** se ejecuta la comprobación
   correspondiente, **Then** falla con un mensaje que identifica el problema.
3. **Given** D-04 resuelta, **When** se abre un PR, **Then** el flujo de CI ejecuta las
   comprobaciones pertinentes y bloquea la integración si fallan. Hasta entonces, la
   documentación indica explícitamente que la CI **no** está configurada.

---

### Edge Cases

- **Falta el archivo de configuración local** (`.env`): el arranque se interrumpe antes de crear
  contenedores con un mensaje que indica copiar el ejemplo; no se usan valores por defecto
  implícitos.
- **Secreto obligatorio sin definir o vacío** (contraseña de base de datos, claves de servidor,
  credenciales de consola, claves de cifrado de sesión): el arranque falla nombrando la variable
  ausente; nunca arranca con claves por defecto conocidas públicamente.
- **Valores de ejemplo usados tal cual**: se permiten en local, pero la documentación advierte que
  son ficticios y que no son válidos fuera del desarrollo local.
- **Docker no instalado o el demonio detenido**: la documentación de resolución de problemas
  describe el síntoma y la solución; la verificación de conectividad informa "backend no
  disponible" y termina con código distinto de 0.
- **Servicios detenidos o caídos** (Nakama o PostgreSQL): la verificación de conectividad falla
  con mensaje claro; Nakama no se da por listo si PostgreSQL no está sano.
- **Puerto local ocupado**: el arranque falla de forma visible; los puertos publicados son
  configurables mediante la configuración local y documentados.
- **Imagen de contenedor no disponible para la arquitectura del equipo** (arm64/amd64): la
  decisión D-03 debe verificar disponibilidad para ambas; si alguna no existe, se documenta la
  limitación y la alternativa.
- **Versión de Unity distinta de la fijada**: la documentación indica que se abra con la versión
  exacta; abrir con otra versión no debe confirmarse en Git (cambios en la versión del proyecto
  se tratan como cambio explícito, Principio VIII).
- **Módulos de plataforma (Android/iOS) no instalados**: la documentación indica cómo añadirlos;
  la build correspondiente no se considera verificada.
- **Versiones incompatibles de herramientas** (Docker/Compose por debajo del mínimo documentado,
  paquete Unity incompatible con la versión fijada): los requisitos previos indican versiones
  mínimas comprobadas; el lockfile de paquetes impide resoluciones distintas entre equipos.
- **Datos locales corruptos o migración fallida**: el procedimiento de limpieza permite volver a
  un estado vacío; nunca se manipulan tablas internas de Nakama para "arreglarlo".
- **Archivos `.meta` huérfanos o ausentes**: no se versionan carpetas vacías de Unity; cada
  carpeta bajo `Assets/` versionada tiene contenido y su `.meta`.

## Requirements *(mandatory)*

Convención (constitución): **MUST** = obligatorio; **SHOULD** = recomendación, omisible con
justificación escrita. Las decisiones pendientes están en su propia sección.

### Functional Requirements

#### Estructura del monorepo

- **FR-001**: El repositorio MUST tener estas áreas de primer nivel, con nombres en minúsculas y
  guiones (se conservan los nombres ya presentes):

  | Área | Contenido | Contenido en esta funcionalidad |
  |------|-----------|---------------------------------|
  | `client-unity/` | Proyecto Unity (raíz del proyecto: `Assets/`, `Packages/`, `ProjectSettings/`). | Proyecto mínimo (Historia 2) + README. |
  | `server-nakama/` | Módulos de servidor autoritativo de Nakama, sus pruebas y su configuración de compilación. | Solo README (D-01 pendiente). |
  | `shared/` | **Exclusivamente** contratos cliente-servidor versionados y su documentación/pruebas. No alberga código de utilidad compartido. | Documentación de contratos (FR-021). |
  | `deploy/local/` | Entorno de desarrollo local: orquestación de contenedores, configuración de Nakama para desarrollo, ejemplo de configuración local. | Entorno local (Historia 1). |
  | `docs/` | Documentación transversal y registro de decisiones de arquitectura (`docs/decisions/`). | Plantilla, índice, ADR iniciales y decisiones pendientes. |
  | `tools/` | Scripts de desarrollo y verificación reproducibles. | Solo si contiene al menos un script de verificación (p. ej. conectividad o comprobaciones locales); si no, no se crea. |

- **FR-002**: Ninguna carpeta versionada MUST estar vacía ni contener solo marcadores sin
  explicación. El archivo mínimo que justifica un área sin contenido técnico todavía es un
  `README.md` que indique propósito, límites, estado y la decisión que la desbloquea. No se usan
  `.gitkeep`.
- **FR-003**: No MUST crearse subcarpetas anticipadas (p. ej. para despliegue de producción,
  plataformas adicionales o mensajes de juego).
- **FR-004**: El cliente y el servidor MUST NOT referenciarse directamente: ningún archivo del
  servidor depende de código del cliente ni viceversa; su único punto común es `shared/`.

#### Proyecto Unity *(bloqueado por D-02)*

- **FR-005**: El proyecto Unity MUST crearse con la versión fijada en D-02 y el pipeline de render
  URP (Universal Render Pipeline, D-07 resuelta), con Android e iOS como únicas plataformas
  objetivo. Los assets de configuración de URP se limitan a los necesarios para la escena inicial
  y se versionan junto con sus `.meta`.
- **FR-006**: Configuración de proyecto MUST: serialización de assets en modo texto, archivos
  `.meta` visibles, backend de scripting compatible con ambas tiendas (IL2CPP) y arquitectura
  Android ARM64; identificador de aplicación y nombre de producto provisionales documentados
  (D-06).
- **FR-007**: Bajo `Assets/_Project/` MUST existir solo carpetas con contenido. Estructura inicial
  mínima:
  - `Scenes/` — la escena inicial (bootstrap) mínima: sin arte, audio, animación ni lógica de
    juego; solo lo necesario para que la escena se reproduzca (p. ej. una cámara).
  - `Settings/` — assets de configuración de URP (pipeline y renderer), limitados a los que
    necesita la escena inicial (D-07).
  - `Tests/EditMode/` — ensamblado de pruebas EditMode con pruebas de la configuración del
    proyecto (FR-037).
  - `Tests/PlayMode/` — ensamblado de pruebas PlayMode con la prueba de carga de la escena
    inicial (FR-037).
  - Las carpetas de código de ejecución (p. ej. `Scripts/` con su definición de ensamblado) se
    crean en la primera funcionalidad que aporte código, no en esta.
- **FR-008**: La escena inicial MUST ser la primera (y única) escena incluida en la build.
- **FR-009**: MUST versionarse: `Assets/` con todos sus `.meta`, `Packages/manifest.json`,
  `Packages/packages-lock.json`, `ProjectSettings/` (incluida la versión del editor). MUST NOT
  versionarse: cachés y artefactos (`Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`,
  builds, `MemoryCaptures/`, proyectos y soluciones generados por el IDE).
- **FR-010**: El manifiesto de paquetes MUST contener solo paquetes con necesidad documentada en
  el README del cliente (Principio IV). Los paquetes que la plantilla de Unity añade por defecto
  sin uso en esta fase MUST eliminarse. Son necesarios: Unity Test Framework y URP (D-07).
- **FR-011**: Abrir y cerrar el proyecto sin cambios MUST NOT generar cambios en archivos
  versionados (Historia 2, escenario 6).
- **FR-012**: El cliente MUST NOT incluir el SDK de Nakama ni ninguna configuración de conexión
  (endpoints, claves) en esta funcionalidad. La conectividad básica se verifica desde el equipo
  anfitrión (FR-038); el SDK se incorpora, con versión fijada y ADR, en la primera funcionalidad
  que lo use.

#### Backend local

- **FR-013**: El entorno local MUST orquestarse con Docker Compose (disponible en el entorno:
  Docker 29.2 / Compose v5) e incluir únicamente dos servicios: PostgreSQL y Nakama, con imágenes
  fijadas a versión exacta (D-03), nunca etiquetas flotantes como `latest`.
- **FR-014**: Nakama MUST arrancar solo cuando PostgreSQL esté sano y MUST aplicar sus propias
  migraciones mediante su mecanismo soportado antes de servir. MUST NOT ejecutarse SQL contra
  tablas internas de Nakama ni cargarse datos de producción o de ejemplo.
- **FR-015**: Ambos servicios MUST tener comprobación de salud; el entorno MUST informar de
  "listo" solo cuando ambas pasen.
- **FR-016**: Exposición de red: MUST publicarse en el anfitrión solo la API de cliente de Nakama
  y su consola de administración, ligadas a loopback y con puertos configurables. PostgreSQL y el
  resto de puertos de Nakama (p. ej. gRPC) MUST NOT publicarse.
- **FR-017**: Los datos de PostgreSQL MUST persistir entre paradas y arranques en almacenamiento
  local fuera de archivos versionados; la limpieza MUST eliminarlos.
- **FR-018**: Todos los secretos (contraseña de base de datos, clave de servidor, clave HTTP de
  runtime, credenciales de consola, claves de cifrado de sesión y de refresco) MUST proceder de
  la configuración local no versionada. La configuración versionada de Nakama MUST NOT contener
  secretos. El arranque MUST fallar nombrando la variable si falta o está vacía.
- **FR-019**: MUST documentarse un procedimiento para cada operación: arrancar, ver estado, ver
  registros, detener (conservando datos), limpiar (eliminando datos) y verificar conectividad.
- **FR-020**: El entorno MUST NOT cargar módulos de servidor personalizados ni montar rutas
  específicas de un runtime hasta que D-01 se resuelva; Nakama arranca solo con su funcionalidad
  incorporada.

#### Contratos compartidos

- **FR-021**: `shared/` MUST incluir documentación inicial que cubra:
  (a) ubicación y organización de las definiciones;
  (b) formato de definición — pendiente (D-05), con criterios de elección;
  (c) esquema de versionado del protocolo y de cada contrato, y cómo se comunica la versión;
  (d) estructura estándar de errores (código estable, sin datos sensibles ni detalles internos);
  (e) reglas de compatibilidad derivadas del Principio II (tolerar campos desconocidos, rechazar
  mensajes inválidos, respuesta definida ante versiones antiguas/incompatibles, datos mínimos);
  (f) lista de comprobación para añadir o cambiar un contrato (definición, cliente, servidor,
  pruebas y documentación en el mismo cambio).
- **FR-022**: `shared/` MUST NOT contener definiciones de mensajes, RPC, opcodes o eventos de juego
  en esta funcionalidad.
- **FR-023**: Los puntos (b) y, si depende del formato, (c) MUST marcarse explícitamente como
  pendientes de D-05 en vez de inventar un formato.

#### Configuración general

- **FR-024**: `.gitignore` MUST cubrir: Unity (FR-009), Docker/datos locales, IDE (Visual Studio,
  Rider, VS Code), sistema operativo (macOS, Windows), builds móviles (paquetes Android, proyectos
  Xcode exportados), registros, y secretos (`.env` y variantes, permitiendo `.env.example`).
- **FR-025**: `.gitignore` MUST NOT incluir reglas específicas de un runtime de servidor hasta
  resolver D-01 (resuelve C-1). Al resolverse, las reglas se añaden en el mismo cambio que el ADR.
- **FR-026**: Las reglas de proyectos/soluciones generados por el IDE MUST limitarse al proyecto
  Unity (resuelve C-2).
- **FR-027**: `.gitignore` MUST NOT ignorar archivos necesarios para reconstruir el proyecto:
  `.meta`, `manifest.json`, `packages-lock.json`, `ProjectSettings/`, lockfiles de dependencias y
  `.env.example`. Verificable con la comprobación de FR-039.
- **FR-028**: MUST existir un `.env.example` (ubicado donde lo lea el entorno local) con todas las
  variables necesarias, valores ficticios evidentes y un comentario por variable. La
  documentación MUST indicar que `.env` no se versiona.
- **FR-029**: MUST existir `.editorconfig` con codificación, finales de línea, indentación y
  espacios finales por tipo de archivo (C#, YAML, JSON, Markdown, scripts), y convenciones de
  estilo C# siguiendo las convenciones oficiales de .NET/Unity.
- **FR-030**: SHOULD existir `.gitattributes` que normalice finales de línea y marque como
  binarios los tipos binarios habituales de Unity, para evitar diferencias espurias entre macOS y
  Windows.

#### Registro de decisiones

- **FR-031**: MUST existir `docs/decisions/` con una plantilla de registro (contexto, opciones,
  decisión, consecuencias, estado, fecha) y un índice.
- **FR-032**: MUST registrarse en esta funcionalidad, como decisiones aceptadas: la estructura del
  monorepo, el entorno local con contenedores, el pipeline de render URP (D-07) y la
  verificación de conectividad solo desde el anfitrión, sin SDK en el cliente (Clarifications). Y, cuando se resuelvan: D-02, D-03 y cualquier
  otra decisión pendiente resuelta durante el plan.
- **FR-033**: Las decisiones pendientes MUST listarse en un único documento (dentro de
  `docs/decisions/`) con opciones, qué bloquean y qué información falta; README y otros
  documentos enlazan a él sin duplicar su contenido.

#### Documentación

- **FR-034**: El README raíz MUST contener: descripción breve del proyecto, requisitos previos
  con versiones mínimas comprobadas (o enlace a la decisión pendiente), mapa de áreas, primeros
  pasos (backend y cliente), comandos de uso, cómo ejecutar comprobaciones, estado de la CI,
  enlace al registro de decisiones, flujo de trabajo (spec-kit y convenciones Git de la
  constitución) y resolución de problemas para los casos límite de esta spec.
- **FR-035**: Cada procedimiento MUST describirse en un único documento; los demás enlazan
  (fuente única). El README de cada área describe solo lo propio de esa área.
- **FR-036**: La documentación MUST NOT contener secretos ni valores que parezcan reales.

#### Calidad y verificación

- **FR-037**: Pruebas automáticas del cliente (Unity Test Framework, Principio VI):
  - EditMode: verifican invariantes de configuración (escena inicial en la build en primera
    posición; solo Android/iOS configurados; backend de scripting y arquitectura de FR-006;
    serialización en texto; URP asignado como pipeline activo).
  - PlayMode: la escena inicial carga y se reproduce sin errores registrados.
  Estas pruebas se escriben primero y se comprueba que fallan antes de configurar el proyecto
  (Red–Green), siempre que la configuración lo permita.
- **FR-038**: Verificación de conectividad del backend: comprobación reproducible que confirma que
  la API de cliente de Nakama responde a su comprobación de salud; termina con código 0 si está
  disponible y distinto de 0 con mensaje claro si no.
- **FR-039**: Comprobaciones locales reproducibles, documentadas y ejecutables sin el editor de
  Unity:
  (a) la configuración del entorno local es válida;
  (b) ningún archivo de secretos (`.env`) ni dato local está versionado;
  (c) los archivos imprescindibles (FR-027) no están ignorados.
- **FR-040**: Lo que no admite prueba automática (apertura en editor, builds de plataforma,
  exposición de puertos) MUST tener pasos de validación manual documentados con resultado
  esperado.
- **FR-041**: Formato y análisis: C# sigue `.editorconfig` (FR-029). La comprobación automática
  de formato/análisis de C# se añade solo con una herramienta verificada como compatible con la
  versión de Unity fijada (D-09). Las comprobaciones de servidor (formato, análisis, pruebas) se
  definen al resolver D-01, según la constitución.
- **FR-042**: CI *(bloqueado por D-04)*: al resolverse, MUST existir un flujo mínimo que ejecute
  FR-039 en cada PR y bloquee la integración si falla; con versiones de acciones/herramientas
  fijadas y sin secretos en el repositorio. Las pruebas de Unity en CI se añaden solo cuando la
  licencia en CI esté resuelta. Hasta que exista un flujo funcional, la documentación MUST
  indicar que la CI no está configurada.
- **FR-043**: Ninguna verificación MUST declararse superada sin haberse ejecutado; si no pudo
  ejecutarse (p. ej. falta el editor), se indica qué falta y por qué (Principio VI).
- **FR-044**: El trabajo MUST entregarse en commits atómicos con Conventional Commits y ámbito
  (p. ej. `chore(repo)`, `feat(deploy)`, `feat(client)`, `docs(shared)`), en la rama
  `feat/001-base-proyecto-entorno`.

### Recomendaciones (SHOULD)

- **R-01**: Ofrecer un único punto de entrada para los comandos habituales (p. ej. un script en
  `tools/`), siempre que sea un envoltorio fino sobre comandos estándar documentados y no oculte
  errores.
- **R-02**: Nivel de registro de Nakama en desarrollo útil para depurar, sin registrar cargas
  completas ni credenciales.
- **R-03**: Documentar cómo ejecutar las pruebas de Unity en modo batch, aunque la CI aún no las
  ejecute.
- **R-04**: Decidir el uso de Git LFS (D-08) antes de añadir el primer recurso binario grande.

### Decisiones pendientes

| ID | Decisión | Opciones | Qué bloquea exactamente | Información necesaria / cómo se resuelve |
|----|----------|----------|--------------------------|------------------------------------------|
| **D-01** | Runtime de módulos Nakama | **Go**: máximo rendimiento y tipado; el plugin debe compilarse con la versión exacta de Nakama y su toolchain, lo que acopla actualizaciones. **TypeScript/JavaScript**: tipado (TS) y tooling conocido; se ejecuta en la VM JS embebida de Nakama, con empaquetado a un único archivo, y las pruebas en Node no equivalen al runtime. **Lua**: más simple, menos tooling y tipado. | Código y estructura interna de `server-nakama/`; compilación y pruebas de servidor; formato/análisis de servidor; reglas de `.gitignore` del runtime; carga de módulos en el entorno local; trabajo de CI del servidor. **No bloquea** las Historias 1–3. | Experiencia del equipo, requisitos de rendimiento de partidas autoritativas, coste de mantenimiento de actualizaciones, tooling de pruebas. Se resuelve con ADR antes de la primera lógica de servidor (constitución). |
| **D-02** | Versión exacta de Unity (LTS) y módulos | Una versión LTS vigente de la línea Unity 6 en el momento de decidir (recomendado) u otra LTS soportada. | **Toda la Historia 2** (FR-005–FR-011, FR-037) y requisitos previos del cliente en el README. | Unity Hub/Editor instalados con módulos Android (incl. SDK/NDK/JDK) e iOS; tipo de licencia; compatibilidad con el futuro SDK de Nakama para Unity y con imágenes de CI si se usan. Registrar en ADR. |
| **D-03** | Versiones exactas de Nakama y PostgreSQL | Última versión estable de Nakama y una versión de PostgreSQL que su documentación declare soportada. | **Historia 1** (FR-013). | **Resuelta en el plan** (research R-1): Nakama 3.42.0 y PostgreSQL 16.16-alpine; se registra en ADR-0002. |
| **D-04** | Proveedor de CI | GitHub Actions (el remoto actual es GitHub) u otro. | FR-042 y Historia 5, escenario 3. | Confirmación del responsable, coste de minutos, gestión de licencia Unity en CI. |
| **D-05** | Formato de definición de contratos | p. ej. esquemas JSON, Protocol Buffers u otro con generación para C# y el runtime elegido. | FR-021(b) y la primera definición de contrato; **no** bloquea esta funcionalidad. | Depende de D-01 (soporte en el runtime) y de las necesidades de tamaño/frecuencia de mensajes en directo. |
| **D-06** | Nombre de producto e identificador de aplicación definitivos | — | Nada en esta fase: se usan valores provisionales documentados. | Antes de cualquier publicación o configuración de firma. |
| **D-08** | Uso de Git LFS para binarios | Usar LFS / no usarlo. | Nada en esta fase (no hay binarios). | Antes del primer recurso de arte/audio. |
| **D-09** | Herramienta de formato/análisis automático de C# | Verificar en el plan qué herramienta funciona con los proyectos generados por la versión de Unity fijada. | Solo la comprobación automática de formato C# (FR-041). | **Aplazada** (research R-7): solo `.editorconfig` hasta D-04 o hasta que haya código de ejecución en el cliente. |

### Key Entities

- **Área del repositorio**: carpeta de primer nivel con propósito, límites y README propios.
- **Configuración local**: conjunto de variables (secretos y puertos) que cada persona define a
  partir del ejemplo versionado; nunca se versiona.
- **Entorno local**: servicios de backend (base de datos y servidor de juego) con estados
  arrancado, detenido (con datos) y limpio (sin datos).
- **Registro de decisión (ADR)**: contexto, opciones, decisión, consecuencias, estado y fecha.
- **Decisión pendiente**: opciones, qué bloquea, información necesaria y momento límite.
- **Contrato**: definición versionada de un mensaje/RPC/evento entre cliente y servidor (en esta
  fase solo sus reglas, sin instancias).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Una persona que no participó en la creación de la base pone en marcha el backend
  local y supera la verificación de conectividad siguiendo solo la documentación, en ≤ 15 minutos
  sin contar descargas de herramientas e imágenes.
- **SC-002**: 3 ciclos consecutivos de arrancar → verificar → detener → arrancar → verificar
  terminan con éxito y conservan los datos; tras la limpieza, el siguiente arranque parte de un
  estado vacío.
- **SC-003**: Con el entorno en marcha, se exponen exactamente 2 puntos de acceso en el equipo
  anfitrión, ambos solo en la interfaz local; la base de datos no es accesible desde el anfitrión.
- **SC-004**: 0 secretos y 0 datos locales en el historial de la rama, comprobado por la
  verificación de FR-039.
- **SC-005**: Cada caso límite de configuración (falta la configuración local, falta un secreto,
  servicios detenidos, puerto ocupado) produce un mensaje que permite identificar la causa sin
  leer código, en el 100 % de los casos probados.
- **SC-006** *(tras D-02)*: Desde un clon limpio, el cliente abre con 0 errores de compilación, el
  100 % de las pruebas EditMode y PlayMode pasan, y se obtienen la build de desarrollo Android y
  el proyecto Xcode de iOS sin errores.
- **SC-007** *(tras D-02)*: Abrir y cerrar el cliente sin cambios deja 0 archivos modificados o
  nuevos en Git.
- **SC-008**: El 100 % de las áreas versionadas tienen README o contenido propio; 0 carpetas
  vacías o con solo marcadores.
- **SC-009**: El 100 % de las decisiones pendientes aparecen en un único documento con qué
  bloquean y qué información falta; 0 procedimientos descritos de forma divergente en más de un
  documento.
- **SC-010**: 0 referencias a un runtime de servidor concreto en configuración versionada
  mientras D-01 siga abierta.

## Assumptions

- La pila (Unity/C#, Nakama, PostgreSQL, monorepo) y las convenciones de la constitución v1.1.0
  son definitivas para esta funcionalidad.
- Se conservan los nombres de carpeta ya presentes en local; `shared/` se reserva solo para
  contratos (alternativa descartada: crear `contracts/`, por coherencia con lo existente).
- El entorno local se usa solo en máquinas de desarrollo; no es base para despliegue de
  producción (fuera de alcance), y `deploy/` solo contendrá `local/` en esta fase.
- Las personas desarrolladoras usan macOS (necesario para exportar iOS) o Windows/Linux para
  Android y backend; la exportación de iOS solo se valida en macOS con Xcode.
- Docker y Docker Compose son aceptables como requisito previo para el backend local (están
  disponibles en el entorno actual).
- Los identificadores de aplicación y nombre de producto son provisionales (D-06).
- La conectividad básica del backend se comprueba desde el equipo anfitrión mediante la
  comprobación de salud de Nakama (confirmado en Clarifications).
- Licencias, firma de aplicaciones, cuentas de desarrollador de tiendas y publicación quedan fuera
  de alcance.

## Out of Scope

- Mecánicas, reglas, balance, economía o contenido del juego.
- Cuentas, autenticación completa, matchmaking, partidas, chat, clasificación o progresión.
- Compras, anuncios, analítica, publicación en tiendas, firma de builds.
- Arte, diseño visual final, animaciones y sonido.
- Despliegue de producción, dominios, escalado, alta disponibilidad, monitorización avanzada.
- Elección del runtime de Nakama, del proveedor de CI, del formato de contratos o de servicios
  externos (se documentan como decisiones pendientes).
- Migraciones propias de PostgreSQL (no hay esquema propio en esta fase).
