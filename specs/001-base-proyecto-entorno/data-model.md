# Data Model: Base del proyecto y entorno de desarrollo

Esta funcionalidad no tiene datos de dominio. Las "entidades" son los elementos de configuración
y de gobierno que define la spec (sección *Key Entities*). Aquí se recogen sus campos, reglas de
validación y transiciones de estado.

## Área del repositorio

| Campo | Regla |
|-------|-------|
| Ruta | De primer nivel, en minúsculas y con guiones: `client-unity`, `server-nakama`, `shared`, `deploy`, `docs`, `tools`. |
| README | Obligatorio si el área aún no tiene contenido técnico (FR-002). Debe incluir propósito, límites, estado y la decisión que la desbloquea. |
| Dependencias | `client-unity` y `server-nakama` solo dependen de `shared`, nunca el uno del otro (FR-004). |

## Configuración local (`deploy/local/.env`)

Definición completa de las variables en [contracts/local-environment.md](contracts/local-environment.md).

| Regla | Origen |
|-------|--------|
| **no versionado**: `.env` no se versiona; `.env.example` sí, con valores ficticios evidentes. | FR-028, Principio VII |
| **secretos obligatorios**: si una variable secreta falta o está vacía, el arranque aborta nombrándola. | FR-018 |
| **POSTGRES_PASSWORD**: alfanumérica. | research R-4 |
| **claves de cifrado de sesión**: cadena no vacía. Nakama 3.42.0 no exige una longitud concreta (verificado al arrancar con los valores de `.env.example` y con claves de 64 caracteres hexadecimales). Se recomiendan valores aleatorios (`openssl rand -hex 32`). | Verificado en implementación (T015) |
| **puertos opcionales**: tienen un valor por defecto documentado. | FR-016 |

## Entorno local

Servicios: `postgres` y `nakama`. Estados y transiciones:

```text
            up -d --wait                      stop
[limpio] ───────────────► [arrancado] ───────────────► [detenido (con datos)]
   ▲                         │  ▲                           │
   │                         │  └────── start / up ─────────┘
   │        down -v          │
   └─────────────────────────┴──────────── down -v ─────────┘
```

| Estado | Condición observable |
|--------|----------------------|
| limpio | No hay contenedores ni volumen del proyecto. |
| arrancado | Ambos servicios están sanos y `check-backend.sh` devuelve 0. |
| detenido | Los contenedores están parados, el volumen existe y `check-backend.sh` devuelve un código distinto de 0. |
| fallido | `up --wait` devuelve un código distinto de 0: falta configuración, el puerto está ocupado, la imagen no está disponible o la migración falla. Se sale con `down` o `down -v`. |

Invariantes:
- `nakama` nunca queda sano si `postgres` no lo está.
- Las migraciones solo se aplican con `nakama migrate up`.

## Registro de decisión (ADR)

| Campo | Regla |
|-------|-------|
| Número | Cuatro dígitos, secuencial y sin reutilizar. |
| Título | Corto y en minúsculas con guiones dentro del nombre del archivo. |
| Estado | `propuesta` → `aceptada` → (`sustituida por NNNN` \| `obsoleta`). |
| Fecha | ISO 8601. |
| Secciones | Contexto, Opciones consideradas, Decisión, Consecuencias. |

## Decisión pendiente (`docs/decisions/pendientes.md`)

| Campo | Regla |
|-------|-------|
| ID | `D-NN`, estable (el mismo que en la spec). |
| Opciones | Al menos dos, o el criterio de elección. |
| Bloquea | Requisitos o áreas concretas. |
| Información necesaria | Qué hace falta para resolverla. |
| Límite | Antes de qué hito debe resolverse. |

Transición: cuando se resuelve una decisión, se crea su ADR y la entrada de `pendientes.md` se
sustituye por un enlace a ese ADR **en el mismo commit**.

## Contrato (solo reglas en esta fase)

No hay instancias. Las reglas (versionado, errores y compatibilidad) se documentan en
`shared/README.md` según FR-021. El formato queda pendiente (D-05).
