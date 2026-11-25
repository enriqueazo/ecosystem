using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecosystem.Connection
{
    /// <summary>Servidor de juego visto por el núcleo; el adaptador de Nakama lo implementa (research.md R-7).</summary>
    public interface IGameServer
    {
        /// <summary>Autenticación de invitado por dispositivo con create=true (FR-001).</summary>
        Task<ServerResult<AuthenticatedSession>> AuthenticateDeviceAsync(string installationId, CancellationToken cancellationToken);

        /// <summary>Reconstruye una sesión a partir de tokens guardados, sin llamar al servidor; falla si no se pueden leer.</summary>
        ServerResult<AuthenticatedSession> Restore(SessionTokens tokens);

        /// <summary>Renueva la sesión con el token de refresco.</summary>
        Task<ServerResult<AuthenticatedSession>> RefreshAsync(AuthenticatedSession session, CancellationToken cancellationToken);

        /// <summary>Llama al RPC protocol_version y devuelve el texto JSON de la respuesta, sin interpretar.</summary>
        Task<ServerResult<string>> GetProtocolVersionAsync(AuthenticatedSession session, CancellationToken cancellationToken);
    }

    /// <summary>Almacén local de la sesión. Load devuelve null si no hay sesión o no se puede leer.</summary>
    public interface ISessionStore
    {
        SessionTokens Load();
        void Save(SessionTokens tokens);
        void Clear();
    }

    /// <summary>Almacén local del identificador de instalación. Load devuelve null si no existe.</summary>
    public interface IInstallationIdStore
    {
        string Load();
        void Save(string installationId);
    }

    /// <summary>Espera cancelable (permite un reloj falso en las pruebas).</summary>
    public interface IDelay
    {
        Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken);
    }

    /// <summary>Hora actual en UTC (permite fijarla en las pruebas).</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Punto de extensión de autenticación (FR-004). Hoy solo existe el invitado; las cuentas
    /// vinculadas serán otros autenticadores sobre la misma cuenta (userId).
    /// </summary>
    public interface IAuthenticator
    {
        Task<ServerResult<AuthenticatedSession>> AuthenticateAsync(CancellationToken cancellationToken);
    }
}
