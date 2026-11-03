using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.MapGame.Rules;

namespace hp55games.MapGame.Features.Gameplay.Run
{
    /// <summary>Unico punto di traduzione ContentKind (Rules) → TileType (vista), D-022.</summary>
    public static class ContentKindMap
    {
        public static TileType ToTileType(ContentKind kind)
        {
            switch (kind)
            {
                case ContentKind.Start: return TileType.Start;
                case ContentKind.Empty: return TileType.Empty;
                case ContentKind.Boss:
                case ContentKind.Monster1:
                case ContentKind.Monster2: return TileType.Enemy;
                case ContentKind.MiniBossWeapon:
                case ContentKind.MiniBossSilverKey:
                case ContentKind.MiniBossGoldKey:
                case ContentKind.MiniBossCoins: return TileType.MiniBoss;
                case ContentKind.Trap1:
                case ContentKind.Trap2: return TileType.Trap;
                case ContentKind.Bush: return TileType.Bush;
                case ContentKind.ChestSimpleBerries:
                case ContentKind.ChestSimpleWeapon:
                case ContentKind.ChestSimpleCoins: return TileType.Chest;
                case ContentKind.ChestSilver: return TileType.ChestSilver;
                case ContentKind.ChestGold: return TileType.ChestGold;
                case ContentKind.Fountain: return TileType.Fountain;
                case ContentKind.Witch: return TileType.Witch;
                case ContentKind.Hunter: return TileType.Hunter;
                case ContentKind.Smith: return TileType.Smith;
                case ContentKind.Inn: return TileType.Inn;
                case ContentKind.Trader: return TileType.Trader;
                case ContentKind.Farmer: return TileType.Farmer;
                case ContentKind.Villagers: return TileType.Villagers;
                default: return TileType.Void;
            }
        }

        public static HexCoord ToHexCoord(int index, int width) => HexCoord.FromOffsetOddQ(index % width, index / width);

        public static int ToIndex(HexCoord coord, int width, int height)
        {
            coord.ToOffsetOddQ(out int col, out int row);
            if (col < 0 || col >= width || row < 0 || row >= height) return -1;
            return row * width + col;
        }
    }
}
