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

            var session = await EstablishSessionAsync(cancellationToken);
            if (!session.IsSuccess)
            {
                FailWith(session.Failure);
                return;
            }

            Session = session.Value;
            SetState(ConnectionState.Connected);
        }

        // Margen de validez local (data-model.md → Sesión.Validez local): si el servidor rechaza la
        // sesión antes, prevalece el servidor.
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(60);

        private bool IsExpired(DateTime expiresAtUtc) => _deps.Clock.UtcNow >= expiresAtUtc - ExpiryMargin;

        /// <summary>
        /// FR-006: sesión guardada vigente → se usa; caducada → se refresca; no renovable o ilegible →
        /// reautenticación de invitado con la misma instalación (la misma cuenta).
        /// </summary>
        private async Task<ServerResult<AuthenticatedSession>> EstablishSessionAsync(CancellationToken cancellationToken)
        {
            var stored = _deps.Sessions.Load();
            if (stored != null)
            {
                var restored = _deps.Server.Restore(stored);
                if (!restored.IsSuccess)
                {
                    _deps.Sessions.Clear();
                }
                else if (!IsExpired(restored.Value.ExpiresAtUtc))
                {
                    return restored;
                }
                else if (!IsExpired(restored.Value.RefreshExpiresAtUtc))
                {
                    var refreshed = await _deps.Server.RefreshAsync(restored.Value, cancellationToken);
                    if (refreshed.IsSuccess)
                    {
                        _deps.Sessions.Save(refreshed.Value.Tokens);
                        return refreshed;
                    }
                    if (refreshed.Failure != FailureKind.SessionRejected)
                    {
                        return refreshed;
                    }
                }
            }

            return await AuthenticateAsync(cancellationToken);
        }

        private async Task<ServerResult<AuthenticatedSession>> AuthenticateAsync(CancellationToken cancellationToken)
        {
            var authenticated = await _deps.Authenticator.AuthenticateAsync(cancellationToken);
            if (authenticated.IsSuccess)
            {
                _deps.Sessions.Save(authenticated.Value.Tokens);
            }
            return authenticated;
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
