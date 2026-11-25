using System.Linq;
using Ecosystem.Territory;
using NUnit.Framework;
using static Ecosystem.Tests.EditMode.Territory.TerritoryTestGeometry;

namespace Ecosystem.Tests.EditMode.Territory
{
    /// <summary>
    /// data-model.md → TerritoryLayout: validación (FR-003), disposición inicial en disco (spec 005 FR-001),
    /// contigüidad sin huecos ni solapamientos (FR-004, SC-003) y límites para el encuadre.
    /// </summary>
    public sealed class TerritoryLayoutTests
    {
        private static readonly HexMetrics Metrics = HexMetrics.Default;

        private static TerritoryLayout InitialLayout()
        {
            var result = TerritoryLayout.Create(InitialCells());
            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);
            return result.Layout;
        }

        [Test]
        public void Create_WithTheInitialDiscCells_IsOkAndKeepsTheOrder()
        {
            var layout = InitialLayout();

            Assert.That(layout.Cells, Is.EqualTo(InitialCells()));
            Assert.That(layout.Cells.Distinct().Count(), Is.EqualTo(55));
        }

        [Test]
        public void InitialLayout_HasOneHundredThirtyEightAdjacentPairs()
        {
            Assert.That(InitialLayout().AdjacentPairs(), Is.EqualTo(138));
        }

        [Test]
        public void InitialLayout_NoTwoPiecesAreCloserThanOneWidth()
        {
            var cells = InitialLayout().Cells;
            for (var i = 0; i < cells.Count; i++)
            {
                for (var j = i + 1; j < cells.Count; j++)
                {
                    var distance = HorizontalDistance(Metrics.Center(cells[i]), Metrics.Center(cells[j]));
                    Assert.That(distance, Is.GreaterThanOrEqualTo(Metrics.Width - EdgeTolerance(Metrics)), $"{cells[i]} y {cells[j]} se solapan.");
                }
            }
        }

        [Test]
        public void InitialLayout_EveryCellHasItsExpectedNeighbours()
        {
            var cells = InitialLayout().Cells;
            foreach (var cell in cells)
            {
                var expected = cell.Neighbours().Count(cells.Contains);
                var actual = cells.Count(other => !other.Equals(cell) && Metrics.AreNeighbours(cell, other));
                Assert.That(actual, Is.EqualTo(expected), cell.ToString());
            }

            // Celdas interiores del disco: rodeadas por completo.
            Assert.That(new HexCell(4, 4).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(3, 3).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(5, 5).Neighbours().All(cells.Contains), Is.True);
            Assert.That(new HexCell(2, 4).Neighbours().All(cells.Contains), Is.True);
        }

