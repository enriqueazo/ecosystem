using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Arte aprobado del territorio (contracts/visual-direction.md §3 de las specs 004 y 005): texturas
    /// de 512 px con su importación para móvil, materiales mate que las usan y prefabs por variante.
    /// Las comprobaciones de la hierba son las de la spec 004, sin cambios (spec 005 FR-006).
    /// </summary>
    public sealed class TerritoryArtAssetTests
    {
        private const string Art = "Assets/_Project/Art/Territory";

        private static readonly object[] Textures =
        {
            new object[] { $"{Art}/Textures/T_Grass_Top.png", TextureWrapMode.Clamp, TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Earth_Side.png", TextureWrapMode.Repeat, TextureWrapMode.Clamp },
            // Spec 005 (contracts/visual-direction.md §3).
            new object[] { $"{Art}/Textures/T_Snow_Top.png", TextureWrapMode.Clamp, TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Snow_Side.png", TextureWrapMode.Repeat, TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Sand_Top.png", TextureWrapMode.Clamp, TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Sand_Side.png", TextureWrapMode.Repeat, TextureWrapMode.Clamp },
            new object[] { $"{Art}/Textures/T_Water_Top.png", TextureWrapMode.Repeat, TextureWrapMode.Repeat },
            new object[] { $"{Art}/Textures/T_Water_Side.png", TextureWrapMode.Repeat, TextureWrapMode.Clamp },
        };

        private static readonly object[] Materials =
        {
            new object[] { $"{Art}/Materials/M_Grass_Top.mat", $"{Art}/Textures/T_Grass_Top.png" },
            new object[] { $"{Art}/Materials/M_Earth_Side.mat", $"{Art}/Textures/T_Earth_Side.png" },
            new object[] { $"{Art}/Materials/M_Snow_Top.mat", $"{Art}/Textures/T_Snow_Top.png" },
            new object[] { $"{Art}/Materials/M_Snow_Side.mat", $"{Art}/Textures/T_Snow_Side.png" },
            new object[] { $"{Art}/Materials/M_Sand_Top.mat", $"{Art}/Textures/T_Sand_Top.png" },
            new object[] { $"{Art}/Materials/M_Sand_Side.mat", $"{Art}/Textures/T_Sand_Side.png" },
            new object[] { $"{Art}/Materials/M_Water_Top.mat", $"{Art}/Textures/T_Water_Top.png" },
            new object[] { $"{Art}/Materials/M_Water_Side.mat", $"{Art}/Textures/T_Water_Side.png" },
        };

        private static readonly object[] Prefabs =
        {
            new object[] { $"{Art}/Prefabs/HexTile.prefab", $"{Art}/Meshes/HexTile.asset", new[] { $"{Art}/Materials/M_Grass_Top.mat", $"{Art}/Materials/M_Earth_Side.mat" } },
            new object[] { $"{Art}/Prefabs/SnowTile.prefab", $"{Art}/Meshes/HexTile.asset", new[] { $"{Art}/Materials/M_Snow_Top.mat", $"{Art}/Materials/M_Snow_Side.mat" } },
            new object[] { $"{Art}/Prefabs/SandTile.prefab", $"{Art}/Meshes/HexTile.asset", new[] { $"{Art}/Materials/M_Sand_Top.mat", $"{Art}/Materials/M_Sand_Side.mat" } },
            new object[] { $"{Art}/Prefabs/WaterTile.prefab", $"{Art}/Meshes/WaterTile.asset", new[] { $"{Art}/Materials/M_Water_Top.mat", $"{Art}/Materials/M_Water_Side.mat" } },
        };

        /// <summary>Cada prefab de variante: su malla, sus materiales por submalla y sin sombras.</summary>
        [TestCaseSource(nameof(Prefabs))]
        public void Prefab_UsesItsMeshAndMaterialsWithoutShadows(string prefabPath, string meshPath, string[] materialPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, $"Falta {prefabPath}.");

            var filter = prefab.GetComponent<MeshFilter>();
            var renderer = prefab.GetComponent<MeshRenderer>();
            Assert.That(prefab.GetComponents<Component>(), Has.Length.EqualTo(3), "Solo Transform, MeshFilter y MeshRenderer.");
            Assert.That(filter.sharedMesh, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)));
            Assert.That(renderer.sharedMaterials, Is.EqualTo(System.Array.ConvertAll(materialPaths, AssetDatabase.LoadAssetAtPath<Material>)));
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
        }

        [TestCaseSource(nameof(Textures))]
        public void Texture_IsImportedForMobileAt512(string path, TextureWrapMode wrapU, TextureWrapMode wrapV)
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
            Assert.That(importer.wrapModeV, Is.EqualTo(wrapV));

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

        /// <summary>RiverTile: sin malla propia (la pone la vista), orilla, tierra y agua, sin sombras.</summary>
        [Test]
        public void RiverTilePrefab_HasGrassEarthAndWaterMaterialsWithoutAMesh()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Prefabs/RiverTile.prefab");
            Assert.That(prefab, Is.Not.Null, "Falta RiverTile.prefab.");

            var renderer = prefab.GetComponent<MeshRenderer>();
            Assert.That(prefab.GetComponents<Component>(), Has.Length.EqualTo(3), "Solo Transform, MeshFilter y MeshRenderer.");
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh, Is.Null);
            Assert.That(renderer.sharedMaterials, Is.EqualTo(new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>($"{Art}/Materials/M_Grass_Top.mat"),
                AssetDatabase.LoadAssetAtPath<Material>($"{Art}/Materials/M_Earth_Side.mat"),
                AssetDatabase.LoadAssetAtPath<Material>($"{Art}/Materials/M_Water_Top.mat"),
            }));
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
        }
    }
}
