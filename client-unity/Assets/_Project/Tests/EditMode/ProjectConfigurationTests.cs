using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecosystem.Tests.EditMode
{
    /// <summary>
    /// Invariantes de configuración del proyecto que exige la spec 001 (FR-006, FR-008, FR-037).
    /// Comprueban ajustes del editor, no lógica de juego.
    /// </summary>
    public sealed class ProjectConfigurationTests
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        // Identificador provisional hasta resolver D-06 (docs/decisions/pendientes.md).
        private const string ProvisionalApplicationIdentifier = "com.ecosystem.dev";

        private static readonly TestCaseData[] MobileTargets =
        {
            new TestCaseData(NamedBuildTarget.Android).SetName("{m}(Android)"),
            new TestCaseData(NamedBuildTarget.iOS).SetName("{m}(iOS)")
        };

        [Test]
        public void BuildScenes_ContainOnlyBootstrapSceneAtIndexZero()
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(enabledScenes.Select(scene => scene.path), Is.EqualTo(new[] { BootstrapScenePath }));
        }

        [TestCaseSource(nameof(MobileTargets))]
        public void ScriptingBackend_IsIl2Cpp(NamedBuildTarget target)
        {
            Assert.That(PlayerSettings.GetScriptingBackend(target), Is.EqualTo(ScriptingImplementation.IL2CPP));
        }

        [Test]
        public void AndroidTargetArchitectures_AreArm64Only()
        {
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
        }

        [Test]
        public void AssetSerialization_IsForceText()
        {
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText));
        }

        [Test]
        public void MetaFiles_AreVisible()
        {
            Assert.That(VersionControlSettings.mode, Is.EqualTo("Visible Meta Files"));
        }

        [Test]
        public void DefaultRenderPipeline_IsUrp()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
        }

        [Test]
        public void EveryQualityLevel_UsesUrp()
        {
            var levelCount = QualitySettings.names.Length;
            Assert.That(levelCount, Is.GreaterThan(0));

            for (var level = 0; level < levelCount; level++)
            {
                var pipeline = QualitySettings.GetRenderPipelineAssetAt(level);
                Assert.That(
                    pipeline,
                    Is.Null.Or.InstanceOf<UniversalRenderPipelineAsset>(),
                    $"El nivel de calidad '{QualitySettings.names[level]}' usa un pipeline que no es URP.");
            }
        }

        [TestCaseSource(nameof(MobileTargets))]
        public void ApplicationIdentifier_IsProvisionalValue(NamedBuildTarget target)
        {
            Assert.That(PlayerSettings.GetApplicationIdentifier(target), Is.EqualTo(ProvisionalApplicationIdentifier));
        }
    }
}
