using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
    /// US4 con red real (quickstart B6 y B8, SC-003): servidor que no escucha, servidor que acepta
    /// la conexión pero nunca responde (agota el tiempo límite) y salida de la escena a mitad.
    /// No necesita Nakama.
    /// </summary>
    public sealed class ResilienceIntegrationTests
    {
        private TcpListener _silentServer;

        [TearDown]
        public void StopSilentServer()
        {
            _silentServer?.Stop();
            _silentServer = null;
            foreach (var name in new[] { "ECOSYSTEM_NAKAMA_HOST", "ECOSYSTEM_NAKAMA_PORT", "ECOSYSTEM_NAKAMA_SERVER_KEY" })
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }

        // Acepta conexiones TCP y nunca contesta: cada petición HTTP agota el tiempo límite.
        private int StartSilentServer()
        {
            _silentServer = new TcpListener(IPAddress.Loopback, 0);
            _silentServer.Start();
            _silentServer.BeginAcceptTcpClient(OnAccept, _silentServer);
            return ((IPEndPoint)_silentServer.LocalEndpoint).Port;
        }

        private static void OnAccept(IAsyncResult result)
        {
            var listener = (TcpListener)result.AsyncState;
            try
            {
                listener.EndAcceptTcpClient(result);
                listener.BeginAcceptTcpClient(OnAccept, listener);
            }
            catch (ObjectDisposedException)
            {
                // Se detuvo el servidor.
            }
        }

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static ConnectionFlow NewFlow(int port)
        {
            var config = new ConnectionConfig("127.0.0.1", port, "clave-de-prueba", false);
            var server = new NakamaGameServer(config);
            return new ConnectionFlow(new ConnectionDependencies
            {
                Config = ConfigResult.Valid(config),
                Server = server,
                Authenticator = new GuestDeviceAuthenticator(server, new InstallationIdentity(new MemoryInstallationIdStore())),
                Sessions = new MemorySessionStore(),
                Clock = new SystemClock(),
                Delay = new TaskDelay(),
                Random = new System.Random(),
            });
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ClosedPort_GoesOffline_AfterRetries()
        {
            var flow = NewFlow(FreePort());
            var stopwatch = Stopwatch.StartNew();

            yield return TaskYield.Await(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Offline), flow.Problem);
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(60));
            UnityEngine.Debug.Log($"[Prueba] Puerto cerrado → Offline en {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator SilentServer_TimesOut_AndGoesOfflineWithinSixtySeconds()
        {
            var flow = NewFlow(StartSilentServer());
            var stopwatch = Stopwatch.StartNew();

            yield return TaskYield.Await(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Offline), flow.Problem);
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThanOrEqualTo(60), "SC-003");
            // Cota inferior para distinguir «tiempo agotado» de «conexión rechazada» (≈ 6 s); el tiempo
            // límite de UnityWebRequest puede cortar algo antes de 10 s, de ahí el 90 %.
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(4 * NakamaGameServer.RequestTimeoutSeconds * 0.9),
                "Cada uno de los 4 intentos debe agotar el tiempo límite");
            UnityEngine.Debug.Log($"[Prueba] Servidor mudo → Offline en {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LeavingTheSceneMidConnection_CancelsWithoutErrors()
        {
            var port = StartSilentServer();
            Environment.SetEnvironmentVariable("ECOSYSTEM_NAKAMA_HOST", "127.0.0.1");
            Environment.SetEnvironmentVariable("ECOSYSTEM_NAKAMA_PORT", port.ToString());
            Environment.SetEnvironmentVariable("ECOSYSTEM_NAKAMA_SERVER_KEY", "clave-de-prueba");
            LogAssert.Expect(LogType.Log, "[Conexión] Connecting");

            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap.unity", LoadSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<ConnectionScreen>();
            Assert.That(screen.State, Is.EqualTo(ConnectionState.Connecting));

            var empty = SceneManager.CreateScene("Vacia");
            yield return SceneManager.UnloadSceneAsync("Bootstrap");
            for (var i = 0; i < 30; i++) yield return null;
            yield return new WaitForSeconds(2);

            Assert.That(empty.isLoaded, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
