using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ecosystem.Connection
{
    /// <summary>Configuración de conexión (data-model.md → Configuración de conexión).</summary>
    public sealed class ConnectionConfig
    {
        public ConnectionConfig(string host, int port, string serverKey, bool useSsl)
        {
            Host = host;
            Port = port;
            ServerKey = serverKey;
            UseSsl = useSsl;
        }

        public string Host { get; }
        public int Port { get; }
        public string ServerKey { get; }
        public bool UseSsl { get; }
    }

    /// <summary>Configuración válida o el motivo por el que no lo es (sin valores secretos).</summary>
    public sealed class ConfigResult
    {
        private ConfigResult(ConnectionConfig config, string problem)
        {
            Config = config;
            Problem = problem;
        }

        public ConnectionConfig Config { get; }
        public string Problem { get; }
        public bool IsValid => Config != null;

        public static ConfigResult Valid(ConnectionConfig config) => new ConfigResult(config ?? throw new ArgumentNullException(nameof(config)), null);
        public static ConfigResult Invalid(string problem) => new ConfigResult(null, problem);
    }

    /// <summary>
    /// Carga la configuración según contracts/client-config.md: ECOSYSTEM_NAKAMA_CONFIG=none la
    /// desactiva; las variables ECOSYSTEM_NAKAMA_* completas tienen precedencia; si no, el archivo
    /// local. Los mensajes nombran campos, nunca valores.
    /// </summary>
    public static class ConnectionConfigParser
    {
        public const string LocalFile = "client-unity/LocalConfig/connection.local.json";
        public const string ExampleFile = "client-unity/LocalConfig/connection.example.json";

        public static ConfigResult Load(Func<string, string> getEnvironmentVariable, string localFileText)
        {
            if (getEnvironmentVariable == null) throw new ArgumentNullException(nameof(getEnvironmentVariable));

            if (string.Equals(getEnvironmentVariable("ECOSYSTEM_NAKAMA_CONFIG"), "none", StringComparison.OrdinalIgnoreCase))
            {
                return ConfigResult.Invalid("Configuración desactivada con ECOSYSTEM_NAKAMA_CONFIG=none.");
            }

            var host = getEnvironmentVariable("ECOSYSTEM_NAKAMA_HOST");
            var port = getEnvironmentVariable("ECOSYSTEM_NAKAMA_PORT");
            var key = getEnvironmentVariable("ECOSYSTEM_NAKAMA_SERVER_KEY");
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(key))
            {
                var ssl = getEnvironmentVariable("ECOSYSTEM_NAKAMA_USE_SSL");
                return Build("variables ECOSYSTEM_NAKAMA_*", JValue.CreateString(host), JValue.CreateString(port), JValue.CreateString(key),
                    string.IsNullOrEmpty(ssl) ? null : JValue.CreateString(ssl));
            }

            if (localFileText == null)
            {
                return ConfigResult.Invalid($"Falta {LocalFile}: cópialo de {ExampleFile} y pon la NAKAMA_SERVER_KEY de deploy/local/.env.");
            }

            JObject json;
            try
            {
                json = JObject.Parse(localFileText);
            }
            catch (JsonReaderException)
            {
                return ConfigResult.Invalid($"{LocalFile} no es un JSON válido.");
            }

            return Build(LocalFile, json["host"], json["port"], json["serverKey"], json["useSsl"]);
        }

        private static ConfigResult Build(string source, JToken host, JToken port, JToken serverKey, JToken useSsl)
        {
            if (host == null || host.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)host))
                return ConfigResult.Invalid($"{source}: falta \"host\" o no es válido.");

            int portValue;
            if (port == null || !int.TryParse(port.ToString(), out portValue) || portValue < 1 || portValue > 65535)
                return ConfigResult.Invalid($"{source}: \"port\" debe ser un entero entre 1 y 65535.");

            if (serverKey == null || serverKey.Type != JTokenType.String || string.IsNullOrEmpty((string)serverKey))
                return ConfigResult.Invalid($"{source}: falta \"serverKey\".");

            var ssl = false;
            if (useSsl != null && useSsl.Type != JTokenType.Null)
            {
                if (useSsl.Type == JTokenType.Boolean) ssl = (bool)useSsl;
                else if (!bool.TryParse(useSsl.ToString(), out ssl) || useSsl.Type != JTokenType.String)
                    return ConfigResult.Invalid($"{source}: \"useSsl\" debe ser true o false.");
            }

            return ConfigResult.Valid(new ConnectionConfig((string)host, portValue, (string)serverKey, ssl));
        }
    }
}
