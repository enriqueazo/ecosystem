using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecosystem.Connection
{
    /// <summary>Reloj real.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>Espera real y cancelable.</summary>
    public sealed class TaskDelay : IDelay
    {
        public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken);
    }
}
