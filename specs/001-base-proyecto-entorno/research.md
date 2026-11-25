# Research: Base del proyecto y entorno de desarrollo

**Fecha**: 2026-11-25 · **Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)

Cada apartado resuelve una incógnita del Technical Context o una decisión pendiente de la spec
que el plan puede cerrar. Las decisiones que requieren confirmación del responsable se marcan
como **PROPUESTA** y no se dan por resueltas hasta registrarse en su ADR.

## R-1 · Versiones de Nakama y PostgreSQL (D-03) — RESUELTA

- **Decisión**: Nakama `3.42.0` (imagen `registry.heroiclabs.com/heroiclabs/nakama:3.42.0`) y
  PostgreSQL `16.16` (imagen `postgres:16.16-alpine`). Se fija también el digest del índice
  multiarquitectura de cada imagen (registrado en ADR-0002).
- **Fundamento**:
  - `3.42.0` es la última versión publicada (GitHub `releases/latest`, 2026-11-03).
  - La plantilla oficial `docker-compose-postgres.yml` de Heroic Labs usa la rama mayor
    PostgreSQL 16 (`16.8-alpine`), y el equipo de Heroic Labs confirma en su foro que no hay
    problemas de compatibilidad con PG 16 o superior. `16.16` es la última versión menor de la
    rama 16 publicada en Docker Hub en la fecha de comprobación: misma rama mayor validada por el
    proveedor y con los parches más recientes.
  - Se ha comprobado con `docker manifest inspect` que ambas imágenes publican variantes
    `amd64` y `arm64` (cubre el caso límite de arquitectura de la spec).
- **Alternativas consideradas**:
  - PostgreSQL 17/18: probablemente compatibles, pero la plantilla oficial no las usa; la
    combinación sin respaldo explícito no compensa en la base. Se podrá actualizar con un
    cambio propio (Principio VIII).
  - CockroachDB (compose por defecto de Nakama): descartada; la constitución fija PostgreSQL.
  - Etiquetas flotantes (`latest`, `16-alpine`): prohibidas por FR-013.
- **Registro**: ADR-0002.

## R-2 · Versión de Unity (D-02) — PROPUESTA, requiere confirmación

- **Propuesta**: Unity **6.3 LTS** (línea `6000.3`), con la última revisión `f` disponible en
  Unity Hub en el momento de instalar; la revisión exacta queda fijada por
  `ProjectSettings/ProjectVersion.txt` y se registra en ADR-0005.
- **Fundamento**: es la LTS vigente (soporte hasta diciembre de 2027 según la página oficial de
  Unity); Unity 6.0 LTS terminó su soporte estándar en octubre de 2026; 6.6 es una versión
  *Update* sin soporte largo, y 6.7 LTS aún no está publicada.
- **Alternativas**: esperar a 6.7 LTS (soporte más largo, pero no disponible: bloquearía la
  Historia 2 sin fecha cierta); 6.6 Update (sin LTS, en contra de la estabilidad buscada).
- **Bloqueo**: la Historia 2 no puede ejecutarse hasta que el responsable confirme y se instale
  Unity Hub + editor + módulos *Android Build Support* (con OpenJDK y SDK/NDK) e *iOS Build
  Support*. Xcode ya está instalado en la máquina actual.

## R-3 · Orquestación local — RESUELTA

- **Decisión**: un único `deploy/local/compose.yaml` con los servicios `postgres` y `nakama`,
  basado en la plantilla oficial pero endurecido:
  - Sin `container_name` fijo, sin `links` y con `name:` de proyecto propio para evitar
    colisiones con otros proyectos.
  - `nakama` ejecuta `migrate up` (mecanismo soportado) y después arranca; `depends_on` con
    `condition: service_healthy` sobre `postgres`.
  - Comprobaciones de salud: `pg_isready` en `postgres` y `nakama healthcheck` en `nakama`.
  - Puertos publicados solo para la API de cliente y la consola, ligados a `127.0.0.1` y
    configurables. `postgres` no publica puertos; el puerto gRPC de Nakama no se publica (la
    plantilla oficial publica todos, incluidos 5432 y 8080, contrario a FR-016).
  - Volumen con nombre para los datos de PostgreSQL (fuera del árbol de trabajo); `down -v` lo
    elimina.
  - **Sin** montaje de `/nakama/data` ni de módulos (FR-020); la plantilla oficial monta `./`,
    lo que cargaría módulos de un runtime.
  - Política de reinicio `unless-stopped` o ninguna: `restart: always` de la plantilla oculta
    fallos de arranque en desarrollo.
