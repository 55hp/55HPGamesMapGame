using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Dati puri di una tile. Nessuna logica di rendering qui.
    /// </summary>
    [Serializable]
    public sealed class HexTileData
    {
        public HexCoord Coord;
        public TileState State;
        public TileType Type;

        /// <summary>
        /// Delta HP applicato al reveal: positivo cura (Risorsa), negativo danneggia (Battaglia, Trappola), zero per gli altri tipi.
        /// </summary>
        public int HpRestore;

        /// <summary>
        /// Monete guadagnate al reveal. Assegnate solo a Battaglia (combattimento vinto).
        /// Zero per tutti gli altri tipi. Il valore balance è responsabilità del designer.
        /// </summary>
        public int MoneteGained;

        /// <summary>
        /// True sulla tile Boss, l'obiettivo di missione generato da RandomTileTypeGenerator.
        /// </summary>
        public bool IsObjective;

        public HexTileData(HexCoord coord)
        {
            Coord = coord;
            State = TileState.Sconosciuta;
            Type = TileType.Strada;
            HpRestore = 0;
            IsObjective = false;
        }
    }
}
