using System.Collections.Generic;

using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.Hints
{
    /// <summary>
    /// V1 — Adjacent Icon Only.
    /// Mostra l'icona di categoria SOLO sulle tile Coperta adiacenti alla posizione attuale
    /// del player. Tutte le altre tile Coperta mostrano "?". Nessuna propagazione oltre il
    /// vicinato immediato — corrisponde esattamente al comportamento base descritto nel GDD
    /// (sezione "Tile Icon Behavior").
    /// </summary>
    public sealed class V1HintRenderer : MonoBehaviour, IHintRenderer
    {
        [SerializeField] private HexGrid.HexGridViewSpawner _viewSpawner;

        private HexGrid.HexGridController _grid;

        public void Initialize(HexGrid.HexGridController grid)
        {
            _grid = grid;
        }

        public void OnTileRevealed(HexGrid.HexTileData revealedTile, IReadOnlyList<HexGrid.HexTileData> neighbors)
        {
            RefreshAll(new List<HexGrid.HexTileData>(_grid.Tiles.Values));
        }

        public void RefreshAll(IReadOnlyList<HexGrid.HexTileData> allTiles)
        {
            if (_viewSpawner == null || _grid == null) return;

            var currentNeighbors = new HashSet<HexGrid.HexCoord>();
            foreach (var tile in allTiles)
            {
                if (tile.State != HexGrid.TileState.Scoperta) continue;

                foreach (var neighbor in _grid.GetNeighbors(tile.Coord))
                    currentNeighbors.Add(neighbor.Coord);
            }

            foreach (var tile in allTiles)
            {
                if (!_viewSpawner.Views.TryGetValue(tile.Coord, out var view)) continue;

                if (tile.State != HexGrid.TileState.Coperta)
                {
                    view.HideIcon();
                    continue;
                }

                if (currentNeighbors.Contains(tile.Coord))
                    view.ShowCategoryIcon(tile.Type);
                else
                    view.ShowUnknownIcon();
            }
        }

        public void Clear()
        {
            if (_viewSpawner == null) return;

            foreach (var view in _viewSpawner.Views.Values)
                view.HideIcon();
        }
    }
}
