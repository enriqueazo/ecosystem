using System;

namespace Ecosystem.Connection
{
    /// <summary>
    /// Política de reintentos (data-model.md → Política de reintentos; research.md R-6): 3 reintentos
    /// con esperas de 1, 2 y 4 s (±20 %); solo los fallos transitorios se reintentan.
    /// </summary>
    public static class RetryPolicy
    {
        public const int MaxRetries = 3;
        public const double Jitter = 0.2;

        public static readonly TimeSpan[] BaseDelays =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
        };

        public static bool IsRetryable(FailureKind failure) => failure == FailureKind.Transient;

        public static TimeSpan DelayFor(int retryIndex, Random random)
        {
            var factor = 1 + (random.NextDouble() * 2 - 1) * Jitter;
            return TimeSpan.FromTicks((long)(BaseDelays[retryIndex].Ticks * factor));
        }

        /// <summary>Tiempo máximo hasta Offline: todos los intentos agotan su tiempo límite y todas las esperas son máximas.</summary>
        public static TimeSpan WorstCase(TimeSpan requestTimeout)
        {
            var waits = TimeSpan.Zero;
            foreach (var delay in BaseDelays) waits += TimeSpan.FromTicks((long)(delay.Ticks * (1 + Jitter)));
            return TimeSpan.FromTicks(requestTimeout.Ticks * (MaxRetries + 1)) + waits;
        }
    }
}
