using System;

namespace Ecosystem.Connection
{
    /// <summary>Cómo falla una operación con el servidor (contracts/connection-states.md).</summary>
    public enum FailureKind
    {
        None,
        /// <summary>Sin red, tiempo agotado, 5xx o RPC no encontrado: se reintenta.</summary>
        Transient,
        /// <summary>La sesión o el token de refresco ya no son válidos: se reautentica.</summary>
        SessionRejected,
        /// <summary>La cuenta no permite autenticarse (p. ej. deshabilitada): AccountError.</summary>
        AccountRejected,
        /// <summary>La clave de servidor de la configuración no es válida: ConfigurationError.</summary>
        InvalidServerKey,
    }

    /// <summary>Resultado de una operación con el servidor, sin usar excepciones como flujo normal.</summary>
    public readonly struct ServerResult<T>
    {
        private ServerResult(T value, FailureKind failure)
        {
            Value = value;
            Failure = failure;
        }

        public T Value { get; }
        public FailureKind Failure { get; }
        public bool IsSuccess => Failure == FailureKind.None;

        public static ServerResult<T> Success(T value) => new ServerResult<T>(value, FailureKind.None);

        public static ServerResult<T> Fail(FailureKind failure)
        {
            if (failure == FailureKind.None) throw new ArgumentException("Un fallo necesita un motivo.", nameof(failure));
            return new ServerResult<T>(default, failure);
        }
    }
}
