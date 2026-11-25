# Contrato: grupos de pruebas de Unity y modo batch

Con `UNITY="/Applications/Unity/Hub/Editor/6000.3.29f1/Unity.app/Contents/MacOS/Unity"`, desde la
raíz del repositorio:

| Grupo | Necesita | Comando |
|-------|----------|---------|
| EditMode sin servidor | Nada | `"$UNITY" -batchmode -nographics -projectPath client-unity -runTests -testPlatform EditMode -assemblyNames Ecosystem.Tests.EditMode -testResults editmode.xml -logFile -` |
| PlayMode sin servidor | Nada | `"$UNITY" -batchmode -projectPath client-unity -runTests -testPlatform PlayMode -assemblyNames Ecosystem.Tests.PlayMode -testResults playmode.xml -logFile -` |
| Integración | Entorno local con módulo (`tools/check-backend.sh --module` → 0) y configuración ([client-config.md](client-config.md)) | `"$UNITY" -batchmode -nographics -projectPath client-unity -runTests -testPlatform EditMode -assemblyNames Ecosystem.Tests.Integration -testResults integration.xml -logFile -` |

- Código de salida: `0` si todo pasa y `2` si alguna prueba falla. No se usa `-quit`.
- **Sin servidor**: los dos primeros grupos pasan con el entorno local apagado (SC-006).
- **Integración sin servidor**: falla en ≤ 15 s con «Entorno local no disponible: consulta
  deploy/local/README.md», sin colgarse (FR-025).
- **Para llevarlo a la CI** (no se configura ahora): los mismos comandos, con la licencia de Unity
  como secreto y una acción o imagen de Unity fijada por versión. Las pruebas de integración se
  ejecutarían en el trabajo `backend`, que ya arranca el entorno, con la configuración pasada por
  las variables `ECOSYSTEM_NAKAMA_*`.
