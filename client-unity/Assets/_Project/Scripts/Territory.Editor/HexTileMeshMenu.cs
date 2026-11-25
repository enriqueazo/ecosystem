using System.IO;
using System.Linq;
using Ecosystem.Territory;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Territory.Editor
{
    /// <summary>
    /// Guarda la malla de la pieza generada desde el núcleo (research.md R-3). Se ejecuta desde el
    /// menú o en modo batch con
    /// <c>-executeMethod Ecosystem.Territory.Editor.HexTileMeshMenu.Regenerate</c>.
    /// HexTileMeshAssetTests comprueba que el asset guardado está al día.
    /// </summary>
    public static class HexTileMeshMenu
    {
        public const string MeshPath = "Assets/_Project/Art/Territory/Meshes/HexTile.asset";

        [MenuItem("Ecosystem/Territorio/Regenerar malla de pieza")]
        public static void Regenerate()
        {
            var mesh = CreateMesh(HexPrismMeshBuilder.Build(HexMetrics.Default));
            Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null)
            {
                // Conserva el GUID para no romper las referencias del prefab.
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Territorio] Malla de pieza guardada en {MeshPath}.");
        }

        private static Mesh CreateMesh(HexPrismMeshData data)
        {
            var mesh = new Mesh { name = "HexTile" };
            mesh.SetVertices(data.Vertices.Select(ToVector3).ToArray());
            mesh.SetNormals(data.Normals.Select(ToVector3).ToArray());
            mesh.SetUVs(0, data.Uvs.Select(uv => new Vector2(uv.X, uv.Y)).ToArray());
            mesh.subMeshCount = 2;
            mesh.SetTriangles(data.TopIndices, 0);
            mesh.SetTriangles(data.SideIndices, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
            return mesh;
        }

        private static Vector3 ToVector3(Point3 point) => new Vector3(point.X, point.Y, point.Z);
    }
}
