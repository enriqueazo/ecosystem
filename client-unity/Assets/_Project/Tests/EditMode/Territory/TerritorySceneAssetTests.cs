using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ecosystem.Territory.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Las piezas no se serializan en la escena: TerritoryView las crea al habilitarse
    /// (research.md R-7). El catálogo de terrenos sí referencia los prefabs, pero la escena no
    /// guarda instancias.
    /// </summary>
    public sealed class TerritorySceneAssetTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Territory.unity";
        private const string PrefabPath = "Assets/_Project/Art/Territory/Prefabs/HexTile.prefab";
        private const string MeshPath = "Assets/_Project/Art/Territory/Meshes/HexTile.asset";

        [Test]
        public void TerritoryScene_DoesNotSerializeTileInstances()
        {
            Assert.That(File.Exists(ScenePath), Is.True, $"Falta {ScenePath}.");
            var yaml = File.ReadAllText(ScenePath);
            var prefabGuid = AssetDatabase.AssetPathToGUID(PrefabPath);
            var meshGuid = AssetDatabase.AssetPathToGUID(MeshPath);
            Assert.That(prefabGuid, Is.Not.Empty, $"Falta {PrefabPath}.");
            Assert.That(meshGuid, Is.Not.Empty, $"Falta {MeshPath}.");

            var prefabInstances = Regex.Matches(yaml, @"--- !u!1001 &\d+\s*\nPrefabInstance:[\s\S]*?m_SourcePrefab: \{fileID: \d+, guid: " + prefabGuid);
            Assert.That(prefabInstances.Count, Is.EqualTo(0), "La escena contiene instancias guardadas de HexTile.prefab.");

            var meshFilters = Regex.Matches(yaml, @"MeshFilter:[\s\S]*?m_Mesh: \{fileID: \d+, guid: " + meshGuid);
            Assert.That(meshFilters.Count, Is.EqualTo(0), "La escena contiene MeshFilter guardados con la malla de la pieza.");
        }

        /// <summary>
        /// quickstart A3: abierta en el editor sin Play, la escena ya muestra las 55 piezas y no
        /// queda marcada como modificada (US1, escenario 3).
        /// </summary>
        [Test]
        public void TerritoryScene_OpenedInTheEditor_ShowsFiftyFivePiecesAndStaysClean()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var view = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<TerritoryView>()).Single(v => v != null);

                Assert.That(view.Tiles, Has.Count.EqualTo(55));
                Assert.That(view.Tiles.All(tile => (tile.gameObject.hideFlags & HideFlags.DontSave) == HideFlags.DontSave), Is.True);
                Assert.That(scene.isDirty, Is.False, "Las piezas no deben marcar la escena como modificada.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
