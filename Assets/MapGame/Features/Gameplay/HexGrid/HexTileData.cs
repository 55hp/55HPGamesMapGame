using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Dati puri di una tile. Nessuna logica di rendering qui:
    /// il rendering (icone, bordi, aura) è responsabilità degli IHintRenderer.
    /// </summary>
    [Serializable]
    public sealed class HexTileData
    {
        public HexCoord Coord;
        public TileState State;
        public TileType Type;

        /// <summary>
        /// Magnitudo 1-3, usata da V3 (intensità bordo) e V4 (densità aura).
        /// 0 = non assegnata / non applicabile (es. Type == None).
        /// </summary>
        public int Magnitude;

        /// <summary>
        /// Food restituito al reveal (solo tile Risorsa). 0 per tutti gli altri tipi.
        /// Necessario per il conteggio percorsi: senza ricarica, il salto diretto è
        /// sempre l'unico percorso ottimale (disuguaglianza triangolare).
        /// </summary>
        public int FoodReward;

        /// <summary>
        /// Flag di TEST usato solo dal path counter come bersaglio ("raggiungere questa tile
        /// entro il food budget"). Non è il sistema missioni reale, che non esiste ancora nel GDD.
        /// </summary>
        public bool IsObjective;

        public HexTileData(HexCoord coord)
        {
            Coord = coord;
            State = TileState.CopertaBloccata;
            Type = TileType.None;
            Magnitude = 0;
            FoodReward = 0;
            IsObjective = false;
        }
    }
}
