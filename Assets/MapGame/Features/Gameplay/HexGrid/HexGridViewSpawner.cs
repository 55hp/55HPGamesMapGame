using System.Collections.Generic;

using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Istanzia e posiziona una HexTileView per ogni tile della griglia, usando il
    /// componente Grid nativo di Unity (Cell Layout: Hexagon, Flat) per il
    /// posizionamento world. Aggiorna anche lo sfondo in base allo stato.
    /// Layer puramente visivo: non decide contenuti.
    /// L'ambiente stesso (vedi HexTileView.Reveal) è il sistema di hint.
    /// </summary>
    public sealed class HexGridViewSpawner : MonoBehaviour
    {
        [SerializeField] private HexGridController _grid;
        [SerializeField] private HexTileView _tileViewPrefab;
        [SerializeField] private Grid _unityGrid;

        private readonly Dictionary<HexCoord, HexTileView> _views = new();

        public IReadOnlyDictionary<HexCoord, HexTileView> Views => _views;

        /// <summary>
        /// Returns the world-space center of the cell at <paramref name="coord"/>,
        /// derived from the same placement logic used in OnGridInitialized.
        /// </summary>
        public Vector3 GetWorldPosition(HexCoord coord)
        {
            coord.ToOffsetOddQ(out int col, out int row);
            Vector3 localPos = _unityGrid.GetCellCenterLocal(new Vector3Int(row, col, 0));
            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// Dimensione (world unit) di una cella secondo il Grid nativo di Unity
        /// sottostante (Cell Layout: Hexagon, Flat). Per un layout flat-top x e' la
        /// larghezza orizzontale massima di una tile (i due lati piatti), y
        /// l'incremento verticale tra righe. Usato da MapCameraController per
        /// calcolare l'offset di inquadratura.
        /// </summary>
        public Vector2 CellSize => _unityGrid != null ? (Vector2)_unityGrid.cellSize : Vector2.zero;

        private void Awake()
        {
            if (_grid == null || _tileViewPrefab == null || _unityGrid == null)
            {
                Debug.LogError("[HexGridViewSpawner] _grid, _tileViewPrefab o _unityGrid non assegnati.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _grid.GridInitialized += OnGridInitialized;
            _grid.ReachabilityChanged += OnReachabilityChanged;
        }

        private void OnDisable()
        {
            _grid.GridInitialized -= OnGridInitialized;
            _grid.ReachabilityChanged -= OnReachabilityChanged;
        }

        private void OnGridInitialized()
        {
            foreach (var view in _views.Values)
                if (view != null) Destroy(view.gameObject);
            _views.Clear();

            foreach (var kvp in _grid.Tiles)
            {
                var coord = kvp.Key;
                var tile = kvp.Value;

                var view = Instantiate(_tileViewPrefab, transform);
                coord.ToOffsetOddQ(out int col, out int row);
                view.transform.localPosition = _unityGrid.GetCellCenterLocal(new Vector3Int(row, col, 0));                view.Setup(coord);
                view.ApplyState(tile.Spotting);
                bool tileExplored = tile.Exploration == ExplorationState.Explored && tile.Spotting == SpottingState.Spotted;
                view.SetClickable(_grid.IsClickable(coord) || tileExplored);
                view.SetDifficultyLevel(tile.DifficultyLevel);
                if (tile.Spotting == SpottingState.Spotted)
                    view.Reveal(tile.Type, showIcon: ShouldShowIcon(tile), showAlpha: tileExplored);

                _views[coord] = view;
            }
        }

        private void OnReachabilityChanged()
        {
            foreach (var kvp in _grid.Tiles)
            {
                if (_views.TryGetValue(kvp.Key, out var view))
                {
                    var tile = kvp.Value;
                    bool tileExplored = tile.Exploration == ExplorationState.Explored && tile.Spotting == SpottingState.Spotted;
                    view.ApplyState(tile.Spotting);
                    view.SetClickable(_grid.IsClickable(kvp.Key) || tileExplored);
                    if (tile.Spotting == SpottingState.Spotted)
                        view.Reveal(tile.Type, showIcon: ShouldShowIcon(tile), showAlpha: tileExplored);
                }
            }
        }

        /// <summary>
        /// Icona visibile solo per le tile Scoperta (Explored + Spotted): il contenuto è
        /// già risolto. Tessere Conosciuta (Unexplored + Spotted / cliccabili) non mostrano
        /// l'icona — il bordo DifficultyLevel è l'unico hint visivo prima del reveal.
        /// </summary>
        private bool ShouldShowIcon(HexTileData tile)
        {
            return tile.Exploration == ExplorationState.Explored && tile.Spotting == SpottingState.Spotted;
        }
    }
}
