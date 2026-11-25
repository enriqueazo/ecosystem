using UnityEngine;

namespace Ecosystem.Connection.Nakama
{
    /// <summary>
    /// Identificador de instalación en PlayerPrefs (data-model.md → Identificador de instalación.Clave).
    /// </summary>
    public sealed class PlayerPrefsInstallationIdStore : IInstallationIdStore
    {
        public const string Key = "ecosystem.session.installationId";

        public string Load() => PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null;

        public void Save(string installationId)
        {
            PlayerPrefs.SetString(Key, installationId);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Sesión en PlayerPrefs (research.md R-4; data-model.md → Sesión.authToken y Sesión.refreshToken).
    /// Los tokens nunca se registran.
    /// </summary>
    public sealed class PlayerPrefsSessionStore : ISessionStore
    {
        public const string AuthTokenKey = "ecosystem.session.authToken";
        public const string RefreshTokenKey = "ecosystem.session.refreshToken";

        public SessionTokens Load()
        {
            var auth = PlayerPrefs.GetString(AuthTokenKey, null);
            var refresh = PlayerPrefs.GetString(RefreshTokenKey, null);
            return string.IsNullOrEmpty(auth) || string.IsNullOrEmpty(refresh) ? null : new SessionTokens(auth, refresh);
        }

        public void Save(SessionTokens tokens)
        {
            PlayerPrefs.SetString(AuthTokenKey, tokens.AuthToken);
            PlayerPrefs.SetString(RefreshTokenKey, tokens.RefreshToken);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(AuthTokenKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.Save();
        }
    }
}
