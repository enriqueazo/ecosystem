using System;
using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → TerritoryFraming: el tablero entero dentro del área segura menos el margen,
    /// centrado y ocupando al menos el 70 % de la dimensión que limita (FR-018, SC-004,
    /// research.md R-6).
    /// </summary>
    public sealed class TerritoryFramingTests
    {
        private const float Tilt = 35f;
        private const float Margin = 0.06f;
        private const float Tolerance = 1e-3f;

        private static readonly object[] ReferenceScreens =
        {
            new object[] { "16:9", 1920, 1080 },
            new object[] { "19.5:9", 2532, 1170 },
            new object[] { "4:3", 2048, 1536 },
            new object[] { "9:16", 1080, 1920 },
            new object[] { "9:19.5", 1170, 2532 },
            new object[] { "3:4", 1536, 2048 },
        };

        private static Point3[] BoardCorners() =>
            TerritoryLayout.Create(InitialCells()).Layout.Bounds(HexMetrics.Default).Corners();

        /// <summary>Proyección ortográfica de la cámara resultante a coordenadas de viewport [0,1].</summary>
        private static (float X, float Y) ToViewport(Point3 point, TerritoryFramingResult framing, Point3[] corners, int width, int height)
        {
            var (centerU, centerV) = TerritoryFraming.ProjectedCenter(corners, Tilt);
            var (u, v) = TerritoryFraming.Project(point, Tilt);
            var aspect = (float)width / height;
            var cameraU = centerU + framing.CameraOffsetU;
            var cameraV = centerV + framing.CameraOffsetV;
            return (0.5f + (u - cameraU) / (2f * framing.OrthographicSize * aspect), 0.5f + (v - cameraV) / (2f * framing.OrthographicSize));
        }

        [TestCaseSource(nameof(ReferenceScreens))]
        public void FullScreen_KeepsEveryCornerInsideTheMarginAndFillsAtLeastSeventyPercent(string name, int width, int height)
        {
            var corners = BoardCorners();
            var framing = TerritoryFraming.Compute(corners, Tilt, width, height, new ScreenRect(0, 0, width, height), Margin);

            var viewport = corners.Select(c => ToViewport(c, framing, corners, width, height)).ToArray();
            foreach (var (x, y) in viewport)
            {
                Assert.That(x, Is.InRange(Margin - Tolerance, 1 - Margin + Tolerance), name);
                Assert.That(y, Is.InRange(Margin - Tolerance, 1 - Margin + Tolerance), name);
            }

            var spanX = viewport.Max(p => p.X) - viewport.Min(p => p.X);
            var spanY = viewport.Max(p => p.Y) - viewport.Min(p => p.Y);
            Assert.That(Math.Max(spanX, spanY), Is.GreaterThanOrEqualTo(0.70f), $"{name}: el tablero ocupa muy poco.");
            Assert.That(Math.Max(spanX, spanY), Is.EqualTo(1 - 2 * Margin).Within(Tolerance), $"{name}: la dimensión que limita queda justa en el margen.");

            // Centrado.
            Assert.That((viewport.Max(p => p.X) + viewport.Min(p => p.X)) / 2, Is.EqualTo(0.5f).Within(Tolerance), name);
            Assert.That((viewport.Max(p => p.Y) + viewport.Min(p => p.Y)) / 2, Is.EqualTo(0.5f).Within(Tolerance), name);
            Assert.That(framing.CameraOffsetU, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(framing.CameraOffsetV, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void PortraitWithANotchAtTheTop_StaysInsideTheSafeAreaAndCentredInIt()
        {
            const int width = 1170, height = 2532;
            var safe = new ScreenRect(0, 102, width, 2532 - 102 - 141); // muesca arriba, barra de inicio abajo
            AssertInsideSafeArea(width, height, safe);
        }

        [Test]
        public void LandscapeWithNotchesOnTheSides_StaysInsideTheSafeAreaAndCentredInIt()
        {
            const int width = 2532, height = 1170;
            var safe = new ScreenRect(141, 63, width - 2 * 141, height - 63);
            AssertInsideSafeArea(width, height, safe);
        }

        private static void AssertInsideSafeArea(int width, int height, ScreenRect safe)
        {
            var corners = BoardCorners();
            var framing = TerritoryFraming.Compute(corners, Tilt, width, height, safe, Margin);
            var x0 = safe.X / width;
            var x1 = (safe.X + safe.Width) / width;
            var y0 = safe.Y / height;
            var y1 = (safe.Y + safe.Height) / height;
            var marginX = Margin * (x1 - x0);
            var marginY = Margin * (y1 - y0);

            var viewport = corners.Select(c => ToViewport(c, framing, corners, width, height)).ToArray();
            foreach (var (x, y) in viewport)
            {
                Assert.That(x, Is.InRange(x0 + marginX - Tolerance, x1 - marginX + Tolerance));
                Assert.That(y, Is.InRange(y0 + marginY - Tolerance, y1 - marginY + Tolerance));
            }

            Assert.That((viewport.Max(p => p.X) + viewport.Min(p => p.X)) / 2, Is.EqualTo((x0 + x1) / 2).Within(Tolerance));
            Assert.That((viewport.Max(p => p.Y) + viewport.Min(p => p.Y)) / 2, Is.EqualTo((y0 + y1) / 2).Within(Tolerance));
            var fill = Math.Max(
                (viewport.Max(p => p.X) - viewport.Min(p => p.X)) / (x1 - x0),
                (viewport.Max(p => p.Y) - viewport.Min(p => p.Y)) / (y1 - y0));
            Assert.That(fill, Is.GreaterThanOrEqualTo(0.70f));
        }

        [Test]
        public void Project_UsesTheTiltWithoutYaw()
        {
            var (u, v) = TerritoryFraming.Project(new Point3(2f, 1f, 3f), Tilt);
            var radians = Tilt * Math.PI / 180;

            Assert.That(u, Is.EqualTo(2f));
            Assert.That(v, Is.EqualTo((float)(1 * Math.Cos(radians) + 3 * Math.Sin(radians))).Within(1e-5f));
        }
    }
}
