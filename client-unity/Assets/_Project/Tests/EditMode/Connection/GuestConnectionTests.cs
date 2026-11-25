using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>US1: primer arranque como invitado (FR-001, FR-003).</summary>
    public sealed class GuestConnectionTests
    {
        private FakeGameServer _server;
        private InMemoryInstallationIdStore _installation;
        private InMemorySessionStore _sessions;
        private FakeClock _clock;

        [SetUp]
        public void SetUp()
        {
            _server = new FakeGameServer();
            _installation = new InMemoryInstallationIdStore();
            _sessions = new InMemorySessionStore();
            _clock = new FakeClock();
        }

        private ConnectionFlow Flow(ConfigResult config = null)
        {
            return new ConnectionFlow(new ConnectionDependencies
            {
                Config = config ?? ConfigResult.Valid(new ConnectionConfig("127.0.0.1", 7350, "clave", false)),
                Server = _server,
                Authenticator = new GuestDeviceAuthenticator(_server, new InstallationIdentity(_installation)),
                Sessions = _sessions,
                Clock = _clock,
                Delay = new RecordingDelay(_clock),
                Random = new System.Random(1),
            });
        }

        [Test]
        public void FirstStart_AuthenticatesAsGuestWithInstallationId_AndConnects()
        {
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.AuthenticatedInstallationIds, Is.EqualTo(new[] { _installation.Stored }));
        }

        [Test]
        public void Identity_IsTheServerUserId_NotTheInstallationId()
        {
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Success(Sessions.For("user-del-servidor")));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.Session.UserId, Is.EqualTo("user-del-servidor"));
            Assert.That(flow.Session.UserId, Is.Not.EqualTo(_installation.Stored));
        }

        [Test]
        public void SuccessfulConnection_SavesTheSession()
        {
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(_sessions.Stored, Is.Not.Null);
            Assert.That(_sessions.Stored.AuthToken, Is.EqualTo(flow.Session.Tokens.AuthToken));
        }

        [Test]
        public void InvalidConfiguration_EndsInConfigurationError_WithoutCallingTheServer()
        {
            var flow = Flow(ConfigResult.Invalid("Falta connection.local.json"));

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.ConfigurationError));
            Assert.That(flow.Problem, Does.Contain("connection.local.json"));
            Assert.That(_server.AuthenticatedInstallationIds, Is.Empty);
        }

        [Test]
        public void RejectedServerKey_EndsInConfigurationError()
        {
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.InvalidServerKey));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.ConfigurationError));
        }

        [Test]
        public void StateChanges_AreReportedInOrder()
        {
            var flow = Flow();
            var states = new System.Collections.Generic.List<ConnectionState>();
            flow.StateChanged += states.Add;

            Sync.Run(flow.RunAsync(default));

            Assert.That(states, Is.EqualTo(new[] { ConnectionState.Connecting, ConnectionState.Connected }));
        }
    }
}
