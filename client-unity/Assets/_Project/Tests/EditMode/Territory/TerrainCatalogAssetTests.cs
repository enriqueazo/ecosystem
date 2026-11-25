using System.Linq;
using Ecosystem.Territory;
using Ecosystem.Territory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → TerrainCatalogAsset (research.md R-5): resolución por clave, claves que
    /// faltan, la primera entrada gana y la vista no crea piezas si falta algo.
    /// </summary>
    public sealed class TerrainCatalogAssetTests
    {
        private GameObject _grass;
        private GameObject _snow;
        private GameObject _otherSnow;
        private GameObject _river;
        private Mesh _curve3;
        private Mesh _otherCurve3;
        private TerrainCatalogAsset _catalog;

        private static GameObject FakePrefab(string name)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(false);
            return go;
        }

        [SetUp]
        public void CreateCatalog()
        {
            _grass = FakePrefab("Grass");
            _snow = FakePrefab("Snow");
            _otherSnow = FakePrefab("OtherSnow");
            _river = FakePrefab("River");
            _curve3 = new Mesh { name = "Curve3" };
            _otherCurve3 = new Mesh { name = "OtherCurve3" };
            _catalog = ScriptableObject.CreateInstance<TerrainCatalogAsset>();
            _catalog.SetEntries(
                new[] { (TerrainKind.Grass, _grass), (TerrainKind.Snow, _snow), (TerrainKind.Snow, _otherSnow) },
                _river,
                new[] { (RiverShape.Curve, 3, _curve3), (RiverShape.Curve, 3, _otherCurve3) });
        }

        [TearDown]
        public void DestroyCatalog()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_curve3);
            Object.DestroyImmediate(_otherCurve3);
            foreach (var go in new[] { _grass, _snow, _otherSnow, _river }) Object.DestroyImmediate(go);
        }

        [Test]
        public void ResolvesTerrainPrefabs_TheFirstEntryWins()
        {
            Assert.That(_catalog.TryGetTerrainPrefab(TerrainKind.Grass, out var grass), Is.True);
            Assert.That(grass, Is.SameAs(_grass));
            Assert.That(_catalog.TryGetTerrainPrefab(TerrainKind.Snow, out var snow), Is.True);
            Assert.That(snow, Is.SameAs(_snow));
            Assert.That(_catalog.TryGetTerrainPrefab(TerrainKind.Sand, out _), Is.False);
        }

        [Test]
        public void ResolvesRiverMeshesByShapeAndCanonicalRotation_TheFirstEntryWins()
        {
            Assert.That(_catalog.RiverPrefab, Is.SameAs(_river));
            Assert.That(_catalog.TryGetRiverMesh(RiverShape.Curve, 3, out var mesh), Is.True);
            Assert.That(mesh, Is.SameAs(_curve3));
            Assert.That(_catalog.TryGetRiverMesh(RiverShape.Curve, 2, out _), Is.False);
        }

        [Test]
        public void Missing_ListsTheRequiredKeysWithoutResources()
        {
            var required = new[]
            {
                VisualKey.ForTerrain(TerrainKind.Grass),
                VisualKey.ForTerrain(TerrainKind.Sand),
                VisualKey.ForRiver(RiverShape.Curve, 3),
                VisualKey.ForRiver(RiverShape.Spring, 0),
            };

            Assert.That(_catalog.Missing(required), Is.EqualTo(new[]
            {
                VisualKey.ForTerrain(TerrainKind.Sand),
                VisualKey.ForRiver(RiverShape.Spring, 0),
            }));
        }

        [Test]
        public void Missing_WithoutRiverPrefab_ListsEveryRiverKey()
        {
            _catalog.SetEntries(new[] { (TerrainKind.Grass, _grass) }, null, new[] { (RiverShape.Curve, 3, _curve3) });

            Assert.That(_catalog.Missing(new[] { VisualKey.ForRiver(RiverShape.Curve, 3) }), Is.EqualTo(new[] { VisualKey.ForRiver(RiverShape.Curve, 3) }));
        }

        [Test]
        public void View_WithAnIncompleteCatalog_LogsTheMissingVisualAndCreatesNoPieces()
        {
            var layout = ScriptableObject.CreateInstance<TerritoryLayoutAsset>();
            layout.SetCells(new[]
            {
                new TerritoryCell(new HexCell(0, 0), TerrainKind.Grass),
                new TerritoryCell(new HexCell(1, 0), TerrainKind.Sand),
            });
            var root = new GameObject("Territory") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            var view = root.AddComponent<TerritoryView>();
            try
            {
                view.SetLayout(layout);
                view.SetCatalog(_catalog);

                LogAssert.Expect(LogType.Error, "[Territorio] El catálogo no tiene recursos para variante arena.");
                root.SetActive(true);

                Assert.That(view.Tiles, Is.Empty);
                Assert.That(root.transform.childCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void View_WithACompleteCatalog_InstantiatesThePrefabOfEachVariant()
        {
            var layout = ScriptableObject.CreateInstance<TerritoryLayoutAsset>();
            layout.SetCells(new[]
            {
                new TerritoryCell(new HexCell(0, 0), TerrainKind.Grass),
                new TerritoryCell(new HexCell(1, 0), TerrainKind.Snow),
                new TerritoryCell(new HexCell(0, 1), TerrainKind.River, RiverShape.Curve, 3),
            });
            var root = new GameObject("Territory") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            var view = root.AddComponent<TerritoryView>();
            try
            {
                view.SetLayout(layout);
                view.SetCatalog(_catalog);
                root.SetActive(true);

                Assert.That(view.Tiles, Has.Count.EqualTo(3));
                Assert.That(view.Tiles[0].name, Does.StartWith("Grass"));
                Assert.That(view.Tiles[1].name, Does.StartWith("Snow"));
                Assert.That(view.Tiles[2].name, Does.StartWith("River"));
                Assert.That(view.Tiles[2].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(_curve3));
                Assert.That(view.Tiles.Select(tile => tile.localRotation), Is.All.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(layout);
            }
        }

        // --- El catálogo del proyecto (data-model.md → TerrainCatalogAsset.terrains) ---

        private const string CatalogPath = "Assets/_Project/Data/Territory/TerrainCatalog.asset";
        private const string Prefabs = "Assets/_Project/Art/Territory/Prefabs";

        [TestCase(TerrainKind.Grass, "HexTile.prefab")]
        [TestCase(TerrainKind.Snow, "SnowTile.prefab")]
        [TestCase(TerrainKind.Sand, "SandTile.prefab")]
        [TestCase(TerrainKind.Water, "WaterTile.prefab")]
        public void ProjectCatalog_MapsEachTerrainToItsPrefab(TerrainKind kind, string prefab)
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainCatalogAsset>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Falta {CatalogPath}.");

            Assert.That(catalog.TryGetTerrainPrefab(kind, out var actual), Is.True, kind.ToString());
            Assert.That(actual, Is.EqualTo(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{prefab}")));
            Assert.That(catalog.TerrainEntryCount(kind), Is.EqualTo(1), "Una sola entrada por variante.");
        }
    }
}
