using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Debug
{
    /// <summary>
    /// Mapping da TileType a HexDebugFlags per il progetto MapGame.
    /// I bit Custom0-Custom7 sono riservati al progetto consumer come da spec HexDebugFlags.
    /// </summary>
    public static class MapGameDebugFlags
    {
        public const HexDebugFlags Neutra    = HexDebugFlags.Custom0;
        public const HexDebugFlags Strada    = HexDebugFlags.Road;       // Road è il flag semantico corretto
        public const HexDebugFlags Battaglia = HexDebugFlags.Custom1;
        public const HexDebugFlags Trappola  = HexDebugFlags.Custom2;
        public const HexDebugFlags Risorsa   = HexDebugFlags.Custom3;
        public const HexDebugFlags Npc       = HexDebugFlags.Custom4;
        public const HexDebugFlags Mistery   = HexDebugFlags.Custom5;
        public const HexDebugFlags Boss      = HexDebugFlags.Custom6;
        public const HexDebugFlags StartTile = HexDebugFlags.City;       // City = marcatore Start

        public static HexDebugFlags FromTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Neutra:    return Neutra;
                case TileType.Strada:    return Strada;
                case TileType.Battaglia: return Battaglia;
                case TileType.Trappola:  return Trappola;
                case TileType.Risorsa:   return Risorsa;
                case TileType.NPC:       return Npc;
                case TileType.Mistery:   return Mistery;
                case TileType.Boss:      return Boss;
                default:                 return HexDebugFlags.None;
            }
        }
    }
}
