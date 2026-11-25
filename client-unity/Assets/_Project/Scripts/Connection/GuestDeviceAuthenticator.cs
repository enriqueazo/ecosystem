using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecosystem.Connection
{
    /// <summary>
    /// Único autenticador actual: invitado por dispositivo con el identificador de instalación
    /// (FR-001). La cuenta resultante es la identidad del jugador (userId); las futuras
    /// credenciales se vincularán a ella (FR-004).
    /// </summary>
    public sealed class GuestDeviceAuthenticator : IAuthenticator
    {
        private readonly IGameServer _server;
        private readonly InstallationIdentity _installation;

        public GuestDeviceAuthenticator(IGameServer server, InstallationIdentity installation)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _installation = installation ?? throw new ArgumentNullException(nameof(installation));
        }

        public Task<ServerResult<AuthenticatedSession>> AuthenticateAsync(CancellationToken cancellationToken)
        {
            return _server.AuthenticateDeviceAsync(_installation.GetOrCreate(), cancellationToken);
        }
    }
}
