using System.Collections;
using Ecosystem.Connection;
using Ecosystem.Connection.Nakama;
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

        private ConnectionFlow NewFlow(MemoryInstallationIdStore installation, MemorySessionStore sessions)
        {
            var server = new NakamaGameServer(_config);
            return new ConnectionFlow(new ConnectionDependencies
            {
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
