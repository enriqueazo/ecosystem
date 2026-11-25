using System;

namespace Ecosystem.Connection
{
    /// <summary>Tokens de sesión que se guardan localmente (data-model.md → Sesión).</summary>
    public sealed class SessionTokens
    {
        public SessionTokens(string authToken, string refreshToken)
        {
            AuthToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
        }

        public string AuthToken { get; }
        public string RefreshToken { get; }
    }

    /// <summary>Sesión autenticada: la identidad del jugador es UserId (FR-003), nunca la instalación.</summary>
    public sealed class AuthenticatedSession
    {
        public AuthenticatedSession(SessionTokens tokens, string userId, DateTime expiresAtUtc, DateTime refreshExpiresAtUtc)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            UserId = string.IsNullOrEmpty(userId) ? throw new ArgumentException("UserId vacío.", nameof(userId)) : userId;
            ExpiresAtUtc = expiresAtUtc;
            RefreshExpiresAtUtc = refreshExpiresAtUtc;
        }

        public SessionTokens Tokens { get; }
        public string UserId { get; }
        public DateTime ExpiresAtUtc { get; }
        public DateTime RefreshExpiresAtUtc { get; }
    }
}
