# 0009 · SDK de Nakama en el cliente Unity y sesión de invitado

- **Estado**: aceptada
- **Fecha**: 2026-11-25
- **Sustituye a**: [0004](0004-conectividad-sin-sdk-cliente.md)

## Contexto

ADR-0004 dejó el cliente sin SDK de Nakama hasta la primera funcionalidad que lo necesitara. La
spec 003 conecta por primera vez el cliente con el servidor mediante una sesión de invitado, base
de la identidad que exige el principio I. El responsable decidió solo invitado por ahora, pero sin
impedir un futuro sistema de cuentas. Las pruebas de Unity quedan en local, preparadas para la CI.

## Opciones consideradas

- **SDK oficial `com.heroiclabs.nakama-unity`**: incluye autenticación, sesión, refresco, RPC,
  reintentos y cancelación, y su adaptador HTTP está pensado para Unity.
- **Cliente HTTP propio**: reimplementaría la API de Nakama sin ninguna ventaja.

Para guardar la sesión:
- **`PlayerPrefs`**: el patrón que documenta el SDK.
- **Almacenamiento seguro (Keychain o Keystore)**: exige plugins nativos y probar en dispositivos.

## Decisión

- **SDK**: `com.heroiclabs.nakama-unity` **3.23.0**, instalado por UPM con la URL de Git fijada al
  commit `704f5da244a49d942f383965f5549420522eb8e0` (etiqueta `v3.23.0`). Lo registra
  `packages-lock.json`. Requiere Unity 2022.3 o superior.
- **Identidad**:
  - autenticación por dispositivo con un **identificador de instalación aleatorio** (GUID),
    guardado localmente y nunca derivado del hardware;
  - la identidad del jugador es su **`userId`** de Nakama.
- **Punto de extensión**: la autenticación va detrás de `IAuthenticator`, con un único
  autenticador (`GuestDeviceAuthenticator`). Las futuras credenciales (correo, Google, Apple, Game
  Center) se vincularán a la misma cuenta sin migrar datos.
- **Sesión**:
  - se guarda en `PlayerPrefs` (claves `ecosystem.session.*`);
  - si sigue vigente, se reutiliza; si ha caducado, se refresca; si no se puede renovar, se
    reautentica la misma instalación;
  - caducidades del servidor: **2 h** la sesión y **7 días** el refresco (`deploy/local/compose.yaml`).
- **Arquitectura**:
  - un núcleo `Ecosystem.Connection` sin Unity ni Nakama (puertos y dobles);
  - adaptadores en `Ecosystem.Connection.Nakama`;
  - la escena `Bootstrap` con `ConnectionScreen` (IMGUI provisional).
- **Reintentos**: los gestiona el núcleo (3, con esperas de 1, 2 y 4 s) y se desactivan los del SDK
  y su refresco automático. Tiempo límite de 10 s por petición.
- **Pruebas de integración en PlayMode**: `UnityWebRequestAdapter` es un `MonoBehaviour` con
  corrutinas y no funciona en EditMode.
- **Verificación**: solo en el editor contra `127.0.0.1`. Nakama no se expone fuera.

## Consecuencias

- **Reinstalar la app crea una cuenta de invitado nueva.** Es la limitación aceptada hasta la
  vinculación de cuentas.
- **Revisión obligatoria**: al añadir cuentas registradas, sus credenciales justificarán un
  almacenamiento seguro en lugar de `PlayerPrefs`.
- **Códigos de error verificados** con Nakama 3.42.0, que el adaptador traduce:
  - clave de servidor errónea → 401;
  - refresco inválido → 401;
  - cuenta deshabilitada → 403.
- **Actualizar el SDK** es un cambio propio: nuevo commit fijado, revisión de las API usadas y
  pruebas de integración.
- **Builds de publicación**: no llevan configuración de servidor. Inyectarla queda para una spec
  posterior.
