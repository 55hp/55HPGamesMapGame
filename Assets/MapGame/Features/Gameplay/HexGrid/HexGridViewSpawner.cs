using System.Collections.Generic;

using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Istanzia e posiziona una HexTileView per ogni tile della griglia, usando il
    /// componente Grid nativo di Unity (Cell Layout: Hexagon, Point Top) per il
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
                coord.ToOffsetOddR(out int col, out int row);
                view.transform.localPosition = _unityGrid.GetCellCenterLocal(new Vector3Int(col, row, 0));                view.Setup(coord);
                view.ApplyState(tile.State);
                if (tile.State == TileState.Scoperta)
                    view.Reveal(tile.Type);

                _views[coord] = view;
            }
        }

        private void OnReachabilityChanged()
        {
            foreach (var kvp in _grid.Tiles)
            {
                if (_views.TryGetValue(kvp.Key, out var view))
                {
                    view.ApplyState(kvp.Value.State);
                    if (kvp.Value.State == TileState.Scoperta)
                        view.Reveal(kvp.Value.Type);
                }
            }
        }
    }
}