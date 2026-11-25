using System.Collections;
using System.Diagnostics;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using Ecosystem.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.Integration
{
    /// <summary>
    /// La escena real Bootstrap conecta contra el Nakama local con PlayerPrefs y configuración
    /// reales (quickstart B1, SC-001). Borra la sesión guardada antes y después.
    /// </summary>
    public sealed class BootstrapSceneIntegrationTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        private static void ClearStoredSession()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsInstallationIdStore.Key);
            PlayerPrefs.DeleteKey(PlayerPrefsSessionStore.AuthTokenKey);
            PlayerPrefs.DeleteKey(PlayerPrefsSessionStore.RefreshTokenKey);
            PlayerPrefs.Save();
        }

        [SetUp]
        public void SetUp() => ClearStoredSession();

        [TearDown]
        public void TearDown() => ClearStoredSession();

        [UnityTest, Timeout(60000)]
        public IEnumerator FreshInstall_ConnectsWithinFiveSeconds()
        {
            yield return TaskYield.Await(IntegrationEnvironment.RequireAsync(), _ => { });
            LogAssert.ignoreFailingMessages = false;

            var stopwatch = Stopwatch.StartNew();
            yield return SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            var screen = Object.FindAnyObjectByType<ConnectionScreen>();
            Assert.That(screen, Is.Not.Null);

            while (screen.State == ConnectionState.Connecting && stopwatch.Elapsed.TotalSeconds < 15) yield return null;
            stopwatch.Stop();

            Assert.That(screen.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(5), $"Tardó {stopwatch.Elapsed.TotalSeconds:0.00} s");
            Assert.That(PlayerPrefs.HasKey(PlayerPrefsInstallationIdStore.Key), Is.True);
            UnityEngine.Debug.Log($"[Prueba] Conectado en {stopwatch.Elapsed.TotalSeconds:0.00} s");
        }
    }
}
