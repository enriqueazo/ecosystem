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

        // --- Mapeo superior en red para el agua (spec 005: research.md R-4; data-model.md → Mallas generadas) ---

        private static HexMetrics WaterMetrics => new HexMetrics(Metrics.OuterRadius, RiverMetrics.Default.WaterLevel);

        [Test]
        public void LatticeTopUvs_AreWorldPlanarOverTheHexLattice()
        {
            var metrics = WaterMetrics;
            var mesh = HexPrismMeshBuilder.Build(metrics, TopUv.Lattice);
            foreach (var index in mesh.TopIndices.Distinct())
            {
                var vertex = mesh.Vertices[index];
                var uv = mesh.Uvs[index];
                Assert.That(vertex.Y, Is.EqualTo(metrics.Height));
                Assert.That(uv.X, Is.EqualTo(vertex.X / metrics.Width).Within(1e-6f));
                Assert.That(uv.Y, Is.EqualTo(vertex.Z / (3f * metrics.OuterRadius)).Within(1e-6f));
            }
        }

        [Test]
        public void LatticeMapping_ChangesOnlyTheTopUvs()
        {
            var inscribed = HexPrismMeshBuilder.Build(WaterMetrics);
            var lattice = HexPrismMeshBuilder.Build(WaterMetrics, TopUv.Lattice);

            Assert.That(lattice.Vertices, Is.EqualTo(inscribed.Vertices));
            Assert.That(lattice.Normals, Is.EqualTo(inscribed.Normals));
            Assert.That(lattice.TopIndices, Is.EqualTo(inscribed.TopIndices));
            Assert.That(lattice.SideIndices, Is.EqualTo(inscribed.SideIndices));
            foreach (var index in inscribed.SideIndices.Distinct())
            {
                Assert.That(lattice.Uvs[index], Is.EqualTo(inscribed.Uvs[index]), "Laterales: v = y/hw.");
            }
        }

        [Test]
        public void DefaultBuild_KeepsTheInscribedMappingOfSpec004()
        {
            var byDefault = HexPrismMeshBuilder.Build(Metrics);
            var inscribed = HexPrismMeshBuilder.Build(Metrics, TopUv.Inscribed);

            Assert.That(byDefault.Uvs, Is.EqualTo(inscribed.Uvs));
        }

        /// <summary>
        /// Entre dos celdas vecinas, el desplazamiento de sus centros en coordenadas de textura es
        /// (±1, 0) o (±½, ±½): con una textura periódica en (1, 0), (0, 1) y (½, ½), el agua no
        /// tiene costura entre piezas.
        /// </summary>
        [Test]
        public void LatticeMapping_MovesNeighbourCentresByLatticeVectors()
        {
            var metrics = WaterMetrics;
            var cell = new HexCell(4, 4);
            var center = metrics.Center(cell);
            foreach (var neighbour in cell.Neighbours().Concat(new HexCell(4, 5).Neighbours()))
            {
                var other = metrics.Center(neighbour);
                var du = (other.X - center.X) / metrics.Width;
                var dv = (other.Z - center.Z) / (3f * metrics.OuterRadius);
                var twiceU = du * 2f;
                var twiceV = dv * 2f;
                Assert.That(twiceU, Is.EqualTo((float)System.Math.Round(twiceU)).Within(1e-4f), neighbour.ToString());
                Assert.That(twiceV, Is.EqualTo((float)System.Math.Round(twiceV)).Within(1e-4f), neighbour.ToString());
                Assert.That(((int)System.Math.Round(twiceU) - (int)System.Math.Round(twiceV)) % 2, Is.EqualTo(0), "u y v son a la vez enteros o a la vez medios.");
            }
        }
    }
}
