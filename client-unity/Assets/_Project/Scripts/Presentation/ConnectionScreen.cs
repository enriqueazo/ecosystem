using System;
using System.Threading;
using System.Threading.Tasks;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using Ecosystem.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecosystem.Presentation
{
    /// <summary>
    /// Pantalla de conexión provisional (IMGUI, sin arte final; spec 003 FR-018). Monta las
    /// dependencias, arranca el flujo ligado a la vida del componente (FR-017) y solo refleja
    /// estados y la acción «Reintentar». No registra tokens, la clave ni el identificador completo.
    /// </summary>
    public sealed class ConnectionScreen : MonoBehaviour
    {
        /// <summary>
        /// Escena que se carga al llegar a «conectado» (spec 004 FR-020). Solo es una ruta de la
        /// build: la conexión no conoce nada del territorio (FR-017).
        /// </summary>
        [SerializeField] private string sceneOnConnected = string.Empty;

        private ConnectionFlow _flow;
        private bool _running;
        private bool _navigated;

        public ConnectionState State => _flow?.State ?? ConnectionState.Connecting;

        private void Start()
        {
            var config = ConnectionConfigLoader.Load();
            IGameServer server = config.IsValid ? new NakamaGameServer(config.Config) : new UnavailableGameServer();
            _flow = new ConnectionFlow(new ConnectionDependencies
            {
                Config = config,
                Server = server,
                Authenticator = new GuestDeviceAuthenticator(server, new InstallationIdentity(new PlayerPrefsInstallationIdStore())),
                Sessions = new PlayerPrefsSessionStore(),
                Clock = new SystemClock(),
                Delay = new TaskDelay(),
                Random = new System.Random(),
            });
            _flow.StateChanged += LogState;
            Run();
        }

        private async void Run()
        {
            if (_running) return;
            _running = true;
            try
            {
                await _flow.RunAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Se cerró la escena o la app: cancelación esperada, sin errores (FR-017).
            }
            finally
            {
                _running = false;
            }

            if (this != null && _flow.State == ConnectionState.Connected) ShowSceneOnConnected();
        }

        /// <summary>
        /// Carga la escena tras conectar una sola vez por apertura (spec 004 FR-020, FR-021). El flujo
        /// ya terminó, así que no queda ninguna operación de conexión pendiente.
        /// </summary>
        private void ShowSceneOnConnected()
        {
            if (_navigated) return;
            if (string.IsNullOrEmpty(sceneOnConnected) || SceneUtility.GetBuildIndexByScenePath(sceneOnConnected) < 0)
            {
                Debug.LogError($"[Conexión] Escena tras conectar no disponible: «{sceneOnConnected}» no está en la build.");
                return;
            }

            _navigated = true;
            SceneManager.LoadSceneAsync(sceneOnConnected, LoadSceneMode.Single);
        }

        private void LogState(ConnectionState state)
        {
            var message = $"[Conexión] {state}" + (_flow.Problem != null ? $": {_flow.Problem}" : string.Empty);
            if (state == ConnectionState.ConfigurationError) Debug.LogWarning(message);
            else Debug.Log(message);
        }

        private void OnGUI()
        {
            if (_flow == null) return;
            const float width = 520f;
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, Screen.height / 3f, width, 260f), GUI.skin.box);
            GUILayout.Label(ConnectionTexts.For(_flow.State));
            if (_flow.State == ConnectionState.Connected && _flow.Session != null)
            {
                GUILayout.Label($"Cuenta: {ShortId(_flow.Session.UserId)}");
            }
            GUILayout.Label($"Versión de protocolo del cliente: {ProtocolContracts.ProtocolVersion}");
            if (ConnectionTexts.CanRetry(_flow.State) && !_running && GUILayout.Button("Reintentar"))
            {
                Run();
            }
            GUILayout.EndArea();
        }

        private static string ShortId(string userId) => userId.Length > 8 ? userId.Substring(0, 8) + "…" : userId;
    }
}
