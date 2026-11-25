using System;
using Ecosystem.Territory;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>Utilidades geométricas compartidas por las pruebas del territorio.</summary>
    internal static class TerritoryTestGeometry
    {
        /// <summary>Tolerancia de SC-003: 0,1 % del ancho de la pieza.</summary>
        public static float EdgeTolerance(HexMetrics metrics) => metrics.Width * 0.001f;

        public static float HorizontalDistance(Point3 a, Point3 b)
        {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public static Point3 Minus(Point3 a, Point3 b) => new Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Point3 Cross(Point3 a, Point3 b) =>
            new Point3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        public static float Dot(Point3 a, Point3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static HexCell[] InitialCells()
        {
            var cells = new HexCell[16];
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    cells[row * 4 + column] = new HexCell(column, row);
                }
            }

            return cells;
        }
    }
}
