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

        /// <summary>Versión de protocolo del cliente; por defecto, la de shared/contracts/protocol.json.</summary>
        public int ClientProtocolVersion { get; set; } = Ecosystem.Contracts.ProtocolContracts.ProtocolVersion;
    }

    /// <summary>
    /// Flujo de conexión del cliente (contracts/connection-states.md). Sin dependencias de Unity ni
    /// de Nakama: todo pasa por los puertos de <see cref="ConnectionDependencies"/>.
    /// Cada ejecución es una generación (data-model.md → Generación de intento): si otra ejecución la
    /// supera, sus resultados se descartan sin cambiar el estado ni guardar nada (FR-017).
    /// </summary>
    public sealed class ConnectionFlow
    {
        // Margen de validez local (data-model.md → Sesión.Validez local).
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(60);

        private readonly ConnectionDependencies _deps;
        private int _generation;

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

        /// <summary>
        /// Ejecuta el flujo completo con reintentos (FR-015). Lanza OperationCanceledException si se
        /// cancela, sin pasar por ningún estado de error (FR-017).
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var generation = ++_generation;
            Problem = null;
            SetState(ConnectionState.Connecting);

            if (!_deps.Config.IsValid)
            {
                Apply(new Outcome(ConnectionState.ConfigurationError, _deps.Config.Problem, null));
                return;
            }

            for (var retry = 0; ; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var outcome = await AttemptAsync(cancellationToken);
                if (generation != _generation) return;

                if (outcome.Retryable && retry < RetryPolicy.MaxRetries)
                {
                    await _deps.Delay.WaitAsync(RetryPolicy.DelayFor(retry, _deps.Random), cancellationToken);
                    if (generation != _generation) return;
                    continue;
                }

                Apply(outcome);
                return;
            }
        }

        private sealed class Outcome
        {
            public Outcome(ConnectionState state, string problem, AuthenticatedSession session, bool retryable = false)
            {
                State = state;
                Problem = problem;
                Session = session;
                Retryable = retryable;
            }

            public ConnectionState State { get; }
            public string Problem { get; }
            public AuthenticatedSession Session { get; }
            public bool Retryable { get; }
        }

        /// <summary>Un intento completo: sesión y comprobación de versión de protocolo.</summary>
        private async Task<Outcome> AttemptAsync(CancellationToken cancellationToken)
        {
            var session = await EstablishSessionAsync(cancellationToken);
            if (!session.IsSuccess) return FromFailure(session.Failure, null);

            var protocol = await _deps.Server.GetProtocolVersionAsync(session.Value, cancellationToken);
            if (!protocol.IsSuccess && protocol.Failure == FailureKind.SessionRejected)
            {
                // FR-007: el servidor rechazó la sesión durante el uso → una sola reautenticación.
                _deps.Sessions.Clear();
                session = await _deps.Authenticator.AuthenticateAsync(cancellationToken);
                if (!session.IsSuccess) return FromFailure(session.Failure, null);
                protocol = await _deps.Server.GetProtocolVersionAsync(session.Value, cancellationToken);
            }
            if (!protocol.IsSuccess) return FromFailure(protocol.Failure, session.Value);

            var compatibility = ProtocolCompatibility.Evaluate(protocol.Value, _deps.ClientProtocolVersion);
            switch (compatibility)
            {
                case ConnectionState.Connected:
                    return new Outcome(ConnectionState.Connected, null, session.Value);
                case ConnectionState.UpdateRequired:
                    return new Outcome(compatibility, "El servidor usa una versión de protocolo mayor que la del cliente.", session.Value);
                default:
                    return new Outcome(compatibility,
                        "El servidor no admite la versión de protocolo del cliente o su respuesta no cumple el contrato.", session.Value);
            }
        }

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
                    if (refreshed.IsSuccess || refreshed.Failure != FailureKind.SessionRejected)
                    {
                        return refreshed;
                    }
                }
            }

            return await _deps.Authenticator.AuthenticateAsync(cancellationToken);
        }

        private static Outcome FromFailure(FailureKind failure, AuthenticatedSession session)
        {
            switch (failure)
            {
                case FailureKind.InvalidServerKey:
                    return new Outcome(ConnectionState.ConfigurationError, "El servidor rechazó la clave: revisa \"serverKey\" en la configuración.", session);
                case FailureKind.AccountRejected:
                case FailureKind.SessionRejected:
                    return new Outcome(ConnectionState.AccountError, "El servidor rechazó la autenticación de la cuenta.", session);
                default:
                    return new Outcome(ConnectionState.Offline, "No se pudo contactar con el servidor.", session, retryable: true);
            }
        }

        private void Apply(Outcome outcome)
        {
            if (outcome.Session != null)
            {
                _deps.Sessions.Save(outcome.Session.Tokens);
            }
            Session = outcome.State == ConnectionState.Connected ? outcome.Session : null;
            Problem = outcome.Problem;
            SetState(outcome.State);
        }

        private void SetState(ConnectionState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
