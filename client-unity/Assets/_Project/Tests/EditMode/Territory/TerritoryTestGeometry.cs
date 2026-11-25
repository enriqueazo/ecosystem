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

        /// <summary>Centro del disco inicial (spec FR-001).</summary>
        public static readonly HexCell DiscCenter = new HexCell(4, 4);

        /// <summary>Criterio del disco: distancia axial al centro con q² + q·r + r² ≤ 13.</summary>
        public static bool IsInDisc(HexCell cell)
        {
            var (q, r) = cell.ToAxial();
            var (cq, cr) = DiscCenter.ToAxial();
            var dq = q - cq;
            var dr = r - cr;
            return dq * dq + dq * dr + dr * dr <= 13;
        }

        /// <summary>
        /// Las 55 celdas del disco inicial en orden de fila y columna (data-model.md → Disposición
        /// inicial): filas 0–8 con 3, 6, 7, 8, 7, 8, 7, 6 y 3 piezas.
        /// </summary>
        public static HexCell[] InitialCells()
        {
            var cells = new System.Collections.Generic.List<HexCell>();
            for (var row = -2; row <= 10; row++)
            {
                for (var column = -2; column <= 10; column++)
                {
                    var cell = new HexCell(column, row);
                    if (IsInDisc(cell)) cells.Add(cell);
                }
            }

            return cells.ToArray();
        }
    }
}
