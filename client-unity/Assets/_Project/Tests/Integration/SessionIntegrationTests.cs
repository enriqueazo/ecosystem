using System;
using System.Collections;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.Integration
{
    /// <summary>US2 contra el Nakama local: misma cuenta al volver (spec 003).</summary>
    public sealed class SessionIntegrationTests
    {
        private sealed class OffsetClock : IClock
        {
            public TimeSpan Offset;
            public DateTime UtcNow => DateTime.UtcNow + Offset;
        }

        /// <summary>Envoltorio que cuenta los refrescos reales (sin exponer tokens).</summary>
        private sealed class CountingServer : IGameServer
        {
            private readonly IGameServer _inner;
            public int Refreshes;
            public CountingServer(IGameServer inner) { _inner = inner; }
            public System.Threading.Tasks.Task<ServerResult<AuthenticatedSession>> AuthenticateDeviceAsync(string id, System.Threading.CancellationToken ct) => _inner.AuthenticateDeviceAsync(id, ct);
            public ServerResult<AuthenticatedSession> Restore(SessionTokens tokens) => _inner.Restore(tokens);
            public System.Threading.Tasks.Task<ServerResult<AuthenticatedSession>> RefreshAsync(AuthenticatedSession session, System.Threading.CancellationToken ct) { Refreshes++; return _inner.RefreshAsync(session, ct); }
            public System.Threading.Tasks.Task<ServerResult<string>> GetProtocolVersionAsync(AuthenticatedSession session, System.Threading.CancellationToken ct) => _inner.GetProtocolVersionAsync(session, ct);
        }

        private ConnectionConfig _config;
        private CountingServer _lastServer;
        private MemoryInstallationIdStore _installation;
        private MemorySessionStore _sessions;
        private OffsetClock _clock;

        private IEnumerator Prepare()
        {
            yield return TaskYield.Await(IntegrationEnvironment.RequireAsync(), config => _config = config);
            _installation = new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() };
            _sessions = new MemorySessionStore();
            _clock = new OffsetClock();
        }

        private ConnectionFlow NewFlow(ConnectionConfig config = null)
        {
            var server = new CountingServer(new NakamaGameServer(config ?? _config));
            _lastServer = server;
            return new ConnectionFlow(new ConnectionDependencies
            {
                Config = ConfigResult.Valid(config ?? _config),
                Server = server,
                Authenticator = new GuestDeviceAuthenticator(server, new InstallationIdentity(_installation)),
                Sessions = _sessions,
                Clock = _clock,
                Delay = new TaskDelay(),
                Random = new System.Random(),
            });
        }

        private IEnumerator ConnectFirstTime(Action<string> onUserId)
        {
            var first = NewFlow();
            yield return TaskYield.Await(first.RunAsync(default));
            Assert.That(first.State, Is.EqualTo(ConnectionState.Connected), first.Problem);
            onUserId(first.Session.UserId);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Reopening_WithStoredSession_KeepsTheAccount()
        {
            yield return Prepare();
            string userId = null;
            yield return ConnectFirstTime(id => userId = id);
            var storedAuth = _sessions.Stored.AuthToken;

            var reopened = NewFlow();
            yield return TaskYield.Await(reopened.RunAsync(default));

            Assert.That(reopened.State, Is.EqualTo(ConnectionState.Connected), reopened.Problem);
            Assert.That(reopened.Session.UserId, Is.EqualTo(userId));
            Assert.That(_lastServer.Refreshes, Is.Zero, "La sesión vigente se reutiliza sin refrescar.");
            Assert.That(_sessions.Stored.AuthToken == storedAuth, Is.True, "La sesión guardada no debe cambiar.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ExpiredSession_IsRefreshedByTheServer_AndKeepsTheAccount()
        {
            yield return Prepare();
            string userId = null;
            yield return ConnectFirstTime(id => userId = id);
            _clock.Offset = TimeSpan.FromHours(3);

            var reopened = NewFlow();
            yield return TaskYield.Await(reopened.RunAsync(default));

            Assert.That(reopened.State, Is.EqualTo(ConnectionState.Connected), reopened.Problem);
            Assert.That(reopened.Session.UserId, Is.EqualTo(userId));
            Assert.That(_lastServer.Refreshes, Is.EqualTo(1), "La sesión caducada debe refrescarse en el servidor.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator WithoutStoredTokens_Reauthenticates_AndKeepsTheAccount()
        {
            yield return Prepare();
            string userId = null;
            yield return ConnectFirstTime(id => userId = id);
            _sessions.Clear();

            var reopened = NewFlow();
            yield return TaskYield.Await(reopened.RunAsync(default));

            Assert.That(reopened.State, Is.EqualTo(ConnectionState.Connected), reopened.Problem);
            Assert.That(reopened.Session.UserId, Is.EqualTo(userId));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TenReopenings_KeepTheSameAccount_EvenWithForcedExpiry()
        {
            yield return Prepare();
            string userId = null;
            yield return ConnectFirstTime(id => userId = id);

            for (var i = 0; i < 10; i++)
            {
                _clock.Offset = i % 2 == 0 ? TimeSpan.Zero : TimeSpan.FromHours(3 * i);
                var reopened = NewFlow();
                yield return TaskYield.Await(reopened.RunAsync(default));

                Assert.That(reopened.State, Is.EqualTo(ConnectionState.Connected), $"Reapertura {i + 1}: {reopened.Problem}");
                Assert.That(reopened.Session.UserId, Is.EqualTo(userId), $"Reapertura {i + 1}");
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator WrongServerKey_EndsInConfigurationError()
        {
            yield return Prepare();
            var wrongKey = new ConnectionConfig(_config.Host, _config.Port, "clave-equivocada", _config.UseSsl);

            var flow = NewFlow(wrongKey);
            yield return TaskYield.Await(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.ConfigurationError), flow.Problem);
        }
    }
}
