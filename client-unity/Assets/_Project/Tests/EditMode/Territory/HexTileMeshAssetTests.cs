using Ecosystem.Territory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// La malla guardada coincide con la salida del generador para HexMetrics.Default
    /// (research.md R-3). Si cambia la altura aprobada y no se regenera la malla
    /// (Ecosystem → Territorio → Regenerar malla de pieza), esta prueba falla.
    /// </summary>
    public sealed class HexTileMeshAssetTests
    {
        private const string MeshPath = "Assets/_Project/Art/Territory/Meshes/HexTile.asset";
        private const float Tolerance = 1e-6f;

        [Test]
        public void SavedMesh_MatchesTheGeneratorOutput()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            Assert.That(mesh, Is.Not.Null, $"Falta {MeshPath}: ejecuta Ecosystem → Territorio → Regenerar malla de pieza.");

            var expected = HexPrismMeshBuilder.Build(HexMetrics.Default);
            const string regenerate = " La malla está desactualizada: ejecuta Ecosystem → Territorio → Regenerar malla de pieza.";

            Assert.That(mesh.subMeshCount, Is.EqualTo(2), regenerate);
            Assert.That(mesh.vertexCount, Is.EqualTo(expected.Vertices.Length), regenerate);

            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            for (var i = 0; i < expected.Vertices.Length; i++)
            {
                AssertClose(vertices[i], expected.Vertices[i], $"vértice {i}.{regenerate}");
                AssertClose(normals[i], expected.Normals[i], $"normal {i}.{regenerate}");
                Assert.That(uvs[i].x, Is.EqualTo(expected.Uvs[i].X).Within(Tolerance), $"uv {i}.{regenerate}");
                Assert.That(uvs[i].y, Is.EqualTo(expected.Uvs[i].Y).Within(Tolerance), $"uv {i}.{regenerate}");
            }

            Assert.That(mesh.GetTriangles(0), Is.EqualTo(expected.TopIndices), "Submalla 0 (césped)." + regenerate);
            Assert.That(mesh.GetTriangles(1), Is.EqualTo(expected.SideIndices), "Submalla 1 (tierra)." + regenerate);
        }

        private static void AssertClose(Vector3 actual, Point3 expected, string what)
        {
            Assert.That(actual.x, Is.EqualTo(expected.X).Within(Tolerance), what);
            Assert.That(actual.y, Is.EqualTo(expected.Y).Within(Tolerance), what);
            Assert.That(actual.z, Is.EqualTo(expected.Z).Within(Tolerance), what);
        }
    }
}
