using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>US3: versión de protocolo y contrato de la respuesta (FR-010 a FR-012, FR-007).</summary>
    public sealed class ProtocolCompatibilityTests
    {
        private const int ClientVersion = 5;
        private FakeGameServer _server;
        private InMemoryInstallationIdStore _installation;
        private InMemorySessionStore _sessions;

        [SetUp]
        public void SetUp()
        {
            _server = new FakeGameServer();
            _installation = new InMemoryInstallationIdStore { Stored = "0123456789abcdef0123456789abcdef" };
            _sessions = new InMemorySessionStore();
        }

        private ConnectionFlow Flow()
        {
            var clock = new FakeClock();
            return new ConnectionFlow(new ConnectionDependencies
            {
                Config = ConfigResult.Valid(new ConnectionConfig("127.0.0.1", 7350, "clave", false)),
                Server = _server,
                Authenticator = new GuestDeviceAuthenticator(_server, new InstallationIdentity(_installation)),
                Sessions = _sessions,
                Clock = clock,
                Delay = new RecordingDelay(clock),
                Random = new System.Random(1),
                ClientProtocolVersion = ClientVersion,
            });
        }

        private ConnectionState RunWithPayload(string payload)
        {
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Success(payload));
            var flow = Flow();
            Sync.Run(flow.RunAsync(default));
            return flow.State;
        }

        [Test]
        public void SameVersion_Connects()
        {
            Assert.That(RunWithPayload(Sessions.ProtocolPayload(ClientVersion)), Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(1));
        }

        [Test]
        public void NewerServer_RequiresUpdate_WithoutRetrying()
        {
            Assert.That(RunWithPayload(Sessions.ProtocolPayload(ClientVersion + 1)), Is.EqualTo(ConnectionState.UpdateRequired));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(1));
        }

        [Test]
        public void OlderServer_IsIncompatible()
        {
            Assert.That(RunWithPayload(Sessions.ProtocolPayload(ClientVersion - 1)), Is.EqualTo(ConnectionState.ServerIncompatible));
        }

        [TestCase("{\"moduleVersion\":\"0.1.0\"}", TestName = "Falta protocolVersion")]
        [TestCase("{\"protocolVersion\":\"5\",\"moduleVersion\":\"0.1.0\"}", TestName = "protocolVersion no es entero")]
        [TestCase("{\"protocolVersion\":5,\"moduleVersion\":\"0.1.0\",\"node\":\"x\"}", TestName = "Campo extra")]
        [TestCase("{no-json", TestName = "No es JSON")]
        [TestCase("[]", TestName = "No es objeto")]
        public void ResponseThatBreaksTheContract_IsIncompatible_WithoutRetrying(string payload)
        {
            Assert.That(RunWithPayload(payload), Is.EqualTo(ConnectionState.ServerIncompatible));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(1));
        }

        [Test]
        public void SessionRejectedByTheRpc_ReauthenticatesOnce_AndRetriesTheRpc()
        {
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Fail(FailureKind.SessionRejected));
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Success(Sessions.ProtocolPayload(ClientVersion)));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(2));
            Assert.That(_server.AuthenticatedInstallationIds.Count, Is.EqualTo(2), "Alta inicial + una reautenticación");
        }

        [Test]
        public void SessionRejectedTwice_EndsInAccountError()
        {
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Fail(FailureKind.SessionRejected));
            _server.ProtocolResults.Enqueue(() => ServerResult<string>.Fail(FailureKind.SessionRejected));
            var flow = Flow();

            Sync.Run(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.AccountError));
            Assert.That(_server.ProtocolCalls, Is.EqualTo(2));
        }

        [Test]
        public void DefaultClientVersion_ComesFromTheSharedContracts()
        {
            var deps = new ConnectionDependencies();

            Assert.That(deps.ClientProtocolVersion, Is.EqualTo(Ecosystem.Contracts.ProtocolContracts.ProtocolVersion));
        }
    }

    /// <summary>FR-012: el error del RPC se lee de code/message; el campo error de Nakama se ignora.</summary>
    public sealed class StandardErrorTests
    {
        [Test]
        public void ParsesTheContractCodeFromMessage_IgnoringNakamaErrorField()
        {
            var body = "{\"code\":7,\"error\":{\"stackTrace\":\"secreto interno\"},\"message\":\"{\\\"code\\\":\\\"SESSION_REQUIRED\\\",\\\"message\\\":\\\"x\\\"}\"}";

            Assert.That(StandardError.TryParseCode(body, out var code), Is.True);
            Assert.That(code, Is.EqualTo("SESSION_REQUIRED"));
        }

        [TestCase("")]
        [TestCase("no-json")]
        [TestCase("{\"code\":5,\"message\":\"RPC function not found\"}")]
        public void ReturnsFalse_WhenThereIsNoStandardError(string body)
        {
            Assert.That(StandardError.TryParseCode(body, out _), Is.False);
        }
    }
}
