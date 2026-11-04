using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Tutti i numeri di regola (02/03). Nessun altro file contiene costanti di gioco.
    /// Si crea con <see cref="RulesConfigDefaults.Level1"/> e si può modificare (es. dall'asset in Features)
    /// prima di passarlo al motore; durante la partita va trattato come sola lettura.
    /// </summary>
    /// <summary>
    /// D-040: modalità di gioco. Open = tutta la plancia leggibile e cliccabile (R-002, R-080);
    /// Fog = bordo visibile solo sulle tile adiacenti a una scoperta, clic libero;
    /// FogAdjacent = bordo visibile e clic consentito solo sulle tile adiacenti a una scoperta (esplorazione).
    /// </summary>
    public enum GameMode { Open = 0, Fog = 1, FogAdjacent = 2 }

    public sealed class RulesConfig
    {
        /// <summary>Modalità della partita (D-040). Va impostata prima di creare il motore.</summary>
        public GameMode Mode { get; set; } = GameMode.Open;

        // Nebbia e Ricognizione (D-043)
        /// <summary>Nelle modalità con nebbia il bordo del Boss resta sempre visibile.</summary>
        public bool FogShowBoss { get; set; }
        /// <summary>D-053: in Nebbia gli indizi di riga/colonna non esistono (false). Le Ricognizioni restano.</summary>
        public bool FogClues { get; set; }
        /// <summary>D-055: prezzo dell'indizio di posizione di un NPC (Locanda, solo Nebbia).</summary>
        public int LocateNpcPrice { get; set; } = 8;
        /// <summary>In Nebbia il bordo si vede anche sulle tile adiacenti a una scoperta (comportamento D-040, ora spento di default).</summary>
        public bool FogRevealsAdjacent { get; set; }
        /// <summary>Ricognizioni di partenza nelle modalità con nebbia (forme ScoutShapes, in ordine).</summary>
        public List<int> FogStartScouts { get; set; } = new List<int>();
        /// <summary>Forma della Ricognizione data da ogni linea completata in nebbia (ScoutShapes); -1 = nessuna.</summary>
        public int FogLineCompleteScoutShape { get; set; } = -1;

        // Prezzi (D-044): applicati alle offerte da RepriceOffers.
        public int CluePrice1 { get; set; }
        public int CluePrice2 { get; set; }
        public int ScoutPriceSingle { get; set; }
        public int ScoutPriceCluster { get; set; }
        public int ScoutPriceLine { get; set; }

        // Plancia (R-001, R-008)
        public int Width { get; set; }
        public int Height { get; set; }
        public int StartMinBorderDistance { get; set; }
        public List<DistanceWeight> EndDistanceWeights { get; set; } = new List<DistanceWeight>();

        // Partenza sicura (D-036)
        /// <summary>Livelli di mascheramento vietati nelle tile adiacenti alla partenza.</summary>
        public List<int> StartNeighborForbiddenMaskLevels { get; set; } = new List<int>();
        /// <summary>Contenuti che contano come "fonte di monete raggiungibile presto".</summary>
        public List<ContentKind> StartCoinSourceKinds { get; set; } = new List<ContentKind>();
        /// <summary>Distanza massima (esagoni) dalla partenza entro cui deve esserci almeno una fonte di monete; 0 = nessun vincolo.</summary>
        public int StartCoinSourceMaxDistance { get; set; }

        // Giocatore (R-020..R-029)
        public int StartHp { get; set; }
        public int StartMaxHp { get; set; }
        public int StartCoins { get; set; }
        public int BaseAttack { get; set; }
        public string StartWeaponId { get; set; }

        // Progressione (R-041, R-042)
        public int XpPerLevel { get; set; }
        public int LineCompleteXp { get; set; }
        public int LevelUpAttack { get; set; }
        public int LevelUpCoinBonus { get; set; }
        public int LevelUpMaxHp { get; set; }

        // Indizi come ricompensa (D-039)
        /// <summary>Indizi sul bersaglio della missione per ogni riga o colonna completata.</summary>
        public int LineCompleteClues { get; set; }
        /// <summary>Mostri che, uccisi, danno un indizio su MonsterKillClueTarget.</summary>
        public List<ContentKind> MonsterKillClueKinds { get; set; } = new List<ContentKind>();
        public ClueTarget MonsterKillClueTarget { get; set; }

        // Missione (D-046)
        /// <summary>Attacco permanente ottenuto raggiungendo il bersaglio della missione (0 = nessuna ricompensa).</summary>
        public int MissionRewardAttack { get; set; }
        /// <summary>D-047: Difesa di tutti i mini-boss (applicata da ApplyMiniBossDefense).</summary>
        public int MiniBossDefense { get; set; }
        /// <summary>D-048: prezzo degli oggetti Trinket venduti dagli NPC (applicato da RepriceOffers).</summary>
        public int TrinketPrice { get; set; }

        /// <summary>D-049: oggetti del Mercante attivi (spegnerli serve a misurarli uno per volta col bot).</summary>
        public bool OfferCompass { get; set; } = true;
        public int PurseBonus { get; set; } = 1;
        /// <summary>D-052: danni in meno per colpo con lo Scudo (minimo 0).</summary>
        public int ShieldReduction { get; set; } = 1;
        /// <summary>D-052: colpi parati prima che lo Scudo si rompa (0 = non si rompe).</summary>
        public int ShieldCharges { get; set; } = 0;
        /// <summary>D-052: HP con cui l'Amuleto della Fenice lascia il giocatore.</summary>
        public int PhoenixHp { get; set; } = 1;
        /// <summary>D-052: HP in più curati da ogni Arbusto con l'Erbario.</summary>
        public int HerbariumBonus { get; set; } = 1;
        /// <summary>D-052: prezzi e presenza dei nuovi oggetti (spegnerli serve a misurarli col bot).</summary>
        public int ShieldPrice { get; set; } = 6;
        public int PhoenixPrice { get; set; } = 8;
        public int HerbariumPrice { get; set; } = 4;
        public bool OfferShield { get; set; } = true;
        public bool OfferPhoenix { get; set; } = true;
        public bool OfferHerbarium { get; set; } = true;
        /// <summary>D-050: Attacco di una seconda arma a una mano.</summary>
        public int OffHandWeaponBonus { get; set; } = 1;
        /// <summary>D-050: una seconda arma dà OffHandWeaponBonus solo se il suo bonus è almeno questo (2 = il Coltello non conta).</summary>
        public int OffHandWeaponMinBonus { get; set; } = 0;
        public bool OfferPurse { get; set; } = true;
        public bool OfferTreasureMap { get; set; } = true;
        public bool OfferAmulet { get; set; } = true;
        public int TreasureMapPrice { get; set; } = 4;
        public int AmuletPrice { get; set; } = 5;

        // NPC (R-061)
        public int NpcDefense { get; set; }

        /// <summary>Censimento: quantità per contenuto (R-009). La somma deve essere Width × Height.</summary>
        public Dictionary<ContentKind, int> Census { get; set; } = new Dictionary<ContentKind, int>();
        public Dictionary<ContentKind, ContentDef> Contents { get; set; } = new Dictionary<ContentKind, ContentDef>();
        public Dictionary<string, ItemDef> Items { get; set; } = new Dictionary<string, ItemDef>();
        public Dictionary<ContentKind, NpcDef> Npcs { get; set; } = new Dictionary<ContentKind, NpcDef>();
        public List<MissionDef> Missions { get; set; } = new List<MissionDef>();

        public ContentDef Content(ContentKind kind) => Contents[kind];

        /// <summary>
        /// D-044: riallinea i prezzi delle offerte di indizi e Ricognizione ai campi di prezzo (indizi singoli/doppi,
        /// Ricognizione per raggio). Da chiamare dopo aver cambiato i prezzi.
        /// </summary>
        public void RepriceOffers()
        {
            // D-047: la Difesa dei mini-boss segue MiniBossDefense.
            if (MiniBossDefense > 0)
                foreach (var d in Contents.Values)
                    if (d.ClueTarget == ClueTarget.MiniBoss) d.Defense = MiniBossDefense;
            foreach (var npc in Npcs.Values)
            {
                npc.Offers.RemoveAll(o => (!OfferCompass && o.ItemId == "compass") || (!OfferPurse && o.ItemId == "purse")
                    || (!OfferTreasureMap && (o.ItemId == "map_gold" || o.ItemId == "map_silver"))
                    || (!OfferShield && o.ItemId == "shield") || (!OfferPhoenix && o.ItemId == "phoenix") || (!OfferHerbarium && o.ItemId == "herbarium") || (!OfferAmulet && o.ItemId == "amulet"));
                foreach (var o in npc.Offers)
                {
                    if (o.ItemId == "map_gold" || o.ItemId == "map_silver") { o.Price = TreasureMapPrice; continue; }
                    if (o.ItemId == "shield") { o.Price = ShieldPrice; continue; }
                    if (o.ItemId == "phoenix") { o.Price = PhoenixPrice; continue; }
                    if (o.ItemId == "herbarium") { o.Price = HerbariumPrice; continue; }
                    if (o.Effect == NpcOfferEffect.LocateNpc) { o.Price = LocateNpcPrice; continue; }
                    if (o.FixedPrice) continue;
                    if (o.ItemId == "amulet") { o.Price = AmuletPrice; continue; }
                    if (o.Effect == NpcOfferEffect.Clues) o.Price = o.Amount >= 2 ? CluePrice2 : CluePrice1;
                    else if (o.Effect == NpcOfferEffect.GiveItem && TrinketPrice > 0 && Item(o.ItemId)?.Slot == ItemSlot.Trinket) o.Price = TrinketPrice;
                    else if (o.Effect == NpcOfferEffect.Scout) o.Price = ScoutShapes.IsLine(o.Amount) ? ScoutPriceLine : o.Amount == ScoutShapes.Cluster ? ScoutPriceCluster : ScoutPriceSingle;
                }
            }
        }

        public ItemDef Item(string id) => id != null && Items.TryGetValue(id, out var it) ? it : null;

        public int CensusTotal
        {
            get { int s = 0; foreach (var kv in Census) s += kv.Value; return s; }
        }

        /// <summary>Elenco degli errori di coerenza; vuoto se la config è valida.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Width <= 0 || Height <= 0) errors.Add("Dimensioni non valide");
            if (CensusTotal != Width * Height) errors.Add($"Censimento {CensusTotal} != {Width}x{Height}");
            if (!Census.TryGetValue(ContentKind.Start, out int s) || s != 1) errors.Add("Serve esattamente 1 Start");
            if (!Census.TryGetValue(ContentKind.Boss, out int b) || b != 1) errors.Add("Serve esattamente 1 Boss");
            foreach (var kv in Census)
            {
                if (kv.Value < 0) errors.Add($"Quantità negativa per {kv.Key}");
                if (kv.Value > 0 && !Contents.ContainsKey(kv.Key)) errors.Add($"Manca la definizione di {kv.Key}");
                if (kv.Value > 0 && Contents.TryGetValue(kv.Key, out var def) && def.Category == ContentCategory.Npc && !Npcs.ContainsKey(kv.Key))
                    errors.Add($"Manca l'NPC {kv.Key}");
            }
            foreach (var def in Contents.Values)
                if (def.ItemId != null && Item(def.ItemId) == null) errors.Add($"Oggetto {def.ItemId} di {def.Kind} inesistente");
            foreach (var npc in Npcs.Values)
            {
                if (npc.KillItemId != null && Item(npc.KillItemId) == null) errors.Add($"Premio {npc.KillItemId} di {npc.Kind} inesistente");
                foreach (var o in npc.Offers)
                    if (o.Effect == NpcOfferEffect.GiveItem && Item(o.ItemId) == null) errors.Add($"Offerta {o.Id}: oggetto inesistente");
            }
            if (Item(StartWeaponId) == null) errors.Add("Arma iniziale inesistente");
            if (XpPerLevel <= 0) errors.Add("XpPerLevel deve essere > 0");
            if (EndDistanceWeights.Count == 0) errors.Add("EndDistanceWeights vuoto");
            if (Missions.Count == 0) errors.Add("Nessuna missione");
            if (StartCoins < 0) errors.Add("StartCoins negativo");
            return errors;
        }
    }
}
