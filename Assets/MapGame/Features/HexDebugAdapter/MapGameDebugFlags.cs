using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Mapping da TileType a HexDebugFlags per il progetto MapGame.
    /// I bit Custom0-Custom7 sono riservati al progetto consumer come da spec HexDebugFlags.
    /// I nomi delle costanti seguono 1:1 i membri di TileType: rinominare un TileType
    /// significa rinominare la costante corrispondente qui.
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
