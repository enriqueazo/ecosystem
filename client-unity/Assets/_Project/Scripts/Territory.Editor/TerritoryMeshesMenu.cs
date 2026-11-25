using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ecosystem.Territory;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Territory.Editor
{
    /// <summary>
    /// Guarda las mallas del territorio generadas desde el núcleo (research.md R-2 de la spec 005).
    /// Se ejecuta desde el menú o en modo batch con
    /// <c>-executeMethod Ecosystem.Territory.Editor.TerritoryMeshesMenu.Regenerate</c>.
    /// Conserva el GUID de las mallas que ya existen. HexTileMeshAssetTests y
    /// TerritoryMeshAssetTests comprueban que los assets guardados están al día.
    /// </summary>
    public static class TerritoryMeshesMenu
    {
        public const string MeshesFolder = "Assets/_Project/Art/Territory/Meshes";
        public const string HexTilePath = MeshesFolder + "/HexTile.asset";
        public const string WaterTilePath = MeshesFolder + "/WaterTile.asset";

        [MenuItem("Ecosystem/Territorio/Regenerar mallas del territorio")]
        public static void Regenerate()
        {
            var hex = HexMetrics.Default;
            var river = RiverMetrics.Default;

            Save(HexTilePath, "HexTile", HexPrismMeshBuilder.Build(hex));
            Save(WaterTilePath, "WaterTile", HexPrismMeshBuilder.Build(new HexMetrics(hex.OuterRadius, river.WaterLevel), TopUv.Lattice));

            AssetDatabase.SaveAssets();
            Debug.Log($"[Territorio] Mallas del territorio guardadas en {MeshesFolder}.");
        }

        private static void Save(string path, string name, HexPrismMeshData data) =>
            Save(path, name, data.Vertices, data.Normals, data.Uvs, data.TopIndices, data.SideIndices);

        private static void Save(string path, string name, Point3[] vertices, Point3[] normals, Point2[] uvs, params int[][] submeshes)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices.Select(ToVector3).ToArray());
            mesh.SetNormals(normals.Select(ToVector3).ToArray());
            mesh.SetUVs(0, uvs.Select(uv => new Vector2(uv.X, uv.Y)).ToArray());
            mesh.subMeshCount = submeshes.Length;
            for (var s = 0; s < submeshes.Length; s++) mesh.SetTriangles(submeshes[s], s);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                // Conserva el GUID para no romper las referencias de prefabs y catálogo.
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
        }

        private static Vector3 ToVector3(Point3 point) => new Vector3(point.X, point.Y, point.Z);
    }
}
