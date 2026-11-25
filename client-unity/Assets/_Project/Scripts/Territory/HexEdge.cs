using System;
using System.Collections.Generic;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Las seis aristas de una pieza con vértice arriba, en el orden de
    /// <see cref="HexCell.Neighbours"/>: antihorario visto desde arriba, empezando por el este
    /// (data-model.md → HexEdge; research.md R-3). La normal de la arista e está a 60°·e desde +x
    /// hacia +z (el fondo).
    /// </summary>
    public enum HexEdge
    {
        E = 0,
        NE = 1,
        NW = 2,
        W = 3,
        SW = 4,
        SE = 5,
    }

    public static class HexEdges
    {
        private static readonly string[] Names = { "E", "NE", "NO", "O", "SO", "SE" };

        public static readonly IReadOnlyList<HexEdge> All = Array.AsReadOnly(new[]
        {
            HexEdge.E, HexEdge.NE, HexEdge.NW, HexEdge.W, HexEdge.SW, HexEdge.SE,
        });

        public static HexEdge Opposite(this HexEdge edge) => (HexEdge)(((int)edge + 3) % 6);

        /// <summary>Arista desplazada <paramref name="steps"/> pasos de 60° en sentido antihorario.</summary>
        public static HexEdge Rotate(this HexEdge edge, int steps) => (HexEdge)((((int)edge + steps) % 6 + 6) % 6);

        /// <summary>Nombre en español para los mensajes: E, NE, NO, O, SO o SE.</summary>
        public static string DisplayName(this HexEdge edge) => Names[(int)edge];

        /// <summary>Normal unitaria de la arista en el plano XZ (y = 0).</summary>
        public static Point3 Normal(this HexEdge edge)
        {
            var angle = Math.PI / 3.0 * (int)edge;
            return new Point3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle));
        }
    }
}
