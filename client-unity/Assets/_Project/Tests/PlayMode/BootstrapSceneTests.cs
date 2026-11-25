using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.PlayMode
{
    /// <summary>
    /// La escena inicial carga y se reproduce sin errores (spec 001, US2 escenario 2, FR-037).
    /// </summary>
    public sealed class BootstrapSceneTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [UnityTest]
        public IEnumerator BootstrapScene_LoadsAndRunsWithoutErrors()
        {
            var loading = SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            Assert.That(loading, Is.Not.Null, $"No se pudo cargar {BootstrapScenePath}: ¿está en la build?");

            yield return loading;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(BootstrapScenePath));
            Assert.That(Object.FindAnyObjectByType<Camera>(), Is.Not.Null, "La escena inicial necesita una cámara.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
