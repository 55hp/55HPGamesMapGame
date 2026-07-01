using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.Hints
{
    /// <summary>
    /// Strategia di rendering per una variante di hint mechanic (V1-V4).
    /// Solo scaffolding: le implementazioni concrete (V1Renderer, V2Renderer, ...)
    /// NON vanno scritte finché non richiesto esplicitamente.
    /// </summary>
    public interface IHintRenderer
    {
        /// <summary>Chiamato una volta quando questa variante diventa attiva.</summary>
        void Initialize(HexGrid.HexGridController grid);

        /// <summary>Chiamato ogni volta che una tile passa a Scoperta.</summary>
        void OnTileRevealed(HexGrid.HexTileData revealedTile, IReadOnlyList<HexGrid.HexTileData> neighbors);

        /// <summary>Ricalcola tutti gli hint visibili da zero (cambio variante, reachability, rigenerazione).</summary>
        void RefreshAll(IReadOnlyList<HexGrid.HexTileData> allTiles);

        /// <summary>Ripulisce ogni elemento visivo prodotto da questa strategia (chiamato allo swap).</summary>
        void Clear();
    }
}
