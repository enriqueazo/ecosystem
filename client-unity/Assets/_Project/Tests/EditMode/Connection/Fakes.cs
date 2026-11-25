using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>Servidor falso: cada operación devuelve el siguiente resultado programado.</summary>
    internal sealed class FakeGameServer : IGameServer
    {
        public readonly Queue<Func<ServerResult<AuthenticatedSession>>> AuthResults = new Queue<Func<ServerResult<AuthenticatedSession>>>();
        public readonly Queue<Func<ServerResult<AuthenticatedSession>>> RefreshResults = new Queue<Func<ServerResult<AuthenticatedSession>>>();
        public readonly Queue<Func<ServerResult<string>>> ProtocolResults = new Queue<Func<ServerResult<string>>>();
        public readonly List<string> AuthenticatedInstallationIds = new List<string>();
        public Func<SessionTokens, ServerResult<AuthenticatedSession>> RestoreResult = _ => ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected);
        public int RefreshCalls;
        public int ProtocolCalls;
        public Func<CancellationToken, Task> BeforeEachCall = _ => Task.CompletedTask;

        public async Task<ServerResult<AuthenticatedSession>> AuthenticateDeviceAsync(string installationId, CancellationToken cancellationToken)
        {
            await BeforeEachCall(cancellationToken);
            AuthenticatedInstallationIds.Add(installationId);
            return AuthResults.Count > 0 ? AuthResults.Dequeue()() : ServerResult<AuthenticatedSession>.Success(Sessions.For("user-" + installationId.Substring(0, 6)));
        }

        public ServerResult<AuthenticatedSession> Restore(SessionTokens tokens) => RestoreResult(tokens);

        public async Task<ServerResult<AuthenticatedSession>> RefreshAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            await BeforeEachCall(cancellationToken);
            RefreshCalls++;
            return RefreshResults.Count > 0 ? RefreshResults.Dequeue()() : ServerResult<AuthenticatedSession>.Success(Sessions.For(session.UserId));
        }

        public async Task<ServerResult<string>> GetProtocolVersionAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            await BeforeEachCall(cancellationToken);
            ProtocolCalls++;
            return ProtocolResults.Count > 0 ? ProtocolResults.Dequeue()() : ServerResult<string>.Success(Sessions.ProtocolPayload(Ecosystem.Contracts.ProtocolContracts.ProtocolVersion));
        }
    }

    internal static class Sessions
    {
        public static readonly DateTime Now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

        public static AuthenticatedSession For(string userId, double expiresInSeconds = 7200, double refreshExpiresInSeconds = 604800)
        {
            return new AuthenticatedSession(new SessionTokens("auth-" + userId, "refresh-" + userId), userId,
                Now.AddSeconds(expiresInSeconds), Now.AddSeconds(refreshExpiresInSeconds));
        }

        public static string ProtocolPayload(int version) => "{\"protocolVersion\":" + version + ",\"moduleVersion\":\"0.1.0\"}";
    }

    internal sealed class InMemorySessionStore : ISessionStore
    {
        public SessionTokens Stored;
        public int Clears;
        public SessionTokens Load() => Stored;
        public void Save(SessionTokens tokens) => Stored = tokens;
        public void Clear() { Stored = null; Clears++; }
    }

    internal sealed class InMemoryInstallationIdStore : IInstallationIdStore
    {
        public string Stored;
        public int Saves;
        public string Load() => Stored;
        public void Save(string installationId) { Stored = installationId; Saves++; }
    }

    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = Sessions.Now;
    }

    /// <summary>Espera falsa: no espera; registra las duraciones y avanza el reloj.</summary>
    internal sealed class RecordingDelay : IDelay
    {
        private readonly FakeClock _clock;
        public readonly List<TimeSpan> Waits = new List<TimeSpan>();
        public RecordingDelay(FakeClock clock) { _clock = clock; }

        public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Waits.Add(duration);
            _clock.UtcNow += duration;
            return Task.CompletedTask;
        }
    }

    internal static class Sync
    {
        /// <summary>Ejecuta una tarea que, con los dobles, termina de forma síncrona.</summary>
        public static T Run<T>(Task<T> task)
        {
            Assert.That(task.IsCompleted, Is.True, "Con los dobles, la tarea debería terminar de forma síncrona.");
            return task.GetAwaiter().GetResult();
        }

        public static void Run(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "Con los dobles, la tarea debería terminar de forma síncrona.");
            task.GetAwaiter().GetResult();
        }
    }
}
