# Contrato: configuración de conexión del cliente

## Fuentes, por orden de precedencia

0. Si `ECOSYSTEM_NAKAMA_CONFIG=none`, se ignoran el archivo y las demás variables y el estado es
   `ConfigurationError`. Lo usan las pruebas «sin servidor» para no depender de la configuración
   local.
1. **Variables de entorno**, si están todas definidas. Las usarían la CI o las pruebas en modo
   batch:

   | Variable | Campo |
   |----------|-------|
   | `ECOSYSTEM_NAKAMA_HOST` | `host` |
   | `ECOSYSTEM_NAKAMA_PORT` | `port` |
   | `ECOSYSTEM_NAKAMA_SERVER_KEY` | `serverKey` |
   | `ECOSYSTEM_NAKAMA_USE_SSL` | `useSsl` (`true`/`false`; por defecto, `false`) |

2. **Archivo local** `client-unity/LocalConfig/connection.local.json`, **ignorado por Git**:

   ```json
   { "host": "127.0.0.1", "port": 7350, "serverKey": "<NAKAMA_SERVER_KEY de deploy/local/.env>", "useSsl": false }
   ```

   Se crea copiando `client-unity/LocalConfig/connection.example.json` (versionado, con valores
   ficticios).

3. Si no hay ninguna, el estado es `ConfigurationError`.

## Reglas

- Las reglas de cada campo están en [data-model.md](../data-model.md) → *Configuración de
  conexión*. Una configuración parcial o inválida es `ConfigurationError`, con el campo concreto
  en el registro de desarrollo y sin su valor.
- `LocalConfig/` está fuera de `Assets/`: no se empaqueta ni genera `.meta`. Las builds de esta
  spec no llevan configuración.
- `tools/check-repo.sh --only secrets` falla si `connection.local.json` está en el índice de Git.
- La clave es la `NAKAMA_SERVER_KEY` del `deploy/local/.env` de cada desarrollador. Es la clave
  pública de cliente de Nakama, no la `http_key`, que nunca va en el cliente.
