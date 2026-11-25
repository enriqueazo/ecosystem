using System.Collections.Generic;
using System.Linq;

namespace Ecosystem.Territory
{
    public enum TerritoryLayoutStatus
    {
        Ok,
        Empty,
        Duplicate,
    }

    /// <summary>Resultado de validar una disposición, sin excepciones (principio V).</summary>
    public sealed class TerritoryLayoutResult
    {
        private TerritoryLayoutResult(TerritoryLayoutStatus status, TerritoryLayout layout, HexCell duplicateCell, string message)
        {
            Status = status;
            Layout = layout;
            DuplicateCell = duplicateCell;
            Message = message;
        }

        public TerritoryLayoutStatus Status { get; }

        /// <summary>La disposición válida; null si <see cref="Status"/> no es Ok.</summary>
        public TerritoryLayout Layout { get; }

        /// <summary>La primera celda repetida, si <see cref="Status"/> es Duplicate.</summary>
        public HexCell DuplicateCell { get; }

        /// <summary>Causa legible para el registro de error; vacía si es Ok.</summary>
        public string Message { get; }

        internal static TerritoryLayoutResult Ok(TerritoryLayout layout) =>
            new TerritoryLayoutResult(TerritoryLayoutStatus.Ok, layout, default, string.Empty);

        internal static TerritoryLayoutResult Empty() =>
            new TerritoryLayoutResult(TerritoryLayoutStatus.Empty, null, default, "La disposición del territorio está vacía.");

        internal static TerritoryLayoutResult Duplicate(HexCell cell) =>
            new TerritoryLayoutResult(
                TerritoryLayoutStatus.Duplicate,
                null,
                cell,
                $"La disposición repite la celda (columna {cell.Column}, fila {cell.Row}).");
    }

    /// <summary>Caja alineada con los ejes que contiene el tablero (para el encuadre).</summary>
    public readonly struct Bounds3
    {
        public Bounds3(Point3 min, Point3 max)
        {
            Min = min;
            Max = max;
        }

        public Point3 Min { get; }
        public Point3 Max { get; }

        public Point3[] Corners() => new[]
        {
            new Point3(Min.X, Min.Y, Min.Z), new Point3(Max.X, Min.Y, Min.Z),
            new Point3(Min.X, Max.Y, Min.Z), new Point3(Max.X, Max.Y, Min.Z),
            new Point3(Min.X, Min.Y, Max.Z), new Point3(Max.X, Min.Y, Max.Z),
            new Point3(Min.X, Max.Y, Max.Z), new Point3(Max.X, Max.Y, Max.Z),
        };
    }

    /// <summary>
    /// Disposición del territorio validada e inmutable: al menos una celda y sin repetidas, en el
    /// orden del dato (data-model.md → TerritoryLayout). Es un dato local del cliente, no un
    /// contrato de red (FR-001).
    /// </summary>
    public sealed class TerritoryLayout
    {
        private TerritoryLayout(IReadOnlyList<HexCell> cells)
        {
            Cells = cells;
        }

        public IReadOnlyList<HexCell> Cells { get; }

        public static TerritoryLayoutResult Create(IEnumerable<HexCell> cells)
        {
            if (cells == null) return TerritoryLayoutResult.Empty();

            var ordered = new List<HexCell>();
            var seen = new HashSet<HexCell>();
            foreach (var cell in cells)
            {
                if (!seen.Add(cell)) return TerritoryLayoutResult.Duplicate(cell);
                ordered.Add(cell);
            }

            return ordered.Count == 0
                ? TerritoryLayoutResult.Empty()
                : TerritoryLayoutResult.Ok(new TerritoryLayout(ordered.AsReadOnly()));
        }

        /// <summary>Número de parejas de celdas vecinas (33 en la disposición inicial 4 × 4).</summary>
        public int AdjacentPairs()
        {
            var set = new HashSet<HexCell>(Cells);
            var count = Cells.Sum(cell => cell.Neighbours().Count(set.Contains));
            return count / 2;
        }

        /// <summary>Caja que contiene todas las piezas, de la base (y = 0) a la cara superior.</summary>
        public Bounds3 Bounds(HexMetrics metrics)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            var halfWidth = metrics.Width / 2f;
            foreach (var cell in Cells)
            {
                var center = metrics.Center(cell);
                if (center.X - halfWidth < minX) minX = center.X - halfWidth;
                if (center.X + halfWidth > maxX) maxX = center.X + halfWidth;
                if (center.Z - metrics.OuterRadius < minZ) minZ = center.Z - metrics.OuterRadius;
                if (center.Z + metrics.OuterRadius > maxZ) maxZ = center.Z + metrics.OuterRadius;
            }

            return new Bounds3(new Point3(minX, 0f, minZ), new Point3(maxX, metrics.Height, maxZ));
        }
    }
}
