using System;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Medidas de la pieza hexagonal con vértice arriba. <see cref="Default"/> es la única fuente
    /// de R y h en el código: la usan la colocación, los límites, la malla y su comprobación
    /// (data-model.md → HexMetrics).
    /// </summary>
    public sealed class HexMetrics
    {
        private static readonly float Sqrt3 = (float)Math.Sqrt(3.0);

        /// <summary>Medidas aprobadas (contracts/visual-direction.md §2).</summary>
        public static readonly HexMetrics Default = new HexMetrics(0.5f, 0.35f);

        public HexMetrics(float outerRadius, float height)
        {
            if (outerRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(outerRadius));
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
            OuterRadius = outerRadius;
            Height = height;
        }

        /// <summary>Circunradio R: distancia del centro a un vértice.</summary>
        public float OuterRadius { get; }

        /// <summary>Altura h del prisma, de la base (y = 0) a la cara superior.</summary>
        public float Height { get; }

        /// <summary>Ancho W = √3·R, entre dos lados opuestos; distancia entre centros vecinos.</summary>
        public float Width => Sqrt3 * OuterRadius;

        /// <summary>Distancia en z entre dos filas consecutivas.</summary>
        public float RowStep => 1.5f * OuterRadius;

        /// <summary>Centro de la base de la celda en el mundo (y = 0).</summary>
        public Point3 Center(HexCell cell)
        {
            var x = Width * (cell.Column + (cell.IsOddRow ? 0.5f : 0f));
            var z = -RowStep * cell.Row;
            return new Point3(x, 0f, z);
        }

        /// <summary>
        /// Vértice <paramref name="index"/> (0–5) de la cara superior, relativo al centro, a
        /// 30° + 60°·index desde +x en el plano XZ. El 1 está al fondo (+z) y el 4, al frente (−z).
        /// </summary>
        public Point3 Corner(int index)
        {
            if (index < 0 || index > 5) throw new ArgumentOutOfRangeException(nameof(index));
            var angle = Math.PI / 180.0 * (30 + 60 * index);
            return new Point3((float)(OuterRadius * Math.Cos(angle)), Height, (float)(OuterRadius * Math.Sin(angle)));
        }

        public bool AreNeighbours(HexCell a, HexCell b) => a.IsNeighbourOf(b);
    }
}
