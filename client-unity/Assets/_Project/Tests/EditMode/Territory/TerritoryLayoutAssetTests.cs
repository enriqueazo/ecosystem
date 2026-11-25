using System.Linq;
using Ecosystem.Territory;
using Ecosystem.Territory.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → TerritoryLayoutAsset.cells: el asset solo guarda datos y su validación la
    /// hace el núcleo (FR-001, FR-002).
    /// </summary>
    public sealed class TerritoryLayoutAssetTests
    {
        private const string InitialTerritoryPath = "Assets/_Project/Data/Territory/InitialTerritory.asset";

        [Test]
        public void InitialTerritory_IsOkWithTheSixteenCellsOfTheFourByFourLayout()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TerritoryLayoutAsset>(InitialTerritoryPath);
            Assert.That(asset, Is.Not.Null, $"Falta {InitialTerritoryPath}.");

            var result = asset.ToLayout();

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);
            var expected = from row in Enumerable.Range(0, 4) from column in Enumerable.Range(0, 4) select new HexCell(column, row);
            Assert.That(result.Layout.Cells, Is.EquivalentTo(expected));
            Assert.That(result.Layout.Cells, Has.Count.EqualTo(16));
        }

        private static TerritoryLayoutAsset AssetWith(params HexCell[] cells)
        {
            var asset = ScriptableObject.CreateInstance<TerritoryLayoutAsset>();
            asset.SetCells(cells);
            return asset;
        }

        [Test]
        public void AnotherValidLayout_IsOkWithExactlyItsCells()
        {
            var cells = TerritoryTestGeometry.InitialCells().Take(15).ToArray();
            var asset = AssetWith(cells);
            try
            {
                var result = asset.ToLayout();
                Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok));
                Assert.That(result.Layout.Cells, Is.EqualTo(cells));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void EmptyLayout_IsRejected()
        {
            var asset = AssetWith();
            try
            {
                Assert.That(asset.ToLayout().Status, Is.EqualTo(TerritoryLayoutStatus.Empty));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void LayoutWithARepeatedCell_IsRejected()
        {
            var asset = AssetWith(new HexCell(0, 0), new HexCell(1, 0), new HexCell(0, 0));
            try
            {
                var result = asset.ToLayout();
                Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Duplicate));
                Assert.That(result.DuplicateCell, Is.EqualTo(new HexCell(0, 0)));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
