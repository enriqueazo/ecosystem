using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → HexPrismMesh: prisma con vértice arriba, sombreado plano, dos submallas y
    /// sin cara inferior (FR-006, FR-012, research.md R-3).
    /// </summary>
    public sealed class HexPrismMeshBuilderTests
    {
        private static readonly HexMetrics Metrics = HexMetrics.Default;
        private static HexPrismMeshData Build() => HexPrismMeshBuilder.Build(Metrics);

        [Test]
        public void Mesh_HasThirtyVerticesAndSixteenTriangles()
        {
            var mesh = Build();

            Assert.That(mesh.Vertices, Has.Length.EqualTo(30));
            Assert.That(mesh.Normals, Has.Length.EqualTo(30));
            Assert.That(mesh.Uvs, Has.Length.EqualTo(30));
            Assert.That(mesh.TopIndices.Length / 3, Is.EqualTo(4));
            Assert.That(mesh.SideIndices.Length / 3, Is.EqualTo(12));
        }

        [Test]
        public void TopSubmesh_IsTheTopFaceWithUpwardNormals()
        {
            var mesh = Build();

            foreach (var index in mesh.TopIndices)
            {
                Assert.That(mesh.Vertices[index].Y, Is.EqualTo(Metrics.Height));
                Assert.That(mesh.Normals[index], Is.EqualTo(new Point3(0f, 1f, 0f)));
            }
        }

        [Test]
        public void TopFace_UsesExactlyTheSixCorners()
        {
            var mesh = Build();
            var topVertices = mesh.TopIndices.Distinct().Select(i => mesh.Vertices[i]).ToArray();

            Assert.That(topVertices, Has.Length.EqualTo(6));
            for (var i = 0; i < 6; i++)
            {
                var corner = Metrics.Corner(i);
                Assert.That(topVertices.Any(v => HorizontalDistance(v, corner) < 1e-6f && v.Y == corner.Y), Is.True, $"Falta la esquina {i}.");
            }
        }

        [Test]
        public void SideSubmesh_HasSixFlatQuadsFromBaseToTopWithOutwardNormals()
        {
            var mesh = Build();
            var sideVertices = mesh.SideIndices.Distinct().ToArray();

            Assert.That(sideVertices, Has.Length.EqualTo(24));
            foreach (var index in sideVertices)
            {
                var vertex = mesh.Vertices[index];
                var normal = mesh.Normals[index];
                Assert.That(vertex.Y, Is.EqualTo(0f).Or.EqualTo(Metrics.Height));
                Assert.That(normal.Y, Is.EqualTo(0f));
                Assert.That(Dot(normal, new Point3(vertex.X, 0f, vertex.Z)), Is.GreaterThan(0f), "La normal lateral debe apuntar hacia fuera.");
            }

            var normals = sideVertices.Select(i => mesh.Normals[i]).Distinct().ToArray();
            Assert.That(normals, Has.Length.EqualTo(6), "Sombreado plano: una normal por cara.");
        }

        [Test]
        public void NoVertexBelowTheBase_AndNoDownwardFace()
        {
            var mesh = Build();

            Assert.That(mesh.Vertices.Min(v => v.Y), Is.EqualTo(0f));
            Assert.That(mesh.Normals.Any(n => n.Y < 0f), Is.False, "No hay cara inferior.");
        }

        [Test]
        public void EveryTriangle_IsWoundSoThatItsFrontFacesOutward()
        {
            var mesh = Build();
            foreach (var indices in new[] { mesh.TopIndices, mesh.SideIndices })
            {
                for (var t = 0; t < indices.Length; t += 3)
                {
                    var a = mesh.Vertices[indices[t]];
                    var b = mesh.Vertices[indices[t + 1]];
                    var c = mesh.Vertices[indices[t + 2]];
                    var faceNormal = Cross(Minus(b, a), Minus(c, a));
                    Assert.That(Dot(faceNormal, mesh.Normals[indices[t]]), Is.GreaterThan(0f), $"Triángulo {t / 3} con orden invertido.");
                }
            }
        }

        [Test]
        public void TopUvs_ArePlanarOverTheBoundingSquare()
        {
            var mesh = Build();
            foreach (var index in mesh.TopIndices.Distinct())
            {
                var vertex = mesh.Vertices[index];
                var uv = mesh.Uvs[index];
                Assert.That(uv.X, Is.EqualTo(vertex.X / Metrics.Width + 0.5f).Within(1e-6f));
                Assert.That(uv.Y, Is.EqualTo(vertex.Z / (2f * Metrics.OuterRadius) + 0.5f).Within(1e-6f));
            }
        }

        [Test]
        public void SideUvs_SpanZeroToOneAcrossEachFaceAndFromBaseToTop()
        {
            var mesh = Build();
            foreach (var index in mesh.SideIndices.Distinct())
            {
                var uv = mesh.Uvs[index];
                Assert.That(uv.X, Is.EqualTo(0f).Or.EqualTo(1f));
                Assert.That(uv.Y, Is.EqualTo(mesh.Vertices[index].Y == 0f ? 0f : 1f));
            }
        }
    }
}