- **Comandos**: Docker Compose estándar desde `deploy/local/` (`up -d --wait`, `ps`, `logs`,
  `stop`, `start`, `down`, `down -v`). `--wait` devuelve error si algún servicio no queda sano.
  No se añade envoltorio (R-01 de la spec se descarta: los comandos estándar son cortos y
  ejecutarlos desde `deploy/local/` evita rutas largas).
- **Versiones de herramienta**: verificado con Docker Engine 29.2.1 y Docker Compose v5.0.2
  (macOS arm64). Versiones anteriores no se han comprobado; el README lo indica así en vez de
  inventar un mínimo.

## R-4 · Secretos y configuración local — RESUELTA

- **Decisión**: `deploy/local/.env.example` (versionado) → copia local `deploy/local/.env`
  (ignorada). Compose lee `.env` del directorio del proyecto automáticamente.
- Variables obligatorias con interpolación `${VAR:?mensaje}`: Compose aborta **antes** de crear
  contenedores nombrando la variable y el mensaje indica copiar `.env.example`. Cubre los casos
  "falta `.env`" y "secreto vacío o ausente".
- Los secretos llegan a Nakama como flags de arranque (`--socket.server_key`,
  `--runtime.http_key`, `--console.username`, `--console.password`, `--console.signing_key`,
  `--session.encryption_key`, `--session.refresh_encryption_key`) y como parte de
  `--database.address`. No hay archivo de configuración YAML versionado con secretos; los
  ajustes no secretos (nombre de nodo, nivel de log, caducidad de sesión) también van como flags
  para no crear un archivo adicional.
- **Restricción documentada**: la contraseña de PostgreSQL forma parte de una dirección tipo
  URL, así que debe ser alfanumérica.
- **Riesgo a verificar en implementación**: confirmar en los registros de arranque de Nakama
  3.42.0 que no se imprimen claves ni la contraseña con el nivel de log elegido (Principio VII).
  Si se imprimen, se baja el nivel de log y se documenta.

## R-5 · Verificación de conectividad (FR-038, Q1) — RESUELTA

- **Decisión**: script `tools/check-backend.sh` que hace una petición HTTP a la ruta de
  comprobación de salud de la API de cliente de Nakama en `127.0.0.1` y al puerto configurado.
  Devuelve 0 si responde correctamente; si no, devuelve un código distinto de 0 con un mensaje
  que distingue "Docker no disponible", "servicios no arrancados" y "Nakama no responde".
- **Dependencias**: `bash` y `curl` (presentes por defecto en macOS y en distribuciones Linux
  habituales); sin herramientas nuevas.
- **Alternativa descartada**: conexión desde Unity con el SDK (rechazada en Clarifications).

## R-6 · Comprobaciones locales del repositorio (FR-039) y su prueba (TDD) — RESUELTA

- **Decisión**: script `tools/check-repo.sh` con tres comprobaciones independientes:
  (a) `docker compose config --quiet` usando `.env.example` como archivo de entorno;
  (b) ningún `.env` (salvo `.env.example`) ni dato local está en el índice de Git;
  (c) `git check-ignore` confirma que los archivos imprescindibles no están ignorados.
- **TDD (Principio VI)**: la lógica de los scripts es comprobable, así que se escribe primero
  `tools/tests/run.sh`, un ejecutor en bash sin dependencias que crea repositorios Git
  temporales de prueba (caso válido y cada caso de fallo) y comprueba los códigos de salida y
  mensajes. `check-backend.sh` se prueba contra un puerto sin servicio (fallo) y documenta su
  caso de éxito como verificación de integración manual con el entorno arrancado.
- **Alternativas descartadas**: `bats` u otro framework (añade una dependencia sin necesidad
  demostrable en esta fase); `pre-commit` (herramienta adicional, depende de Python).

## R-7 · Formato y análisis de C# (D-09) — RESUELTA (aplazamiento justificado)

- **Decisión**: en esta fase solo `.editorconfig` con reglas de formato y de nomenclatura de
  .NET, que aplican Rider, Visual Studio y VS Code. No se añade comprobación automática.
- **Fundamento**: herramientas como `dotnet format` necesitan los `.csproj` que genera el editor
  de Unity (están ignorados y no existen sin abrir el editor). Una comprobación en CI requeriría
  ejecutar Unity en modo batch, que depende de licencia (D-04). Además, en esta funcionalidad
  solo hay código de pruebas.
