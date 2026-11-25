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

        /// <summary>Spec FR-017: cada variante con al menos 4 celdas y una en el borde frontal.</summary>
        [TestCase(TerrainKind.Grass)]
        [TestCase(TerrainKind.Snow)]
        [TestCase(TerrainKind.Sand)]
        [TestCase(TerrainKind.Water)]
        public void EveryVariant_HasFourCellsAndOneOnTheFrontBorder(TerrainKind kind)
        {
            var layout = Load();
            var cells = layout.Entries.Where(entry => entry.Terrain == kind).Select(entry => entry.Cell).ToArray();

            Assert.That(cells, Has.Length.GreaterThanOrEqualTo(4), kind.ToString());
            Assert.That(cells.Intersect(layout.FrontBorderCells()), Is.Not.Empty, $"{kind}: ninguna celda en el borde frontal.");
        }

        /// <summary>
        /// Spec FR-017: un río de al menos 5 piezas, con un manantial, algún tramo recto y alguna curva,
        /// que desemboca en una celda de agua siguiendo sus aristas abiertas.
        /// </summary>
        [Test]
        public void HasARiverFromASpringIntoWater()
        {
            var layout = Load();
            var river = layout.Entries.Where(entry => entry.Terrain == TerrainKind.River).ToDictionary(entry => entry.Cell);

            Assert.That(river, Has.Count.GreaterThanOrEqualTo(5));
            Assert.That(river.Values.Count(entry => entry.River == RiverShape.Spring), Is.EqualTo(1));
            Assert.That(river.Values.Count(entry => entry.River == RiverShape.Straight), Is.GreaterThanOrEqualTo(1));
            Assert.That(river.Values.Count(entry => entry.River == RiverShape.Curve), Is.GreaterThanOrEqualTo(1));

            var current = river.Values.Single(entry => entry.River == RiverShape.Spring);
            var previous = (HexCell?)null;
            var visited = 1;
            while (true)
            {
                var exit = RiverPiece.OpenEdges(current.River, current.Rotation)
                    .Select(edge => current.Cell.Neighbour(edge))
                    .Single(cell => !cell.Equals(previous));
                Assert.That(layout.Contains(exit), Is.True, $"El río sale del territorio en {current.Cell}.");
                if (layout.TerrainAt(exit) == TerrainKind.Water) break;

                Assert.That(river.ContainsKey(exit), Is.True, $"El río no continúa desde {current.Cell}.");
                previous = current.Cell;
                current = river[exit];
                visited++;
                Assert.That(visited, Is.LessThanOrEqualTo(river.Count), "El río no puede volver sobre sí mismo.");
            }

            Assert.That(visited, Is.EqualTo(river.Count), "Todas las piezas forman un solo curso.");
        }
    }
}
