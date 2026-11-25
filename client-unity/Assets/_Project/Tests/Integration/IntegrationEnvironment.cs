using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using NUnit.Framework;

namespace Ecosystem.Tests.Integration
{
    /// <summary>
    /// Entorno de las pruebas de integración: configuración (contracts/client-config.md) y
    /// comprobación rápida de que el Nakama local responde (FR-025).
    /// </summary>
    internal static class IntegrationEnvironment
    {
        public static async Task<ConnectionConfig> RequireAsync()
        {
            var config = ConnectionConfigLoader.Load();
            if (!config.IsValid)
            {
                Assert.Fail("Entorno local no disponible: configuración de conexión no válida (" + config.Problem + "). Consulta client-unity/README.md.");
            }

            var scheme = config.Config.UseSsl ? "https" : "http";
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                try
                {
                    var response = await http.GetAsync($"{scheme}://{config.Config.Host}:{config.Config.Port}/healthcheck");
                    if (!response.IsSuccessStatusCode) Assert.Fail($"Entorno local no disponible: healthcheck respondió {(int)response.StatusCode}. Consulta deploy/local/README.md.");
                }
                catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException)
                {
                    Assert.Fail("Entorno local no disponible: Nakama no responde. Consulta deploy/local/README.md.");
                }
            }

            return config.Config;
        }

        public static string NewInstallationId() => Guid.NewGuid().ToString("N");
    }

    internal sealed class MemorySessionStore : ISessionStore
    {
        public SessionTokens Stored;
        public SessionTokens Load() => Stored;
        public void Save(SessionTokens tokens) => Stored = tokens;
        public void Clear() => Stored = null;
    }

    internal sealed class MemoryInstallationIdStore : IInstallationIdStore
    {
        public string Stored;
        public string Load() => Stored;
        public void Save(string installationId) => Stored = installationId;
    }
}
