using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecosystem.Connection
{
    /// <summary>Dependencias del flujo (se montan en la escena o en las pruebas).</summary>
    public sealed class ConnectionDependencies
    {
        public ConfigResult Config { get; set; }
        public IGameServer Server { get; set; }
        public IAuthenticator Authenticator { get; set; }
        public ISessionStore Sessions { get; set; }
        public IClock Clock { get; set; }
        public IDelay Delay { get; set; }
        public Random Random { get; set; }
    }

    /// <summary>
    /// Flujo de conexión del cliente (contracts/connection-states.md). Sin dependencias de Unity
    /// ni de Nakama: todo pasa por los puertos de <see cref="ConnectionDependencies"/>.
    /// </summary>
    public sealed class ConnectionFlow
    {
        private readonly ConnectionDependencies _deps;

        public ConnectionFlow(ConnectionDependencies dependencies)
        {
            _deps = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public ConnectionState State { get; private set; } = ConnectionState.Connecting;

        /// <summary>Sesión activa cuando el estado es Connected.</summary>
        public AuthenticatedSession Session { get; private set; }

        /// <summary>Explicación para desarrolladores del último estado de error (sin secretos).</summary>
        public string Problem { get; private set; }

        public event Action<ConnectionState> StateChanged;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Problem = null;
            SetState(ConnectionState.Connecting);

            if (!_deps.Config.IsValid)
            {
                Fail(ConnectionState.ConfigurationError, _deps.Config.Problem);
                return;
            }

            var authenticated = await _deps.Authenticator.AuthenticateAsync(cancellationToken);
            if (!authenticated.IsSuccess)
            {
                FailWith(authenticated.Failure);
                return;
            }

            Session = authenticated.Value;
            _deps.Sessions.Save(Session.Tokens);
            SetState(ConnectionState.Connected);
        }

        private void FailWith(FailureKind failure)
        {
            switch (failure)
            {
                case FailureKind.InvalidServerKey:
                    Fail(ConnectionState.ConfigurationError, "El servidor rechazó la clave: revisa \"serverKey\" en la configuración.");
                    break;
                case FailureKind.AccountRejected:
                case FailureKind.SessionRejected:
                    Fail(ConnectionState.AccountError, "El servidor rechazó la autenticación de la cuenta.");
                    break;
                default:
                    Fail(ConnectionState.Offline, "No se pudo contactar con el servidor.");
                    break;
            }
        }

        private void Fail(ConnectionState state, string problem)
        {
            Problem = problem;
            SetState(state);
        }

        private void SetState(ConnectionState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
