using System;

namespace Ecosystem.Territory
{
    /// <summary>Variante visual de una celda (data-model.md → TerrainKind). Sin efecto de juego.</summary>
    public enum TerrainKind
    {
        Grass = 0,
        Snow = 1,
        Sand = 2,
        Water = 3,
        River = 4,
    }

    /// <summary>Forma de una pieza de río (data-model.md → RiverShape).</summary>
    public enum RiverShape
    {
        None = 0,
        Spring = 1,
        Straight = 2,
        Curve = 3,
    }

    /// <summary>
    /// Una celda de la disposición con su variante y, si es de río, su forma y su giro
    /// (data-model.md → TerritoryCell). La valida <see cref="TerritoryLayout.Create(System.Collections.Generic.IEnumerable{TerritoryCell})"/>.
    /// </summary>
    public readonly struct TerritoryCell : IEquatable<TerritoryCell>
    {
        public TerritoryCell(HexCell cell, TerrainKind terrain, RiverShape river = RiverShape.None, int rotation = 0)
        {
            Cell = cell;
            Terrain = terrain;
            River = river;
            Rotation = rotation;
        }

        public HexCell Cell { get; }
        public TerrainKind Terrain { get; }
        public RiverShape River { get; }
        public int Rotation { get; }

        public bool Equals(TerritoryCell other) =>
            Cell.Equals(other.Cell) && Terrain == other.Terrain && River == other.River && Rotation == other.Rotation;

        public override bool Equals(object obj) => obj is TerritoryCell other && Equals(other);
        public override int GetHashCode() => (Cell, Terrain, River, Rotation).GetHashCode();
        public override string ToString() => $"{Cell} {Terrain} {River} {Rotation}";
    }

    internal static class TerrainNames
    {
        public static string Of(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Grass: return "hierba";
                case TerrainKind.Snow: return "nieve";
                case TerrainKind.Sand: return "arena";
                case TerrainKind.Water: return "agua";
                case TerrainKind.River: return "río";
                default: return ((int)kind).ToString();
            }
        }

        public static string Of(RiverShape shape)
        {
            switch (shape)
            {
                case RiverShape.Spring: return "manantial";
                case RiverShape.Straight: return "tramo recto";
                case RiverShape.Curve: return "curva";
                default: return ((int)shape).ToString();
            }
        }
    }
}
