using System.Collections.Generic;
using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Coloca una pieza por celda de la disposición, en las posiciones que da el núcleo
    /// (research.md R-7). Funciona también fuera de Play: allí las piezas son DontSave, se ven
    /// pero no se guardan en la escena ni la marcan como modificada.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TerritoryView : MonoBehaviour
    {
        [SerializeField] private TerritoryLayoutAsset layout;
        [SerializeField] private GameObject tilePrefab;

        private readonly List<Transform> _tiles = new List<Transform>();
        private TerritoryLayout _built;
        private bool _rebuildPending;

        /// <summary>Las piezas creadas, en el orden de la disposición.</summary>
        internal IReadOnlyList<Transform> Tiles => _tiles;

        /// <summary>La disposición construida; null si no hay una válida.</summary>
        internal TerritoryLayout Layout => _built;

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

        internal void Rebuild()
        {
            Clear();
            if (tilePrefab == null)
            {
                Debug.LogError("[Territorio] Falta el prefab de pieza en TerritoryView.", this);
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

            _built = result.Layout;
            var metrics = HexMetrics.Default;
            foreach (var cell in _built.Cells)
            {
                var center = metrics.Center(cell);
                var tile = Instantiate(tilePrefab, transform, false);
                tile.name = $"Tile ({cell.Column}, {cell.Row})";
                tile.transform.localPosition = new Vector3(center.X, center.Y, center.Z);
                if (!Application.isPlaying) SetHideFlagsRecursively(tile, HideFlags.DontSave);
                _tiles.Add(tile.transform);
            }
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
