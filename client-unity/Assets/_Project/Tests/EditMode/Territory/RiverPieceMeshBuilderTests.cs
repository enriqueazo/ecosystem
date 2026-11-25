using System;
using System.Collections.Generic;
using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Mallas de las piezas de río (data-model.md → Mallas generadas, invariantes 1–6; spec FR-012 a
    /// FR-015 y SC-005): cortes en el punto medio de cada arista abierta, muescas sin huecos, agua a la
    /// altura de la lámina, áreas que cubren el hexágono, giros, orientación y uniones exactas.
    /// </summary>
    public sealed class RiverPieceMeshBuilderTests
    {
        private static readonly HexMetrics Hex = HexMetrics.Default;
        private static readonly RiverMetrics River = RiverMetrics.Default;
        private static float Tolerance => EdgeTolerance(Hex);
        private static float Apothem => Hex.Width / 2f;

        private static IEnumerable<(RiverShape Shape, int Rotation)> Keys => RiverPiece.CanonicalKeys();

        private static readonly object[] KeyCases = RiverPiece.CanonicalKeys().Select(k => new object[] { k.Shape, k.Rotation }).ToArray();

        private static RiverPieceMeshData Build(RiverShape shape, int rotation) => RiverPieceMeshBuilder.Build(Hex, River, shape, rotation);

        private static Point3 EdgeNormal(HexEdge edge) => edge.Normal();

        private static Point3 Midpoint(HexEdge edge)
        {
            var n = EdgeNormal(edge);
            return new Point3(n.X * Apothem, 0f, n.Z * Apothem);
        }

        /// <summary>Dirección de la arista, del vértice d − 1 al vértice d (antihoraria vista desde arriba).</summary>
        private static Point3 Tangent(HexEdge edge)
        {
            var n = EdgeNormal(edge);
            return new Point3(-n.Z, 0f, n.X);
        }

        private static Point3[] CutPoints(HexEdge edge, float y)
        {
            var m = Midpoint(edge);
            var t = Tangent(edge);
            var half = River.ChannelWidth / 2f;
            return new[]
            {
                new Point3(m.X - t.X * half, y, m.Z - t.Z * half),
                new Point3(m.X + t.X * half, y, m.Z + t.Z * half),
            };
        }

        private static bool OnEdgePlane(Point3 v, HexEdge edge)
        {
            var n = EdgeNormal(edge);
            return Math.Abs(v.X * n.X + v.Z * n.Z - Apothem) < 1e-4f;
        }

        private static bool Close(Point3 a, Point3 b, float tolerance) =>
            Math.Abs(a.X - b.X) < tolerance && Math.Abs(a.Y - b.Y) < tolerance && Math.Abs(a.Z - b.Z) < tolerance;

        private static IEnumerable<Point3> VerticesOf(RiverPieceMeshData mesh, int[] indices) => indices.Distinct().Select(i => mesh.Vertices[i]);

        private static IEnumerable<(int A, int B, int C)> Triangles(int[] indices)
        {
            for (var i = 0; i < indices.Length; i += 3) yield return (indices[i], indices[i + 1], indices[i + 2]);
        }

        private static float TriangleArea3(Point3 a, Point3 b, Point3 c)
        {
            var cross = Cross(Minus(b, a), Minus(c, a));
            return (float)Math.Sqrt(Dot(cross, cross)) / 2f;
        }

        private static float ProjectedArea(Point3 a, Point3 b, Point3 c) =>
            Math.Abs((b.X - a.X) * (c.Z - a.Z) - (c.X - a.X) * (b.Z - a.Z)) / 2f;

        [TestCaseSource(nameof(KeyCases))]
        public void HasThreeNonEmptySubmeshes(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);

            Assert.That(mesh.Submeshes, Has.Length.EqualTo(3));
            Assert.That(mesh.GrassIndices, Is.Not.Empty);
            Assert.That(mesh.SideIndices, Is.Not.Empty);
            Assert.That(mesh.WaterIndices, Is.Not.Empty);
            Assert.That(mesh.Normals, Has.Length.EqualTo(mesh.Vertices.Length));
            Assert.That(mesh.Uvs, Has.Length.EqualTo(mesh.Vertices.Length));
        }

        /// <summary>Invariante 1: cada arista abierta tiene un corte de anchura w centrado en su punto medio.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void OpenEdges_AreCutAtTheirMidpointWithTheChannelWidth(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);
            var water = VerticesOf(mesh, mesh.WaterIndices).ToArray();
            var grass = VerticesOf(mesh, mesh.GrassIndices).ToArray();
            var open = RiverPiece.OpenEdges(shape, rotation);

            foreach (var edge in HexEdges.All)
            {
                var waterOnEdge = water.Where(v => OnEdgePlane(v, edge)).ToArray();
                if (!open.Contains(edge))
                {
                    Assert.That(waterOnEdge, Is.Empty, $"{edge}: arista cerrada sin agua.");
                    continue;
                }

                Assert.That(waterOnEdge, Has.Length.EqualTo(2), $"{edge}: el agua toca la arista en dos puntos.");
                foreach (var cut in CutPoints(edge, River.WaterLevel))
                {
                    Assert.That(waterOnEdge.Any(v => Close(v, cut, 1e-5f)), Is.True, $"{edge}: falta el corte del agua en {cut}.");
                }

                foreach (var cut in CutPoints(edge, Hex.Height))
                {
                    Assert.That(grass.Any(v => Close(v, cut, 1e-5f)), Is.True, $"{edge}: la orilla no llega al corte {cut}.");
                }
            }
        }

        /// <summary>Invariante 2: laterales completos en las aristas cerradas y con la muesca hasta hw en las abiertas, sin huecos.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void Sides_CoverEachEdgeExceptTheNotch(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);
            var open = RiverPiece.OpenEdges(shape, rotation);

            foreach (var edge in HexEdges.All)
            {
                var area = 0f;
                foreach (var (a, b, c) in Triangles(mesh.SideIndices))
                {
                    if (!OnEdgePlane(mesh.Vertices[a], edge) || !OnEdgePlane(mesh.Vertices[b], edge) || !OnEdgePlane(mesh.Vertices[c], edge)) continue;
                    var normal = EdgeNormal(edge);
                    Assert.That(Dot(mesh.Normals[a], normal), Is.EqualTo(1f).Within(1e-4f), $"{edge}: normal del lateral.");
                    area += TriangleArea3(mesh.Vertices[a], mesh.Vertices[b], mesh.Vertices[c]);
                }

                var expected = Hex.OuterRadius * Hex.Height - (open.Contains(edge) ? River.ChannelWidth * (Hex.Height - River.WaterLevel) : 0f);
                Assert.That(area, Is.EqualTo(expected).Within(1e-4f), $"{edge}: área del lateral.");
            }
        }

        /// <summary>Invariante 3: el agua del cauce está a hw y las orillas a h; nada fuera del hexágono.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void WaterIsAtTheWaterLevel_GrassAtTheTop_AndNothingLeavesTheHexagon(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);

            Assert.That(VerticesOf(mesh, mesh.WaterIndices).Select(v => v.Y), Is.All.EqualTo(River.WaterLevel).Within(1e-6f));
            Assert.That(VerticesOf(mesh, mesh.GrassIndices).Select(v => v.Y), Is.All.EqualTo(Hex.Height).Within(1e-6f));
            foreach (var v in mesh.Vertices)
            {
                Assert.That(v.Y, Is.InRange(-1e-6f, Hex.Height + 1e-6f));
                foreach (var edge in HexEdges.All)
                {
                    var n = EdgeNormal(edge);
                    Assert.That(v.X * n.X + v.Z * n.Z, Is.LessThanOrEqualTo(Apothem + 1e-5f), $"{v} fuera por {edge}.");
                }
            }
        }

        /// <summary>Invariante 4: orillas y agua cubren exactamente el hexágono visto desde arriba.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void GrassAndWater_CoverTheHexagonExactly(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);
            var total = Triangles(mesh.GrassIndices).Concat(Triangles(mesh.WaterIndices))
                .Sum(t => ProjectedArea(mesh.Vertices[t.A], mesh.Vertices[t.B], mesh.Vertices[t.C]));
            var hexagon = 3f * (float)Math.Sqrt(3) / 2f * Hex.OuterRadius * Hex.OuterRadius;

            Assert.That(total, Is.EqualTo(hexagon).Within(1e-4f));
        }

        /// <summary>Invariante 5: el giro r es la malla de giro 0 girada 60°·r; las coordenadas planares no giran.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void Rotation_RotatesTheBaseGeometryButNotThePlanarUvs(RiverShape shape, int rotation)
        {
            var baseMesh = Build(shape, 0);
            var mesh = Build(shape, rotation);
            var angle = Math.PI / 3.0 * rotation;
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            Point3 Rotate(Point3 p) => new Point3(p.X * cos - p.Z * sin, p.Y, p.X * sin + p.Z * cos);

            Assert.That(mesh.Vertices, Has.Length.EqualTo(baseMesh.Vertices.Length));
            Assert.That(mesh.Submeshes, Is.EqualTo(baseMesh.Submeshes));
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                Assert.That(Close(mesh.Vertices[i], Rotate(baseMesh.Vertices[i]), 1e-5f), Is.True, $"vértice {i}");
                Assert.That(Close(mesh.Normals[i], Rotate(baseMesh.Normals[i]), 1e-5f), Is.True, $"normal {i}");
            }

            foreach (var i in mesh.GrassIndices.Distinct())
            {
                var v = mesh.Vertices[i];
                Assert.That(mesh.Uvs[i].X, Is.EqualTo(v.X / Hex.Width + 0.5f).Within(1e-5f), "Orilla: mapeo inscrito, como HexTile.");
                Assert.That(mesh.Uvs[i].Y, Is.EqualTo(v.Z / (2f * Hex.OuterRadius) + 0.5f).Within(1e-5f));
            }

            foreach (var i in mesh.WaterIndices.Distinct())
            {
                var v = mesh.Vertices[i];
                Assert.That(mesh.Uvs[i].X, Is.EqualTo(v.X / Hex.Width).Within(1e-5f), "Agua: mapeo en red, como WaterTile.");
                Assert.That(mesh.Uvs[i].Y, Is.EqualTo(v.Z / (3f * Hex.OuterRadius)).Within(1e-5f));
            }

            foreach (var i in mesh.SideIndices.Distinct())
            {
                Assert.That(mesh.Uvs[i].Y, Is.EqualTo(mesh.Vertices[i].Y / Hex.Height).Within(1e-5f), "Laterales y paredes: v = y/h.");
            }
        }

        /// <summary>Invariante 6: sombreado plano y cara frontal hacia fuera (la de su normal) en todos los triángulos.</summary>
        [TestCaseSource(nameof(KeyCases))]
        public void EveryTriangle_IsFlatAndFacesAlongItsNormal(RiverShape shape, int rotation)
        {
            var mesh = Build(shape, rotation);
            foreach (var indices in mesh.Submeshes)
            {
                foreach (var (a, b, c) in Triangles(indices))
                {
                    var normal = mesh.Normals[a];
                    Assert.That(mesh.Normals[b], Is.EqualTo(normal));
                    Assert.That(mesh.Normals[c], Is.EqualTo(normal));
                    var cross = Cross(Minus(mesh.Vertices[b], mesh.Vertices[a]), Minus(mesh.Vertices[c], mesh.Vertices[a]));
                    Assert.That(Dot(cross, normal), Is.GreaterThan(0f), $"Triángulo ({a}, {b}, {c}) del revés.");
                }
            }

            foreach (var i in mesh.GrassIndices.Concat(mesh.WaterIndices))
            {
                Assert.That(mesh.Normals[i], Is.EqualTo(new Point3(0f, 1f, 0f)));
            }
        }

        /// <summary>
        /// SC-005: en cada pareja de piezas con conexión recíproca, los cortes del agua coinciden en
        /// posición, anchura y nivel (≤ 0,1 % de W).
        /// </summary>
        [Test]
        public void ReciprocalPieces_MeetWithTheSameCutsAndLevel()
        {
            var pairs = 0;
            foreach (var a in Keys)
            {
                var meshA = Build(a.Shape, a.Rotation);
                foreach (var edge in RiverPiece.OpenEdges(a.Shape, a.Rotation))
                {
                    var cutsA = VerticesOf(meshA, meshA.WaterIndices).Where(v => OnEdgePlane(v, edge)).ToArray();
                    var offset = new Point3(EdgeNormal(edge).X * Hex.Width, 0f, EdgeNormal(edge).Z * Hex.Width);
                    foreach (var b in Keys.Where(k => RiverPiece.OpenEdges(k.Shape, k.Rotation).Contains(edge.Opposite())))
                    {
                        var meshB = Build(b.Shape, b.Rotation);
                        var cutsB = VerticesOf(meshB, meshB.WaterIndices)
                            .Where(v => OnEdgePlane(v, edge.Opposite()))
                            .Select(v => new Point3(v.X + offset.X, v.Y, v.Z + offset.Z))
                            .ToArray();
                        Assert.That(cutsB, Has.Length.EqualTo(cutsA.Length));
                        foreach (var cut in cutsA)
                        {
                            Assert.That(cutsB.Any(v => Close(v, cut, Tolerance)), Is.True, $"{a} → {b} por {edge}: el corte {cut} no casa.");
                        }

                        pairs++;
                    }
                }
            }

            Assert.That(pairs, Is.GreaterThan(0));
        }

        /// <summary>FR-014: la desembocadura casa con la arista de la pieza de agua, al mismo nivel.</summary>
        [Test]
        public void Mouths_MeetTheWaterTileAtItsLevel()
        {
            var waterTile = HexPrismMeshBuilder.Build(new HexMetrics(Hex.OuterRadius, River.WaterLevel), TopUv.Lattice);
            var waterTop = waterTile.TopIndices.Distinct().Select(i => waterTile.Vertices[i]).ToArray();
            Assert.That(waterTop.Select(v => v.Y), Is.All.EqualTo(River.WaterLevel).Within(1e-6f));

            foreach (var a in Keys)
            {
                var mesh = Build(a.Shape, a.Rotation);
                foreach (var edge in RiverPiece.OpenEdges(a.Shape, a.Rotation))
                {
                    var offset = new Point3(EdgeNormal(edge).X * Hex.Width, 0f, EdgeNormal(edge).Z * Hex.Width);
                    foreach (var cut in VerticesOf(mesh, mesh.WaterIndices).Where(v => OnEdgePlane(v, edge)))
                    {
                        // En el sistema de la pieza de agua vecina, el corte está en su arista opuesta y a su altura.
                        var local = new Point3(cut.X - offset.X, cut.Y, cut.Z - offset.Z);
                        Assert.That(OnEdgePlane(local, edge.Opposite()), Is.True, $"{a} por {edge}");
                        Assert.That(local.Y, Is.EqualTo(River.WaterLevel).Within(1e-6f));
                        Assert.That(HorizontalDistance(local, Midpoint(edge.Opposite())), Is.LessThan(Hex.OuterRadius / 2f), "Dentro de la arista.");
                    }
                }
            }
        }
    }
}
