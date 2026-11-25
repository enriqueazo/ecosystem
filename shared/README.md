# Contratos cliente-servidor

Esta área contiene **solo** los contratos versionados entre el cliente Unity y el servidor
Nakama: definiciones de mensajes, RPC y eventos, más su documentación y sus pruebas de
compatibilidad. No es un lugar para código de utilidad compartido. Cliente y servidor dependen de
`shared/`, nunca el uno del otro ([ADR-0001](../docs/decisions/0001-estructura-monorepo.md)).

> **Estado**: todavía no hay contratos. Este documento fija las reglas que debe cumplir el
> primero. No se inventan mensajes de juego antes de que una funcionalidad los defina.

## a. Organización

Cuando exista el primer contrato:

- Cada contrato vive en un archivo propio, agrupado por dominio funcional (p. ej. cuentas,
  partida). Su nombre coincide con el identificador del contrato.
- Junto a las definiciones van sus pruebas de serialización, validación y compatibilidad
  (constitución, principio VI).
- La estructura concreta de carpetas se decide con el formato (D-05) y se documenta aquí en el
  mismo cambio.

## b. Formato de definición — pendiente

El formato está **pendiente** ([D-05](../docs/decisions/pendientes.md#d-05--formato-de-definición-de-contratos)).
Criterios de elección:

- poder generar o validar código para C# (Unity) y para el runtime de Nakama elegido (D-01);
- definir campos obligatorios, tipos, límites (longitud, rangos) y valores enumerados;
- coste de los mensajes en directo (tamaño y frecuencia);
- herramientas de comprobación de compatibilidad entre versiones.

## c. Versionado

- **Versión de protocolo**: un entero que el cliente envía al establecer la sesión. Sube cuando
  hay un cambio incompatible en cualquier contrato. El servidor declara qué versiones acepta.
- **Versión de contrato**: cada contrato tiene versión propia (entero). Sube con cualquier cambio
  de su definición.
- **Cambio compatible**: añadir un campo opcional, o añadir un valor de enumeración que los
  receptores antiguos pueden ignorar. Sube la versión del contrato, no la del protocolo.
- **Cambio incompatible**: eliminar o renombrar un campo, cambiar un tipo, hacer obligatorio un
  campo opcional, endurecer un límite o cambiar el significado de un valor. Sube la versión del
  contrato **y** la del protocolo.
- *Pendiente de D-05*: cómo se codifica la versión dentro del mensaje y cómo se comprueba
  automáticamente la compatibilidad.

## d. Errores

Toda respuesta de error usa la misma estructura:

| Campo | Regla |
|-------|-------|
| `code` | Identificador estable en texto (p. ej. `INVALID_ARGUMENT`, `UNSUPPORTED_PROTOCOL_VERSION`). Nunca cambia de significado. |
| `message` | Descripción breve para depuración. No se muestra tal cual al jugador y no contiene datos personales, secretos, trazas ni detalles internos. |
| `details` | Opcional. Datos mínimos y no sensibles que ayudan al cliente a reaccionar (p. ej. el campo inválido). |

El cliente decide qué mostrar al jugador a partir de `code`, no de `message`.

## e. Compatibilidad

Reglas de la constitución (principio II):

- Los receptores **toleran campos desconocidos** y los ignoran.
- Los receptores **rechazan mensajes inválidos** (formato, tamaño, límites, permisos o estado) con
  un error definido; nunca los procesan parcialmente.
- Ante una versión de protocolo antigua o incompatible, el servidor responde con un error
  definido (`UNSUPPORTED_PROTOCOL_VERSION`) que indica al cliente que debe actualizarse.
- Los mensajes llevan solo los datos que el receptor necesita, nunca objetos internos completos.
- No se envían actualizaciones de red por fotograma: la frecuencia se define según el diseño.
- El servidor es la autoridad: valida todo valor crítico que llegue del cliente (principio I).

## f. Checklist para añadir o cambiar un contrato

En **un mismo cambio**:

- [ ] Definición del contrato con versión, campos obligatorios, límites, errores y estados.
- [ ] Clasificación del cambio (compatible o incompatible) y versiones actualizadas según *c*.
- [ ] Implementación en el servidor, con validación completa de la entrada.
- [ ] Implementación en el cliente.
- [ ] Pruebas de serialización, validación y compatibilidad, que demuestran que cliente y
      servidor interpretan igual los mensajes.
- [ ] Comportamiento ante desconexión, reintento y duplicados, si el contrato es de tiempo real
      (principio III).
- [ ] Documentación actualizada.
