using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Disposición del territorio como datos: una lista de celdas offset odd-r con su variante y,
    /// si son de río, su forma y su giro (data-model.md → TerritoryLayoutAsset; research.md R-1).
    /// Los campos nuevos valen 0 por defecto, así que el formato de la spec 004 se lee como hierba.
    /// Solo guarda datos; la validación la hace <see cref="TerritoryLayout.Create(IEnumerable{TerritoryCell})"/>.
    /// Es un recurso local, no un contrato de red.
    /// </summary>
    [CreateAssetMenu(menuName = "Ecosystem/Territorio/Disposición", fileName = "TerritoryLayout")]
    public sealed class TerritoryLayoutAsset : ScriptableObject
    {
        [Serializable]
        internal struct Cell
        {
            public int column;
            public int row;
            public TerrainKind terrain;
            public RiverShape river;
            public int rotation;
        }

        [SerializeField] private List<Cell> cells = new List<Cell>();

        public TerritoryLayoutResult ToLayout() =>
            TerritoryLayout.Create(cells?.Select(cell =>
                new TerritoryCell(new HexCell(cell.column, cell.row), cell.terrain, cell.river, cell.rotation)));

        internal void SetCells(IEnumerable<HexCell> newCells) =>
            SetCells(newCells.Select(cell => new TerritoryCell(cell, TerrainKind.Grass)));

        internal void SetCells(IEnumerable<TerritoryCell> newCells)
        {
            cells = newCells.Select(entry => new Cell
            {
                column = entry.Cell.Column,
                row = entry.Cell.Row,
                terrain = entry.Terrain,
                river = entry.River,
                rotation = entry.Rotation,
            }).ToList();
        }
    }
}