        [Test]
        public void Create_WithNull_IsEmpty()
        {
            var result = TerritoryLayout.Create((HexCell[])null);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Empty));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Message, Is.EqualTo("La disposición del territorio está vacía."));
        }

        [Test]
        public void Create_WithNoCells_IsEmpty()
        {
            Assert.That(TerritoryLayout.Create(new HexCell[0]).Status, Is.EqualTo(TerritoryLayoutStatus.Empty));
        }

        [Test]
        public void Create_WithARepeatedCell_ReportsTheFirstDuplicate()
        {
            var cells = new[] { new HexCell(0, 0), new HexCell(1, 0), new HexCell(2, 3), new HexCell(1, 0), new HexCell(2, 3) };

            var result = TerritoryLayout.Create(cells);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Duplicate));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Cell, Is.EqualTo(new HexCell(1, 0)));
            Assert.That(result.Message, Is.EqualTo("La disposición repite la celda (columna 1, fila 0)."));
        }

        [Test]
        public void Bounds_ContainEveryCornerFromBaseToTopFace()
        {
            var layout = InitialLayout();
            var bounds = layout.Bounds(Metrics);

            Assert.That(bounds.Min.Y, Is.EqualTo(0f));
            Assert.That(bounds.Max.Y, Is.EqualTo(Metrics.Height));
            foreach (var cell in layout.Cells)
            {
                var center = Metrics.Center(cell);
                for (var i = 0; i < 6; i++)
                {
                    var corner = Metrics.Corner(i);
                    var x = center.X + corner.X;
                    var z = center.Z + corner.Z;
                    Assert.That(x, Is.InRange(bounds.Min.X - 1e-5f, bounds.Max.X + 1e-5f));
                    Assert.That(z, Is.InRange(bounds.Min.Z - 1e-5f, bounds.Max.Z + 1e-5f));
                }
            }

            // Ajustada: el ancho es 8 piezas (filas 3 y 5, de la columna 0 a la 7 desplazadas) y el fondo, 8 pasos de fila + 2R.
            Assert.That(bounds.Max.X - bounds.Min.X, Is.EqualTo(8f * Metrics.Width).Within(1e-5f));
            Assert.That(bounds.Max.Z - bounds.Min.Z, Is.EqualTo(8 * 1.5f * Metrics.OuterRadius + 2 * Metrics.OuterRadius).Within(1e-5f));
            Assert.That(bounds.Corners(), Has.Length.EqualTo(8));
        }

        // --- Variantes y datos de río (data-model.md → TerritoryLayout, órdenes 1–4; research.md R-6) ---

        private static TerritoryCell Grass(int column, int row) => new TerritoryCell(new HexCell(column, row), TerrainKind.Grass);

        [Test]
        public void Create_WithTerritoryCells_KeepsEntriesAndCellsInTheDataOrder()
        {
            var entries = new[]
            {
                new TerritoryCell(new HexCell(1, 0), TerrainKind.Snow),
                new TerritoryCell(new HexCell(0, 0), TerrainKind.Water),
                // Recto de NO a SE: desemboca en el agua de (0, 0) y sale por el borde (regla de conexión).
                new TerritoryCell(new HexCell(0, 1), TerrainKind.River, RiverShape.Straight, 2),
                new TerritoryCell(new HexCell(1, 1), TerrainKind.Sand),
            };

            var result = TerritoryLayout.Create(entries);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Ok), result.Message);
            Assert.That(result.Layout.Entries, Is.EqualTo(entries));
            Assert.That(result.Layout.Cells, Is.EqualTo(entries.Select(entry => entry.Cell)));
            Assert.That(result.Layout.TerrainAt(new HexCell(0, 0)), Is.EqualTo(TerrainKind.Water));
        }

        [Test]
        public void Create_WithHexCells_MakesEveryCellGrass()
        {
            var layout = TerritoryLayout.Create(new[] { new HexCell(0, 0), new HexCell(1, 0) }).Layout;

            Assert.That(layout.Entries.Select(entry => entry.Terrain), Is.All.EqualTo(TerrainKind.Grass));
            Assert.That(layout.Entries.Select(entry => entry.River), Is.All.EqualTo(RiverShape.None));
            Assert.That(layout.Entries.Select(entry => entry.Rotation), Is.All.EqualTo(0));
        }

        [Test]
        public void Create_WithAnUnknownTerrain_ReportsTheCell()
        {
            var result = TerritoryLayout.Create(new[] { Grass(0, 0), new TerritoryCell(new HexCell(1, 0), (TerrainKind)9) });

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.UnknownTerrain));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Cell, Is.EqualTo(new HexCell(1, 0)));
            Assert.That(result.Message, Is.EqualTo("La celda (columna 1, fila 0) tiene una variante de terreno desconocida (9)."));
        }

        private static readonly object[] InvalidRiverPieces =
        {
            new object[] { TerrainKind.River, RiverShape.None, 0, "es de río y no tiene forma" },
            new object[] { TerrainKind.River, (RiverShape)7, 0, "forma de río desconocida (7)" },
            new object[] { TerrainKind.Sand, (RiverShape)7, 0, "forma de río desconocida (7)" },
            new object[] { TerrainKind.Snow, RiverShape.Curve, 0, "no es de río y tiene forma de río" },
            new object[] { TerrainKind.Grass, RiverShape.None, 2, "no es de río y tiene giro 2" },
            new object[] { TerrainKind.River, RiverShape.Spring, 6, "giro 6 fuera de 0–5" },
            new object[] { TerrainKind.River, RiverShape.Curve, -1, "giro -1 fuera de 0–5" },
        };

        [TestCaseSource(nameof(InvalidRiverPieces))]
        public void Create_WithInvalidRiverData_ReportsTheCause(TerrainKind terrain, RiverShape river, int rotation, string cause)
        {
            var result = TerritoryLayout.Create(new[] { Grass(0, 0), new TerritoryCell(new HexCell(1, 0), terrain, river, rotation) });

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.InvalidRiverPiece));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Cell, Is.EqualTo(new HexCell(1, 0)));
            Assert.That(result.Message, Is.EqualTo($"La celda (columna 1, fila 0) tiene datos de río no válidos: {cause}."));
        }

        [Test]
        public void Create_ReportsTheFirstErrorInTheFixedOrder()
        {
            var unknown = new TerritoryCell(new HexCell(0, 0), (TerrainKind)9);
            var badRiver = new TerritoryCell(new HexCell(1, 0), TerrainKind.River);
            var duplicate = Grass(2, 0);

            // Repetida gana a variante desconocida y a datos de río, aunque vaya después en el dato.
            Assert.That(TerritoryLayout.Create(new[] { unknown, badRiver, duplicate, duplicate }).Status, Is.EqualTo(TerritoryLayoutStatus.Duplicate));
            // Variante desconocida gana a datos de río, aunque vaya después en el dato.
            Assert.That(TerritoryLayout.Create(new[] { badRiver, unknown }).Status, Is.EqualTo(TerritoryLayoutStatus.UnknownTerrain));
        }

        [Test]
        public void RequiredVisuals_ListsTheUsedTerrainsAndCanonicalRiverPieces()
        {
            var layout = TerritoryLayout.Create(new[]
            {
                Grass(0, 0),
                new TerritoryCell(new HexCell(1, 0), TerrainKind.Snow),
                new TerritoryCell(new HexCell(2, 0), TerrainKind.Snow),
                // Recto E–O con giro 3 (canónico 0) unido a una curva O–SE que sale por el borde.
                new TerritoryCell(new HexCell(0, 1), TerrainKind.River, RiverShape.Straight, 3),
                new TerritoryCell(new HexCell(1, 1), TerrainKind.River, RiverShape.Curve, 3),
            }).Layout;

            Assert.That(layout.RequiredVisuals(), Is.EquivalentTo(new[]
            {
                VisualKey.ForTerrain(TerrainKind.Grass),
                VisualKey.ForTerrain(TerrainKind.Snow),
                VisualKey.ForRiver(RiverShape.Straight, 0),
                VisualKey.ForRiver(RiverShape.Curve, 3),
            }));
        }

        [Test]
        public void VisualKey_DescribesItselfInSpanish()
        {
            Assert.That(VisualKey.ForTerrain(TerrainKind.Snow).ToString(), Is.EqualTo("variante nieve"));
            Assert.That(VisualKey.ForTerrain(TerrainKind.Water).ToString(), Is.EqualTo("variante agua"));
            Assert.That(VisualKey.ForRiver(RiverShape.Curve, 3).ToString(), Is.EqualTo("pieza de río curva, giro 3"));
            Assert.That(VisualKey.ForRiver(RiverShape.Straight, 1).ToString(), Is.EqualTo("pieza de río tramo recto, giro 1"));
            Assert.That(VisualKey.ForRiver(RiverShape.Spring, 0).ToString(), Is.EqualTo("pieza de río manantial, giro 0"));
        }

        // --- Conexa y bordes (data-model.md → TerritoryLayout, orden 5 y Derivados; spec FR-003) ---

        [Test]
        public void Create_WithALooseCell_IsDisconnectedAndNamesIt()
        {
            var cells = new[] { new HexCell(0, 0), new HexCell(1, 0), new HexCell(5, 0), new HexCell(0, 1) };

            var result = TerritoryLayout.Create(cells);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Disconnected));
            Assert.That(result.Layout, Is.Null);
            Assert.That(result.Cell, Is.EqualTo(new HexCell(5, 0)));
            Assert.That(result.Message, Is.EqualTo("La disposición no es conexa: la celda (columna 5, fila 0) no está unida al resto."));
        }

        [Test]
        public void Create_WithTwoGroups_NamesTheFirstUnreachedCellInDataOrder()
        {
            var cells = new[] { new HexCell(0, 0), new HexCell(1, 0), new HexCell(4, 3), new HexCell(5, 3), new HexCell(0, 1) };

            var result = TerritoryLayout.Create(cells);

            Assert.That(result.Status, Is.EqualTo(TerritoryLayoutStatus.Disconnected));
            Assert.That(result.Cell, Is.EqualTo(new HexCell(4, 3)));
        }

        [Test]
        public void Create_WithAnInteriorHole_IsStillValid()
        {
            var center = new HexCell(4, 4);
            var ring = center.Neighbours().ToArray();

            Assert.That(TerritoryLayout.Create(ring).Status, Is.EqualTo(TerritoryLayoutStatus.Ok));
        }

        [Test]
        public void Create_DataErrorsComeBeforeDisconnected()
        {
            var cells = new[]
            {
                new TerritoryCell(new HexCell(0, 0), TerrainKind.Grass),
                new TerritoryCell(new HexCell(5, 0), TerrainKind.River),
            };

            Assert.That(TerritoryLayout.Create(cells).Status, Is.EqualTo(TerritoryLayoutStatus.InvalidRiverPiece));
        }

        [Test]
        public void BorderCells_AreTheCellsWithANeighbourOutside()
        {
            var center = new HexCell(4, 4);
            var layout = TerritoryLayout.Create(new[] { center }.Concat(center.Neighbours())).Layout;

            Assert.That(layout.BorderCells(), Is.EquivalentTo(center.Neighbours()));
            Assert.That(layout.BorderCells(), Has.No.Member(center));
        }

        [Test]
        public void FrontBorderCells_AreTheCellsWithTheirSouthWestOrSouthEastNeighbourOutside()
        {
            var center = new HexCell(4, 4);
            var layout = TerritoryLayout.Create(new[] { center }.Concat(center.Neighbours())).Layout;

            // De la flor de 7 celdas, quedan en el frente las de las aristas E, O, SO y SE del centro.
            Assert.That(layout.FrontBorderCells(), Is.EquivalentTo(new[]
            {
                center.Neighbour(HexEdge.E), center.Neighbour(HexEdge.W),
                center.Neighbour(HexEdge.SW), center.Neighbour(HexEdge.SE),
            }));
        }
    }
}
