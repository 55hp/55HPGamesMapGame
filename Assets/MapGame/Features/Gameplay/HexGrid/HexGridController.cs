using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using hp55games.Mobile.Core.Juice;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Controller di griglia esagonale. Gestisce stato tile, reachability e reveal.
    /// Nessuna logica di rendering: notifica gli ascoltatori con eventi C# puri.
    /// HP correnti vivono in IGameContextService.Lives; _maxHp è locale a MapGame
    /// perché Core non ha concetto di "massimo".
    /// </summary>
    public sealed class HexGridController : MonoBehaviour
    {
        [Header("Griglia")]
        [SerializeField] private int _width = 6;
        [SerializeField] private int _height = 6;

        [Header("Generazione mappa")]
        [SerializeField] private int _randomSeed = 12345;

        [Header("Sopravvivenza")]
        [SerializeField] private int _maxHp = 20;

        private IMapGenerator        _mapGenerator;
        private IGameContextService  _context;
        private IEventBus            _bus;
        private IFeedbackService     _feedbackService;

        private readonly Dictionary<HexCoord, HexTileData> _tiles = new();
        private HexCoord _playerCoord;
        private HexCoord _objectiveCoord;

        public HexCoord PlayerCoord    => _playerCoord;
        public HexCoord ObjectiveCoord => _objectiveCoord;
        public IReadOnlyDictionary<HexCoord, HexTileData> Tiles => _tiles;

        /// <summary>La griglia è stata generata ed è pronta (setup iniziale o rigenerazione).</summary>
        public event Action GridInitialized;

        /// <summary>Una tile è passata a Scoperta. Include i vicini per aggiornarne gli hint.</summary>
        public event Action<HexTileData, IReadOnlyList<HexTileData>> TileRevealed;

        /// <summary>La reachability (Coperta vs CopertaBloccata) è stata ricalcolata.</summary>
        public event Action ReachabilityChanged;

        private void Awake()
        {
            _context = ServiceRegistry.Resolve<IGameContextService>();
            _bus     = ServiceRegistry.Resolve<IEventBus>();

            _mapGenerator = new RandomTileTypeGenerator();
            BuildGrid();
        }

        private void Start()
        {
            // IFeedbackService resolved in Start() to avoid ordering issues:
            // FeedbackService.Awake() may not have run yet if both live in the same scene.
            ServiceRegistry.TryResolve<IFeedbackService>(out _feedbackService);
        }

        /// <summary>Rigenera la griglia da zero (nuovo seed = nuova mappa).</summary>
        public void BuildGrid()
        {
            var result = _mapGenerator.Generate(_width, _height, _randomSeed);

            _tiles.Clear();
            foreach (var kvp in result.Tiles)
                _tiles[kvp.Key] = kvp.Value;

            _playerCoord   = result.StartCoord;
            _objectiveCoord = result.ObjectiveCoord;

            _context.Lives = _maxHp;
            _bus?.Publish(new HpChangedEvent());

            RecomputeReachability();
            GridInitialized?.Invoke();
        }

        private void RecomputeReachability()
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile.State == TileState.Scoperta) continue;

                bool adjacentToRevealed = false;
                foreach (var neighbor in GetNeighbors(tile.Coord))
                {
                    if (neighbor.State == TileState.Scoperta)
                    {
                        adjacentToRevealed = true;
                        break;
                    }
                }

                tile.State = adjacentToRevealed ? TileState.Coperta : TileState.CopertaBloccata;
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
        /// Tenta di rivelare una tile. Ritorna false se non in griglia o non Coperta.
        /// Se la tile è Strada, esegue un flood-fill BFS su tutte le Strada Coperta connesse.
        /// HP aggiornato per ogni tile rivelata; HpChangedEvent pubblicato una sola volta
        /// al termine dell'intera azione (tap + cascade).
        /// GameOver feedback emesso una sola volta se HP raggiunge 0.
        /// </summary>
        public bool TryRevealTile(HexCoord target)
        {
            if (!_tiles.TryGetValue(target, out var tile)) return false;
            if (tile.State != TileState.Coperta) return false;

            tile.State    = TileState.Scoperta;
            _playerCoord  = target;
            AccumulateHp(tile);

            if (tile.Type == TileType.Strada)
                CascadeStrada(target);

            RecomputeReachability();

            var neighbors = GetNeighbors(target);
            TileRevealed?.Invoke(tile, neighbors);

            // Publish once after all HP changes from this player action.
            _bus?.Publish(new HpChangedEvent());
            _feedbackService?.Play("hp_changed");

            if (_context.Lives <= 0)
                _feedbackService?.Play("game_over");

            return true;
        }

        /// <summary>
        /// Applica l'HpRestore di una tile appena rivelata a _context.Lives (clamped).
        /// Non pubblica eventi — il publish avviene una sola volta per azione in TryRevealTile.
        /// </summary>
        private void AccumulateHp(HexTileData tile)
        {
            _context.Lives = Mathf.Clamp(_context.Lives + tile.HpRestore, 0, _maxHp);
        }

        /// <summary>
        /// BFS flood-fill: rivela automaticamente tutte le tile Strada Coperta
        /// raggiungibili dalla coordinata di partenza, senza emettere eventi di reveal.
        /// Accumula HP per ogni tile rivelata; il publish è responsabilità di TryRevealTile.
        /// </summary>
        private void CascadeStrada(HexCoord origin)
        {
            var queue = new Queue<HexCoord>();
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var coord = queue.Dequeue();
                foreach (var neighbor in GetNeighbors(coord))
                {
                    if (neighbor.Type == TileType.Strada && neighbor.State == TileState.Coperta)
                    {
                        neighbor.State = TileState.Scoperta;
                        AccumulateHp(neighbor);
                        queue.Enqueue(neighbor.Coord);
                    }
                }
            }
        }
    }
}
