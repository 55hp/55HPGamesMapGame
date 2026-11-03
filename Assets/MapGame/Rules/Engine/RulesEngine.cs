using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Motore di regole a decisioni pendenti (01_architettura §6). Puro e deterministico:
    /// stesso seed + stesse scelte = stessa sequenza di eventi.
    ///
    /// Uso: new RulesEngine(config, seed) → Start() → loop { Reveal(index) | Submit(optionId) }.
    /// Mentre CurrentDecision != null, Reveal è rifiutato.
    /// </summary>
    public sealed class RulesEngine
    {
        private const ulong EngineSalt = 0x5DEECE66DUL; // separa il flusso di eventi da quello della mappa

        private readonly RulesConfig _config;
        private readonly IRandomSource _rng;
        private readonly bool[] _revealed;
        private readonly bool[] _typeShown;
        private readonly HashSet<LineRef> _completedLines = new HashSet<LineRef>();
        private readonly HashSet<LineRef> _usedClueLines = new HashSet<LineRef>();
        private readonly Dictionary<LineRef, (ClueTarget target, int count)> _clueResults = new Dictionary<LineRef, (ClueTarget, int)>();
        private readonly List<Decision> _pending = new List<Decision>();
        private List<RuleEvent> _events;
        private List<Decision> _newDecisions;
        private bool _started;

        public RulesConfig Config => _config;
        public MapLayout Layout { get; }
        public RunState State { get; } = new RunState();
        public Decision CurrentDecision => _pending.Count > 0 ? _pending[0] : null;
        public int PendingDecisionCount => _pending.Count;
        public IReadOnlyDictionary<LineRef, (ClueTarget target, int count)> ClueResults => _clueResults;

        public RulesEngine(RulesConfig config, ulong seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Layout = CensusMapGenerator.Generate(config, seed);
            _rng = new SplitMix64Random(seed ^ EngineSalt);
            _revealed = new bool[config.Width * config.Height];
            _typeShown = new bool[_revealed.Length];
        }

        // ---------------------------------------------------------------- lettura

        public bool IsRevealed(int index) => _revealed[index];

        /// <summary>Tipo svelato dalla Lanterna senza scoprire la tile.</summary>
        public bool IsTypeShown(int index) => _typeShown[index];

        public bool IsRevealable(int index) =>
            _started && State.Status == RunStatus.Playing && _pending.Count == 0
            && index >= 0 && index < _revealed.Length && !_revealed[index];

        public bool IsLineFree(LineRef line) => !_usedClueLines.Contains(line);

        public IEnumerable<LineRef> AllLines()
        {
            for (int r = 0; r < _config.Height; r++) yield return LineRef.Row(r);
            for (int c = 0; c < _config.Width; c++) yield return LineRef.Col(c);
        }

        public ContentDef ContentAt(int index) => _config.Content(Layout.Tiles[index]);

        // ---------------------------------------------------------------- comandi

        /// <summary>Avvia la partita: scopre partenza e Vuote, sceglie la missione, accoda gli indizi di partenza.</summary>
        public IReadOnlyList<RuleEvent> Start()
        {
            if (_started) throw new InvalidOperationException("Partita già avviata");
            if (!Layout.Success) throw new InvalidOperationException("Mappa non valida: " + Layout.FailureReason);
            Begin();
            _started = true;

            State.Hp = _config.StartHp;
            State.MaxHp = _config.StartMaxHp;
            State.BaseAttack = _config.BaseAttack;
            State.Weapon = _config.Item(_config.StartWeaponId);
            State.Status = RunStatus.Playing;

            for (int i = 0; i < _revealed.Length; i++)
            {
                var k = Layout.Tiles[i];
                if (k == ContentKind.Start || k == ContentKind.Empty)
                {
                    _revealed[i] = true;
                    Emit(RuleEventType.TileRevealed, e => { e.TileIndex = i; e.Kind = k; });
                }
            }

            // R-070 / D-026: missione a caso dal seed.
            var mission = _config.Missions[_rng.Range(0, _config.Missions.Count)];
            State.Mission = mission;
            Emit(RuleEventType.MissionStarted, e => { e.ClueTarget = mission.Target; e.Amount = mission.StartClues; });
            for (int i = 0; i < mission.StartClues; i++) ReceiveClue(mission.Target);

            // Linee già complete per le Vuote (R-041 conta solo linee completate da una mossa: qui no XP).
            for (int i = 0; i < _revealed.Length; i++) if (_revealed[i]) MarkCompletedLinesSilently(i);

            return End();
        }

        /// <summary>Scopre una tile (R-002). Ritorna gli eventi; lista vuota se la mossa non è legale.</summary>
        public IReadOnlyList<RuleEvent> Reveal(int index)
        {
            if (!IsRevealable(index)) return Array.Empty<RuleEvent>();
            Begin();

            _revealed[index] = true;
            var kind = Layout.Tiles[index];
            var def = _config.Content(kind);
            Emit(RuleEventType.TileRevealed, e => { e.TileIndex = index; e.Kind = kind; });

            switch (def.Category)
            {
                case ContentCategory.Monster: ResolveMonster(index, def); break;
                case ContentCategory.Trap: ResolveTrap(index, def); break;
                case ContentCategory.Heal: ResolveHeal(def); break;
                case ContentCategory.Chest: ResolveChest(index, def); break;
                case ContentCategory.Npc: OpenNpc(index, def); break;
            }

            if (State.Status == RunStatus.Playing)
            {
                CheckLines(index);
                if (State.HasEffect(ItemEffect.Lantern)) LanternReveal(index);
            }

            return End();
        }

        /// <summary>Risponde alla decisione corrente. Lista vuota se l'opzione non esiste o è disabilitata.</summary>
        public IReadOnlyList<RuleEvent> Submit(string optionId)
        {
            var d = CurrentDecision;
            if (d == null || State.Status != RunStatus.Playing) return Array.Empty<RuleEvent>();
            var opt = d.Find(optionId);
            if (opt == null || !opt.Enabled) return Array.Empty<RuleEvent>();

            Begin();
            _pending.RemoveAt(0);
            switch (d.Kind)
            {
                case DecisionKind.Npc: SubmitNpc(d, opt); break;
                case DecisionKind.LevelUp: SubmitLevelUp(opt); break;
                case DecisionKind.ClueLine: SubmitClueLine(d, opt); break;
                case DecisionKind.Item: SubmitItem(d, opt); break;
            }
            return End();
        }

        // ---------------------------------------------------------------- risoluzioni

        private void ResolveMonster(int index, ContentDef def)
        {
            int atk = State.Attack;
            if (atk >= def.Defense)
            {
                Emit(RuleEventType.EncounterWon, e => { e.TileIndex = index; e.Kind = def.Kind; e.Attack = atk; e.Defense = def.Defense; });
                if (def.IsObjective) { Win(); return; }
                GainCoins(def.Coins);
                if (def.ItemId != null) FindItem(def.ItemId);
                if (def.GivesKey != KeyKind.None) GainKey(def.GivesKey);
                GainXp(def.Xp);
            }
            else
            {
                int dmg = Math.Max(0, def.Defense - atk); // R-034
                Emit(RuleEventType.EncounterLost, e => { e.TileIndex = index; e.Kind = def.Kind; e.Attack = atk; e.Defense = def.Defense; e.Amount = dmg; });
                TakeDamage(dmg);
                if (def.IsObjective && State.Status == RunStatus.Playing) Lose(LossReason.BossFled); // R-039
            }
        }

        private void ResolveTrap(int index, ContentDef def)
        {
            if (State.HasEffect(ItemEffect.DisarmTraps))
            {
                Emit(RuleEventType.TrapDisarmed, e => { e.TileIndex = index; e.Kind = def.Kind; });
                return;
            }
            Emit(RuleEventType.TrapTriggered, e => { e.TileIndex = index; e.Kind = def.Kind; e.Amount = def.TrapDamage; });
            TakeDamage(def.TrapDamage);
        }

        private void ResolveHeal(ContentDef def)
        {
            if (def.FullHeal) Heal(State.MaxHp);
            else Heal(def.Heal);
        }

        private void ResolveChest(int index, ContentDef def)
        {
            if (!State.HasKey(def.RequiresKey))
            {
                Emit(RuleEventType.ChestVanished, e => { e.TileIndex = index; e.Kind = def.Kind; e.Key = def.RequiresKey; });
                return;
            }
            Emit(RuleEventType.ChestOpened, e => { e.TileIndex = index; e.Kind = def.Kind; });
            if (def.Heal > 0) Heal(def.Heal);
            GainCoins(def.Coins);
            if (def.ItemId != null) FindItem(def.ItemId);
        }

        private void OpenNpc(int index, ContentDef def)
        {
            var npc = _config.Npcs[def.Kind];
            var d = new Decision { Kind = DecisionKind.Npc, TileIndex = index, Npc = def.Kind };
            if (npc.Fightable)
                d.Options.Add(new DecisionOption { Id = OptionIds.Attack, LabelKey = "ui.npc.attack" });
            foreach (var offer in npc.Offers)
            {
                var o = new DecisionOption { Id = offer.Id, LabelKey = offer.NameKey, Price = offer.Price };
                if (offer.OncePerRun && offer.Effect == NpcOfferEffect.SmithUpgrade && State.SmithUpgradeBought)
                { o.Enabled = false; o.DisabledReasonKey = "ui.npc.reason.already_bought"; }
                else if (State.Coins < offer.Price)
                { o.Enabled = false; o.DisabledReasonKey = "ui.npc.reason.no_coins"; }
                d.Options.Add(o);
            }
            d.Options.Add(new DecisionOption { Id = OptionIds.Close, LabelKey = "ui.npc.close" });
            Emit(RuleEventType.NpcOpened, e => { e.TileIndex = index; e.Kind = def.Kind; });
            Push(d);
        }

        private void SubmitNpc(Decision d, DecisionOption opt)
        {
            var npc = _config.Npcs[d.Npc];
            if (opt.Id == OptionIds.Close) return;

            if (opt.Id == OptionIds.Attack)
            {
                int atk = State.Attack, def = _config.NpcDefense;
                if (atk >= def)
                {
                    Emit(RuleEventType.NpcKilled, e => { e.TileIndex = d.TileIndex; e.Kind = d.Npc; e.Attack = atk; e.Defense = def; });
                    GainCoins(npc.KillCoins);
                    if (npc.KillItemId != null) FindItem(npc.KillItemId);
                }
                else
                {
                    int dmg = def - atk; // R-063 / D-011
                    Emit(RuleEventType.NpcFled, e => { e.TileIndex = d.TileIndex; e.Kind = d.Npc; e.Attack = atk; e.Defense = def; e.Amount = dmg; });
                    TakeDamage(dmg);
                }
                return;
            }

            NpcOfferDef offer = null;
            foreach (var o in npc.Offers) if (o.Id == opt.Id) offer = o;
            if (offer == null) return;

            State.Coins -= offer.Price;
            Emit(RuleEventType.CoinsSpent, e => { e.Amount = offer.Price; e.OfferId = offer.Id; });
            Emit(RuleEventType.OfferBought, e => { e.TileIndex = d.TileIndex; e.Kind = d.Npc; e.OfferId = offer.Id; e.Amount = offer.Price; });
            switch (offer.Effect)
            {
                case NpcOfferEffect.Heal: Heal(offer.Amount); break;
                case NpcOfferEffect.FullHeal: Heal(State.MaxHp); break;
                case NpcOfferEffect.GiveItem: Equip(_config.Item(offer.ItemId)); break; // acquisto = scelta esplicita
                case NpcOfferEffect.SmithUpgrade:
                    State.SmithBonus += offer.Amount;
                    State.SmithUpgradeBought = true;
                    break;
                case NpcOfferEffect.Clues:
                    for (int i = 0; i < offer.Amount; i++) ReceiveClue(offer.ClueTarget);
                    break;
            }
        }

        // ---------------------------------------------------------------- risorse

        private void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            State.Hp -= amount;
            Emit(RuleEventType.DamageTaken, e => e.Amount = amount);
            if (State.Hp < 1) Lose(LossReason.Death); // R-021
        }

        private void Heal(int amount)
        {
            int before = State.Hp;
            State.Hp = Math.Min(State.MaxHp, State.Hp + amount); // R-004
            int healed = State.Hp - before;
            if (healed > 0) Emit(RuleEventType.Healed, e => e.Amount = healed);
        }

        private void GainCoins(int amount)
        {
            if (amount <= 0) return;
            int total = amount + State.CoinBonusPerGain; // R-038, per evento
            State.Coins += total;
            Emit(RuleEventType.CoinsGained, e => e.Amount = total);
        }

        private void GainKey(KeyKind key)
        {
            if (key == KeyKind.Silver) State.HasSilverKey = true;
            if (key == KeyKind.Gold) State.HasGoldKey = true;
            Emit(RuleEventType.KeyObtained, e => e.Key = key);
        }

        private void GainXp(int amount)
        {
            if (amount <= 0) return;
            int beforeLevel = State.Level;
            State.Xp += amount;
            Emit(RuleEventType.XpGained, e => e.Amount = amount);
            int newLevel = 1 + State.Xp / _config.XpPerLevel;
            for (int lvl = beforeLevel + 1; lvl <= newLevel; lvl++)
            {
                State.Level = lvl;
                int l = lvl;
                Emit(RuleEventType.LevelUp, e => e.Amount = l);
                var d = new Decision { Kind = DecisionKind.LevelUp };
                d.Options.Add(new DecisionOption { Id = OptionIds.LevelAttack, LabelKey = "ui.levelup.attack" });
                d.Options.Add(new DecisionOption { Id = OptionIds.LevelCoins, LabelKey = "ui.levelup.coins" });
                d.Options.Add(new DecisionOption { Id = OptionIds.LevelMaxHp, LabelKey = "ui.levelup.maxhp" });
                Push(d);
            }
        }

        private void SubmitLevelUp(DecisionOption opt)
        {
            if (opt.Id == OptionIds.LevelAttack) State.LevelAttackBonus += _config.LevelUpAttack;
            else if (opt.Id == OptionIds.LevelCoins) State.CoinBonusPerGain += _config.LevelUpCoinBonus;
            else if (opt.Id == OptionIds.LevelMaxHp) State.MaxHp += _config.LevelUpMaxHp; // R-043: non cura
            Emit(RuleEventType.LevelUpChosen, e => e.OfferId = opt.Id);
        }

        // ---------------------------------------------------------------- oggetti

        /// <summary>
        /// Oggetto trovato (forziere, uccisione). Slot vuoto: si equipaggia da solo (R-024/025).
        /// Slot occupato: decisione Equipaggia/Lascia (D-030, Athena, da rivedere).
        /// </summary>
        private void FindItem(string itemId)
        {
            var item = _config.Item(itemId);
            var current = State.Equipped(item.Slot);
            if (current == null) { Equip(item); return; }
            var d = new Decision { Kind = DecisionKind.Item, NewItemId = item.Id, CurrentItemId = current.Id };
            d.Options.Add(new DecisionOption { Id = OptionIds.Equip, LabelKey = "ui.item.equip", ItemId = item.Id });
            d.Options.Add(new DecisionOption { Id = OptionIds.Discard, LabelKey = "ui.item.discard", ItemId = item.Id });
            Push(d);
        }

        private void SubmitItem(Decision d, DecisionOption opt)
        {
            if (opt.Id == OptionIds.Equip) Equip(_config.Item(d.NewItemId));
            else Emit(RuleEventType.ItemDiscarded, e => e.ItemId = d.NewItemId);
        }

        private void Equip(ItemDef item)
        {
            var old = State.Equipped(item.Slot);
            if (old != null) Emit(RuleEventType.ItemDiscarded, e => e.ItemId = old.Id); // R-025
            State.SetEquipped(item.Slot, item);
            Emit(RuleEventType.ItemEquipped, e => e.ItemId = item.Id);
        }

        // ---------------------------------------------------------------- linee e indizi

        private void CheckLines(int index)
        {
            int w = _config.Width, col = index % w, row = index / w;
            int completed = 0;
            foreach (var line in new[] { LineRef.Row(row), LineRef.Col(col) })
            {
                if (_completedLines.Contains(line) || !IsLineComplete(line)) continue;
                _completedLines.Add(line);
                completed++;
                var l = line;
                Emit(RuleEventType.LineCompleted, e => e.Line = l);
            }
            GainXp(completed * _config.LineCompleteXp); // R-041
        }

        private void MarkCompletedLinesSilently(int index)
        {
            int w = _config.Width;
            foreach (var line in new[] { LineRef.Row(index / w), LineRef.Col(index % w) })
                if (IsLineComplete(line)) _completedLines.Add(line);
        }

        private bool IsLineComplete(LineRef line)
        {
            int w = _config.Width, h = _config.Height;
            int len = line.IsRow ? w : h;
            for (int i = 0; i < len; i++)
            {
                int idx = line.IsRow ? line.Index * w + i : i * w + line.Index;
                if (!_revealed[idx]) return false;
            }
            return true;
        }

        private void ReceiveClue(ClueTarget target)
        {
            Emit(RuleEventType.ClueReceived, e => e.ClueTarget = target);
            Push(new Decision { Kind = DecisionKind.ClueLine, ClueTarget = target });
        }

        private void SubmitClueLine(Decision d, DecisionOption opt)
        {
            var line = opt.Line;
            _usedClueLines.Add(line); // R-094: slot permanente
            int count = Layout.ClueCount(d.ClueTarget, line);
            _clueResults[line] = (d.ClueTarget, count);
            Emit(RuleEventType.ClueUsed, e => { e.Line = line; e.ClueTarget = d.ClueTarget; e.Amount = count; });
        }

        /// <summary>Ricalcola le linee libere di una decisione indizio; se non ce ne sono l'indizio è perso (R-096).</summary>
        private void NormalizePending()
        {
            while (_pending.Count > 0 && _pending[0].Kind == DecisionKind.ClueLine)
            {
                var d = _pending[0];
                d.Options.Clear();
                foreach (var line in AllLines())
                    if (IsLineFree(line))
                        d.Options.Add(new DecisionOption { Id = OptionIds.Line(line), LabelKey = "ui.clue.line", Line = line });
                if (d.Options.Count > 0) return;
                _pending.RemoveAt(0);
                Emit(RuleEventType.ClueLost, e => e.ClueTarget = d.ClueTarget);
            }
        }

        // ---------------------------------------------------------------- Lanterna

        private void LanternReveal(int index)
        {
            var candidates = new List<int>();
            foreach (int nb in HexGridMath.Neighbors(index, _config.Width, _config.Height))
                if (!_revealed[nb] && !_typeShown[nb]) candidates.Add(nb);
            if (candidates.Count == 0) return;
            int pick = candidates[_rng.Range(0, candidates.Count)];
            _typeShown[pick] = true;
            var kind = Layout.Tiles[pick];
            Emit(RuleEventType.TileTypeShown, e => { e.TileIndex = pick; e.Kind = kind; });
        }

        // ---------------------------------------------------------------- fine partita

        private void Win()
        {
            State.Status = RunStatus.Won;
            _pending.Clear(); _newDecisions.Clear();
            Emit(RuleEventType.GameWon, e => { });
        }

        private void Lose(LossReason reason)
        {
            if (State.Status != RunStatus.Playing) return;
            State.Status = RunStatus.Lost;
            State.LossReason = reason;
            _pending.Clear(); _newDecisions.Clear();
            Emit(RuleEventType.GameLost, e => e.LossReason = reason);
        }

        // ---------------------------------------------------------------- infrastruttura

        private void Begin()
        {
            _events = new List<RuleEvent>();
            _newDecisions = new List<Decision>();
        }

        private IReadOnlyList<RuleEvent> End()
        {
            // Le decisioni nate da questa azione vanno in testa, nell'ordine in cui sono nate.
            if (State.Status == RunStatus.Playing) _pending.InsertRange(0, _newDecisions);
            _newDecisions = null;
            NormalizePending();
            var result = _events;
            _events = null;
            return result;
        }

        private void Push(Decision d)
        {
            if (State.Status == RunStatus.Playing) _newDecisions.Add(d);
        }

        private void Emit(RuleEventType type, Action<RuleEvent> fill)
        {
            var e = new RuleEvent { Type = type };
            fill(e);
            _events.Add(e);
        }
    }
}
