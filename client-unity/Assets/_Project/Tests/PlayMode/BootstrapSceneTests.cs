using System;
using System.Collections;
using System.Text.RegularExpressions;
using Ecosystem.Connection;
using Ecosystem.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.PlayMode
{
    /// <summary>
    /// La escena inicial carga y se reproduce sin errores (spec 001) y su pantalla de conexión
    /// arranca (spec 003). Fuerza ECOSYSTEM_NAKAMA_CONFIG=none para no depender de la configuración
    /// local ni del servidor (contracts/client-config.md).
    /// </summary>
    public sealed class BootstrapSceneTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ConfigVariable = "ECOSYSTEM_NAKAMA_CONFIG";
        private string _previousConfig;

        [SetUp]
        public void DisableConnectionConfig()
        {
            _previousConfig = Environment.GetEnvironmentVariable(ConfigVariable);
            Environment.SetEnvironmentVariable(ConfigVariable, "none");
        }

        [TearDown]
        public void RestoreConnectionConfig()
        {
            Environment.SetEnvironmentVariable(ConfigVariable, _previousConfig);
        }

        // Registros que la pantalla de conexión emite sin configuración; cualquier otro hace fallar la prueba.
        private static void ExpectConnectionLogs()
        {
            LogAssert.Expect(LogType.Log, "[Conexión] Connecting");
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[Conexión\] ConfigurationError: .*ECOSYSTEM_NAKAMA_CONFIG"));
        }

        [UnityTest]
        public IEnumerator BootstrapScene_LoadsAndRunsWithoutErrors()
        {
            ExpectConnectionLogs();
            var loading = SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            Assert.That(loading, Is.Not.Null, $"No se pudo cargar {BootstrapScenePath}: ¿está en la build?");

            yield return loading;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(BootstrapScenePath));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<Camera>(), Is.Not.Null, "La escena inicial necesita una cámara.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ConnectionScreen_WithoutConfiguration_ShowsConfigurationError()
        {
            ExpectConnectionLogs();
            yield return SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var screen = UnityEngine.Object.FindAnyObjectByType<ConnectionScreen>();
            Assert.That(screen, Is.Not.Null, "Bootstrap debe contener la pantalla de conexión.");
            Assert.That(screen.State, Is.EqualTo(ConnectionState.ConfigurationError));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
