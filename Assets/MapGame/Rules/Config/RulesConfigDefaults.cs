using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Dati del livello 1, trascritti da 02_regole, 03_contenuti e data/*.csv.
    /// Unico punto del codice in cui compaiono questi numeri.
    /// </summary>
    public static class RulesConfigDefaults
    {
        public const string Knife = "knife";
        public const string WeaponL2 = "weapon_l2";
        public const string WeaponL3 = "weapon_l3";
        public const string WeaponL4 = "weapon_l4";
        public const string HunterBow = "hunter_bow";
        public const string SmithHammer = "smith_hammer";
        public const string WitchStaff = "witch_staff";
        public const string MiniBossWeapon = "miniboss_weapon";
        public const string Dragonslayer = "dragonslayer";
        public const string Ring2 = "ring_2";
        public const string HunterTools = "hunter_tools";
        public const string Lantern = "lantern";
        public const string Compass = "compass";   // D-049
        public const string Purse = "purse";       // D-049
        public const string Amulet = "amulet";     // D-049

        public static RulesConfig Level1()
        {
            var c = new RulesConfig
            {
                Width = 10,
                Height = 10,
                StartMinBorderDistance = 3,
                EndDistanceWeights = new List<DistanceWeight>
                {
                    new DistanceWeight(4, 1), new DistanceWeight(5, 2), new DistanceWeight(6, 3),
                    new DistanceWeight(7, 2), new DistanceWeight(8, 1),
                },
                StartHp = 5,
                StartMaxHp = 5,
                // D-036 (Franci, 04/11/2026): 5 monete iniziali = prezzo di 1 indizio o del Fabbro.
                StartCoins = 5,
                // D-036: partenza sicura. Niente livelli 2, 5, 6 adiacenti alla partenza; almeno un
                // Piccolo 1 o un forziere di monete entro 3 esagoni.
                StartNeighborForbiddenMaskLevels = new List<int> { 2, 5, 6 },
                StartCoinSourceKinds = new List<ContentKind> { ContentKind.Monster1, ContentKind.ChestSimpleCoins },
                StartCoinSourceMaxDistance = 3,
                // D-031 (Athena, 03/11/2026, da rivedere): Attacco base 1 invece di 0 (R-023).
                // Con 0 il Coltello (+1) non batte il Piccolo 1 (Difesa 2) e la progressione non parte:
                // bot Greedy 2% di vittorie. Con 1: 48% (Random 0%). Vedi report spec 0012.
                BaseAttack = 1,
                StartWeaponId = Knife,
                XpPerLevel = 10,
                LineCompleteXp = 1,
                LevelUpAttack = 1,
                LevelUpCoinBonus = 1,
                LevelUpMaxHp = 1,
                NpcDefense = 5,
                // D-039 (Franci, 04/11/2026): +1 indizio sul bersaglio della missione per linea completata;
                // i Piccoli 2 uccisi danno un indizio sugli Arbusti [DEFAULT: quali mostri e quale bersaglio].
                LineCompleteClues = 1,
                MonsterKillClueKinds = new List<ContentKind> { ContentKind.Monster2 },
                MonsterKillClueTarget = ClueTarget.Bush,
            };

            // Oggetti (03 §6, data/items.csv)
            void Item(string id, ItemSlot slot, int bonus, ItemEffect fx = ItemEffect.None) => c.Items[id] = new ItemDef(id, slot, bonus, fx);
            Item(Knife, ItemSlot.Weapon, 1);
            Item(WeaponL2, ItemSlot.Weapon, 2);
            Item(WeaponL3, ItemSlot.Weapon, 3);
            Item(WeaponL4, ItemSlot.Weapon, 4);
            Item(HunterBow, ItemSlot.Weapon, 5);
            Item(SmithHammer, ItemSlot.Weapon, 5);
            Item(WitchStaff, ItemSlot.Weapon, 5);
            Item(MiniBossWeapon, ItemSlot.Weapon, 5);
            Item(Dragonslayer, ItemSlot.Weapon, 6);
            Item(Ring2, ItemSlot.Ring, 2);
            Item(HunterTools, ItemSlot.Trinket, 0, ItemEffect.DisarmTraps);
            Item(Lantern, ItemSlot.Trinket, 0, ItemEffect.Lantern);
            Item(Compass, ItemSlot.Trinket, 0, ItemEffect.Compass);
            Item(Purse, ItemSlot.Trinket, 0, ItemEffect.Purse);
            Item(Amulet, ItemSlot.Ring, 1);

            // Censimento (03 §1, data/tile_census.csv) e definizioni
            void Def(ContentKind k, int qty, ContentDef d) { d.Kind = k; c.Census[k] = qty; c.Contents[k] = d; }
            Def(ContentKind.Start, 1, new ContentDef { Category = ContentCategory.None });
            // D-041 (Franci, 04/11/2026): Vuota coperta, bordo neutro (livello 0); toccarla non fa nulla ma conta per le linee.
            Def(ContentKind.Empty, 14, new ContentDef { Category = ContentCategory.None, MaskLevel = 0 });
            // D-037 (Franci, 04/11/2026): Boss di nuovo al livello 6, unico e riconoscibile dal bordo (annulla D-033).
            Def(ContentKind.Boss, 1, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 6, Defense = 8, IsObjective = true, ClueTarget = ClueTarget.Boss });
            Def(ContentKind.MiniBossWeapon, 1, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 5, Defense = 5, Xp = 3, ItemId = MiniBossWeapon, ClueTarget = ClueTarget.MiniBoss });
            Def(ContentKind.MiniBossSilverKey, 1, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 5, Defense = 5, Xp = 3, GivesKey = KeyKind.Silver, ClueTarget = ClueTarget.MiniBoss });
            Def(ContentKind.MiniBossGoldKey, 1, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 5, Defense = 5, Xp = 3, GivesKey = KeyKind.Gold, ClueTarget = ClueTarget.MiniBoss });
            Def(ContentKind.MiniBossCoins, 2, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 5, Defense = 5, Xp = 3, Coins = 5, ClueTarget = ClueTarget.MiniBoss });
            Def(ContentKind.Monster1, 15, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 1, Defense = 2, Xp = 1, Coins = 2, ClueTarget = ClueTarget.MonstersNormal });
            Def(ContentKind.Monster2, 10, new ContentDef { Category = ContentCategory.Monster, MaskLevel = 2, Defense = 3, Xp = 2, Coins = 3, ClueTarget = ClueTarget.MonstersNormal });
            Def(ContentKind.Trap1, 10, new ContentDef { Category = ContentCategory.Trap, MaskLevel = 1, TrapDamage = 1 });
            Def(ContentKind.Trap2, 5, new ContentDef { Category = ContentCategory.Trap, MaskLevel = 2, TrapDamage = 2 });
            Def(ContentKind.Bush, 20, new ContentDef { Category = ContentCategory.Heal, MaskLevel = 1, Heal = 1, ClueTarget = ClueTarget.Bush });
            Def(ContentKind.ChestSimpleBerries, 1, new ContentDef { Category = ContentCategory.Chest, MaskLevel = 1, Heal = 2 });
            Def(ContentKind.ChestSimpleWeapon, 1, new ContentDef { Category = ContentCategory.Chest, MaskLevel = 1, ItemId = WeaponL2 });
            Def(ContentKind.ChestSimpleCoins, 1, new ContentDef { Category = ContentCategory.Chest, MaskLevel = 1, Coins = 4 });
            Def(ContentKind.ChestSilver, 1, new ContentDef { Category = ContentCategory.Chest, MaskLevel = 3, ItemId = WeaponL4, RequiresKey = KeyKind.Silver });
            Def(ContentKind.ChestGold, 1, new ContentDef { Category = ContentCategory.Chest, MaskLevel = 5, ItemId = Dragonslayer, RequiresKey = KeyKind.Gold });
            Def(ContentKind.Fountain, 1, new ContentDef { Category = ContentCategory.Heal, MaskLevel = 4, FullHeal = true, ClueTarget = ClueTarget.Fountain });
            Def(ContentKind.Witch, 1, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 5, ClueTarget = ClueTarget.Witch });
            Def(ContentKind.Hunter, 1, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 5, ClueTarget = ClueTarget.Hunter });
            Def(ContentKind.Smith, 1, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 5, ClueTarget = ClueTarget.Smith });
            Def(ContentKind.Inn, 2, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 5, ClueTarget = ClueTarget.Inn });
            Def(ContentKind.Trader, 2, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 5, ClueTarget = ClueTarget.Trader });
            Def(ContentKind.Farmer, 3, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 3 });
            Def(ContentKind.Villagers, 3, new ContentDef { Category = ContentCategory.Npc, MaskLevel = 3 });

            // NPC (03 §3, data/npc_offers.csv). Prezzi indizi: 1 per 5, 2 per 9 (D-027).
            NpcOfferDef Clue(string id, ClueTarget t, int n, int price) =>
                new NpcOfferDef { Id = id, Effect = NpcOfferEffect.Clues, ClueTarget = t, Amount = n, Price = price };
            // D-043: Ricognizione (solo nelle modalità con nebbia). Prezzo da RepriceOffers.
            NpcOfferDef Scout(string id, int shape) =>
                new NpcOfferDef { Id = id, Effect = NpcOfferEffect.Scout, Amount = shape };

            c.Npcs[ContentKind.Hunter] = new NpcDef
            {
                Kind = ContentKind.Hunter, Fightable = true, KillItemId = HunterBow,
                Offers =
                {
                    new NpcOfferDef { Id = "hunter_game", Price = 3, Effect = NpcOfferEffect.Heal, Amount = 3 },
                    new NpcOfferDef { Id = "hunter_tools", Price = 10, Effect = NpcOfferEffect.GiveItem, ItemId = HunterTools },
                    Clue("hunter_clue_1", ClueTarget.MiniBoss, 1, 5),
                    Clue("hunter_clue_2", ClueTarget.MiniBoss, 2, 9),
                },
            };
            c.Npcs[ContentKind.Smith] = new NpcDef
            {
                Kind = ContentKind.Smith, Fightable = true, KillItemId = SmithHammer,
                Offers =
                {
                    new NpcOfferDef { Id = "smith_upgrade", Price = 5, Effect = NpcOfferEffect.SmithUpgrade, Amount = 1, OncePerRun = true },
                    Clue("smith_clue_1", ClueTarget.Inn, 1, 5),
                    Clue("smith_clue_2", ClueTarget.Inn, 2, 9),
                },
            };
            c.Npcs[ContentKind.Witch] = new NpcDef
            {
                Kind = ContentKind.Witch, Fightable = true, KillItemId = WitchStaff,
                Offers =
                {
                    new NpcOfferDef { Id = "witch_ring", Price = 8, Effect = NpcOfferEffect.GiveItem, ItemId = Ring2 },
                    new NpcOfferDef { Id = "witch_lantern", Price = 10, Effect = NpcOfferEffect.GiveItem, ItemId = Lantern },
                    Clue("witch_clue_1", ClueTarget.Hunter, 1, 5),
                    Clue("witch_clue_2", ClueTarget.Hunter, 2, 9),
                },
            };
            c.Npcs[ContentKind.Inn] = new NpcDef
            {
                Kind = ContentKind.Inn, Fightable = false,
                Offers =
                {
                    new NpcOfferDef { Id = "inn_rest", Price = 5, Effect = NpcOfferEffect.FullHeal },
                    Clue("inn_clue_trader_1", ClueTarget.Trader, 1, 5),
                    Clue("inn_clue_trader_2", ClueTarget.Trader, 2, 9),
                    Clue("inn_clue_monsters_1", ClueTarget.MonstersNormal, 1, 5),
                    Clue("inn_clue_monsters_2", ClueTarget.MonstersNormal, 2, 9),
                    Scout("inn_scout_0", ScoutShapes.Single), Scout("inn_scout_1", ScoutShapes.Cluster),
                    Scout("inn_scout_row", ScoutShapes.Row), Scout("inn_scout_col", ScoutShapes.Column),
                },
            };
            c.Npcs[ContentKind.Trader] = new NpcDef
            {
                Kind = ContentKind.Trader, Fightable = true, KillCoins = 10,
                Offers =
                {
                    new NpcOfferDef { Id = "trader_weapon", Price = 10, Effect = NpcOfferEffect.GiveItem, ItemId = WeaponL3 },
                    // D-038 (Franci, 04/11/2026): con il Boss visibile al livello 6 gli indizi del Mercante
                    // vanno su un bersaglio di livello 5 estratto tra quelli ancora coperti (annulla D-028).
                    new NpcOfferDef { Id = "trader_clue_1", Effect = NpcOfferEffect.Clues, RandomTargetMaskLevel = 5, Amount = 1, Price = 5 },
                    new NpcOfferDef { Id = "trader_clue_2", Effect = NpcOfferEffect.Clues, RandomTargetMaskLevel = 5, Amount = 2, Price = 9 },
                    // D-049 (Franci, 04/11/2026): nuovi oggetti del Mercante.
                    new NpcOfferDef { Id = "trader_compass", Price = 6, Effect = NpcOfferEffect.GiveItem, ItemId = Compass },
                    new NpcOfferDef { Id = "trader_purse", Price = 6, Effect = NpcOfferEffect.GiveItem, ItemId = Purse },
                    new NpcOfferDef { Id = "trader_map", Price = 4, Effect = NpcOfferEffect.TreasureMap },
                    new NpcOfferDef { Id = "trader_amulet", Price = 5, Effect = NpcOfferEffect.GiveItem, ItemId = Amulet },
                },
            };
            c.Npcs[ContentKind.Farmer] = new NpcDef
            {
                Kind = ContentKind.Farmer, Fightable = false,
                Offers =
                {
                    new NpcOfferDef { Id = "farmer_food", Price = 3, Effect = NpcOfferEffect.Heal, Amount = 1 },
                    Clue("farmer_clue_1", ClueTarget.Bush, 1, 5),
                    Clue("farmer_clue_2", ClueTarget.Bush, 2, 9),
                },
            };
            c.Npcs[ContentKind.Villagers] = new NpcDef
            {
                Kind = ContentKind.Villagers, Fightable = false,
                Offers =
                {
                    Clue("villagers_clue_fountain", ClueTarget.Fountain, 1, 5),
                    Clue("villagers_clue_smith", ClueTarget.Smith, 1, 5),
                    Clue("villagers_clue_bush", ClueTarget.Bush, 1, 5),
                    Scout("villagers_scout_0", ScoutShapes.Single), Scout("villagers_scout_1", ScoutShapes.Cluster),
                    Scout("villagers_scout_row", ScoutShapes.Row), Scout("villagers_scout_col", ScoutShapes.Column),
                },
            };

            // Missioni (R-070, data/missions.csv)
            void Mission(string id, ClueTarget t) => c.Missions.Add(new MissionDef { Id = id, Target = t, StartClues = 2 });
            Mission("hunt_witch", ClueTarget.Witch);
            Mission("hunt_hunter", ClueTarget.Hunter);
            Mission("hunt_smith", ClueTarget.Smith);
            Mission("hunt_inn", ClueTarget.Inn);
            Mission("hunt_trader", ClueTarget.Trader);
            Mission("hunt_miniboss", ClueTarget.MiniBoss);

            // D-043 (Franci, 04/11/2026; valori iterati da Athena col bot, vedi data/balance/confronti.md):
            // in nebbia si parte con due Ricognizioni da 7 tile e una da 1; ogni linea completata ne dà una da 7.
            c.FogStartScouts = new List<int> { 1, 1, 0 };
            c.FogLineCompleteScoutShape = 1;
            // D-044: indizi -20/-22% (4/7, proporzione 1,75 vs 1,8); Ricognizione 1 (una tile) e 5 (sette tile).
            c.CluePrice1 = 4; c.CluePrice2 = 7;
            c.ScoutPriceSingle = 1; c.ScoutPriceCluster = 5;
            // D-045 (Franci, 04/11/2026): Ricognizione di riga o colonna intera, 7 monete, solo da Locanda e Paesani.
            c.ScoutPriceLine = 7;
            // D-046/D-047/D-048 (Franci, 04/11/2026; valori scelti col bot): ricompensa missione, mini-boss, Trinket.
            c.MissionRewardAttack = 1;
            c.MiniBossDefense = 5; // con 4 Classica ed Esplorazione salgono al 94% / 88% (bot Greedy): troppo facile
            c.TrinketPrice = 6;
            c.RepriceOffers();
            return c;
        }
    }
}
