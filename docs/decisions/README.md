# Registro de decisiones de arquitectura (ADR)

Las decisiones técnicas importantes se registran aquí con su contexto, las opciones
consideradas, la decisión y sus consecuencias, como exige la constitución del proyecto.

## Índice

| Nº | Decisión | Estado |
|----|----------|--------|
| [0000](0000-plantilla.md) | Plantilla | — |
| [0001](0001-estructura-monorepo.md) | Estructura del monorepo | aceptada |
| [0002](0002-entorno-local-nakama-postgresql.md) | Entorno local con Docker Compose: Nakama 3.42.0 y PostgreSQL 16.16 | aceptada |
| [0003](0003-pipeline-render-urp.md) | Pipeline de render del cliente: URP | aceptada |
| [0004](0004-conectividad-sin-sdk-cliente.md) | Verificar la conectividad del backend sin SDK en el cliente | aceptada |
| [0005](0005-unity-63-lts.md) | Versión de Unity: 6.3 LTS | aceptada |
| [0006](0006-runtime-nakama-typescript.md) | Runtime de los módulos de Nakama: TypeScript | aceptada |
| [0008](0008-contratos-json-versionados.md) | Formato de los contratos cliente-servidor: JSON versionado | aceptada |

Las decisiones todavía abiertas están en [pendientes.md](pendientes.md).

## Crear un ADR

1. Copia [0000-plantilla.md](0000-plantilla.md) como `NNNN-titulo-con-guiones.md`, con el
   siguiente número libre de cuatro dígitos. Los números no se reutilizan.
2. Rellena contexto, opciones, decisión y consecuencias, con estado `propuesta` o `aceptada` y la
   fecha en formato ISO (`AAAA-MM-DD`).
3. Añádelo a la tabla del índice.

## Resolver una decisión pendiente

En **el mismo commit**:
1. Crea el ADR de la decisión.
2. Quita su entrada de la sección *Abiertas* de [pendientes.md](pendientes.md) y añade una fila
   en *Resueltas* que enlace al ADR.
3. Actualiza la documentación que dependía de esa decisión.

## Cambiar una decisión aceptada

No se edita para cambiar su contenido. Se crea un ADR nuevo que la sustituye y se cambia el
estado de la anterior a `sustituida por NNNN`.
