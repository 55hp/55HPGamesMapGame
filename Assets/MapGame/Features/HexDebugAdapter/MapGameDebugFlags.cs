using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Mapping da TileType a HexDebugFlags per il progetto MapGame.
    /// I bit Custom0-Custom7 sono riservati al progetto consumer come da spec HexDebugFlags.
    ///
    /// Copertura parziale: le costanti/il case di FromTileType coprono solo i TileType
    /// legacy (Void, Path/Road, Enemy, Shop/Trader, Goods, Npc, Chance, Boss, Miniboss) —
    /// gli 8 bit Custom0-Custom7 disponibili sono gia' tutti assegnati a questi. I
    /// TileType introdotti dopo lo split (Trap, Fountain, Tree, Key, Chest, Bush,
    /// BeeHive, TurnipSprout, MoneyBag, Inn, Witch, Farmer, Hunter) non hanno una
    /// costante/flag propria e FromTileType ritorna HexDebugFlags.None per loro: gli
    /// analyzer che dipendono da questo mapping (es. EventClusterSpacingAnalyzer,
    /// ReachabilityAnalyzer) non li vedono come "evento"/"tile chiave".
    /// </summary>
    public static class MapGameDebugFlags
    {
        public const HexDebugFlags Void      = HexDebugFlags.Custom0;
        public const HexDebugFlags Path      = HexDebugFlags.Road;       // Road è il flag semantico corretto
        public const HexDebugFlags Enemy     = HexDebugFlags.Custom1;
        public const HexDebugFlags Shop      = HexDebugFlags.Custom2;
        public const HexDebugFlags Goods     = HexDebugFlags.Custom3;
        public const HexDebugFlags Npc       = HexDebugFlags.Custom4;
        public const HexDebugFlags Chance    = HexDebugFlags.Custom5;
        public const HexDebugFlags Boss      = HexDebugFlags.Custom6;
        public const HexDebugFlags Miniboss  = HexDebugFlags.Custom7;
        public const HexDebugFlags StartTile = HexDebugFlags.City;       // City = marcatore Start

        public static HexDebugFlags FromTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Void:     return Void;
                case TileType.Path:     return Path;
                case TileType.Enemy:    return Enemy;
                case TileType.Shop:     return Shop;
                case TileType.Goods:    return Goods;
                case TileType.Npc:      return Npc;
                case TileType.Chance:   return Chance;
                case TileType.Boss:     return Boss;
                case TileType.Miniboss: return Miniboss;
                default:                return HexDebugFlags.None;
            }
        }
    }
}
