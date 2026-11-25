using System;
using System.Threading;
using System.Threading.Tasks;
using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>US4: sin conexión, reintentos, cancelación y resultados tardíos (FR-014 a FR-017).</summary>
    public sealed class ResilienceTests
    {
        private FakeGameServer _server;
        private InMemoryInstallationIdStore _installation;
        private InMemorySessionStore _sessions;
        private FakeClock _clock;
        private RecordingDelay _delay;

        [SetUp]
        public void SetUp()
        {
            _server = new FakeGameServer();
            _installation = new InMemoryInstallationIdStore { Stored = "0123456789abcdef0123456789abcdef" };
            _sessions = new InMemorySessionStore();
            _clock = new FakeClock();
            _delay = new RecordingDelay(_clock);
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
                Delay = _delay,
                Random = new Random(7),
            });
        }

        private void FailAuthentication(int times)
        {
            for (var i = 0; i < times; i++)
            {
                _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.Transient));
            }
        }

        [Test]
        public void ServerUnavailable_RetriesThreeTimes_ThenGoesOffline()
        {
            FailAuthentication(10);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Offline));
            Assert.That(_server.AuthenticatedInstallationIds.Count, Is.EqualTo(4), "Primer intento + 3 reintentos");
            Assert.That(_delay.Waits.Count, Is.EqualTo(3));
        }

        [Test]
        public void TransientFailureThenSuccess_Connects()
        {
            FailAuthentication(2);
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.AuthenticatedInstallationIds.Count, Is.EqualTo(3));
        }

        [Test]
        public void TimedOutProtocolCall_IsRetriedAsTransient()
        {
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Fail(FailureKind.Transient));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(2));
        }

        [Test]
        public void DefinitiveRejections_AreNotRetried()
        {
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Fail(FailureKind.InvalidServerKey));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.ConfigurationError));
            Assert.That(_delay.Waits, Is.Empty);
        }

        [Test]
        public void ManualRetryFromOffline_RunsTheWholeFlowAgain()
        {
            FailAuthentication(4);
            var flow = Flow();
            Sync.Run(flow.RunAsync(default));
            Assert.That(flow.State, Is.EqualTo(ConnectionState.Offline));

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(1));
        }

        [Test]
        public void Cancellation_StopsWithoutAnErrorState()
        {
            FailAuthentication(10);
            var flow = Flow();
            var states = new System.Collections.Generic.List<ConnectionState>();
            flow.StateChanged += states.Add;
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => flow.RunAsync(cts.Token).GetAwaiter().GetResult());
            }

            Assert.That(states, Has.None.EqualTo(ConnectionState.Offline));
            Assert.That(states, Has.None.EqualTo(ConnectionState.AccountError));
            Assert.That(states, Has.None.EqualTo(ConnectionState.ConfigurationError));
        }

        [Test]
        public void LateResultOfASupersededAttempt_IsDiscarded()
        {
            var firstCallGate = new TaskCompletionSource<bool>();
            var calls = 0;
            _server.BeforeEachCall = _ => ++calls == 1 ? firstCallGate.Task : Task.CompletedTask;
            // El intento vigente consume su resultado antes; el superado, después (al abrirse la puerta).
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Success(Sessions.For("user-vigente")));
            _server.AuthResults.Enqueue(() => ServerResult<AuthenticatedSession>.Success(Sessions.For("user-tardio")));
            var flow = Flow();

            var first = flow.RunAsync(default);
            Sync.Run(flow.RunAsync(default));
            firstCallGate.SetResult(true);
            first.GetAwaiter().GetResult();

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(flow.Session.UserId, Is.EqualTo("user-vigente"));
            Assert.That(_sessions.Stored.AuthToken, Is.EqualTo("auth-user-vigente"), "El intento superado no guarda su sesión.");
        }
    }
}
