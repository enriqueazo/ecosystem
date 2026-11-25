using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecosystem.Territory
{
    public enum TerritoryLayoutStatus
    {
        Ok,
        Empty,
        Duplicate,
        UnknownTerrain,
        InvalidRiverPiece,
    }

    /// <summary>
    /// Resultado de validar una disposición, sin excepciones (principio V). Con un error, indica la
    /// primera celda afectada y, si aplica, la arista (data-model.md → TerritoryLayout).
    /// </summary>
    public sealed class TerritoryLayoutResult
    {
        private TerritoryLayoutResult(TerritoryLayoutStatus status, TerritoryLayout layout, HexCell cell, HexEdge? edge, string message)
        {
            Status = status;
            Layout = layout;
            Cell = cell;
            Edge = edge;
            Message = message;
        }

        public TerritoryLayoutStatus Status { get; }

        /// <summary>La disposición válida; null si <see cref="Status"/> no es Ok.</summary>
        public TerritoryLayout Layout { get; }

        /// <summary>La celda que causa el error; sin valor útil si es Ok o Empty.</summary>
        public HexCell Cell { get; }

        /// <summary>La arista del error, cuando el error es de una arista.</summary>
        public HexEdge? Edge { get; }

        /// <summary>Causa legible para el registro de error; vacía si es Ok.</summary>
        public string Message { get; }

        internal static TerritoryLayoutResult Ok(TerritoryLayout layout) =>
            new TerritoryLayoutResult(TerritoryLayoutStatus.Ok, layout, default, null, string.Empty);

        internal static TerritoryLayoutResult Error(TerritoryLayoutStatus status, HexCell cell, string message, HexEdge? edge = null) =>
            new TerritoryLayoutResult(status, null, cell, edge, message);

        internal static string Describe(HexCell cell) => $"(columna {cell.Column}, fila {cell.Row})";
    }

    /// <summary>
    /// Clave del catálogo de recursos: una variante que no es río, o una pieza de río con su giro
    /// canónico (data-model.md → TerritoryLayout.RequiredVisuals).
    /// </summary>
    public readonly struct VisualKey : IEquatable<VisualKey>
    {
        private VisualKey(TerrainKind terrain, RiverShape river, int rotation)
        {
            Terrain = terrain;
            River = river;
            Rotation = rotation;
        }

        public TerrainKind Terrain { get; }
        public RiverShape River { get; }
        public int Rotation { get; }
        public bool IsRiver => Terrain == TerrainKind.River;

        public static VisualKey ForTerrain(TerrainKind terrain) => new VisualKey(terrain, RiverShape.None, 0);

        public static VisualKey ForRiver(RiverShape shape, int canonicalRotation) =>
            new VisualKey(TerrainKind.River, shape, canonicalRotation);

        public bool Equals(VisualKey other) => Terrain == other.Terrain && River == other.River && Rotation == other.Rotation;
        public override bool Equals(object obj) => obj is VisualKey other && Equals(other);
        public override int GetHashCode() => (Terrain, River, Rotation).GetHashCode();

        public override string ToString() => IsRiver
            ? $"pieza de río {TerrainNames.Of(River)}, giro {Rotation}"
            : $"variante {TerrainNames.Of(Terrain)}";
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
    /// Disposición del territorio validada e inmutable: celdas con su variante y, si son de río, su
    /// forma y su giro, en el orden del dato (data-model.md → TerritoryLayout). Es un dato local del
    /// cliente, no un contrato de red.
    /// </summary>
    public sealed class TerritoryLayout
    {
        private readonly Dictionary<HexCell, TerritoryCell> _byCell;

        private TerritoryLayout(IReadOnlyList<TerritoryCell> entries, Dictionary<HexCell, TerritoryCell> byCell)
        {
            Entries = entries;
            Cells = entries.Select(entry => entry.Cell).ToList().AsReadOnly();
            _byCell = byCell;
        }

        /// <summary>Las celdas con su variante, en el orden del dato.</summary>
        public IReadOnlyList<TerritoryCell> Entries { get; }

        /// <summary>Las celdas de <see cref="Entries"/>, en el mismo orden.</summary>
        public IReadOnlyList<HexCell> Cells { get; }

        public bool Contains(HexCell cell) => _byCell.ContainsKey(cell);

        /// <summary>Variante de una celda de la disposición.</summary>
        public TerrainKind TerrainAt(HexCell cell) => _byCell[cell].Terrain;

        /// <summary>Todas las celdas son hierba.</summary>
        public static TerritoryLayoutResult Create(IEnumerable<HexCell> cells) =>
            Create(cells?.Select(cell => new TerritoryCell(cell, TerrainKind.Grass)));

        /// <summary>
        /// Valida y devuelve el primer error en el orden fijo de research.md R-6: vacía, repetida,
        /// variante desconocida y datos de río.
        /// </summary>
        public static TerritoryLayoutResult Create(IEnumerable<TerritoryCell> cells)
        {
            var entries = cells?.ToList();
            if (entries == null || entries.Count == 0)
            {
                return TerritoryLayoutResult.Error(TerritoryLayoutStatus.Empty, default, "La disposición del territorio está vacía.");
            }

            var byCell = new Dictionary<HexCell, TerritoryCell>();
            foreach (var entry in entries)
            {
                if (byCell.ContainsKey(entry.Cell))
                {
                    return TerritoryLayoutResult.Error(
                        TerritoryLayoutStatus.Duplicate,
                        entry.Cell,
                        $"La disposición repite la celda {TerritoryLayoutResult.Describe(entry.Cell)}.");
                }

                byCell.Add(entry.Cell, entry);
            }

            foreach (var entry in entries)
            {
                if (!Enum.IsDefined(typeof(TerrainKind), entry.Terrain))
                {
                    return TerritoryLayoutResult.Error(
                        TerritoryLayoutStatus.UnknownTerrain,
                        entry.Cell,
                        $"La celda {TerritoryLayoutResult.Describe(entry.Cell)} tiene una variante de terreno desconocida ({(int)entry.Terrain}).");
                }
            }

            foreach (var entry in entries)
            {
                var cause = InvalidRiverCause(entry);
                if (cause != null)
                {
                    return TerritoryLayoutResult.Error(
                        TerritoryLayoutStatus.InvalidRiverPiece,
                        entry.Cell,
                        $"La celda {TerritoryLayoutResult.Describe(entry.Cell)} tiene datos de río no válidos: {cause}.");
                }
            }

            return TerritoryLayoutResult.Ok(new TerritoryLayout(entries.AsReadOnly(), byCell));
        }

        private static string InvalidRiverCause(TerritoryCell entry)
        {
            if (!RiverPiece.IsDefined(entry.River)) return $"forma de río desconocida ({(int)entry.River})";

            if (entry.Terrain == TerrainKind.River)
            {
                if (entry.River == RiverShape.None) return "es de río y no tiene forma";
                if (entry.Rotation < 0 || entry.Rotation > 5) return $"giro {entry.Rotation} fuera de 0–5";
                return null;
            }

            if (entry.River != RiverShape.None) return "no es de río y tiene forma de río";
            if (entry.Rotation != 0) return $"no es de río y tiene giro {entry.Rotation}";
            return null;
        }

        /// <summary>
        /// Claves del catálogo que necesita la disposición: cada variante usada que no es río y cada
        /// pieza de río con su giro canónico, sin repetir y en orden de primera aparición.
        /// </summary>
        public IReadOnlyList<VisualKey> RequiredVisuals()
        {
            var keys = new List<VisualKey>();
            foreach (var entry in Entries)
            {
                var key = entry.Terrain == TerrainKind.River
                    ? VisualKey.ForRiver(entry.River, RiverPiece.Canonical(entry.River, entry.Rotation))
                    : VisualKey.ForTerrain(entry.Terrain);
                if (!keys.Contains(key)) keys.Add(key);
            }

            return keys.AsReadOnly();
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
