using System;
using System.Collections.Generic;

using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Controller di griglia esagonale isolato per il test della hint mechanic
    /// (scena 6x6 dedicata). Gestisce stato tile, food, reachability e reveal.
    /// Nessuna logica di rendering: notifica gli ascoltatori (IHintRenderer,
    /// tramite HintVariantSwitcher) con eventi C# puri.
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        [Header("Griglia")]
        [SerializeField] private int _width = 6;
        [SerializeField] private int _height = 6;

        [Header("Risorse")]
        [SerializeField] private int _startingFood = 12;

        [Header("Generazione mappa")]
        [SerializeField] private int _randomSeed = 12345;

        private IMapGenerator _mapGenerator;
        private readonly Dictionary<HexCoord, HexTileData> _tiles = new();
        private HexCoord _playerCoord;
        private HexCoord _objectiveCoord;
        private int _currentFood;
        private int _lastPathsFound;

        public int CurrentFood => _currentFood;
        public HexCoord PlayerCoord => _playerCoord;
        public HexCoord ObjectiveCoord => _objectiveCoord;
        public int LastPathsFound => _lastPathsFound;
        public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

        /// <summary>La griglia è stata generata ed è pronta (setup iniziale o rigenerazione).</summary>
        public event Action GridInitialized;

        /// <summary>Una tile è passata a Scoperta. Include i vicini per aggiornarne gli hint.</summary>
        public event Action<HexTileData, IReadOnlyList<HexTileData>> TileRevealed;

        /// <summary>La reachability (Coperta vs CopertaBloccata) è stata ricalcolata.</summary>
        public event Action ReachabilityChanged;

        private void Awake()
        {
            _mapGenerator = new NaiveWeightedMapGenerator(NaiveWeightedMapGenerator.Config.Default);
            BuildGrid();
        }

        /// <summary>Rigenera la griglia da zero (nuovo seed = nuova mappa). Utile per confrontare
        /// le varianti hint su mappe/difficoltà diverse.</summary>
        public void BuildGrid()
        {
            var result = _mapGenerator.Generate(_width, _height, _startingFood, _randomSeed);

            _tiles.Clear();
            foreach (var kvp in result.Tiles)
                _tiles[kvp.Key] = kvp.Value;

            _playerCoord = result.StartCoord;
            _objectiveCoord = result.ObjectiveCoord;
            _lastPathsFound = result.PathsFound;
            _currentFood = _startingFood;

            RecomputeReachability();
            GridInitialized?.Invoke();
        }

        private void RecomputeReachability()
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile.State == TileState.Scoperta) continue;

                int cost = tile.Coord.DistanceTo(_playerCoord);
                tile.State = cost <= _currentFood ? TileState.Coperta : TileState.CopertaBloccata;
            }

            ReachabilityChanged?.Invoke();
        }

        public IReadOnlyList<HexTileData> GetNeighbors(HexCoord coord)
        {
            var result = new List<HexTileData>(6);
            for (int dir = 0; dir < 6; dir++)
            {
                if (_tiles.TryGetValue(coord.GetNeighbor(dir), out var neighbor))
                    result.Add(neighbor);
            }
            return result;
        }

        /// <summary>
        /// Tenta di rivelare (raggiungere) una tile. Ritorna false se non valida,
        /// non Coperta, o non raggiungibile col food attuale.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            if (!_tiles.TryGetValue(target, out var tile)) return false;
            if (tile.State != TileState.Coperta) return false;

            int cost = target.DistanceTo(_playerCoord);
            if (cost > _currentFood) return false;

            _currentFood -= cost;
            _currentFood += tile.FoodReward;
            _playerCoord = target;
            tile.State = TileState.Scoperta;

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            return true;
        }
    }
}
