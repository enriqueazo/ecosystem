using System;
using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → HexCell y HexMetrics: centros offset odd-r con vértice arriba, conversión a
    /// axial y vecinos a distancia W exacta (FR-002, FR-004, SC-003).
    /// </summary>
    public sealed class HexCellTests
    {
        private static readonly HexMetrics Metrics = HexMetrics.Default;

        [Test]
        public void Default_HasOuterRadiusHalfAndWidthSqrt3TimesRadius()
        {
            Assert.That(Metrics.OuterRadius, Is.EqualTo(0.5f));
            Assert.That(Metrics.Width, Is.EqualTo((float)(Math.Sqrt(3) * 0.5)).Within(1e-6f));
        }

        [TestCase(0, 0, 0f, 0f)]
        [TestCase(1, 0, 1f, 0f)]
        [TestCase(0, 1, 0.5f, -1f)]
        [TestCase(3, 1, 3.5f, -1f)]
        [TestCase(2, 2, 2f, -2f)]
        [TestCase(0, 3, 0.5f, -3f)]
        public void Center_OddRowsShiftHalfAPieceRight_AndRowsGrowTowardTheFront(int column, int row, float widths, float rowSteps)
        {
            var center = Metrics.Center(new HexCell(column, row));

            Assert.That(center.X, Is.EqualTo(widths * Metrics.Width).Within(1e-5f));
            Assert.That(center.Y, Is.EqualTo(0f));
            Assert.That(center.Z, Is.EqualTo(rowSteps * 1.5f * Metrics.OuterRadius).Within(1e-5f));
        }

        [Test]
        public void Center_NegativeRowsKeepTheOddRShift()
        {
            Assert.That(Metrics.Center(new HexCell(0, -1)).X, Is.EqualTo(0.5f * Metrics.Width).Within(1e-5f));
            Assert.That(Metrics.Center(new HexCell(0, -2)).X, Is.EqualTo(0f).Within(1e-5f));
        }

        [TestCase(0, 0, 0, 0)]
        [TestCase(3, 0, 3, 0)]
        [TestCase(0, 1, 0, 1)]
        [TestCase(1, 2, 0, 2)]
        [TestCase(2, 3, 1, 3)]
        [TestCase(0, -1, 1, -1)]
        public void ToAxial_FollowsTheOddRConversion(int column, int row, int q, int r)
        {
            Assert.That(new HexCell(column, row).ToAxial(), Is.EqualTo((q, r)));
        }

        [Test]
        public void FromAxial_IsTheInverseOfToAxial()
        {
            for (var row = -3; row <= 3; row++)
            {
                for (var column = -3; column <= 3; column++)
                {
                    var cell = new HexCell(column, row);
                    var (q, r) = cell.ToAxial();
                    Assert.That(HexCell.FromAxial(q, r), Is.EqualTo(cell));
                }
            }
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(-1, -3)]
        public void Neighbours_AreSixDistinctCellsAtExactlyOneWidth(int column, int row)
        {
            var cell = new HexCell(column, row);
            var neighbours = cell.Neighbours().ToArray();

            Assert.That(neighbours, Has.Length.EqualTo(6));
            Assert.That(neighbours.Distinct().Count(), Is.EqualTo(6));
            Assert.That(neighbours, Has.None.EqualTo(cell));
            foreach (var neighbour in neighbours)
            {
                var distance = HorizontalDistance(Metrics.Center(cell), Metrics.Center(neighbour));
                Assert.That(distance, Is.EqualTo(Metrics.Width).Within(EdgeTolerance(Metrics)), $"{cell} → {neighbour}");
                Assert.That(Metrics.AreNeighbours(cell, neighbour), Is.True);
            }
        }

        [Test]
        public void NonNeighbours_AreFartherThanOneWidth()
        {
            var origin = new HexCell(1, 1);
            for (var row = -2; row <= 4; row++)
            {
                for (var column = -2; column <= 4; column++)
                {
                    var other = new HexCell(column, row);
                    if (other.Equals(origin) || Metrics.AreNeighbours(origin, other)) continue;

                    var distance = HorizontalDistance(Metrics.Center(origin), Metrics.Center(other));
                    Assert.That(distance, Is.GreaterThan(Metrics.Width + EdgeTolerance(Metrics)), $"{origin} → {other}");
                }
            }
        }

        [Test]
        public void Corners_ArePointyTopAtTheTopFaceHeight()
        {
            var corners = Enumerable.Range(0, 6).Select(Metrics.Corner).ToArray();

            Assert.That(corners.Select(c => c.Y), Is.All.EqualTo(Metrics.Height));
            Assert.That(corners.Max(c => c.Z), Is.EqualTo(Metrics.OuterRadius).Within(1e-6f), "Vértice al fondo (+z).");
            Assert.That(corners.Min(c => c.Z), Is.EqualTo(-Metrics.OuterRadius).Within(1e-6f), "Vértice al frente (−z).");
            Assert.That(corners.Max(c => c.X), Is.EqualTo(Metrics.Width / 2f).Within(1e-6f));
            foreach (var corner in corners)
            {
                Assert.That(HorizontalDistance(corner, new Point3(0f, 0f, 0f)), Is.EqualTo(Metrics.OuterRadius).Within(1e-6f));
            }
        }

        [Test]
        public void Equality_IsByValue()
        {
            Assert.That(new HexCell(2, 3), Is.EqualTo(new HexCell(2, 3)));
            Assert.That(new HexCell(2, 3), Is.Not.EqualTo(new HexCell(3, 2)));
            Assert.That(new HexCell(2, 3).GetHashCode(), Is.EqualTo(new HexCell(2, 3).GetHashCode()));
        }
    }
}
