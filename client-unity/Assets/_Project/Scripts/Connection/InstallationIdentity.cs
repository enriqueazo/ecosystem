using System;
using System.Text.RegularExpressions;

namespace Ecosystem.Connection
{
    /// <summary>
    /// Identificador de instalación (data-model.md → Identificador de instalación): aleatorio,
    /// generado en la primera apertura, nunca derivado del hardware (FR-002). Solo se regenera si
    /// falta o no cumple el formato; nunca por un error de red o de autenticación.
    /// </summary>
    public sealed class InstallationIdentity
    {
        private static readonly Regex Format = new Regex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant);
        private readonly IInstallationIdStore _store;

        public InstallationIdentity(IInstallationIdStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public static bool IsValid(string id) => id != null && Format.IsMatch(id);

        /// <summary>Forma abreviada para registros: nunca se registra el identificador completo.</summary>
        public static string Abbreviate(string id) => IsValid(id) ? id.Substring(0, 6) + "…" : "(inválido)";

        public string GetOrCreate()
        {
            var id = _store.Load();
            if (IsValid(id)) return id;
            id = Guid.NewGuid().ToString("N");
            _store.Save(id);
            return id;
        }
    }
}
