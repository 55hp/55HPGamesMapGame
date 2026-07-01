using System.Collections.Generic;

using UnityEngine;

using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Gameplay.Hints
{
    /// <summary>
    /// Punto di swap unico per la hint mechanic in test.
    /// Cambia _activeVariant da Inspector (o chiama SwapTo a runtime) per confrontare le varianti.
    ///
    /// Nessuna variante è implementata qui: gli slot _v1Renderer.._v4Renderer vanno assegnati
    /// con i componenti concreti (che implementano IHintRenderer) SOLO quando esplicitamente
    /// richiesto. Finché uno slot è vuoto, quella variante semplicemente non mostra hint
    /// (comportamento sicuro, nessun errore).
    /// </summary>
    public sealed class HintVariantSwitcher : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private HexGridController _grid;

        [Header("Variante attiva")]
        [SerializeField] private HintVariant _activeVariant = HintVariant.V1_AdjacentIconOnly;

        [Header("Slot renderer — assegnare componenti che implementano IHintRenderer")]
        [SerializeField] private MonoBehaviour _v1Renderer;
        [SerializeField] private MonoBehaviour _v2Renderer;
        [SerializeField] private MonoBehaviour _v3Renderer;
        [SerializeField] private MonoBehaviour _v4Renderer;

        private IHintRenderer _active;
        private readonly Dictionary<HintVariant, IHintRenderer> _renderers = new();

        private void Awake()
        {
            if (_grid == null)
            {
                Debug.LogError("[HintVariantSwitcher] HexGridController non assegnato.");
                enabled = false;
                return;
            }

            RegisterRenderer(HintVariant.V1_AdjacentIconOnly, _v1Renderer);
            RegisterRenderer(HintVariant.V2_BorderColorDirectional, _v2Renderer);
            RegisterRenderer(HintVariant.V3_BorderColorIntensity, _v3Renderer);
            RegisterRenderer(HintVariant.V4_CompositeAura, _v4Renderer);
        }

        private void RegisterRenderer(HintVariant variant, MonoBehaviour candidate)
        {
            if (candidate is IHintRenderer renderer)
                _renderers[variant] = renderer;
        }

        private void OnEnable()
        {
            _grid.GridInitialized += OnGridInitialized;
            _grid.TileRevealed += OnTileRevealed;
            _grid.ReachabilityChanged += OnReachabilityChanged;
        }

        private void OnDisable()
        {
            _grid.GridInitialized -= OnGridInitialized;
            _grid.TileRevealed -= OnTileRevealed;
            _grid.ReachabilityChanged -= OnReachabilityChanged;
        }

        private void Start()
        {
            SwapTo(_activeVariant);
        }

        /// <summary>Cambia variante attiva a runtime (utile per confronti rapidi in Play mode).</summary>
        public void SwapTo(HintVariant variant)
        {
            _active?.Clear();

            if (!_renderers.TryGetValue(variant, out _active) || _active == null)
            {
                Debug.LogWarning($"[HintVariantSwitcher] Nessun renderer assegnato per {variant}. " +
                                  "Nessun hint verrà mostrato finché non viene implementato e collegato.");
                _active = null;
                _activeVariant = variant;
                return;
            }

            _activeVariant = variant;
            _active.Initialize(_grid);
            _active.RefreshAll(new List<HexTileData>(_grid.Tiles.Values));
        }

        private void OnGridInitialized()
        {
            _active?.RefreshAll(new List<HexTileData>(_grid.Tiles.Values));
        }

        private void OnTileRevealed(HexTileData tile, IReadOnlyList<HexTileData> neighbors)
        {
            _active?.OnTileRevealed(tile, neighbors);
        }

        private void OnReachabilityChanged()
        {
            _active?.RefreshAll(new List<HexTileData>(_grid.Tiles.Values));
        }
    }
}
