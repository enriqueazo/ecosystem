using System.Collections.Generic;
using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Coloca una pieza por celda de la disposición, en las posiciones que da el núcleo, con los
    /// recursos de su variante según el catálogo (research.md R-5 y R-7 de las specs 005 y 004). Funciona también fuera de Play: allí las piezas son DontSave, se ven
    /// pero no se guardan en la escena ni la marcan como modificada.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TerritoryView : MonoBehaviour
    {
        [SerializeField] private TerritoryLayoutAsset layout;
        [SerializeField] private TerrainCatalogAsset catalog;

        private readonly List<Transform> _tiles = new List<Transform>();
        private TerritoryLayout _built;
        private bool _rebuildPending;

        /// <summary>Las piezas creadas, en el orden de la disposición.</summary>
        internal IReadOnlyList<Transform> Tiles => _tiles;

        /// <summary>La disposición construida; null si no hay una válida.</summary>
        internal TerritoryLayout Layout => _built;

        /// <summary>El catálogo de recursos asignado.</summary>
        internal TerrainCatalogAsset Catalog => catalog;

        private void OnEnable() => Rebuild();

        private void OnDisable() => Clear();

        private void OnValidate()
        {
            // Unity no permite instanciar dentro de OnValidate: se reconstruye en el siguiente Update.
            _rebuildPending = true;
        }

        private void Update()
        {
            if (!_rebuildPending) return;
            _rebuildPending = false;
            if (isActiveAndEnabled) Rebuild();
        }

        internal void SetLayout(TerritoryLayoutAsset newLayout)
        {
            layout = newLayout;
            if (isActiveAndEnabled) Rebuild();
        }

        internal void SetCatalog(TerrainCatalogAsset newCatalog)
        {
            catalog = newCatalog;
            if (isActiveAndEnabled) Rebuild();
        }

        internal void Rebuild()
        {
            Clear();
            if (catalog == null)
            {
                Debug.LogError("[Territorio] Falta el catálogo de terrenos en TerritoryView.", this);
                return;
            }

            if (layout == null)
            {
                Debug.LogError("[Territorio] Falta la disposición del territorio en TerritoryView.", this);
                return;
            }

            // Disposición no válida: error de configuración del proyecto, no un estado del jugador (FR-003).
            var result = layout.ToLayout();
            if (result.Status != TerritoryLayoutStatus.Ok)
            {
                Debug.LogError("[Territorio] " + result.Message, layout);
                return;
            }

            // Sin recursos para alguna variante: error de configuración y ninguna pieza (spec US5).
            var missing = catalog.Missing(result.Layout.RequiredVisuals());
            if (missing.Count > 0)
            {
                Debug.LogError("[Territorio] El catálogo no tiene recursos para " + string.Join("; ", missing) + ".", catalog);
                return;
            }

            _built = result.Layout;
            var metrics = HexMetrics.Default;
            foreach (var entry in _built.Entries)
            {
                var cell = entry.Cell;
                var center = metrics.Center(cell);
                var source = SourceFor(entry);
                var tile = Instantiate(source, transform, false);
                tile.name = $"{source.name} ({cell.Column}, {cell.Row})";
                tile.transform.localPosition = new Vector3(center.X, center.Y, center.Z);
                tile.transform.localRotation = Quaternion.identity;
                if (entry.Terrain == TerrainKind.River)
                {
                    // Las mallas de río ya vienen giradas (research.md R-2): la instancia no gira.
                    catalog.TryGetRiverMesh(entry.River, RiverPiece.Canonical(entry.River, entry.Rotation), out var mesh);
                    tile.GetComponent<MeshFilter>().sharedMesh = mesh;
                }

                if (!Application.isPlaying) SetHideFlagsRecursively(tile, HideFlags.DontSave);
                _tiles.Add(tile.transform);
            }
        }

        private GameObject SourceFor(TerritoryCell entry)
        {
            if (entry.Terrain == TerrainKind.River) return catalog.RiverPrefab;
            catalog.TryGetTerrainPrefab(entry.Terrain, out var prefab);
            return prefab;
        }

        private void Clear()
        {
            foreach (var tile in _tiles)
            {
                if (tile == null) continue;
                if (Application.isPlaying) Destroy(tile.gameObject);
                else DestroyImmediate(tile.gameObject);
            }

            _tiles.Clear();
            _built = null;
        }

        private static void SetHideFlagsRecursively(GameObject root, HideFlags flags)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = flags;
            }
        }
    }
}
