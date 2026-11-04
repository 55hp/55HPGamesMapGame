using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>Esito di una partita simulata (07 §3).</summary>
    public sealed class SimResult
    {
        public ulong Seed;
        public string Mission;
        public RunStatus Status;
        public LossReason Loss;
        public int Level;
        public int AttackAtEnd;
        public int CoinsSpent;
        public int Reveals;
        // Metriche di confronto (baseline 04/11/2026)
        public int HpAtEnd;
        public int DamageTaken;
        public int EncountersLost;
        public int TrapsTriggered;
        public int Kills;
        public int MiniBossKills;
        public int NpcsOpened;
        public int OffersBought;
        public int CluesReceived;
        public int CluesUsed;
        public int CluesLost;
        /// <summary>Tocchi su tile coperte di cui il bot non vedeva il bordo (nebbia).</summary>
        public int BlindClicks;
        /// <summary>Somma delle mosse legali disponibili a ogni scelta di tile (per la media).</summary>
        public long LegalChoicesSum;
        public int TileChoices;
        /// <summary>Scelte di tile in cui esisteva almeno una mossa senza rischio (livello 0, 1, 3, 4 visibile).</summary>
        public int ChoicesWithSafeMove;
    }

    /// <summary>
    /// Politica del bot. Vede solo ciò che vede il giocatore: livello di mascheramento delle tile coperte,
    /// tile svelate, stato di run, risultati degli indizi.
    /// </summary>
    public interface IPlayerPolicy
    {
        int ChooseTile(RulesEngine engine);
        string ChooseOption(RulesEngine engine, Decision decision);
    }

    /// <summary>Sceglie a caso tra le mosse legali.</summary>
    public sealed class RandomPolicy : IPlayerPolicy
    {
        private readonly IRandomSource _rng;
        public RandomPolicy(ulong seed) { _rng = new SplitMix64Random(seed); }

        public int ChooseTile(RulesEngine e)
        {
            var legal = new List<int>();
            for (int i = 0; i < e.Layout.Count; i++) if (e.IsRevealable(i)) legal.Add(i);
            return legal[_rng.Range(0, legal.Count)];
        }

        public string ChooseOption(RulesEngine e, Decision d)
        {
            var legal = d.Options.FindAll(o => o.Enabled);
            return legal[_rng.Range(0, legal.Count)].Id;
        }
    }

    /// <summary>
    /// Bot "ragionevole" (07 §3): livelli bassi prima, compra Attacco, affronta il Boss solo se lo batte.
    /// </summary>
    public sealed class GreedyPolicy : IPlayerPolicy
    {
        private readonly IRandomSource _rng;
        private readonly string _levelUp;
        /// <param name="levelUp">"default" (Attacco, HP max se HP ≤ 2), "attack", "coins": per misurare le opzioni del level up.</param>
        public GreedyPolicy(ulong seed, string levelUp = "default", bool shopFirst = false) { _rng = new SplitMix64Random(seed); _levelUp = levelUp ?? "default"; _shopFirst = shopFirst; }

        /// <summary>D-049: se vero, a un NPC compra gli oggetti che vuole prima di attaccarlo (misura dei negozi).</summary>
        private readonly bool _shopFirst;

        public int ChooseTile(RulesEngine e)
        {
            var s = e.State;
            var cfg = e.Config;
            int bestScore = int.MinValue;
            var best = new List<int>();
            for (int i = 0; i < e.Layout.Count; i++)
            {
                if (!e.IsRevealable(i)) continue;
                int score = Score(e, i, s, cfg);
                if (score > bestScore) { bestScore = score; best.Clear(); }
                if (score == bestScore) best.Add(i);
            }
            return best[_rng.Range(0, best.Count)];
        }

        private static int Score(RulesEngine e, int i, RunState s, RulesConfig cfg)
        {
            var def = e.ContentAt(i);
            int mask = def.MaskLevel;
            // La Lanterna svela il tipo: il bot lo usa come farebbe un giocatore.
            if (e.IsTypeShown(i)) return ScoreKnown(def, s, cfg);
            int atk = s.Attack, hp = s.Hp;
            // D-040/D-043: senza bordo visibile il bot non sa nulla della tile; preferisce le vicine alla partenza
            // (il Boss è sempre lontano dalla partenza, 4–8 esagoni) e quelle al confine dello scoperto.
            if (!e.IsMaskVisible(i)) return BlindScore(e, i);
            switch (mask)
            {
                case 1: return hp >= 3 || atk >= 2 ? 50 : 30;
                case 3: return 60;
                case 4: return hp < s.MaxHp ? 70 : 5;
                case 2: return atk >= 3 ? 40 : (hp >= 4 ? 10 : -50);
                // Aprire un NPC non costa nulla: il rischio del livello 5 sono i mini-boss (Difesa pubblica, D-047).
                case 5: return atk >= MiniBossDefense(cfg) ? 45 : (hp >= 4 ? 20 : -40);
                case 6: return atk >= cfg.Content(ContentKind.Boss).Defense ? 1000 : -1000;
                case 0: return 25; // Vuota (D-041): nessun rischio, avanza le linee
                default: return 0;
            }
        }

        public static int MiniBossDefense(RulesConfig cfg) => cfg.Content(ContentKind.MiniBossCoins).Defense;

        public static int BlindScore(RulesEngine e, int i)
        {
            int w = e.Config.Width;
            int d = HexGridMath.Distance(i, e.Layout.StartIndex, w);
            int score = d <= e.Config.StartMinBorderDistance ? 20 : 14 - d;
            if (e.IsAdjacentToRevealed(i)) score += 3;
            return score;
        }

        public static List<int> ScoutAreaOf(int center, int shape, int w, int h)
        {
            var area = new List<int>();
            if (shape == ScoutShapes.Row) { for (int c = 0; c < w; c++) area.Add((center / w) * w + c); return area; }
            if (shape == ScoutShapes.Column) { for (int r = 0; r < h; r++) area.Add(r * w + center % w); return area; }
            area.Add(center);
            if (shape == ScoutShapes.Cluster) area.AddRange(HexGridMath.Neighbors(center, w, h));
            return area;
        }

        /// <summary>D-043: centro della Ricognizione con più tile nascoste nell'area, vicino alla partenza a parità.</summary>
        public static string ChooseScout(RulesEngine e, Decision d, IRandomSource rng)
        {
            int w = e.Config.Width, h = e.Config.Height;
            int bestScore = int.MinValue;
            var best = new List<string>();
            foreach (var o in d.Options)
            {
                int hidden = 0;
                var area = ScoutAreaOf(o.TileIndex, d.ScoutShape, w, h);
                foreach (int idx in area) if (!e.IsRevealed(idx) && !e.IsMaskVisible(idx)) hidden++;
                int score = hidden * 10 - HexGridMath.Distance(o.TileIndex, e.Layout.StartIndex, w) * 2
                    + (e.IsAdjacentToRevealed(o.TileIndex) ? 5 : 0);
                if (score > bestScore) { bestScore = score; best.Clear(); }
                if (score == bestScore) best.Add(o.Id);
            }
            return best[rng.Range(0, best.Count)];
        }

        private static int ScoreKnown(ContentDef def, RunState s, RulesConfig cfg)
        {
            switch (def.Category)
            {
                case ContentCategory.Monster:
                    if (def.IsObjective) return s.Attack >= def.Defense ? 1000 : -1000;
                    return s.Attack >= def.Defense ? 80 : -60;
                case ContentCategory.Trap: return s.HasEffect(ItemEffect.DisarmTraps) ? 20 : -80;
                case ContentCategory.Heal: return s.Hp < s.MaxHp ? 75 : 1;
                case ContentCategory.Chest: return s.HasKey(def.RequiresKey) ? 90 : -10;
                default: return 55;
            }
        }

        private static string WantedItem(RulesEngine e, NpcDef npc, Func<string, string> pick)
        {
            foreach (var o in npc.Offers)
            {
                if (pick(o.Id) == null) continue;
                if (o.Effect == NpcOfferEffect.SmithUpgrade) return o.Id;
                if (o.Effect == NpcOfferEffect.GiveItem)
                {
                    var it = e.Config.Item(o.ItemId);
                    if (it.Slot == ItemSlot.Trinket) return o.Id; // D-050: cumulabili
                    if (BestHandScoreWith(e, it) > CurrentHandScore(e)) return o.Id;
                }
            }
            return null;
        }

        // ------------------------------------------------ D-050: mani

        /// <summary>Valore delle mani per il bot: Attacco, +1 per la Bussola in Nebbia.</summary>
        internal static int HandScore(RulesEngine e, ItemDef a, ItemDef b)
        {
            int v = RunState.HandsAttackOf(a, b, e.State.OffHandWeaponBonus, e.State.OffHandWeaponMinBonus);
            if (e.Config.Mode == GameMode.Fog && (a?.Effect == ItemEffect.Compass || b?.Effect == ItemEffect.Compass)) v += 1;
            if (a?.Effect == ItemEffect.Shield || b?.Effect == ItemEffect.Shield) v += 1; // D-052: lo Scudo vale circa +1
            return v;
        }

        internal static int CurrentHandScore(RulesEngine e) => HandScore(e, e.State.MainHand, e.State.OffHand);

        /// <summary>Mani dopo aver preso item lasciando drop ("all", un id, o null se c'è posto), come fa il motore.</summary>
        internal static void HandsAfter(RulesEngine e, ItemDef item, string drop, out ItemDef main, out ItemDef off)
        {
            main = e.State.MainHand; off = e.State.OffHand;
            if (drop == "all") { main = null; off = null; }
            else if (drop != null)
            {
                if (main != null && main.Id == drop) { main = off; off = null; }
                else if (off != null && off.Id == drop) off = null;
            }
            if (item.TwoHanded || main == null) { if (item.TwoHanded) off = null; main = item; }
            else off = item;
        }

        internal static int BestHandScoreWith(RulesEngine e, ItemDef item)
        {
            var s = e.State;
            var drops = new List<string>();
            if (s.FreeHands >= item.HandsNeeded) drops.Add(null);
            else if (item.TwoHanded) drops.Add("all");
            else if (s.MainHand != null && s.MainHand.TwoHanded) drops.Add(s.MainHand.Id);
            else { if (s.MainHand != null) drops.Add(s.MainHand.Id); if (s.OffHand != null) drops.Add(s.OffHand.Id); }
            int best = int.MinValue;
            foreach (var dr in drops) { HandsAfter(e, item, dr, out var a, out var b); best = Math.Max(best, HandScore(e, a, b)); }
            return best;
        }

        private static string ChooseItem(RulesEngine e, Decision d)
        {
            var item = e.Config.Item(d.NewItemId);
            string best = OptionIds.Discard; int bestScore = CurrentHandScore(e);
            foreach (var o in d.Options)
            {
                if (!OptionIds.IsEquip(o.Id)) continue;
                HandsAfter(e, item, o.Id == OptionIds.EquipAll ? "all" : o.ItemId, out var a, out var b);
                int sc = HandScore(e, a, b);
                if (sc > bestScore) { bestScore = sc; best = o.Id; }
            }
            return best;
        }

        public string ChooseOption(RulesEngine e, Decision d)
        {
            var s = e.State;
            var cfg = e.Config;
            switch (d.Kind)
            {
                case DecisionKind.LevelUp:
                    if (_levelUp == "coins") return OptionIds.LevelCoins;
                    if (_levelUp == "attack") return OptionIds.LevelAttack;
                    return s.Hp <= 2 ? OptionIds.LevelMaxHp : OptionIds.LevelAttack;
                case DecisionKind.Item:
                    return ChooseItem(e, d);
                case DecisionKind.ClueLine:
                    return d.Options[_rng.Range(0, d.Options.Count)].Id;
                case DecisionKind.Scout:
                    return ChooseScout(e, d, _rng);
                case DecisionKind.Npc:
                    return ChooseNpc(e, d, _shopFirst);
            }
            return d.Options.Find(o => o.Enabled).Id;
        }

        private static string ChooseNpc(RulesEngine e, Decision d, bool shopFirst)
        {
            var s = e.State;
            var cfg = e.Config;
            var npc = cfg.Npcs[d.Npc];
            string Pick(string id) { var o = d.Find(id); return o != null && o.Enabled ? id : null; }
            if (shopFirst)
            {
                var want = WantedItem(e, npc, Pick);
                if (want != null) return want;
            }

            if (npc.Fightable && s.Attack >= cfg.NpcDefense)
            {
                var kill = cfg.Item(npc.KillItemId);
                if (kill == null || BestHandScoreWith(e, kill) > CurrentHandScore(e)) return OptionIds.Attack;
            }
            if (s.Hp <= 2)
                foreach (var id in new[] { "inn_rest", "hunter_game", "farmer_food" })
                    if (Pick(id) != null) return id;
            var wanted = WantedItem(e, npc, Pick);
            if (wanted != null) return wanted;
            if (s.Hp < s.MaxHp && Pick("inn_rest") != null && s.Hp <= s.MaxHp - 3) return "inn_rest";
            // D-043: in nebbia la visione vale: Ricognizione grande se possibile, altrimenti piccola.
            foreach (var o in npc.Offers) if (o.Effect == NpcOfferEffect.Scout && o.Amount == ScoutShapes.Cluster && Pick(o.Id) != null) return o.Id;
            foreach (var o in npc.Offers) if (o.Effect == NpcOfferEffect.Scout && o.Amount == ScoutShapes.Single && Pick(o.Id) != null) return o.Id;
            return OptionIds.Close;
        }
    }

    public static class BalanceSimulator
    {
        public static SimResult Play(RulesConfig config, ulong seed, IPlayerPolicy policy)
        {
            var e = new RulesEngine(config, seed);
            e.Start();
            var r = new SimResult { Seed = seed, Mission = e.State.Mission.Id };
            int spent = 0, reveals = 0, guard = 0;
            void Track(IReadOnlyList<RuleEvent> evs)
            {
                foreach (var ev in evs)
                {
                    switch (ev.Type)
                    {
                        case RuleEventType.CoinsSpent: spent += ev.Amount; break;
                        case RuleEventType.DamageTaken: r.DamageTaken += ev.Amount; break;
                        case RuleEventType.EncounterLost: r.EncountersLost++; break;
                        case RuleEventType.TrapTriggered: r.TrapsTriggered++; break;
                        case RuleEventType.EncounterWon:
                            r.Kills++;
                            if (ev.Kind == ContentKind.MiniBossCoins || ev.Kind == ContentKind.MiniBossGoldKey
                                || ev.Kind == ContentKind.MiniBossSilverKey || ev.Kind == ContentKind.MiniBossWeapon) r.MiniBossKills++;
                            break;
                        case RuleEventType.NpcKilled: r.Kills++; break;
                        case RuleEventType.NpcOpened: r.NpcsOpened++; break;
                        case RuleEventType.OfferBought: r.OffersBought++; break;
                        case RuleEventType.ClueReceived: r.CluesReceived++; break;
                        case RuleEventType.ClueUsed: r.CluesUsed++; break;
                        case RuleEventType.ClueLost: r.CluesLost++; break;
                    }
                }
            }
            while (e.State.Status == RunStatus.Playing && guard++ < 1000)
            {
                var d = e.CurrentDecision;
                if (d != null)
                {
                    Track(e.Submit(policy.ChooseOption(e, d)));
                    continue;
                }
                int legal = 0; bool safe = false;
                for (int i = 0; i < e.Layout.Count; i++)
                {
                    if (!e.IsRevealable(i)) continue;
                    legal++;
                    if (e.IsMaskVisible(i))
                    {
                        int m = e.ContentAt(i).MaskLevel;
                        if (m == 0 || m == 1 || m == 3 || m == 4) safe = true;
                    }
                }
                r.LegalChoicesSum += legal; r.TileChoices++;
                if (safe) r.ChoicesWithSafeMove++;
                int tile = policy.ChooseTile(e);
                if (!e.IsMaskVisible(tile)) r.BlindClicks++;
                Track(e.Reveal(tile));
                reveals++;
            }
            r.HpAtEnd = e.State.Hp;
            r.Status = e.State.Status;
            r.Loss = e.State.LossReason;
            r.Level = e.State.Level;
            r.AttackAtEnd = e.State.Attack;
            r.CoinsSpent = spent;
            r.Reveals = reveals;
            return r;
        }
    }
}
