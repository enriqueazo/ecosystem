using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Arte aprobado del territorio (contracts/visual-direction.md §3; FR-012, FR-014): texturas de
    /// 512 px con su importación para móvil y materiales mate que las usan.
    /// </summary>
    public sealed class TerritoryArtAssetTests
    {
        private const string Art = "Assets/_Project/Art/Territory";

        private static readonly object[] Textures =
        {
            new object[] { $"{Art}/Textures/T_Grass_Top.png", TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Earth_Side.png", TextureWrapMode.Repeat },
        };

        private static readonly object[] Materials =
        {
            new object[] { $"{Art}/Materials/M_Grass_Top.mat", $"{Art}/Textures/T_Grass_Top.png" },
            new object[] { $"{Art}/Materials/M_Earth_Side.mat", $"{Art}/Textures/T_Earth_Side.png" },
        };

        [TestCaseSource(nameof(Textures))]
        public void Texture_IsImportedForMobileAt512(string path, TextureWrapMode wrapU)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, $"Falta {path}.");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture.width, Is.EqualTo(512));
            Assert.That(texture.height, Is.EqualTo(512));
            Assert.That(importer.maxTextureSize, Is.EqualTo(512));
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.mipmapEnabled, Is.True);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(importer.wrapModeU, Is.EqualTo(wrapU));
            Assert.That(importer.wrapModeV, Is.EqualTo(TextureWrapMode.Clamp));

            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                Assert.That(settings.overridden, Is.True, platform);
                Assert.That(settings.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6), platform);
                Assert.That(settings.maxTextureSize, Is.EqualTo(512), platform);
            }
        }

        [TestCaseSource(nameof(Materials))]
        public void Material_UsesItsTextureWithAWhiteTintAndNoSpecular(string materialPath, string texturePath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, $"Falta {materialPath}.");

            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Simple Lit"));
            Assert.That(material.GetTexture("_BaseMap"), Is.EqualTo(AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)));
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(Color.white));
            Assert.That(material.GetFloat("_SpecularHighlights"), Is.EqualTo(1f), "Sin reflejos especulares (mate).");
            Assert.That(material.IsKeywordEnabled("_SPECULAR_COLOR"), Is.False);
            Assert.That(material.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"), Is.True);
        }
    }
}
