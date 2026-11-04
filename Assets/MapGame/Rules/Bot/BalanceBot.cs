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
        public GreedyPolicy(ulong seed) { _rng = new SplitMix64Random(seed); }

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
            // D-040: senza bordo visibile il bot non sa nulla della tile (nebbia).
            if (!e.IsMaskVisible(i)) return 15;
            switch (mask)
            {
                case 1: return hp >= 3 || atk >= 2 ? 50 : 30;
                case 3: return 60;
                case 4: return hp < s.MaxHp ? 70 : 5;
                case 2: return atk >= 3 ? 40 : (hp >= 4 ? 10 : -50);
                case 5: return atk >= cfg.NpcDefense ? 45 : (hp >= 4 ? 20 : -40);
                case 6: return atk >= cfg.Content(ContentKind.Boss).Defense ? 1000 : -1000;
                case 0: return 25; // Vuota (D-041): nessun rischio, avanza le linee
                default: return 0;
            }
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

        public string ChooseOption(RulesEngine e, Decision d)
        {
            var s = e.State;
            var cfg = e.Config;
            switch (d.Kind)
            {
                case DecisionKind.LevelUp:
                    return s.Hp <= 2 ? OptionIds.LevelMaxHp : OptionIds.LevelAttack;
                case DecisionKind.Item:
                    var ni = cfg.Item(d.NewItemId); var ci = cfg.Item(d.CurrentItemId);
                    return ni.AttackBonus >= ci.AttackBonus ? OptionIds.Equip : OptionIds.Discard;
                case DecisionKind.ClueLine:
                    return d.Options[_rng.Range(0, d.Options.Count)].Id;
                case DecisionKind.Npc:
                    return ChooseNpc(e, d);
            }
            return d.Options.Find(o => o.Enabled).Id;
        }

        private static string ChooseNpc(RulesEngine e, Decision d)
        {
            var s = e.State;
            var cfg = e.Config;
            var npc = cfg.Npcs[d.Npc];
            string Pick(string id) { var o = d.Find(id); return o != null && o.Enabled ? id : null; }

            if (npc.Fightable && s.Attack >= cfg.NpcDefense)
            {
                var kill = cfg.Item(npc.KillItemId);
                if (kill == null || kill.AttackBonus > (s.Weapon?.AttackBonus ?? 0)) return OptionIds.Attack;
            }
            if (s.Hp <= 2)
                foreach (var id in new[] { "inn_rest", "hunter_game", "farmer_food" })
                    if (Pick(id) != null) return id;
            foreach (var o in npc.Offers)
            {
                if (Pick(o.Id) == null) continue;
                if (o.Effect == NpcOfferEffect.SmithUpgrade) return o.Id;
                if (o.Effect == NpcOfferEffect.GiveItem)
                {
                    var it = cfg.Item(o.ItemId);
                    var cur = s.Equipped(it.Slot);
                    if (cur == null || it.AttackBonus > cur.AttackBonus) return o.Id;
                }
            }
            if (s.Hp < s.MaxHp && Pick("inn_rest") != null && s.Hp <= s.MaxHp - 3) return "inn_rest";
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
