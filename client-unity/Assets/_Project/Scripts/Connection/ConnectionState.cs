namespace Ecosystem.Connection
{
    /// <summary>
    /// Estados de conexión del cliente (spec 003, data-model.md → Estado de conexión;
    /// contracts/connection-states.md).
    /// </summary>
    public enum ConnectionState
    {
        Connecting,
        Connected,
        Offline,
        UpdateRequired,
        ServerIncompatible,
        AccountError,
        ConfigurationError,
    }

    /// <summary>Textos provisionales para el jugador, sin detalles técnicos (FR-018).</summary>
    public static class ConnectionTexts
    {
        public static string For(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connecting: return "Conectando…";
                case ConnectionState.Connected: return "Conectado";
                case ConnectionState.Offline: return "Sin conexión con el servidor.";
                case ConnectionState.UpdateRequired: return "Hay una versión nueva. Actualiza el juego para seguir.";
                case ConnectionState.ServerIncompatible: return "El servidor aún no admite esta versión. Inténtalo más tarde.";
                case ConnectionState.AccountError: return "No se puede acceder a la cuenta.";
                default: return "Configuración de conexión ausente o no válida.";
            }
        }

        /// <summary>Estados desde los que el jugador puede pulsar «Reintentar».</summary>
        public static bool CanRetry(ConnectionState state)
        {
            return state == ConnectionState.Offline || state == ConnectionState.ServerIncompatible;
        }
    }
}
