using System.Collections;
using System.Diagnostics;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using Ecosystem.Presentation;
using Ecosystem.Territory.View;
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
        private const string TerritoryScenePath = "Assets/_Project/Scenes/Territory.unity";

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

        /// <summary>
        /// Al llegar a «conectado», Bootstrap carga el territorio una sola vez y en ≤ 2 s
        /// (spec 004 FR-020, SC-006; contracts/territory-scene.md → Paso desde Bootstrap).
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator Connected_LoadsTerritoryOnce()
        {
            yield return TaskYield.Await(IntegrationEnvironment.RequireAsync(), _ => { });
            LogAssert.ignoreFailingMessages = false;

            var territoryLoads = 0;
            void CountLoads(Scene scene, LoadSceneMode mode)
            {
                if (scene.path == TerritoryScenePath) territoryLoads++;
            }

            // US4 escenario 4: ni errores ni avisos (los registros informativos de la conexión son normales).
            var problems = new System.Collections.Generic.List<string>();
            void CollectProblems(string message, string stackTrace, LogType type)
            {
                if (type != LogType.Log) problems.Add($"{type}: {message}");
            }

            SceneManager.sceneLoaded += CountLoads;
            Application.logMessageReceived += CollectProblems;
            try
            {
                yield return SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
                var screen = Object.FindAnyObjectByType<ConnectionScreen>();
                Assert.That(screen, Is.Not.Null);

                var waiting = Stopwatch.StartNew();
                while (screen.State == ConnectionState.Connecting && waiting.Elapsed.TotalSeconds < 15) yield return null;
                Assert.That(screen.State, Is.EqualTo(ConnectionState.Connected));

                var toTerritory = Stopwatch.StartNew();
                while (SceneManager.GetActiveScene().path != TerritoryScenePath && toTerritory.Elapsed.TotalSeconds < 5) yield return null;
                toTerritory.Stop();
                yield return null;

                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(TerritoryScenePath), "No se mostró el territorio tras conectar.");
                Assert.That(toTerritory.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(2), $"Tardó {toTerritory.Elapsed.TotalSeconds:0.00} s");
                var view = Object.FindAnyObjectByType<TerritoryView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.Tiles, Has.Count.EqualTo(16));

                var settle = Stopwatch.StartNew();
                while (settle.Elapsed.TotalSeconds < 2) yield return null;
                Assert.That(territoryLoads, Is.EqualTo(1), "El territorio debe cargarse una sola vez.");
                Assert.That(problems, Is.Empty, "Errores o avisos durante la conexión o el paso al territorio.");
                UnityEngine.Debug.Log($"[Prueba] Territorio visible {toTerritory.Elapsed.TotalSeconds:0.00} s después de conectar");
            }
            finally
            {
                SceneManager.sceneLoaded -= CountLoads;
                Application.logMessageReceived -= CollectProblems;
            }
        }
    }
}
