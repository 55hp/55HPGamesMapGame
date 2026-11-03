namespace hp55games.MapGame.Rules
{
    /// <summary>Contenuto di una tile (03_contenuti §1). Valori espliciti: non rinumerare.</summary>
    public enum ContentKind
    {
        Start = 0,
        Empty = 1,
        Boss = 2,
        MiniBossWeapon = 3,
        MiniBossSilverKey = 4,
        MiniBossGoldKey = 5,
        MiniBossCoins = 6,
        Monster1 = 7,
        Monster2 = 8,
        Trap1 = 9,
        Trap2 = 10,
        Bush = 11,
        ChestSimpleBerries = 12,
        ChestSimpleWeapon = 13,
        ChestSimpleCoins = 14,
        ChestSilver = 15,
        ChestGold = 16,
        Fountain = 17,
        Witch = 18,
        Hunter = 19,
        Smith = 20,
        Inn = 21,
        Trader = 22,
        Farmer = 23,
        Villagers = 24,
    }

    /// <summary>Bersagli degli indizi (05_indizi §1).</summary>
    public enum ClueTarget
    {
        Boss = 0,
        MiniBoss = 1,
        MonstersNormal = 2,
        Witch = 3,
        Hunter = 4,
        Smith = 5,
        Inn = 6,
        Trader = 7,
        Bush = 8,
        Fountain = 9,
    }

    /// <summary>Famiglia di comportamento al click.</summary>
    public enum ContentCategory
    {
        None,       // Start, Empty
        Monster,    // Monster1/2, mini-boss, Boss
        Trap,
        Heal,       // Bush, Fountain
        Chest,
        Npc,
    }

    public enum KeyKind { None = 0, Silver = 1, Gold = 2 }
}
