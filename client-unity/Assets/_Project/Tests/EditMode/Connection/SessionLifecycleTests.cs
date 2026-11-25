using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>US2: misma cuenta al volver (FR-005 a FR-008; data-model.md → Sesión).</summary>
    public sealed class SessionLifecycleTests
    {
        private const string InstallationId = "0123456789abcdef0123456789abcdef";
        private FakeGameServer _server;
        private InMemoryInstallationIdStore _installation;
        private InMemorySessionStore _sessions;
        private FakeClock _clock;

        [SetUp]
        public void SetUp()
        {
            _server = new FakeGameServer();
            _installation = new InMemoryInstallationIdStore { Stored = InstallationId };
            _sessions = new InMemorySessionStore();
            _clock = new FakeClock();
        }

        private ConnectionFlow Flow()
        {
            return new ConnectionFlow(new ConnectionDependencies
            {
                Config = ConfigResult.Valid(new ConnectionConfig("127.0.0.1", 7350, "clave", false)),
                Server = _server,
                Authenticator = new GuestDeviceAuthenticator(_server, new InstallationIdentity(_installation)),
                Sessions = _sessions,
                Clock = _clock,
                Delay = new RecordingDelay(_clock),
                Random = new System.Random(1),
            });
        }

        private void StoreSession(string userId, double expiresInSeconds, double refreshExpiresInSeconds = 604800)
        {
            var session = Sessions.For(userId, expiresInSeconds, refreshExpiresInSeconds);
            _sessions.Stored = session.Tokens;
            _server.RestoreResult = _ => ServerResult<AuthenticatedSession>.Success(session);
        }

        [Test]
        public void ValidStoredSession_IsUsed_WithoutAuthenticatingOrRefreshing()
        {
            StoreSession("user-1", expiresInSeconds: 3600);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(flow.Session.UserId, Is.EqualTo("user-1"));
            Assert.That(_server.AuthenticatedInstallationIds, Is.Empty);
            Assert.That(_server.RefreshCalls, Is.Zero);
        }

        [Test]
        public void ExpiredSession_IsRefreshed_AndKeepsTheAccount()
        {
            StoreSession("user-1", expiresInSeconds: -10);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(flow.Session.UserId, Is.EqualTo("user-1"));
            Assert.That(_server.RefreshCalls, Is.EqualTo(1));
            Assert.That(_server.AuthenticatedInstallationIds, Is.Empty);
        }

        [Test]
        public void SessionAboutToExpire_WithinTheSixtySecondMargin_IsRefreshed()
        {
            StoreSession("user-1", expiresInSeconds: 30);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(_server.RefreshCalls, Is.EqualTo(1));
        }

        [Test]
        public void RejectedRefresh_ReauthenticatesOnce_WithTheSameInstallationId()
        {
            StoreSession("user-1", expiresInSeconds: -10);
            _server.RefreshResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected));
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Success(Sessions.For("user-1")));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(flow.Session.UserId, Is.EqualTo("user-1"));
            Assert.That(_server.AuthenticatedInstallationIds, Is.EqualTo(new[] { InstallationId }));
        }

        [Test]
        public void ExpiredRefreshToken_SkipsRefresh_AndReauthenticates()
        {
            StoreSession("user-1", expiresInSeconds: -10, refreshExpiresInSeconds: -5);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(_server.RefreshCalls, Is.Zero);
            Assert.That(_server.AuthenticatedInstallationIds, Is.EqualTo(new[] { InstallationId }));
            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
        }

        [Test]
        public void UnreadableStoredTokens_AreDiscarded_AndTheGuestReauthenticates()
        {
            _sessions.Stored = new SessionTokens("basura", "basura");
            _server.RestoreResult = _ => ServerResult<AuthenticatedSession>.Fail(FailureKind.SessionRejected);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(_sessions.Clears, Is.EqualTo(1));
            Assert.That(_server.AuthenticatedInstallationIds, Is.EqualTo(new[] { InstallationId }));
            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_installation.Stored, Is.EqualTo(InstallationId), "El identificador de instalación no se toca.");
        }

        [Test]
        public void DisabledAccount_EndsInAccountError_WithoutLooping()
        {
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.AccountRejected));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.AccountError));
            Assert.That(_server.AuthenticatedInstallationIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void RefreshRejectedByAccount_EndsInAccountError_WithoutReauthenticating()
        {
            StoreSession("user-1", expiresInSeconds: -10);
            _server.RefreshResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.AccountRejected));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.AccountError));
            Assert.That(_server.AuthenticatedInstallationIds, Is.Empty);
        }

        [Test]
        public void RefreshedTokens_AreSaved()
        {
            StoreSession("user-1", expiresInSeconds: -10);
            _server.RefreshResults.Enqueue(() => ServerResult<AuthenticatedSession>.Success(
                new AuthenticatedSession(new SessionTokens("auth-nuevo", "refresh-nuevo"), "user-1", Sessions.Now.AddHours(2), Sessions.Now.AddDays(7))));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(_sessions.Stored.AuthToken, Is.EqualTo("auth-nuevo"));
            Assert.That(_sessions.Stored.RefreshToken, Is.EqualTo("refresh-nuevo"));
        }

        [Test]
        public void RepeatedRuns_NeverCreateANewInstallationId()
        {
            for (var i = 0; i < 5; i++)
            {
                Sync.Run(Flow().RunAsync(default));
            }

            Assert.That(_installation.Saves, Is.Zero);
            Assert.That(_server.AuthenticatedInstallationIds, Has.All.EqualTo(InstallationId));
        }
    }
}
