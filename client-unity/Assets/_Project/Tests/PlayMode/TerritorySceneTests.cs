using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Ecosystem.Presentation;
using Ecosystem.Territory;
using Ecosystem.Territory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ecosystem.Tests.PlayMode
{
    /// <summary>
    /// Escena del territorio sin servidor (contracts/territory-scene.md, invariantes 1, 2 y 4, y
    /// composición; FR-009, FR-010, FR-014, FR-016, SC-001, SC-002).
    /// </summary>
    public sealed class TerritorySceneTests
    {
        public const string TerritoryScenePath = "Assets/_Project/Scenes/Territory.unity";

        // Valores aprobados (contracts/visual-direction.md §2). Hasta la aprobación, los de partida.
        private static readonly Color32 ApprovedBackground = new Color32(0x8E, 0x9B, 0xAA, 0xFF);
        private const float ApprovedTiltDegrees = 35f;

        private TerritoryView _view;
        private Camera _camera;

        public static IEnumerator LoadTerritoryScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TerritoryScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(TerritoryScenePath, LoadSceneMode.Single);
#endif
            yield return null;
        }

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return LoadTerritoryScene();
            _view = Object.FindAnyObjectByType<TerritoryView>();
            _camera = Object.FindAnyObjectByType<Camera>();
        }

        [Test]
        public void Scene_LoadsWithoutErrorsOrWarnings()
        {
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(TerritoryScenePath));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Scene_ShowsFiftyFivePiecesAtTheirLayoutPositionsWithoutRotation()
        {
            Assert.That(_view, Is.Not.Null, "La escena necesita un TerritoryView.");
            Assert.That(_view.Tiles, Has.Count.EqualTo(55));
            Assert.That(_view.Tiles.Select(tile => tile.localRotation), Is.All.EqualTo(Quaternion.identity));

            var expected = TerritoryLayout.Create(DiscCells()).Layout;
            var positions = _view.Tiles.Select(tile => tile.localPosition).ToArray();
            foreach (var cell in expected.Cells)
            {
                var center = HexMetrics.Default.Center(cell);
                var target = new Vector3(center.X, center.Y, center.Z);
                Assert.That(positions.Any(p => Vector3.Distance(p, target) < 1e-4f), Is.True, $"Falta la pieza en {cell}.");
            }
        }

        /// <summary>
        /// Invariantes 3 y 6 de contracts/territory-scene.md (spec 005): cada pieza usa la malla y
        /// los materiales del prefab de su variante y ninguna proyecta ni recibe sombras. La hierba
        /// sigue siendo HexTile, como en la spec 004.
        /// </summary>
        [Test]
        public void Pieces_UseTheResourcesOfTheirVariantAndCastNoShadows()
        {
            var catalog = _view.Catalog;
            Assert.That(catalog, Is.Not.Null, "TerritoryView necesita el catálogo.");
            Assert.That(_view.Tiles, Has.Count.EqualTo(_view.Layout.Entries.Count));

            for (var i = 0; i < _view.Tiles.Count; i++)
            {
                var entry = _view.Layout.Entries[i];
                var tile = _view.Tiles[i];
                var filter = tile.GetComponent<MeshFilter>();
                var renderer = tile.GetComponent<MeshRenderer>();
                Assert.That(filter.sharedMesh, Is.Not.Null, entry.ToString());
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off), entry.ToString());
                Assert.That(renderer.receiveShadows, Is.False, entry.ToString());

                if (entry.Terrain == TerrainKind.River)
                {
                    Assert.That(catalog.TryGetRiverMesh(entry.River, RiverPiece.Canonical(entry.River, entry.Rotation), out var riverMesh), Is.True);
                    Assert.That(filter.sharedMesh, Is.SameAs(riverMesh), entry.ToString());
                    Assert.That(renderer.sharedMaterials, Is.EqualTo(catalog.RiverPrefab.GetComponent<MeshRenderer>().sharedMaterials), entry.ToString());
                    continue;
                }

                Assert.That(catalog.TryGetTerrainPrefab(entry.Terrain, out var prefab), Is.True, entry.ToString());
                Assert.That(filter.sharedMesh, Is.SameAs(prefab.GetComponent<MeshFilter>().sharedMesh), entry.ToString());
                Assert.That(renderer.sharedMaterials, Is.EqualTo(prefab.GetComponent<MeshRenderer>().sharedMaterials), entry.ToString());
                if (entry.Terrain == TerrainKind.Grass)
                {
                    Assert.That(filter.sharedMesh.name, Is.EqualTo("HexTile"));
                    Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2), "Césped (submalla 0) y tierra (submalla 1).");
                }
            }
        }

        /// <summary>La composición de la escena usa las cuatro variantes (spec FR-017).</summary>
        [Test]
        public void Scene_ShowsTheFourVariants()
        {
            var kinds = _view.Layout.Entries.Select(entry => entry.Terrain).Distinct().ToArray();

            Assert.That(kinds, Is.SupersetOf(new[] { TerrainKind.Grass, TerrainKind.Snow, TerrainKind.Sand, TerrainKind.Water }));
        }

        [Test]
        public void Camera_IsOrthographicTiltedWithoutYawOverASolidBackground()
        {
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(_camera.orthographic, Is.True);

            var euler = _camera.transform.rotation.eulerAngles;
            Assert.That(euler.x, Is.EqualTo(ApprovedTiltDegrees).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(euler.y, 0f), Is.EqualTo(0f).Within(0.01f), "Sin giro horizontal.");
            Assert.That(Mathf.DeltaAngle(euler.z, 0f), Is.EqualTo(0f).Within(0.01f), "Sin alabeo.");

            Assert.That(_camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Color32 background = _camera.backgroundColor;
            Assert.That(background, Is.EqualTo(ApprovedBackground));
            Assert.That(_camera.GetUniversalAdditionalCameraData().renderPostProcessing, Is.False, "Sin posprocesado.");
        }

        [Test]
        public void Lighting_HasNoSkyNoFogFlatAmbientAndNoShadows()
        {
            Assert.That(RenderSettings.skybox, Is.Null);
            Assert.That(RenderSettings.fog, Is.False);
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Assert.That(lights, Has.Length.EqualTo(1));
            Assert.That(lights[0].type, Is.EqualTo(LightType.Directional));
            Assert.That(lights[0].shadows, Is.EqualTo(LightShadows.None));
        }

        // Lista cerrada de componentes: excluye animación, partículas, audio, volúmenes y conexión
        // (contracts/territory-scene.md). Los módulos de animación, partículas y audio ni siquiera
        // están en el proyecto.
        private static readonly System.Type[] AllowedComponents =
        {
            typeof(Transform), typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(Light),
            typeof(UniversalAdditionalLightData), typeof(TerritoryView), typeof(TerritoryCamera),
            typeof(MeshFilter), typeof(MeshRenderer),
        };

        [Test]
        public void Scene_OnlyHasTerritoryCameraAndLightComponents()
        {
            var components = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true));
            foreach (var component in components)
            {
                Assert.That(
                    AllowedComponents.Any(type => type.IsInstanceOfType(component)),
                    Is.True,
                    $"Componente no permitido en la escena del territorio: {component.GetType().Name} en {component.name}.");
            }

            Assert.That(Object.FindObjectsByType<Volume>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<ConnectionScreen>(FindObjectsSortMode.None), Is.Empty);
        }

        private static readonly float[] ReferenceAspects = { 16f / 9f, 19.5f / 9f, 4f / 3f, 9f / 16f, 9f / 19.5f, 3f / 4f };

        /// <summary>Invariante 3 de contracts/territory-scene.md (FR-018, SC-004).</summary>
        [Test]
        public void Framing_KeepsTheWholeBoardInsideTheMarginForEveryReferenceAspect([ValueSource(nameof(ReferenceAspects))] float aspect)
        {
            var framing = _camera.GetComponent<TerritoryCamera>();
            Assert.That(framing, Is.Not.Null, "La cámara necesita TerritoryCamera.");
            var corners = _view.Layout.Bounds(HexMetrics.Default).Corners();
            try
            {
                const int height = 1000;
                var width = Mathf.RoundToInt(height * aspect);
                _camera.aspect = (float)width / height;
                framing.Apply(width, height, new Rect(0, 0, width, height));

                foreach (var corner in corners)
                {
                    var world = _view.transform.TransformPoint(new Vector3(corner.X, corner.Y, corner.Z));
                    var viewport = _camera.WorldToViewportPoint(world);
                    Assert.That(viewport.x, Is.InRange(0.06f - 1e-3f, 0.94f + 1e-3f), $"aspecto {aspect:0.###}");
                    Assert.That(viewport.y, Is.InRange(0.06f - 1e-3f, 0.94f + 1e-3f), $"aspecto {aspect:0.###}");
                    Assert.That(viewport.z, Is.GreaterThan(_camera.nearClipPlane), "Delante de la cámara.");
                }
            }
            finally
            {
                _camera.ResetAspect();
            }
        }

        /// <summary>Las 55 celdas del disco de la spec 005 (FR-001): q² + q·r + r² ≤ 13 respecto a (4, 4).</summary>
        private static HexCell[] DiscCells()
        {
            var (cq, cr) = new HexCell(4, 4).ToAxial();
            return (from row in Enumerable.Range(-2, 13)
                    from column in Enumerable.Range(-2, 13)
                    let cell = new HexCell(column, row)
                    let axial = cell.ToAxial()
                    let dq = axial.Q - cq
                    let dr = axial.R - cr
                    where dq * dq + dq * dr + dr * dr <= 13
                    select cell).ToArray();
        }

        private static TerritoryLayoutAsset AssetWith(params HexCell[] cells)
        {
            var asset = ScriptableObject.CreateInstance<TerritoryLayoutAsset>();
            asset.SetCells(cells);
            return asset;
        }

        /// <summary>US5: la escena coloca exactamente las piezas de la disposición que recibe.</summary>
        [Test]
        public void View_WithFifteenCells_ShowsFifteenPieces()
        {
            var cells = Enumerable.Range(0, 15).Select(i => new HexCell(i % 4, i / 4)).ToArray();
            _view.SetLayout(AssetWith(cells));

            Assert.That(_view.Tiles, Has.Count.EqualTo(15));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>FR-003: una disposición no válida se rechaza con un error definido y sin piezas.</summary>
        [Test]
        public void View_WithAnEmptyLayout_LogsAnErrorAndShowsNoPieces()
        {
            LogAssert.Expect(LogType.Error, "[Territorio] La disposición del territorio está vacía.");
            _view.SetLayout(AssetWith(new HexCell[0]));

            Assert.That(_view.Tiles, Is.Empty);
        }

        [Test]
        public void View_WithARepeatedCell_LogsAnErrorAndShowsNoPieces()
        {
            LogAssert.Expect(LogType.Error, "[Territorio] La disposición repite la celda (columna 2, fila 1).");
            _view.SetLayout(AssetWith(new HexCell(2, 1), new HexCell(0, 0), new HexCell(2, 1)));

            Assert.That(_view.Tiles, Is.Empty);
        }

        [Test]
        public void View_WithoutALayout_LogsAnErrorAndShowsNoPieces()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^\[Territorio\] "));
            _view.SetLayout(null);

            Assert.That(_view.Tiles, Is.Empty);
        }

        // --- Datos no válidos de la spec 005 (contracts/territory-scene.md → Datos no válidos; SC-006) ---

        private static TerritoryLayoutAsset AssetWith(params TerritoryCell[] cells)
        {
            var asset = ScriptableObject.CreateInstance<TerritoryLayoutAsset>();
            asset.SetCells(cells);
            return asset;
        }

        private static TerritoryCell Cell(int column, int row, TerrainKind kind, RiverShape river = RiverShape.None, int rotation = 0) =>
            new TerritoryCell(new HexCell(column, row), kind, river, rotation);

        private static readonly object[] InvalidLayouts =
        {
            new object[] { "no conexa", new[] { Cell(0, 0, TerrainKind.Grass), Cell(5, 0, TerrainKind.Grass) }, @"^\[Territorio\] La disposición no es conexa" },
            new object[] { "variante desconocida", new[] { Cell(0, 0, TerrainKind.Grass), Cell(1, 0, (TerrainKind)9) }, @"^\[Territorio\] La celda \(columna 1, fila 0\) tiene una variante de terreno desconocida" },
            new object[] { "río sin forma", new[] { Cell(0, 0, TerrainKind.Grass), Cell(1, 0, TerrainKind.River) }, @"^\[Territorio\] La celda \(columna 1, fila 0\) tiene datos de río no válidos" },
            new object[] { "río que acaba en nieve", new[] { Cell(0, 0, TerrainKind.River, RiverShape.Straight, 0), Cell(1, 0, TerrainKind.Snow) }, @"^\[Territorio\] El río de la celda \(columna 0, fila 0\)" },
        };

        [TestCaseSource(nameof(InvalidLayouts))]
        public void View_WithInvalidData_LogsTheCauseAndShowsNoPieces(string name, TerritoryCell[] cells, string message)
        {
            LogAssert.Expect(LogType.Error, new Regex(message));
            _view.SetLayout(AssetWith(cells));

            Assert.That(_view.Tiles, Is.Empty, name);
            Assert.That(_view.GetComponentsInChildren<MeshRenderer>(false), Is.Empty, $"{name}: ninguna pieza visible.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void View_WithACatalogMissingAUsedVariant_LogsItAndShowsNoPieces()
        {
            var complete = _view.Catalog;
            var incomplete = ScriptableObject.CreateInstance<TerrainCatalogAsset>();
            complete.TryGetTerrainPrefab(TerrainKind.Grass, out var grass);
            complete.TryGetTerrainPrefab(TerrainKind.Sand, out var sand);
            complete.TryGetTerrainPrefab(TerrainKind.Water, out var water);
            var meshes = RiverPiece.CanonicalKeys().Select(key =>
            {
                complete.TryGetRiverMesh(key.Shape, key.Rotation, out var mesh);
                return (key.Shape, key.Rotation, mesh);
            });
            incomplete.SetEntries(new[] { (TerrainKind.Grass, grass), (TerrainKind.Sand, sand), (TerrainKind.Water, water) }, complete.RiverPrefab, meshes);

            LogAssert.Expect(LogType.Error, "[Territorio] El catálogo no tiene recursos para variante nieve.");
            _view.SetCatalog(incomplete);

            Assert.That(_view.Tiles, Is.Empty);
            Assert.That(_view.GetComponentsInChildren<MeshRenderer>(false), Is.Empty, "Ninguna pieza visible.");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// US5, escenario 1: otra disposición válida (7 celdas con un río de borde a borde) coloca
        /// exactamente sus piezas, con los recursos de cada una.
        /// </summary>
        [Test]
        public void View_WithAnotherValidLayout_PlacesExactlyItsPiecesAndResources()
        {
            var cells = new[]
            {
                Cell(3, 4, TerrainKind.River, RiverShape.Straight, 0),
                Cell(4, 4, TerrainKind.River, RiverShape.Straight, 3),
                Cell(5, 4, TerrainKind.River, RiverShape.Straight, 0),
                Cell(4, 3, TerrainKind.Snow),
                Cell(3, 3, TerrainKind.Grass),
                Cell(3, 5, TerrainKind.Sand),
                Cell(4, 5, TerrainKind.Water),
            };
            _view.SetLayout(AssetWith(cells));

            Assert.That(_view.Tiles, Has.Count.EqualTo(7));
            _view.Catalog.TryGetRiverMesh(RiverShape.Straight, 0, out var straight);
            for (var i = 0; i < cells.Length; i++)
            {
                var center = HexMetrics.Default.Center(cells[i].Cell);
                Assert.That(Vector3.Distance(_view.Tiles[i].localPosition, new Vector3(center.X, center.Y, center.Z)), Is.LessThan(1e-4f));
                if (cells[i].Terrain == TerrainKind.River)
                {
                    Assert.That(_view.Tiles[i].GetComponent<MeshFilter>().sharedMesh, Is.SameAs(straight), cells[i].ToString());
                }
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Camera_DoesNotMoveOverTime()
        {
            var position = _camera.transform.position;
            var rotation = _camera.transform.rotation;
            var size = _camera.orthographicSize;

            for (var frame = 0; frame < 120; frame++) yield return null;

            Assert.That(_camera.transform.position, Is.EqualTo(position));
            Assert.That(_camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(_camera.orthographicSize, Is.EqualTo(size));
        }

        /// <summary>La composición de la escena muestra un río de al menos 5 piezas (spec FR-017, US3).</summary>
        [Test]
        public void Scene_ShowsARiverOfAtLeastFivePieces()
        {
            var river = _view.Layout.Entries.Where(entry => entry.Terrain == TerrainKind.River).ToArray();

            Assert.That(river, Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(river.Select(entry => entry.River), Is.SupersetOf(new[] { RiverShape.Spring, RiverShape.Straight, RiverShape.Curve }));
        }
    }
}
