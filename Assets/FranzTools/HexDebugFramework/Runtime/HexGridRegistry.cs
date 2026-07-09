using System.Collections.Generic;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework
{
    public sealed class HexGridRegistry
    {
        private readonly Dictionary<Vector2Int, IHexCell> _cells =
            new Dictionary<Vector2Int, IHexCell>();

        /// <summary>
        /// Scansiona la scena attiva e raccoglie tutti i MonoBehaviour che implementano IHexCell.
        /// Usa questo percorso quando le celle sono già istanziate come GameObject in scena.
        /// </summary>
        public void PopulateFromScene()
        {
            _cells.Clear();
            var candidates = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in candidates)
            {
                if (mb is IHexCell cell)
                    Add(cell);
            }
        }

        /// <summary>
        /// Popola il registry direttamente da una sequenza di IHexCell.
        /// Conserva l'istanza originale così com'è — nessun wrap in HexDebugData.
        /// </summary>
        public void Populate(IEnumerable<IHexCell> cells)
        {
            _cells.Clear();
            foreach (var cell in cells)
                Add(cell);
        }

        public bool TryGetTile(Vector2Int position, out IHexCell data) =>
            _cells.TryGetValue(position, out data);

        public IReadOnlyCollection<IHexCell> AllTiles => _cells.Values;

        // ---

        private void Add(IHexCell cell)
        {
            _cells[cell.Coordinates] = cell;
        }
    }
}
