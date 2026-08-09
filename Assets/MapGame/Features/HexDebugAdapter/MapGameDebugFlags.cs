using hp55games.Tools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.HexDebugAdapter
{
    /// <summary>
    /// Mapping da TileType a HexDebugFlags per il progetto MapGame.
    /// I bit Custom0-Custom7 sono riservati al progetto consumer come da spec HexDebugFlags.
    ///
    /// Con lo split di TileType (13 tipi content oltre a Void/Road) gli 8 bit Custom
    /// disponibili non bastano per un mapping 1:1, quindi i tipi sono raggruppati per
    /// categoria, seguendo esattamente lo split gia' documentato dagli attributi
    /// [Obsolete] su TileType:
    /// - Boss/Miniboss non hanno piu' un bit proprio: sono Enemy (Miniboss e' Enemy con
    ///   DifficultyLevel piu' alto; Boss e' Enemy con IsObjective=true, gia' coperto da
    ///   HexDebugFlags.PointOfInterest indipendentemente dal TileType).
    /// - Resource (ex Goods) copre Bush, BeeHive, TurnipSprout, MoneyBag.
    /// - NpcService (ex Npc) copre Inn, Witch, Farmer, Hunter (Trader ha gia' il suo bit).
    /// - Chance copre Trap, Fountain, Tree, Chest.
    /// - Key ha un bit dedicato: attiva la chiave di sessione, e' progression-critical.
    /// Custom7 resta libero per usi futuri.
    /// </summary>
    public static class MapGameDebugFlags
    {
        public const HexDebugFlags Void       = HexDebugFlags.Custom0;
        public const HexDebugFlags Road       = HexDebugFlags.Road;
        public const HexDebugFlags Enemy      = HexDebugFlags.Custom1;
        public const HexDebugFlags Trader     = HexDebugFlags.Custom2;
        public const HexDebugFlags Resource   = HexDebugFlags.Custom3;   // Bush, BeeHive, TurnipSprout, MoneyBag
        public const HexDebugFlags NpcService = HexDebugFlags.Custom4;   // Inn, Witch, Farmer, Hunter
        public const HexDebugFlags Chance     = HexDebugFlags.Custom5;   // Trap, Fountain, Tree, Chest
        public const HexDebugFlags Key        = HexDebugFlags.Custom6;
        public const HexDebugFlags StartTile  = HexDebugFlags.City;      // City = marcatore Start

        public static HexDebugFlags FromTileType(TileType type)
        {
            switch (type)
            {
                case TileType.Void:         return Void;
                case TileType.Road:         return Road;
                case TileType.Enemy:        return Enemy;
                case TileType.Trader:       return Trader;
                case TileType.Bush:
                case TileType.BeeHive:
                case TileType.TurnipSprout:
                case TileType.MoneyBag:     return Resource;
                case TileType.Inn:
                case TileType.Witch:
                case TileType.Farmer:
                case TileType.Hunter:       return NpcService;
                case TileType.Trap:
                case TileType.Fountain:
                case TileType.Tree:
                case TileType.Chest:        return Chance;
                case TileType.Key:          return Key;
                default:                    return HexDebugFlags.None;
            }
        }
    }
}
