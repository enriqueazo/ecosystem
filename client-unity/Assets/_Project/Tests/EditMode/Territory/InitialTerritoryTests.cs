using System.Linq;
using Ecosystem.Territory;
using Ecosystem.Territory.View;
using NUnit.Framework;
using UnityEditor;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// La disposición inicial guardada en InitialTerritory.asset (data-model.md → Disposición
    /// inicial; spec FR-001, FR-017, SC-002). Sustituye las comprobaciones del tablero 4 × 4.
    /// </summary>
    public sealed class InitialTerritoryTests
    {
        private const string InitialTerritoryPath = "Assets/_Project/Data/Territory/InitialTerritory.asset";

        private static TerritoryLayout Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TerritoryLayoutAsset>(InitialTerritoryPath);
            Assert.That(asset, Is.Not.Null, $"Falta {InitialTerritoryPath}.");
            var result = asset.ToLayout();
            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);
            return result.Layout;
        }

        [Test]
        public void HasExactlyTheFiftyFiveDiscCells()
        {
            var layout = Load();

            Assert.That(layout.Cells, Has.Count.EqualTo(55));
            Assert.That(layout.Cells.All(IsInDisc), Is.True, "Ninguna celda fuera del disco.");
            Assert.That(layout.Cells, Is.EquivalentTo(InitialCells()), "Ninguna celda del disco falta (sin huecos).");
        }

        [Test]
        public void HasNineRowsWithTheExpectedWidths()
        {
            var rows = Load().Cells.GroupBy(cell => cell.Row).OrderBy(group => group.Key).ToArray();

            Assert.That(rows.Select(group => group.Key), Is.EqualTo(Enumerable.Range(0, 9)));
            Assert.That(rows.Select(group => group.Count()), Is.EqualTo(new[] { 3, 6, 7, 8, 7, 8, 7, 6, 3 }));
            Assert.That(rows.Select(group => (group.Min(c => c.Column), group.Max(c => c.Column))), Is.EqualTo(new[]
            {
                (3, 5), (1, 6), (1, 7), (0, 7), (1, 7), (0, 7), (1, 7), (1, 6), (3, 5),
            }));
        }

        [Test]
        public void HasOneHundredThirtyEightPairsAndTwentyFourBorderCells()
        {
            var layout = Load();

            Assert.That(layout.AdjacentPairs(), Is.EqualTo(138));
            Assert.That(layout.BorderCells(), Has.Count.EqualTo(24));
            Assert.That(layout.Cells.Count - layout.BorderCells().Count, Is.EqualTo(31), "Celdas interiores.");
        }

        [Test]
        public void IsASingleConnectedGroupInDataOrder()
        {
            var layout = Load();

            // Ok ya exige que sea conexa; además, el dato va en orden de fila y columna.
            var ordered = layout.Cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column);
            Assert.That(layout.Cells, Is.EqualTo(ordered));
        }
    }
}
