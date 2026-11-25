using Ecosystem.Territory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Las mallas generadas de la spec 005 están al día con su generador (data-model.md → Mallas
    /// generadas; research.md R-2). HexTile lo comprueba HexTileMeshAssetTests. Si una falla,
    /// ejecuta Ecosystem → Territorio → Regenerar mallas del territorio.
    /// </summary>
    public sealed class TerritoryMeshAssetTests
    {
        private const string Meshes = "Assets/_Project/Art/Territory/Meshes";
        private const float Tolerance = 1e-6f;
        private const string Regenerate = " La malla está desactualizada: ejecuta Ecosystem → Territorio → Regenerar mallas del territorio.";

        [Test]
        public void WaterTile_MatchesTheGeneratorOutput()
        {
            var expected = HexPrismMeshBuilder.Build(
                new HexMetrics(HexMetrics.Default.OuterRadius, RiverMetrics.Default.WaterLevel), TopUv.Lattice);
            AssertMatches($"{Meshes}/WaterTile.asset", expected.Vertices, expected.Normals, expected.Uvs, expected.TopIndices, expected.SideIndices);
        }

        internal static void AssertMatches(string path, Point3[] vertices, Point3[] normals, Point2[] uvs, params int[][] submeshes)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Assert.That(mesh, Is.Not.Null, $"Falta {path}.{Regenerate}");
            Assert.That(mesh.subMeshCount, Is.EqualTo(submeshes.Length), path + Regenerate);
            Assert.That(mesh.vertexCount, Is.EqualTo(vertices.Length), path + Regenerate);

            var actualVertices = mesh.vertices;
            var actualNormals = mesh.normals;
            var actualUvs = mesh.uv;
            for (var i = 0; i < vertices.Length; i++)
            {
                AssertClose(actualVertices[i], vertices[i], $"{path}: vértice {i}.{Regenerate}");
                AssertClose(actualNormals[i], normals[i], $"{path}: normal {i}.{Regenerate}");
                Assert.That(actualUvs[i].x, Is.EqualTo(uvs[i].X).Within(Tolerance), $"{path}: uv {i}.{Regenerate}");
                Assert.That(actualUvs[i].y, Is.EqualTo(uvs[i].Y).Within(Tolerance), $"{path}: uv {i}.{Regenerate}");
            }

            for (var s = 0; s < submeshes.Length; s++)
            {
                Assert.That(mesh.GetTriangles(s), Is.EqualTo(submeshes[s]), $"{path}: submalla {s}.{Regenerate}");
            }
        }

        private static void AssertClose(Vector3 actual, Point3 expected, string what)
        {
            Assert.That(actual.x, Is.EqualTo(expected.X).Within(Tolerance), what);
            Assert.That(actual.y, Is.EqualTo(expected.Y).Within(Tolerance), what);
            Assert.That(actual.z, Is.EqualTo(expected.Z).Within(Tolerance), what);
        }
    }
}
