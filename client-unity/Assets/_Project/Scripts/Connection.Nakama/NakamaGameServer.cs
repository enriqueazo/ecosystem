using System;
using System.Threading;
using System.Threading.Tasks;
using Nakama;

namespace Ecosystem.Connection.Nakama
{
    /// <summary>
    /// Adaptador del SDK de Nakama al puerto <see cref="IGameServer"/> (research.md R-1, R-6).
    /// Traduce excepciones del SDK a <see cref="FailureKind"/>; nunca registra tokens ni la clave.
    /// </summary>
    public sealed class NakamaGameServer : IGameServer
    {
        public const int RequestTimeoutSeconds = 10;
        private const string ProtocolVersionRpc = "protocol_version";
        private readonly IClient _client;

        public NakamaGameServer(ConnectionConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            // Sin refresco automático ni reintentos del SDK: el flujo los controla (R-6).
            _client = new Client(config.UseSsl ? "https" : "http", config.Host, config.Port, config.ServerKey,
                UnityWebRequestAdapter.Instance, autoRefreshSession: false)
            {
                Timeout = RequestTimeoutSeconds,
                GlobalRetryConfiguration = new RetryConfiguration(1000, 0),
            };
        }

        public Task<ServerResult<AuthenticatedSession>> AuthenticateDeviceAsync(string installationId, CancellationToken cancellationToken)
        {
            return Call(async () => Map(await _client.AuthenticateDeviceAsync(installationId, null, true, null, null, cancellationToken)),
                ClassifyAuthentication, cancellationToken);
        }

        public ServerResult<AuthenticatedSession> Restore(SessionTokens tokens)
        {
            try
            {
                var session = Session.Restore(tokens.AuthToken, tokens.RefreshToken);
                return session == null
                    ? ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected)
                    : ServerResult<AuthenticatedSession>.Success(Map(session));
            }
            catch (Exception)
            {
                // Tokens ilegibles: se descartan y se reautentica (data-model.md → Sesión.Datos ilegibles).
                return ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected);
            }
        }

        public Task<ServerResult<AuthenticatedSession>> RefreshAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            return Call(async () => Map(await _client.SessionRefreshAsync(ToSdk(session), null, null, cancellationToken)),
                ClassifySessionCall, cancellationToken);
        }

        public Task<ServerResult<string>> GetProtocolVersionAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            return Call(async () => (await _client.RpcAsync(ToSdk(session), ProtocolVersionRpc, "{}", null, cancellationToken)).Payload,
                ClassifyRpc, cancellationToken);
        }

        private static async Task<ServerResult<T>> Call<T>(Func<Task<T>> action, Func<ApiResponseException, FailureKind> classify,
            CancellationToken cancellationToken)
        {
            try
            {
                return ServerResult<T>.Success(await action());
            }
            catch (ApiResponseException e)
            {
                return ServerResult<T>.Fail(classify(e));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Sin red, tiempo agotado u otros fallos de transporte: transitorios.
                return ServerResult<T>.Fail(FailureKind.Transient);
            }
        }

        // Autenticación: 401 = clave de servidor rechazada; 403 = cuenta deshabilitada (verificado en T019).
        private static FailureKind ClassifyAuthentication(ApiResponseException e)
        {
            if (IsTransient(e)) return FailureKind.Transient;
            if (e.StatusCode == 401) return FailureKind.InvalidServerKey;
            return FailureKind.AccountRejected;
        }

        private static FailureKind ClassifySessionCall(ApiResponseException e)
        {
            if (IsTransient(e)) return FailureKind.Transient;
            if (e.StatusCode == 401) return FailureKind.SessionRejected;
            return FailureKind.AccountRejected;
        }

        // RPC: 401 = sesión rechazada; 404 (módulo no cargado) y 5xx son transitorios (R-6).
        private static FailureKind ClassifyRpc(ApiResponseException e)
        {
            if (e.StatusCode == 401) return FailureKind.SessionRejected;
            return FailureKind.Transient;
        }

        private static bool IsTransient(ApiResponseException e) => e.StatusCode <= 0 || e.StatusCode >= 500 || e.StatusCode == 408;

        private static AuthenticatedSession Map(ISession session)
        {
            return new AuthenticatedSession(new SessionTokens(session.AuthToken, session.RefreshToken), session.UserId,
                DateTimeOffset.FromUnixTimeSeconds(session.ExpireTime).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(session.RefreshExpireTime).UtcDateTime);
        }

        private static ISession ToSdk(AuthenticatedSession session) => Session.Restore(session.Tokens.AuthToken, session.Tokens.RefreshToken);
    }
}
