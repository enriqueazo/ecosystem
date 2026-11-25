# 0005 · Versión de Unity: 6.3 LTS

- **Estado**: aceptada
- **Fecha**: 2026-11-25

## Contexto

La constitución exige fijar la versión de Unity antes de la primera compilación del cliente
(principio VIII, decisión D-02). El cliente es para Android e iOS y se crea con URP
([ADR-0003](0003-pipeline-render-urp.md)).

## Opciones consideradas

- **Unity 6.3 LTS** (rama `6000.3`): la LTS vigente, con soporte hasta diciembre de 2027 según
  Unity.
- **Esperar a Unity 6.7 LTS**: soporte más largo, pero aún sin publicar y sin fecha cierta;
  bloquearía el cliente.
- **Unity 6.6**: versión *Update*, sin soporte largo.
- **Unity 6.0 LTS**: su soporte estándar terminó en octubre de 2026.

## Decisión

Se usa **Unity 6.3 LTS**. La revisión exacta queda fijada por
`client-unity/ProjectSettings/ProjectVersion.txt`. Revisión inicial: **6000.3.29f1**.

Módulos necesarios, verificados en la instalación inicial:

- *Android Build Support*, con OpenJDK, Android SDK y NDK incluidos.
- *iOS Build Support*, más Xcode en macOS para abrir el proyecto exportado.

Licencia: **Unity Personal**, de forma provisional. Cambiar de plan (p. ej. a Pro) no cambia
esta decisión de versión, pero sí afecta a la ejecución de Unity en CI (D-04) y a los requisitos
de uso de Unity Personal. Debe revisarse antes de configurar la CI o de publicar.

## Consecuencias

- El proyecto se abre solo con la revisión indicada en `ProjectVersion.txt`. Abrirlo con otra
  revisión y confirmar el cambio está prohibido: actualizar Unity, incluso a otra revisión de
  6000.3, es un cambio propio y explícito que actualiza este registro o crea uno que lo
  sustituya.
- Las versiones de los paquetes (URP, Test Framework) las fija el lockfile que resuelve esta
  versión.
- La ejecución en CI con licencia sigue pendiente (D-04).
