using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Disposición del territorio como datos: una lista de celdas offset odd-r
    /// (data-model.md → TerritoryLayoutAsset; research.md R-2). Solo guarda datos; la validación
    /// la hace <see cref="TerritoryLayout.Create"/>. Es un recurso local, no un contrato de red.
    /// </summary>
    [CreateAssetMenu(menuName = "Ecosystem/Territorio/Disposición", fileName = "TerritoryLayout")]
    public sealed class TerritoryLayoutAsset : ScriptableObject
    {
        [Serializable]
        internal struct Cell
        {
            public int column;
            public int row;
        }

        [SerializeField] private List<Cell> cells = new List<Cell>();

        public TerritoryLayoutResult ToLayout() =>
            TerritoryLayout.Create(cells?.Select(cell => new HexCell(cell.column, cell.row)));

        internal void SetCells(IEnumerable<HexCell> newCells)
        {
            cells = newCells.Select(cell => new Cell { column = cell.Column, row = cell.Row }).ToList();
        }
    }
}
