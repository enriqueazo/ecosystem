using System.Collections.Generic;
using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Reglas de conexión del río (spec FR-016; data-model.md → TerritoryLayout, orden 6 y Regla de
    /// conexión). Cada arista abierta da a una pieza de río con la arista enfrentada abierta, a agua o
    /// fuera del territorio.
    /// </summary>
    public sealed class RiverConnectionTests
    {
        private static TerritoryCell River(int column, int row, RiverShape shape, int rotation) =>
            new TerritoryCell(new HexCell(column, row), TerrainKind.River, shape, rotation);

        private static TerritoryCell Plain(int column, int row, TerrainKind kind) =>
            new TerritoryCell(new HexCell(column, row), kind);

        private static TerritoryLayoutResult Create(params TerritoryCell[] cells) => TerritoryLayout.Create(cells);

        private static void AssertOk(TerritoryLayoutResult result) =>
            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);

        // --- Aceptadas ---

        [Test]
        public void ReciprocalStraights_WithBothEndsOutside_AreValid()
        {
            AssertOk(Create(River(0, 0, RiverShape.Straight, 0), River(1, 0, RiverShape.Straight, 0)));
        }

        [Test]
        public void AnEdgeIntoWater_IsAMouth()
        {
            AssertOk(Create(River(0, 0, RiverShape.Straight, 0), Plain(1, 0, TerrainKind.Water)));
            AssertOk(Create(River(0, 0, RiverShape.Spring, 0), Plain(1, 0, TerrainKind.Water)));
        }

        [Test]
        public void AnEdgeOutsideTheTerritory_LeavesThroughTheBorder()
        {
            AssertOk(Create(River(0, 0, RiverShape.Curve, 3), Plain(1, 0, TerrainKind.Grass)));
        }

        [Test]
        public void AClosedRingOfCurvesWithoutSpring_IsValidData()
        {
            var center = new HexCell(4, 4);
            var ring = HexEdges.All.Select(edge => center.Neighbour(edge)).ToArray();
            var cells = new List<TerritoryCell> { new TerritoryCell(center, TerrainKind.Grass) };
            for (var i = 0; i < ring.Length; i++)
            {
                var previous = EdgeTowards(ring[i], ring[(i + 5) % 6]);
                var next = EdgeTowards(ring[i], ring[(i + 1) % 6]);
                cells.Add(new TerritoryCell(ring[i], TerrainKind.River, RiverShape.Curve, CurveRotation(previous, next)));
            }

            AssertOk(TerritoryLayout.Create(cells));
        }

        [Test]
        public void TwoNeighbouringRiversThatDoNotConnect_AreValid()
        {
            // (0, 1) está al SE de (0, 0); los dos van de E a O y la arista común está cerrada en ambas.
            AssertOk(Create(River(0, 0, RiverShape.Straight, 0), River(0, 1, RiverShape.Straight, 0)));
        }

        // --- Rechazadas ---

        [TestCase(TerrainKind.Grass, "de hierba")]
        [TestCase(TerrainKind.Snow, "de nieve")]
        [TestCase(TerrainKind.Sand, "de arena")]
        public void AnEdgeIntoLand_IsBroken(TerrainKind kind, string what)
        {
            var result = Create(River(0, 0, RiverShape.Straight, 0), Plain(1, 0, kind));

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.BrokenRiver));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Cell, Is.EqualTo(new HexCell(0, 0)));
            Assert.That(result.Edge, Is.EqualTo(HexEdge.E));
            Assert.That(result.Message, Is.EqualTo($"El río de la celda (columna 0, fila 0) no continúa por la arista E: la vecina es {what}."));
        }

        [Test]
        public void AnEdgeIntoARiverPieceWithThatEdgeClosed_IsBroken_FromEitherDataOrder()
        {
            var open = River(0, 0, RiverShape.Straight, 0);
            var closed = River(1, 0, RiverShape.Straight, 1);
            const string message = "El río de la celda (columna 0, fila 0) no continúa por la arista E: la vecina es una pieza de río con esa arista cerrada.";

            foreach (var cells in new[] { new[] { open, closed }, new[] { closed, open } })
            {
                var result = TerritoryLayout.Create(cells);
                Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.BrokenRiver));
                Assert.That(result.Cell, Is.EqualTo(new HexCell(0, 0)));
                Assert.That(result.Edge, Is.EqualTo(HexEdge.E));
                Assert.That(result.Message, Is.EqualTo(message));
            }
        }

        [Test]
        public void TheEdgeNameIsInSpanish()
        {
            // Manantial en (4, 4) abierto al SO (giro 4) hacia (3, 5), que es de nieve.
            var result = Create(River(4, 4, RiverShape.Spring, 4), Plain(3, 5, TerrainKind.Snow));

            Assert.That(result.Edge, Is.EqualTo(HexEdge.SW));
            Assert.That(result.Message, Does.Contain("arista SO"));
        }

        [Test]
        public void Disconnected_ComesBeforeBrokenRiver()
        {
            var result = Create(River(0, 0, RiverShape.Straight, 0), Plain(1, 0, TerrainKind.Grass), Plain(6, 6, TerrainKind.Grass));

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Disconnected));
        }

        private static HexEdge EdgeTowards(HexCell from, HexCell to) => HexEdges.All.Single(edge => from.Neighbour(edge).Equals(to));

        private static int CurveRotation(HexEdge a, HexEdge b)
        {
            for (var r = 0; r < 6; r++)
            {
                var open = RiverPiece.OpenEdges(RiverShape.Curve, r);
                if (open.Contains(a) && open.Contains(b)) return r;
            }

            Assert.Fail($"Ninguna curva une {a} y {b}.");
            return -1;
        }
    }
}