- **Reapertura**: al resolver D-04 y la licencia de CI, o cuando exista código de ejecución en
  el cliente.

## R-8 · Pruebas del cliente Unity — RESUELTA

- **EditMode** (ensamblado solo para Editor, referencia a Unity Test Framework):
  - la escena inicial es la única escena de la build y está en la posición 0;
  - el backend de scripting de Android e iOS es IL2CPP;
  - la arquitectura de Android es solo ARM64;
  - la serialización de assets es *Force Text*;
  - el pipeline de render activo es un asset de URP;
  - el identificador de aplicación provisional está definido para Android e iOS.
- **PlayMode**: carga la escena inicial y comprueba que no se registran errores ni excepciones.
- **Red→Green**: las pruebas se crean con el proyecto recién generado desde la plantilla y deben
  fallar (p. ej. escena por defecto, Mono, otras arquitecturas) antes de aplicar la
  configuración.
- **Verificación de API**: los nombres exactos de las API de `PlayerSettings`,
  `EditorBuildSettings`, `EditorSettings` y `GraphicsSettings` se confirman contra la
  documentación de la versión fijada en R-2 antes de escribir las pruebas (Principio V).
- **Modo batch** (documentado, R-03 de la spec): `-batchmode -projectPath client-unity
  -runTests -testPlatform EditMode|PlayMode -testResults <ruta>`, sin `-quit`, porque
  `-runTests` termina el proceso por sí mismo.

## R-9 · Proyecto Unity con URP (D-07) — RESUELTA

- **Decisión**: crear el proyecto desde la plantilla **Universal 3D** de Unity Hub, sin muestras
  de la plantilla. Después:
  - eliminar los recursos de muestra que incluye la plantilla (escena de ejemplo, materiales,
    texturas, `TutorialInfo`, `Readme`);
  - conservar solo un par de assets de URP (pipeline + renderer) en `Assets/_Project/Settings/`;
  - quitar del manifiesto los paquetes no usados: se dejan URP, Test Framework y las
    dependencias que URP exija, y la lista exacta se documenta en el README del cliente.
- **Nota**: `Assets/_Project/Settings/` amplía la estructura mínima de FR-007 porque los assets de
  URP necesitan una ubicación con contenido real; es coherente con FR-002.
- **Input System**: si la plantilla lo añade y no hay código que lo use, se elimina; vuelve con
  la primera funcionalidad de controles.

## R-10 · Gitignore y gitattributes — RESUELTA

- **`.gitignore`**:
  - quitar la sección Go (C-1) y las rutas `/deploy/data/` y `/deploy/volumes/`, que no se usan
    con el volumen con nombre;
  - limitar `*.csproj`, `*.sln` y similares a `/client-unity/` (C-2);
  - añadir `.vscode/`, la carpeta `.utmp/` de Unity, builds móviles (`*.apk`, `*.aab`, `*.ipa`,
    export de Xcode bajo `Builds/`), `*.unitypackage` y `crashlytics-build.properties`;
  - conservar `.env` / `.env.*` / `!.env.example`.
- **`.gitattributes`**: `* text=auto eol=lf`; `*.cs`, `*.unity`, `*.prefab`, `*.asset`, `*.meta`
  como texto; `*.sh` con `eol=lf`; tipos binarios habituales (`png`, `jpg`, `psd`, `fbx`, `wav`,
  `mp3`, `ogg`, `ttf`, `otf`) como `binary`.
- **Git LFS (D-08)**: no se activa; sigue pendiente hasta el primer recurso binario.

## R-11 · CI (D-04) — SIGUE PENDIENTE

- No se crea ningún flujo de CI. El README indica "CI no configurada" y enlaza a D-04.
- Cuando se decida el proveedor, el flujo mínimo es ejecutar `tools/tests/run.sh` y
  `tools/check-repo.sh` en cada PR, con versiones de acciones o imágenes fijadas. Ninguna de las
  dos comprobaciones necesita secretos.

## Fuentes

- Nakama: notas de versión en GitHub (`releases/latest` = v3.42.0) y la plantilla
  `docker-compose-postgres.yml` del repositorio `heroiclabs/nakama`.
- Foro de Heroic Labs: compatibilidad de PostgreSQL 16 o superior.
- Docker Hub: etiquetas de `postgres` (`16.16-alpine`), con manifiestos comprobados localmente.
- Unity: página "Unity 6 Releases & Support" (estado de las LTS).
