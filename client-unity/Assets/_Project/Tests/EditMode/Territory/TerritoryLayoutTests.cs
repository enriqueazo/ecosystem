using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → TerritoryLayout: validación (FR-003), disposición inicial 4 × 4 (FR-002),
    /// contigüidad sin huecos ni solapamientos (FR-004, SC-003) y límites para el encuadre.
    /// </summary>
    public sealed class TerritoryLayoutTests
    {
        private static readonly HexMetrics Metrics = HexMetrics.Default;

        private static TerritoryLayout InitialLayout()
        {
            var result = TerritoryLayout.Create(InitialCells());
            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);
            return result.Layout;
        }

        [Test]
        public void Create_WithTheSixteenInitialCells_IsOkAndKeepsTheOrder()
        {
            var layout = InitialLayout();

            Assert.That(layout.Cells, Is.EqualTo(InitialCells()));
            Assert.That(layout.Cells.Distinct().Count(), Is.EqualTo(16));
        }

        [Test]
        public void InitialLayout_HasThirtyThreeAdjacentPairs()
        {
            Assert.That(InitialLayout().AdjacentPairs(), Is.EqualTo(33));
        }

        [Test]
        public void InitialLayout_NoTwoPiecesAreCloserThanOneWidth()
        {
            var cells = InitialLayout().Cells;
            for (var i = 0; i < cells.Count; i++)
            {
                for (var j = i + 1; j < cells.Count; j++)
                {
                    var distance = HorizontalDistance(Metrics.Center(cells[i]), Metrics.Center(cells[j]));
                    Assert.That(distance, Is.GreaterThanOrEqualTo(Metrics.Width - EdgeTolerance(Metrics)), $"{cells[i]} y {cells[j]} se solapan.");
                }
            }
        }

        [Test]
        public void InitialLayout_EveryCellHasItsExpectedNeighbours()
        {
            var cells = InitialLayout().Cells;
            foreach (var cell in cells)
            {
                var expected = cell.Neighbours().Count(cells.Contains);
                var actual = cells.Count(other => !other.Equals(cell) && Metrics.AreNeighbours(cell, other));
                Assert.That(actual, Is.EqualTo(expected), cell.ToString());
            }

            // Celdas interiores del 4 × 4 odd-r: rodeadas por completo.
            Assert.That(new HexCell(1, 1).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(1, 2).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(2, 1).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(2, 2).Neighbours().All(cells.Contains), Is.True);
        }

        [Test]
        public void Create_WithNull_IsEmpty()
        {
            var result = TerritoryLayout.Create(null);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Empty));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Message, Is.EqualTo("La disposición del territorio está vacía."));
        }

        [Test]
        public void Create_WithNoCells_IsEmpty()
        {
            Assert.That(TerritoryLayout.Create(new HexCell[0]).Status, Is.EqualTo(TerritoryLayoutStatus.Empty));
        }

        [Test]
        public void Create_WithARepeatedCell_ReportsTheFirstDuplicate()
        {
            var cells = new[] { new HexCell(0, 0), new HexCell(1, 0), new HexCell(2, 3), new HexCell(1, 0), new HexCell(2, 3) };

            var result = TerritoryLayout.Create(cells);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Duplicate));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.DuplicateCell, Is.EqualTo(new HexCell(1, 0)));
            Assert.That(result.Message, Is.EqualTo("La disposición repite la celda (columna 1, fila 0)."));
        }

        [Test]
        public void Bounds_ContainEveryCornerFromBaseToTopFace()
        {
            var layout = InitialLayout();
            var bounds = layout.Bounds(Metrics);

            Assert.That(bounds.Min.Y, Is.EqualTo(0f));
            Assert.That(bounds.Max.Y, Is.EqualTo(Metrics.Height));
            foreach (var cell in layout.Cells)
            {
                var center = Metrics.Center(cell);
                for (var i = 0; i < 6; i++)
                {
                    var corner = Metrics.Corner(i);
                    var x = center.X + corner.X;
                    var z = center.Z + corner.Z;
                    Assert.That(x, Is.InRange(bounds.Min.X - 1e-5f, bounds.Max.X + 1e-5f));
                    Assert.That(z, Is.InRange(bounds.Min.Z - 1e-5f, bounds.Max.Z + 1e-5f));
                }
            }

            // Ajustada: el ancho es 4,5 piezas (filas impares desplazadas) y el fondo, 3 pasos de fila + 2R.
            Assert.That(bounds.Max.X - bounds.Min.X, Is.EqualTo(4.5f * Metrics.Width).Within(1e-5f));
            Assert.That(bounds.Max.Z - bounds.Min.Z, Is.EqualTo(3 * 1.5f * Metrics.OuterRadius + 2 * Metrics.OuterRadius).Within(1e-5f));
            Assert.That(bounds.Corners(), Has.Length.EqualTo(8));
        }
    }
}
