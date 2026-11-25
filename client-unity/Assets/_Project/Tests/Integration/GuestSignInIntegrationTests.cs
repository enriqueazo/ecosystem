using System.Collections;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
using Ecosystem.Contracts;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.Integration
{
    /// <summary>US1 contra el Nakama local: alta de invitado (spec 003).</summary>
    public sealed class GuestSignInIntegrationTests
    {
        private ConnectionConfig _config;

        private IEnumerator RequireEnvironment()
        {
            yield return TaskYield.Await(IntegrationEnvironment.RequireAsync(), config => _config = config);
        }

        private ConnectionFlow NewFlow(MemoryInstallationIdStore installation, MemorySessionStore sessions, int? clientProtocolVersion = null)
        {
            var server = new NakamaGameServer(_config);
            return new ConnectionFlow(new ConnectionDependencies
            {
                ClientProtocolVersion = clientProtocolVersion ?? ProtocolContracts.ProtocolVersion,
                Config = ConfigResult.Valid(_config),
                Server = server,
                Authenticator = new GuestDeviceAuthenticator(server, new InstallationIdentity(installation)),
                Sessions = sessions,
                Clock = new SystemClock(),
                Delay = new TaskDelay(),
                Random = new System.Random(),
            });
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator NewInstallation_CreatesAccount_AndConnects()
        {
            yield return RequireEnvironment();
            var installation = new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() };
            var flow = NewFlow(installation, new MemorySessionStore());

            yield return TaskYield.Await(flow.RunAsync(default));

            Assert.That(flow.State, Is.EqualTo(ConnectionState.Connected), flow.Problem);
            Assert.That(flow.Session.UserId, Is.Not.Empty);
            Assert.That(flow.Session.UserId, Is.Not.EqualTo(installation.Stored));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ProtocolVersionRpc_ReturnsAResponseThatMatchesTheContract()
        {
            yield return RequireEnvironment();
            var server = new NakamaGameServer(_config);
            ServerResult<AuthenticatedSession> session = default;
            yield return TaskYield.Await(server.AuthenticateDeviceAsync(IntegrationEnvironment.NewInstallationId(), default), s => session = s);
            Assert.That(session.IsSuccess, Is.True, session.Failure.ToString());

            ServerResult<string> rpc = default;
            yield return TaskYield.Await(server.GetProtocolVersionAsync(session.Value, default), r => rpc = r);

            Assert.That(rpc.IsSuccess, Is.True, rpc.Failure.ToString());
            var response = JToken.Parse(rpc.Value);
            Assert.That(ProtocolVersionContract.ValidateResponse(response), Is.True, rpc.Value);
            Assert.That((int)response["protocolVersion"], Is.EqualTo(ProtocolContracts.ProtocolVersion));
        }

        // quickstart B9 / SC-004: versión del cliente por detrás y por delante de la del servidor real.
        [UnityTest, Timeout(60000)]
        public IEnumerator OlderClient_RequiresUpdate_NewerClient_IsServerIncompatible()
        {
            yield return RequireEnvironment();
            var older = NewFlow(new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() }, new MemorySessionStore(),
                ProtocolContracts.ProtocolVersion - 1);
            var newer = NewFlow(new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() }, new MemorySessionStore(),
                ProtocolContracts.ProtocolVersion + 1);

            yield return TaskYield.Await(older.RunAsync(default));
            yield return TaskYield.Await(newer.RunAsync(default));

            Assert.That(older.State, Is.EqualTo(ConnectionState.UpdateRequired), older.Problem);
            Assert.That(newer.State, Is.EqualTo(ConnectionState.ServerIncompatible), newer.Problem);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TwoInstallations_GetDifferentAccounts()
        {
            yield return RequireEnvironment();
            var first = NewFlow(new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() }, new MemorySessionStore());
            var second = NewFlow(new MemoryInstallationIdStore { Stored = IntegrationEnvironment.NewInstallationId() }, new MemorySessionStore());

            yield return TaskYield.Await(first.RunAsync(default));
            yield return TaskYield.Await(second.RunAsync(default));

            Assert.That(first.State, Is.EqualTo(ConnectionState.Connected), first.Problem);
            Assert.That(second.State, Is.EqualTo(ConnectionState.Connected), second.Problem);
            Assert.That(first.Session.UserId, Is.Not.EqualTo(second.Session.UserId));
        }
    }
}
