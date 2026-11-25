using Ecosystem.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ecosystem.Connection
{
    /// <summary>
    /// Compara la versión de protocolo del servidor con la del cliente, validando antes la respuesta
    /// con el contrato generado (FR-010, FR-011). Una respuesta que no cumple el contrato es una
    /// incompatibilidad, no un error transitorio.
    /// </summary>
    public static class ProtocolCompatibility
    {
        public static ConnectionState Evaluate(string responseJson, int clientProtocolVersion)
        {
            JToken response;
            try
            {
                response = JToken.Parse(responseJson ?? string.Empty);
            }
            catch (JsonReaderException)
            {
                return ConnectionState.ServerIncompatible;
            }

            if (!ProtocolVersionContract.ValidateResponse(response))
            {
                return ConnectionState.ServerIncompatible;
            }

            var serverVersion = (int)response["protocolVersion"];
            if (serverVersion == clientProtocolVersion) return ConnectionState.Connected;
            return serverVersion > clientProtocolVersion ? ConnectionState.UpdateRequired : ConnectionState.ServerIncompatible;
        }
    }

    /// <summary>
    /// Error estándar de shared/README.md → Errores: el código del contrato viaja como JSON dentro de
    /// "message"; el campo "error" de Nakama (traza) se ignora siempre (FR-012).
    /// </summary>
    public static class StandardError
    {
        public static bool TryParseCode(string responseBody, out string code)
        {
            code = null;
            try
            {
                var body = JToken.Parse(responseBody ?? string.Empty) as JObject;
                var message = body?["message"];
                if (message == null || message.Type != JTokenType.String) return false;
                var inner = JToken.Parse((string)message) as JObject;
                var innerCode = inner?["code"];
                if (innerCode == null || innerCode.Type != JTokenType.String) return false;
                code = (string)innerCode;
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }
}
