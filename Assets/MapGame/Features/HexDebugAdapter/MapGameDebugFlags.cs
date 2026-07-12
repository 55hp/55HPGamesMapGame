using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Mapping da TileType a HexDebugFlags per il progetto MapGame.
    /// I bit Custom0-Custom7 sono riservati al progetto consumer come da spec HexDebugFlags.
    /// Revisione 2026-07-10 per il TileType refactor: Void sostituisce Neutra (stesso bit),
    /// Enemy sostituisce Battaglia (stesso bit), Shop prende il bit liberato da Trappola
    /// (rimosso, confluito in Mistery), Miniboss e' nuovo e usa l'ultimo bit libero.
    /// </summary>
    public static class MapGameDebugFlags
    {
        public const HexDebugFlags Void      = HexDebugFlags.Custom0;
        public const HexDebugFlags Strada    = HexDebugFlags.Road;       // Road è il flag semantico corretto
        public const HexDebugFlags Enemy     = HexDebugFlags.Custom1;
        public const HexDebugFlags Shop      = HexDebugFlags.Custom2;
        public const HexDebugFlags Risorsa   = HexDebugFlags.Custom3;
        public const HexDebugFlags Npc       = HexDebugFlags.Custom4;
        public const HexDebugFlags Mistery   = HexDebugFlags.Custom5;
        public const HexDebugFlags Boss      = HexDebugFlags.Custom6;
        public const HexDebugFlags Miniboss  = HexDebugFlags.Custom7;
        public const HexDebugFlags StartTile = HexDebugFlags.City;       // City = marcatore Start

        public static HexDebugFlags FromTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Void:     return Void;
                case TileType.Path:   return Strada;
                case TileType.Enemy:    return Enemy;
                case TileType.Shop:     return Shop;
                case TileType.Goods:  return Risorsa;
                case TileType.Npc:      return Npc;
                case TileType.Chance:  return Mistery;
                case TileType.Boss:     return Boss;
                case TileType.Miniboss: return Miniboss;
                default:                return HexDebugFlags.None;
            }
        }
    }
}
