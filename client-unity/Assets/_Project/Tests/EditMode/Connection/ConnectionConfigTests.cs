using System.Collections.Generic;
using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>contracts/client-config.md y data-model.md → Configuración de conexión (FR-020).</summary>
    public sealed class ConnectionConfigTests
    {
        private const string ValidFile = "{\"host\":\"127.0.0.1\",\"port\":7350,\"serverKey\":\"clave-secreta-xyz\",\"useSsl\":false}";

        private static ConfigResult Load(Dictionary<string, string> env, string file)
        {
            return ConnectionConfigParser.Load(name => env != null && env.TryGetValue(name, out var value) ? value : null, file);
        }

        [Test]
        public void ValidFile_ProducesConfig()
        {
            var result = Load(null, ValidFile);

            Assert.That(result.IsValid, Is.True, result.Problem);
            Assert.That(result.Config.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(result.Config.Port, Is.EqualTo(7350));
            Assert.That(result.Config.ServerKey, Is.EqualTo("clave-secreta-xyz"));
            Assert.That(result.Config.UseSsl, Is.False);
        }

        [Test]
        public void CompleteEnvironment_TakesPrecedenceOverFile()
        {
            var env = new Dictionary<string, string>
            {
                ["ECOSYSTEM_NAKAMA_HOST"] = "10.0.0.1",
                ["ECOSYSTEM_NAKAMA_PORT"] = "17350",
                ["ECOSYSTEM_NAKAMA_SERVER_KEY"] = "clave-env",
            };

            var result = Load(env, ValidFile);

            Assert.That(result.Config.Host, Is.EqualTo("10.0.0.1"));
            Assert.That(result.Config.Port, Is.EqualTo(17350));
            Assert.That(result.Config.ServerKey, Is.EqualTo("clave-env"));
        }

        [Test]
        public void ConfigNone_ForcesConfigurationError_EvenWithFile()
        {
            var result = Load(new Dictionary<string, string> { ["ECOSYSTEM_NAKAMA_CONFIG"] = "none" }, ValidFile);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Problem, Does.Contain("ECOSYSTEM_NAKAMA_CONFIG"));
        }

        [Test]
        public void MissingConfiguration_NamesTheFileToCreate()
        {
            var result = Load(null, null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Problem, Does.Contain("client-unity/LocalConfig/connection.local.json"));
            Assert.That(result.Problem, Does.Contain("connection.example.json"));
        }

        [TestCase("{\"port\":7350,\"serverKey\":\"clave-secreta-xyz\"}", "host")]
        [TestCase("{\"host\":\"127.0.0.1\",\"port\":0,\"serverKey\":\"clave-secreta-xyz\"}", "port")]
        [TestCase("{\"host\":\"127.0.0.1\",\"port\":\"x\",\"serverKey\":\"clave-secreta-xyz\"}", "port")]
        [TestCase("{\"host\":\"127.0.0.1\",\"port\":7350,\"serverKey\":\"\"}", "serverKey")]
        [TestCase("{\"host\":\"127.0.0.1\",\"port\":7350,\"serverKey\":\"clave-secreta-xyz\",\"useSsl\":\"si\"}", "useSsl")]
        [TestCase("{no es json", "JSON")]
        public void InvalidField_NamesTheField_ButNeverTheKeyValue(string file, string field)
        {
            var result = Load(null, file);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Problem, Does.Contain(field));
            Assert.That(result.Problem, Does.Not.Contain("clave-secreta-xyz"));
        }
    }
}
