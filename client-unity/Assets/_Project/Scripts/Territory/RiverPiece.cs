using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Aristas abiertas y giro canónico de las piezas de río (data-model.md → RiverShape,
    /// RiverPiece.OpenEdges y RiverPiece.Canonical; research.md R-3).
    /// </summary>
    public static class RiverPiece
    {
        private static readonly HexEdge[] SpringBase = { HexEdge.E };
        private static readonly HexEdge[] StraightBase = { HexEdge.E, HexEdge.W };
        private static readonly HexEdge[] CurveBase = { HexEdge.E, HexEdge.NW };

        public static bool IsDefined(RiverShape shape) => Enum.IsDefined(typeof(RiverShape), shape);

        /// <summary>Aristas abiertas con el giro dado (pasos de 60° antihorarios). None no abre ninguna.</summary>
        public static IReadOnlyList<HexEdge> OpenEdges(RiverShape shape, int rotation)
        {
            HexEdge[] baseEdges;
            switch (shape)
            {
                case RiverShape.Spring: baseEdges = SpringBase; break;
                case RiverShape.Straight: baseEdges = StraightBase; break;
                case RiverShape.Curve: baseEdges = CurveBase; break;
                default: return Array.Empty<HexEdge>();
            }

            return baseEdges.Select(edge => edge.Rotate(rotation)).ToArray();
        }

        /// <summary>Giro que identifica la geometría: el recto es simétrico cada 180°.</summary>
        public static int Canonical(RiverShape shape, int rotation) =>
            shape == RiverShape.Straight ? ((rotation % 3) + 3) % 3 : rotation;

        /// <summary>Las 15 combinaciones (forma, giro canónico) con geometría distinta.</summary>
        public static IEnumerable<(RiverShape Shape, int Rotation)> CanonicalKeys()
        {
            for (var r = 0; r < 6; r++) yield return (RiverShape.Spring, r);
            for (var r = 0; r < 3; r++) yield return (RiverShape.Straight, r);
            for (var r = 0; r < 6; r++) yield return (RiverShape.Curve, r);
        }
    }
}
