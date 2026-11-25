using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → HexEdge y research.md R-3: orden, nombres, arista opuesta, vecina por arista
    /// y correspondencia con las caras laterales del prisma.
    /// </summary>
    public sealed class HexEdgeTests
    {
        private static readonly HexEdge[] AllEdges = { HexEdge.E, HexEdge.NE, HexEdge.NW, HexEdge.W, HexEdge.SW, HexEdge.SE };

        [Test]
        public void Edges_FollowTheAxialNeighbourOrder()
        {
            Assert.That(AllEdges.Select(edge => (int)edge), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(HexEdges.All, Is.EqualTo(AllEdges));
        }

        [TestCase(HexEdge.E, "E")]
        [TestCase(HexEdge.NE, "NE")]
        [TestCase(HexEdge.NW, "NO")]
        [TestCase(HexEdge.W, "O")]
        [TestCase(HexEdge.SW, "SO")]
        [TestCase(HexEdge.SE, "SE")]
        public void DisplayName_IsTheSpanishName(HexEdge edge, string name)
        {
            Assert.That(edge.DisplayName(), Is.EqualTo(name));
        }

        [TestCase(HexEdge.E, HexEdge.W)]
        [TestCase(HexEdge.NE, HexEdge.SW)]
        [TestCase(HexEdge.NW, HexEdge.SE)]
        [TestCase(HexEdge.W, HexEdge.E)]
        [TestCase(HexEdge.SW, HexEdge.NE)]
        [TestCase(HexEdge.SE, HexEdge.NW)]
        public void Opposite_IsThreeStepsAway(HexEdge edge, HexEdge opposite)
        {
            Assert.That(edge.Opposite(), Is.EqualTo(opposite));
        }

        [TestCase(4, 4)]
        [TestCase(3, 5)]
        [TestCase(0, 0)]
        [TestCase(-2, -3)]
        public void Neighbour_MatchesTheNeighboursInOrder(int column, int row)
        {
            var cell = new HexCell(column, row);
            var neighbours = cell.Neighbours().ToArray();

            foreach (var edge in AllEdges)
            {
                Assert.That(cell.Neighbour(edge), Is.EqualTo(neighbours[(int)edge]), $"{cell} por {edge}");
                Assert.That(cell.Neighbour(edge).Neighbour(edge.Opposite()), Is.EqualTo(cell), "Ida y vuelta.");
            }
        }

        [Test]
        public void Neighbour_OnEvenAndOddRows_PointsBackAndFrontAsNamed()
        {
            // Fila par (4): el fondo es la fila 3 y el frente, la 5. Las filas impares van media pieza a la derecha.
            var even = new HexCell(4, 4);
            Assert.That(even.Neighbour(HexEdge.E), Is.EqualTo(new HexCell(5, 4)));
            Assert.That(even.Neighbour(HexEdge.NE), Is.EqualTo(new HexCell(4, 3)));
            Assert.That(even.Neighbour(HexEdge.NW), Is.EqualTo(new HexCell(3, 3)));
            Assert.That(even.Neighbour(HexEdge.W), Is.EqualTo(new HexCell(3, 4)));
            Assert.That(even.Neighbour(HexEdge.SW), Is.EqualTo(new HexCell(3, 5)));
            Assert.That(even.Neighbour(HexEdge.SE), Is.EqualTo(new HexCell(4, 5)));

            var odd = new HexCell(4, 5);
            Assert.That(odd.Neighbour(HexEdge.NE), Is.EqualTo(new HexCell(5, 4)));
            Assert.That(odd.Neighbour(HexEdge.NW), Is.EqualTo(new HexCell(4, 4)));
            Assert.That(odd.Neighbour(HexEdge.SW), Is.EqualTo(new HexCell(4, 6)));
            Assert.That(odd.Neighbour(HexEdge.SE), Is.EqualTo(new HexCell(5, 6)));
        }

        /// <summary>
        /// La normal de la arista e está a 60°·e desde +x hacia +z, y apunta hacia el centro de la
        /// vecina. La cara lateral k del prisma (vértices k y k+1) es la arista (k+1) mod 6.
        /// </summary>
        [Test]
        public void EdgeNormal_PointsToTheNeighbourAndMatchesTheSideFaceOfThePrism()
        {
            var metrics = HexMetrics.Default;
            var cell = new HexCell(4, 4);
            var center = metrics.Center(cell);

            foreach (var edge in AllEdges)
            {
                var neighbour = metrics.Center(cell.Neighbour(edge));
                var direction = new Point3(neighbour.X - center.X, 0f, neighbour.Z - center.Z);
                var normal = edge.Normal();
                Assert.That(direction.X / metrics.Width, Is.EqualTo(normal.X).Within(1e-5f), $"{edge} x");
                Assert.That(direction.Z / metrics.Width, Is.EqualTo(normal.Z).Within(1e-5f), $"{edge} z");
            }

            var mesh = HexPrismMeshBuilder.Build(metrics);
            for (var k = 0; k < 6; k++)
            {
                var sideNormal = mesh.Normals[6 + 4 * k];
                var edgeNormal = ((HexEdge)((k + 1) % 6)).Normal();
                Assert.That(sideNormal.X, Is.EqualTo(edgeNormal.X).Within(1e-5f), $"cara {k}");
                Assert.That(sideNormal.Z, Is.EqualTo(edgeNormal.Z).Within(1e-5f), $"cara {k}");
            }
        }
    }
}
