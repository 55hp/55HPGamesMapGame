using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Bot che usa gli indizi (07 §3, baseline 04/11/2026). Vede solo ciò che vede il giocatore: bordi visibili,
    /// tile scoperte, risultati degli indizi, tabella pubblica livello → contenuti e censimento.
    /// Stima, per ogni tile coperta, la probabilità che contenga un bersaglio di indizio e la usa per i livelli
    /// incerti (soprattutto il 5: NPC contro mini-boss). Sceglie la linea dell'indizio che copre più candidati.
    /// Per il resto (level up, oggetti, NPC) si comporta come GreedyPolicy.
    /// </summary>
    public sealed class ClueAwarePolicy : IPlayerPolicy
    {
        private readonly IRandomSource _rng;
        private readonly GreedyPolicy _fallback;

        public ClueAwarePolicy(ulong seed)
        {
            _rng = new SplitMix64Random(seed);
            _fallback = new GreedyPolicy(seed ^ 0x5EEDUL);
        }

        // ------------------------------------------------------------ tile

        // Cache per scelta: calcolate una volta per ChooseTile (le stime non cambiano durante la scelta).
        private RulesEngine _ctxEngine;
        private bool[,] _levels;                 // [target, livello] il bersaglio ha contenuti a quel livello
        private readonly Dictionary<ClueTarget, double> _prior = new Dictionary<ClueTarget, double>();
        private readonly Dictionary<(LineRef, ClueTarget), double> _lineP = new Dictionary<(LineRef, ClueTarget), double>();

        public int ChooseTile(RulesEngine e)
        {
            PrepareContext(e);
            int bestScore = int.MinValue;
            var best = new List<int>();
            for (int i = 0; i < e.Layout.Count; i++)
            {
                if (!e.IsRevealable(i)) continue;
                int score = Score(e, i);
                if (score > bestScore) { bestScore = score; best.Clear(); }
                if (score == bestScore) best.Add(i);
            }
            return best[_rng.Range(0, best.Count)];
        }

        private void PrepareContext(RulesEngine e)
        {
            var cfg = e.Config;
            if (_ctxEngine == null || _ctxEngine.Config != cfg)
            {
                int n = Enum.GetValues(typeof(ClueTarget)).Length;
                _levels = new bool[n, 8];
                foreach (var kv in cfg.Census)
                {
                    if (kv.Value <= 0) continue;
                    var d = cfg.Contents[kv.Key];
                    if (d.ClueTarget.HasValue) _levels[(int)d.ClueTarget.Value, d.MaskLevel] = true;
                }
            }
            _ctxEngine = e;
            _prior.Clear();
            _lineP.Clear();
        }

        private int Score(RulesEngine e, int i)
        {
            var s = e.State;
            var cfg = e.Config;
            var def = e.ContentAt(i);
            if (e.IsTypeShown(i)) return KnownScore(def, s, cfg);
            if (!e.IsMaskVisible(i)) return GreedyPolicy.BlindScore(e, i);

            int atk = s.Attack, hp = s.Hp;
            switch (def.MaskLevel)
            {
                case 0: return 25;
                case 1:
                {
                    // Arbusti inutili a HP pieni: con un indizio sugli Arbusti si rimandano.
                    double pBush = Probability(e, i, ClueTarget.Bush);
                    int baseScore = hp >= 3 || atk >= 2 ? 50 : 30;
                    return hp >= s.MaxHp ? baseScore - (int)(10 * pBush) : baseScore + (int)(15 * pBush);
                }
                case 2:
                {
                    if (atk < 3) return hp >= 4 ? 10 : -50;
                    double pMon = Probability(e, i, ClueTarget.MonstersNormal);
                    return 30 + (int)(20 * pMon);
                }
                case 3: return 60;
                case 4: return hp < s.MaxHp ? 70 : 5;
                case 5:
                {
                    if (atk >= cfg.NpcDefense) return 45;
                    double pMb = Probability(e, i, ClueTarget.MiniBoss);
                    double pNpc = 0;
                    foreach (var t in NpcTargets) pNpc += Probability(e, i, t);
                    if (pNpc > 1) pNpc = 1;
                    int dmg = cfg.NpcDefense - atk; // stesso danno di un mini-boss (Difesa 5)
                    double ev = pNpc * 35 - pMb * (dmg >= hp ? 200 : 20 * dmg);
                    return (int)Math.Round(ev);
                }
                case 6: return atk >= cfg.Content(ContentKind.Boss).Defense ? 1000 : -1000;
                default: return 0;
            }
        }

        private static readonly ClueTarget[] NpcTargets =
            { ClueTarget.Witch, ClueTarget.Hunter, ClueTarget.Smith, ClueTarget.Inn, ClueTarget.Trader };

        private static int KnownScore(ContentDef def, RunState s, RulesConfig cfg)
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

        // ------------------------------------------------------------ stima dagli indizi

        /// <summary>
        /// Probabilità che la tile coperta i contenga il bersaglio t. Senza indizi: frequenza residua del bersaglio
        /// tra le tile candidate. Con un indizio su una linea che passa da i: (rimasti sulla linea) / (candidati coperti
        /// sulla linea); 0 o 1 se la linea è risolta. Con due linee vince la più netta.
        /// </summary>
        private double Probability(RulesEngine e, int i, ClueTarget t)
        {
            var cfg = e.Config;
            if (e.IsMaskVisible(i) && !_levels[(int)t, e.ContentAt(i).MaskLevel]) return 0;

            double best = -1;
            int w = cfg.Width;
            var row = LineRef.Row(i / w);
            var col = LineRef.Col(i % w);
            foreach (var line in new[] { row, col })
            {
                if (!e.ClueResults.TryGetValue(line, out var res) || res.target != t) continue;
                if (!_lineP.TryGetValue((line, t), out double p))
                {
                    int found = 0, candidates = 0;
                    foreach (int idx in LineTiles(cfg, line))
                    {
                        if (e.IsRevealed(idx)) { if (CensusMapGenerator.Matches(cfg, e.Layout.Tiles[idx], t)) found++; continue; }
                        if (IsCandidate(e, idx, t)) candidates++;
                    }
                    int left = res.count - found;
                    p = candidates == 0 ? 0 : Math.Max(0, Math.Min(1, (double)left / candidates));
                    _lineP[(line, t)] = p;
                }
                if (p == 0 || p == 1) return p;
                if (best < 0 || Math.Abs(p - 0.5) > Math.Abs(best - 0.5)) best = p;
            }
            if (best >= 0) return best;

            if (!_prior.TryGetValue(t, out double prior))
            {
                int remaining = 0, pool = 0;
                foreach (var kv in cfg.Census)
                    if (cfg.Contents[kv.Key].ClueTarget == t) remaining += kv.Value;
                for (int idx = 0; idx < e.Layout.Count; idx++)
                {
                    if (e.IsRevealed(idx)) { if (CensusMapGenerator.Matches(cfg, e.Layout.Tiles[idx], t)) remaining--; continue; }
                    if (IsCandidate(e, idx, t)) pool++;
                }
                prior = pool == 0 ? 0 : Math.Max(0, Math.Min(1, (double)remaining / pool));
                _prior[t] = prior;
            }
            return prior;
        }

        private bool IsCandidate(RulesEngine e, int idx, ClueTarget t) =>
            !e.IsMaskVisible(idx) || _levels[(int)t, e.ContentAt(idx).MaskLevel];

        private static IEnumerable<int> LineTiles(RulesConfig cfg, LineRef line)
        {
            int w = cfg.Width, h = cfg.Height;
            int len = line.IsRow ? w : h;
            for (int k = 0; k < len; k++) yield return line.IsRow ? line.Index * w + k : k * w + line.Index;
        }

        /// <summary>Linea con indizio che ha più tile coperte senza bordo visibile (candidata a una Ricognizione di linea).</summary>
        private static LineRef? BestCluedLine(RulesEngine e, bool row)
        {
            LineRef? best = null; int bestHidden = 1;
            foreach (var kv in e.ClueResults)
            {
                if (kv.Key.IsRow != row || kv.Value.count == 0) continue;
                int hidden = 0;
                foreach (int idx in LineTiles(e.Config, kv.Key)) if (!e.IsRevealed(idx) && !e.IsMaskVisible(idx)) hidden++;
                if (hidden > bestHidden) { bestHidden = hidden; best = kv.Key; }
            }
            return best;
        }

        // ------------------------------------------------------------ decisioni

        public string ChooseOption(RulesEngine e, Decision d)
        {
            if (d.Kind == DecisionKind.Npc)
            {
                // Compra indizi sui pericoli (mini-boss, mostri) se l'Attacco non basta ancora: valgono di più.
                var s = e.State;
                // D-045: Ricognizione di linea su una riga/colonna che ha già un indizio (la rende leggibile come in Classica).
                foreach (var o in d.Options)
                {
                    if (!o.Enabled || !o.Id.Contains("_scout_row") && !o.Id.Contains("_scout_col")) continue;
                    bool row = o.Id.Contains("_scout_row");
                    if (BestCluedLine(e, row) != null) return o.Id;
                }
                if (s.Attack < e.Config.NpcDefense && s.Hp > 2)
                    foreach (var o in d.Options)
                        if (o.Enabled && o.Price > 0 && o.Id.Contains("clue")
                            && (o.ClueTarget == ClueTarget.MiniBoss || o.ClueTarget == ClueTarget.MonstersNormal)) return o.Id;
                return _fallback.ChooseOption(e, d);
            }
            if (d.Kind == DecisionKind.Scout && ScoutShapes.IsLine(d.ScoutShape))
            {
                var line = BestCluedLine(e, d.ScoutShape == ScoutShapes.Row);
                if (line != null && d.Find(OptionIds.Line(line.Value)) != null) return OptionIds.Line(line.Value);
            }
            if (d.Kind != DecisionKind.ClueLine) return _fallback.ChooseOption(e, d);
            PrepareContext(e);
            // Linea con più tile coperte candidate per il bersaglio: massima informazione.
            int bestScore = int.MinValue;
            var best = new List<string>();
            foreach (var o in d.Options)
            {
                if (!o.Enabled) continue;
                int score = 0;
                foreach (int idx in LineTiles(e.Config, o.Line))
                    if (!e.IsRevealed(idx) && IsCandidate(e, idx, d.ClueTarget)) score++;
                if (score > bestScore) { bestScore = score; best.Clear(); }
                if (score == bestScore) best.Add(o.Id);
            }
            return best[_rng.Range(0, best.Count)];
        }
    }
}
