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
        public void Scene_ShowsSixteenPiecesAtTheirLayoutPositions()
        {
            Assert.That(_view, Is.Not.Null, "La escena necesita un TerritoryView.");
            Assert.That(_view.Tiles, Has.Count.EqualTo(16));

            var expected = TerritoryLayout.Create(Enumerable.Range(0, 16).Select(i => new HexCell(i % 4, i / 4))).Layout;
            var positions = _view.Tiles.Select(tile => tile.localPosition).ToArray();
            foreach (var cell in expected.Cells)
            {
                var center = HexMetrics.Default.Center(cell);
                var target = new Vector3(center.X, center.Y, center.Z);
                Assert.That(positions.Any(p => Vector3.Distance(p, target) < 1e-4f), Is.True, $"Falta la pieza en {cell}.");
            }
        }

        [Test]
        public void Pieces_UseTheTileMeshAndCastNoShadows()
        {
            foreach (var tile in _view.Tiles)
            {
                var filter = tile.GetComponent<MeshFilter>();
                var renderer = tile.GetComponent<MeshRenderer>();
                Assert.That(filter.sharedMesh, Is.Not.Null);
                Assert.That(filter.sharedMesh.name, Is.EqualTo("HexTile"));
                Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2), "Césped (submalla 0) y tierra (submalla 1).");
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(renderer.receiveShadows, Is.False);
            }
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
            _view.SetLayout(AssetWith());

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
    }
}
