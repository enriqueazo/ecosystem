using Ecosystem.Connection;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Connection
{
    /// <summary>data-model.md → Identificador de instalación (FR-002).</summary>
    public sealed class InstallationIdTests
    {
        [Test]
        public void GetOrCreate_GeneratesAndSaves_WhenMissing()
        {
            var store = new InMemoryInstallationIdStore();

            var id = new InstallationIdentity(store).GetOrCreate();

            Assert.That(InstallationIdentity.IsValid(id), Is.True, id);
            Assert.That(store.Stored, Is.EqualTo(id));
        }

        [Test]
        public void GetOrCreate_ReusesExistingValidId()
        {
            var store = new InMemoryInstallationIdStore { Stored = "0123456789abcdef0123456789abcdef" };

            var id = new InstallationIdentity(store).GetOrCreate();

            Assert.That(id, Is.EqualTo("0123456789abcdef0123456789abcdef"));
            Assert.That(store.Saves, Is.Zero);
        }

        [TestCase("")]
        [TestCase("demasiado-corto")]
        [TestCase("0123456789ABCDEF0123456789ABCDEF")]
        [TestCase("0123456789abcdef0123456789abcdeg")]
        public void GetOrCreate_RegeneratesOnlyInvalidIds(string invalid)
        {
            var store = new InMemoryInstallationIdStore { Stored = invalid };

            var id = new InstallationIdentity(store).GetOrCreate();

            Assert.That(id, Is.Not.EqualTo(invalid));
            Assert.That(InstallationIdentity.IsValid(id), Is.True);
        }

        [Test]
        public void GeneratedIds_AreDifferentEachTime()
        {
            var first = new InstallationIdentity(new InMemoryInstallationIdStore()).GetOrCreate();
            var second = new InstallationIdentity(new InMemoryInstallationIdStore()).GetOrCreate();

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void Abbreviate_ShowsOnlySixCharacters()
        {
            Assert.That(InstallationIdentity.Abbreviate("0123456789abcdef0123456789abcdef"), Is.EqualTo("012345…"));
        }
    }
}
