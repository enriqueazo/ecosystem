using System;
using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// Triangulación por recorte de orejas de polígonos simples (research.md R-2): cubre el polígono
    /// entero, con n − 2 triángulos en sentido antihorario y sin excepciones con datos degenerados.
    /// </summary>
    public sealed class PolygonTriangulatorTests
    {
        private static Point2 P(float x, float y) => new Point2(x, y);

        private static float SignedArea(Point2 a, Point2 b, Point2 c) =>
            ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) / 2f;

        private static float PolygonArea(Point2[] polygon)
        {
            var area = 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                area += a.X * b.Y - b.X * a.Y;
            }

            return Math.Abs(area) / 2f;
        }

        private static void AssertCovers(Point2[] polygon)
        {
            var indices = PolygonTriangulator.Triangulate(polygon);

            Assert.That(indices.Length, Is.EqualTo((polygon.Length - 2) * 3), "n − 2 triángulos.");
            var total = 0f;
            for (var t = 0; t < indices.Length; t += 3)
            {
                var area = SignedArea(polygon[indices[t]], polygon[indices[t + 1]], polygon[indices[t + 2]]);
                Assert.That(area, Is.GreaterThan(0f), $"Triángulo {t / 3} en sentido antihorario.");
                total += area;
            }

            Assert.That(total, Is.EqualTo(PolygonArea(polygon)).Within(1e-4f), "La suma de áreas es la del polígono.");
            Assert.That(indices.All(i => i >= 0 && i < polygon.Length), Is.True);
        }

        [Test]
        public void Convex_IsCovered()
        {
            AssertCovers(new[] { P(0, 0), P(2, 0), P(3, 1), P(2, 2), P(0, 2) });
        }

        [Test]
        public void Concave_IsCovered()
        {
            AssertCovers(new[] { P(0, 0), P(4, 0), P(4, 4), P(2, 1), P(0, 4) });
        }

        [Test]
        public void UShape_IsCovered()
        {
            AssertCovers(new[] { P(0, 0), P(3, 0), P(3, 3), P(2, 3), P(2, 1), P(1, 1), P(1, 3), P(0, 3) });
        }

        [Test]
        public void ClockwiseInput_IsCoveredCounterClockwise()
        {
            AssertCovers(new[] { P(0, 3), P(1, 3), P(1, 1), P(2, 1), P(2, 3), P(3, 3), P(3, 0), P(0, 0) });
        }

        [Test]
        public void Keyhole_IsCovered()
        {
            // Cuadrado con un cauce que entra por la derecha y acaba en una poza: la forma de la orilla del manantial.
            var theta = Math.Asin(0.1 / 0.3);
            var xp = (float)Math.Sqrt(0.3 * 0.3 - 0.1 * 0.1);
            var arc = Enumerable.Range(0, 17).Select(k =>
            {
                var angle = -theta - k * (2 * Math.PI - 2 * theta) / 16;
                return P(0.3f * (float)Math.Cos(angle), 0.3f * (float)Math.Sin(angle));
            });
            var polygon = new[] { P(1, -1), P(1, -0.1f), P(xp, -0.1f) }
                .Concat(arc.Skip(1).Take(15))
                .Concat(new[] { P(xp, 0.1f), P(1, 0.1f), P(1, 1), P(-1, 1), P(-1, -1) })
                .ToArray();

            AssertCovers(polygon);
        }

        [Test]
        public void Degenerate_ReturnsADefinedResultWithoutThrowing()
        {
            Assert.That(PolygonTriangulator.Triangulate(new Point2[0]), Is.Empty);
            Assert.That(PolygonTriangulator.Triangulate(new[] { P(0, 0), P(1, 1) }), Is.Empty);
            Assert.That(PolygonTriangulator.Triangulate(new[] { P(0, 0), P(1, 1), P(2, 2) }), Is.Empty, "Colineales: área cero.");
            Assert.That(PolygonTriangulator.Triangulate(null), Is.Empty);
        }
    }
}
