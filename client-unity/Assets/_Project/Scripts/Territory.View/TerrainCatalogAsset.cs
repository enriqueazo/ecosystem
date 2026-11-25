using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ecosystem.Territory.View
{
    /// <summary>
    /// Asocia cada variante y cada pieza de río con sus recursos visuales (data-model.md →
    /// TerrainCatalogAsset; research.md R-5). Si una clave se repite, gana la primera entrada.
    /// </summary>
    [CreateAssetMenu(menuName = "Ecosystem/Territorio/Catálogo de terrenos", fileName = "TerrainCatalog")]
    public sealed class TerrainCatalogAsset : ScriptableObject
    {
        [Serializable]
        internal struct TerrainEntry
        {
            public TerrainKind kind;
            public GameObject prefab;
        }

        [Serializable]
        internal struct RiverMeshEntry
        {
            public RiverShape shape;
            public int rotation;
            public Mesh mesh;
        }

        [SerializeField] private List<TerrainEntry> terrains = new List<TerrainEntry>();
        [SerializeField] private GameObject riverPrefab;
        [SerializeField] private List<RiverMeshEntry> riverMeshes = new List<RiverMeshEntry>();

        /// <summary>Prefab común de las piezas de río; la malla la pone la vista.</summary>
        public GameObject RiverPrefab => riverPrefab;

        public bool TryGetTerrainPrefab(TerrainKind kind, out GameObject prefab)
        {
            foreach (var entry in terrains)
            {
                if (entry.kind != kind || entry.prefab == null) continue;
                prefab = entry.prefab;
                return true;
            }

            prefab = null;
            return false;
        }

        /// <summary>Malla de una pieza de río por su forma y su giro canónico.</summary>
        public bool TryGetRiverMesh(RiverShape shape, int canonicalRotation, out Mesh mesh)
        {
            foreach (var entry in riverMeshes)
            {
                if (entry.shape != shape || entry.rotation != canonicalRotation || entry.mesh == null) continue;
                mesh = entry.mesh;
                return true;
            }

            mesh = null;
            return false;
        }

        /// <summary>Las claves pedidas que el catálogo no puede resolver, en el orden recibido.</summary>
        public IReadOnlyList<VisualKey> Missing(IEnumerable<VisualKey> required) =>
            required.Where(key => !Has(key)).ToList().AsReadOnly();

        private bool Has(VisualKey key) => key.IsRiver
            ? riverPrefab != null && TryGetRiverMesh(key.River, key.Rotation, out _)
            : TryGetTerrainPrefab(key.Terrain, out _);

        internal void SetEntries(
            IEnumerable<(TerrainKind Kind, GameObject Prefab)> newTerrains,
            GameObject newRiverPrefab,
            IEnumerable<(RiverShape Shape, int Rotation, Mesh Mesh)> newRiverMeshes)
        {
            terrains = newTerrains.Select(t => new TerrainEntry { kind = t.Kind, prefab = t.Prefab }).ToList();
            riverPrefab = newRiverPrefab;
            riverMeshes = newRiverMeshes.Select(r => new RiverMeshEntry { shape = r.Shape, rotation = r.Rotation, mesh = r.Mesh }).ToList();
        }
    }
}
