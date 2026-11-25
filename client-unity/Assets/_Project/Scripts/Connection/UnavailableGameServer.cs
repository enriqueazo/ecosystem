using System.Threading;
using System.Threading.Tasks;

namespace Ecosystem.Connection
{
    /// <summary>
    /// Servidor que nunca responde. Se usa cuando la configuración no es válida: el flujo termina
    /// en ConfigurationError antes de llamarlo, pero así ninguna dependencia es nula.
    /// </summary>
    public sealed class UnavailableGameServer : IGameServer
    {
        public Task<ServerResult<AuthenticatedSession>> AuthenticateDeviceAsync(string installationId, CancellationToken cancellationToken) =>
            Task.FromResult(ServerResult<AuthenticatedSession>.Fail(FailureKind.Transient));

        public ServerResult<AuthenticatedSession> Restore(SessionTokens tokens) => ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected);

        public Task<ServerResult<AuthenticatedSession>> RefreshAsync(AuthenticatedSession session, CancellationToken cancellationToken) =>
            Task.FromResult(ServerResult<AuthenticatedSession>.Fail(FailureKind.Transient));

        public Task<ServerResult<string>> GetProtocolVersionAsync(AuthenticatedSession session, CancellationToken cancellationToken) =>
            Task.FromResult(ServerResult<string>.Fail(FailureKind.Transient));
    }
}
