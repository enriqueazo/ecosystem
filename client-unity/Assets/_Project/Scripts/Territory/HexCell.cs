using System;
using System.Collections.Generic;

namespace Ecosystem.Territory
{
    /// <summary>
    /// Celda de la rejilla en coordenadas offset «odd-r» para hexágonos con vértice arriba:
    /// la fila crece hacia el frente del tablero y las filas impares se desplazan media pieza a la
    /// derecha (data-model.md → HexCell; research.md R-2).
    /// </summary>
    public readonly struct HexCell : IEquatable<HexCell>
    {
        // Direcciones axiales de los seis vecinos.
        private static readonly (int Q, int R)[] AxialDirections =
        {
            (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1),
        };

        public HexCell(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }
        public int Row { get; }

        public bool IsOddRow => (Row & 1) != 0;

        public (int Q, int R) ToAxial() => (Column - (Row - (Row & 1)) / 2, Row);

        public static HexCell FromAxial(int q, int r) => new HexCell(q + (r - (r & 1)) / 2, r);

        public IEnumerable<HexCell> Neighbours()
        {
            var (q, r) = ToAxial();
            foreach (var (dq, dr) in AxialDirections)
            {
                yield return FromAxial(q + dq, r + dr);
            }
        }

        public bool IsNeighbourOf(HexCell other)
        {
            var (q, r) = ToAxial();
            var (oq, or) = other.ToAxial();
            var dq = oq - q;
            var dr = or - r;
            foreach (var direction in AxialDirections)
            {
                if (direction.Q == dq && direction.R == dr) return true;
            }

            return false;
        }

        public bool Equals(HexCell other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is HexCell other && Equals(other);
        public override int GetHashCode() => (Column, Row).GetHashCode();
        public override string ToString() => $"(columna {Column}, fila {Row})";
    }
}
