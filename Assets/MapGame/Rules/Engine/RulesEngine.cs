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
        private readonly bool[] _scouted;

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
            _scouted = new bool[_revealed.Length];
        }

        // ---------------------------------------------------------------- lettura

        public bool IsRevealed(int index) => _revealed[index];

        /// <summary>Tipo svelato dalla Lanterna senza scoprire la tile.</summary>
        public bool IsTypeShown(int index) => _typeShown[index];

        public bool IsRevealable(int index) =>
            _started && State.Status == RunStatus.Playing && _pending.Count == 0
            && index >= 0 && index < _revealed.Length && !_revealed[index]
            && (_config.Mode != GameMode.FogAdjacent || IsAdjacentToRevealed(index));

        /// <summary>
        /// D-040: il giocatore vede il livello di mascheramento (bordo) di questa tile coperta?
        /// Open: sempre. Fog e FogAdjacent: solo se adiacente a una tile scoperta.
        /// </summary>
        public bool IsMaskVisible(int index)
        {
            if (index < 0 || index >= _revealed.Length || _revealed[index]) return false;
            if (_config.Mode == GameMode.Open || _scouted[index]) return true;
            if (_config.FogShowBoss && index == Layout.BossIndex) return true; // D-043, sottomodalità
            // D-043: in Nebbia i bordi adiacenti non si vedono più (salvo FogRevealsAdjacent); in Esplorazione sì.
            bool adjacentRule = _config.Mode == GameMode.FogAdjacent || _config.FogRevealsAdjacent
                || State.HasEffect(ItemEffect.Compass); // D-049: la Bussola dà in Nebbia la vista dell'Esplorazione
            return adjacentRule && IsAdjacentToRevealed(index);
        }

        public bool IsScouted(int index) => _scouted[index];

        /// <summary>D-043: tile coperte di cui il giocatore non vede il bordo.</summary>
        private bool IsHidden(int index) => !_revealed[index] && !IsMaskVisible(index);

        private IEnumerable<int> ScoutArea(int center, int shape)
        {
            int w = _config.Width, h = _config.Height;
            switch (shape)
            {
                case ScoutShapes.Row:
                    for (int c = 0; c < w; c++) yield return (center / w) * w + c;
                    yield break;
                case ScoutShapes.Column:
                    for (int r = 0; r < h; r++) yield return r * w + center % w;
                    yield break;
            }
            yield return center;
            if (shape == ScoutShapes.Cluster)
                foreach (int nb in HexGridMath.Neighbors(center, w, h)) yield return nb;
        }

        public bool IsAdjacentToRevealed(int index)
        {
            foreach (int nb in HexGridMath.Neighbors(index, _config.Width, _config.Height))
                if (_revealed[nb]) return true;
            return false;
        }

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
            State.Coins = _config.StartCoins;
            State.BaseAttack = _config.BaseAttack;
            State.MainHand = _config.Item(_config.StartWeaponId);
            State.OffHandWeaponBonus = _config.OffHandWeaponBonus;
            State.OffHandWeaponMinBonus = _config.OffHandWeaponMinBonus;
            State.Status = RunStatus.Playing;

            for (int i = 0; i < _revealed.Length; i++)
            {
                var k = Layout.Tiles[i];
                // D-041: le Vuote sono coperte come le altre (bordo neutro, livello 0); scoperta solo la partenza.
                if (k == ContentKind.Start)
                {
                    _revealed[i] = true;
                    Emit(RuleEventType.TileRevealed, e => { e.TileIndex = i; e.Kind = k; });
                }
            }

            // R-070 / D-026: missione a caso dal seed.
            var mission = _config.Missions[_rng.Range(0, _config.Missions.Count)];
            State.Mission = mission;
            Emit(RuleEventType.MissionStarted, e => { e.ClueTarget = mission.Target; e.Amount = CluesActive ? mission.StartClues : 0; });
            for (int i = 0; i < mission.StartClues; i++) ReceiveClue(mission.Target);
            if (_config.Mode != GameMode.Open)
                foreach (int radius in _config.FogStartScouts) ReceiveScout(radius); // D-043

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
            Emit(RuleEventType.DecisionResolved, e => { e.OfferId = optionId; e.TileIndex = d.TileIndex; });
            switch (d.Kind)
            {
                case DecisionKind.Npc: SubmitNpc(d, opt); break;
                case DecisionKind.LevelUp: SubmitLevelUp(opt); break;
                case DecisionKind.ClueLine: SubmitClueLine(d, opt); break;
                case DecisionKind.Item: SubmitItem(d, opt); break;
                case DecisionKind.Scout: SubmitScout(d, opt); break;
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
                CheckMission(def, index);
                GainXp(def.Xp);
                if (_config.MonsterKillClueKinds.Contains(def.Kind)) ReceiveClue(_config.MonsterKillClueTarget); // D-039
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
            else Heal(def.Heal + (def.Kind == ContentKind.Bush && State.HasEffect(ItemEffect.Herbarium) ? _config.HerbariumBonus : 0)); // D-052
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
            var d = BuildNpcDecision(index, def.Kind);
            Emit(RuleEventType.NpcOpened, e => { e.TileIndex = index; e.Kind = def.Kind; });
            CheckMission(def, index);
            Push(d);
        }

        /// <summary>Pannello di un NPC con le opzioni valide adesso (D-054: si ricostruisce dopo ogni Ricognizione comprata).</summary>
        private Decision BuildNpcDecision(int index, ContentKind kind)
        {
            var npc = _config.Npcs[kind];
            var d = new Decision { Kind = DecisionKind.Npc, TileIndex = index, Npc = kind };
            if (npc.Fightable)
            {
                int atk = State.Attack, defense = _config.NpcDefense;
                d.Options.Add(new DecisionOption
                {
                    Id = OptionIds.Attack, LabelKey = "ui.npc.attack",
                    PredictedWin = atk >= defense, PredictedDamage = atk >= defense ? 0 : defense - atk,
                });
            }
            ClueTarget? randomTarget = null;
            foreach (var offer in npc.Offers)
            {
                // D-054: le Ricognizioni non finiscono (Stock -1 = infinito), gli oggetti sono pezzi unici (Stock 1).
                var o = new DecisionOption { Id = offer.Id, LabelKey = offer.NameKey, Price = offer.Price,
                    Stock = offer.Effect == NpcOfferEffect.Scout ? -1 : offer.Effect == NpcOfferEffect.GiveItem ? 1 : 0 };
                // D-043: la Ricognizione esiste solo con la nebbia e se resta qualcosa da vedere (R-067).
                if (offer.Effect == NpcOfferEffect.Scout && (_config.Mode == GameMode.Open || !AnyHidden())) continue;
                // D-049: la Bussola serve solo in Nebbia; la Mappa solo se resta un forziere d'oro o d'argento da trovare.
                if (offer.Effect == NpcOfferEffect.GiveItem && _config.Item(offer.ItemId)?.Effect == ItemEffect.Compass && _config.Mode != GameMode.Fog) continue;
                // D-051: una Mappa compare solo se il suo forziere è ancora da trovare.
                var mapFx = offer.Effect == NpcOfferEffect.GiveItem ? _config.Item(offer.ItemId)?.Effect : null;
                if ((mapFx == ItemEffect.MapGold || mapFx == ItemEffect.MapSilver) && !State.HasItem(offer.ItemId)
                    && MapTarget(mapFx.Value) < 0) continue;
                if (offer.Effect == NpcOfferEffect.Clues && !CluesActive) continue; // D-053
                if (offer.Effect == NpcOfferEffect.LocateNpc)
                {
                    // D-055: solo dove gli indizi normali non ci sono (Nebbia); NPC estratto tra quelli localizzabili.
                    if (CluesActive) continue;
                    var npcTarget = PickLocatableNpc();
                    if (npcTarget == null) continue;
                    o.ClueTarget = npcTarget.Value;
                }
                if (offer.Effect == NpcOfferEffect.Clues)
                {
                    // D-038: un indizio su contenuti già tutti scoperti non esiste: l'offerta non compare.
                    ClueTarget target;
                    if (offer.RandomTargetMaskLevel > 0)
                    {
                        if (randomTarget == null) randomTarget = PickRandomClueTarget(offer.RandomTargetMaskLevel);
                        if (randomTarget == null) continue;
                        target = randomTarget.Value;
                    }
                    else target = offer.ClueTarget;
                    if (!HasCoveredTarget(target)) continue;
                    o.ClueTarget = target;
                }
                if (offer.OncePerRun && offer.Effect == NpcOfferEffect.SmithUpgrade && State.SmithUpgradeBought)
                { o.Enabled = false; o.DisabledReasonKey = "ui.npc.reason.already_bought"; }
                else if (offer.Effect == NpcOfferEffect.GiveItem && State.HasItem(offer.ItemId))
                { o.Enabled = false; o.DisabledReasonKey = "ui.npc.reason.already_owned"; } // D-050
                else if (State.Coins < offer.Price)
                { o.Enabled = false; o.DisabledReasonKey = "ui.npc.reason.no_coins"; }
                d.Options.Add(o);
            }
            d.Options.Add(new DecisionOption { Id = OptionIds.Close, LabelKey = "ui.npc.close" });
            return d;
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
                case NpcOfferEffect.GiveItem: FindItem(offer.ItemId); break; // D-050: con le mani piene si sceglie cosa lasciare
                case NpcOfferEffect.SmithUpgrade:
                    State.SmithBonus += offer.Amount;
                    State.SmithUpgradeBought = true;
                    break;
                case NpcOfferEffect.Clues:
                    for (int i = 0; i < offer.Amount; i++) ReceiveClue(opt.ClueTarget);
                    break;
                case NpcOfferEffect.Scout:
                    ReceiveScout(offer.Amount);
                    break;
                case NpcOfferEffect.LocateNpc:
                    LocateNpc(opt.ClueTarget);
                    break;
                case NpcOfferEffect.TreasureMap: break; // D-051: sostituita dalle Mappe Trinket
            }
            // D-054/D-055 (Franci, 05/11/2026): comprare non chiude l'NPC; lo chiudono solo l'attacco e "Vai via".
            // Il pannello torna dopo le scelte nate dall'acquisto (Ricognizione da posare, mani piene).
            if (State.Status == RunStatus.Playing) Push(BuildNpcDecision(d.TileIndex, d.Npc));
        }

        // ---------------------------------------------------------------- risorse

        private void TakeDamage(int amount)
        {
            if (State.HasEffect(ItemEffect.Shield) && amount > 0) // D-052
            {
                amount = Math.Max(0, amount - _config.ShieldReduction);
                if (_config.ShieldCharges > 0 && --State.ShieldChargesLeft <= 0)
                    DropHand(State.MainHand?.Effect == ItemEffect.Shield ? State.MainHand : State.OffHand); // lo Scudo si rompe
            }
            if (amount <= 0) return;
            State.Hp -= amount;
            Emit(RuleEventType.DamageTaken, e => e.Amount = amount);
            if (State.Hp < 1 && State.HasEffect(ItemEffect.Phoenix)) // D-052: la Fenice si consuma
            {
                var phoenix = State._trinkets.Find(t => t.Effect == ItemEffect.Phoenix);
                State._trinkets.Remove(phoenix);
                State.Hp = Math.Min(State.MaxHp, _config.PhoenixHp);
                Emit(RuleEventType.PhoenixSaved, e => { e.ItemId = phoenix.Id; e.Amount = State.Hp; });
            }
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
            int total = amount + State.CoinBonusPerGain // R-038, per evento
                + (State.HasEffect(ItemEffect.Purse) ? _config.PurseBonus : 0); // D-049
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
        /// D-050: oggetto trovato o comprato. Trinket: si aggiunge (uno per tipo). Mani: se ci sono abbastanza
        /// mani libere si equipaggia da solo; altrimenti decisione su cosa lasciare (un'opzione per oggetto in mano,
        /// "tutto" per un'arma a due mani) oppure lasciare l'oggetto nuovo.
        /// </summary>
        private void FindItem(string itemId)
        {
            var item = _config.Item(itemId);
            if (item.Slot == ItemSlot.Trinket)
            {
                if (State.HasItem(item.Id)) { Emit(RuleEventType.ItemDiscarded, e => e.ItemId = item.Id); return; }
                State._trinkets.Add(item);
                Emit(RuleEventType.ItemEquipped, e => e.ItemId = item.Id);
                if (item.Effect == ItemEffect.MapGold || item.Effect == ItemEffect.MapSilver) ShowType(MapTarget(item.Effect)); // D-051
                return;
            }
            if (State.FreeHands >= item.HandsNeeded) { Place(item); return; }

            var d = new Decision { Kind = DecisionKind.Item, NewItemId = item.Id, CurrentItemId = State.MainHand?.Id };
            if (item.TwoHanded || State.MainHand != null && State.MainHand.TwoHanded)
            {
                // Un'arma a due mani entra solo lasciando tutto; con un'arma a due mani in mano si lascia quella.
                if (item.TwoHanded) d.Options.Add(new DecisionOption { Id = OptionIds.EquipAll, LabelKey = "ui.item.equip", ItemId = null });
                else d.Options.Add(new DecisionOption { Id = OptionIds.EquipReplacing(State.MainHand.Id), LabelKey = "ui.item.equip", ItemId = State.MainHand.Id });
            }
            else
            {
                foreach (var held in new[] { State.MainHand, State.OffHand })
                    if (held != null) d.Options.Add(new DecisionOption { Id = OptionIds.EquipReplacing(held.Id), LabelKey = "ui.item.equip", ItemId = held.Id });
            }
            d.Options.Add(new DecisionOption { Id = OptionIds.Discard, LabelKey = "ui.item.discard", ItemId = item.Id });
            Push(d);
        }

        private void SubmitItem(Decision d, DecisionOption opt)
        {
            var item = _config.Item(d.NewItemId);
            if (opt.Id == OptionIds.Discard) { Emit(RuleEventType.ItemDiscarded, e => e.ItemId = d.NewItemId); return; }
            if (opt.Id == OptionIds.EquipAll) { DropHand(State.OffHand); DropHand(State.MainHand); }
            else DropHand(opt.ItemId == State.MainHand?.Id ? State.MainHand : State.OffHand);
            Place(item);
        }

        private void DropHand(ItemDef held)
        {
            if (held == null) return;
            if (State.MainHand == held) { State.MainHand = State.OffHand; State.OffHand = null; }
            else if (State.OffHand == held) State.OffHand = null;
            Emit(RuleEventType.ItemDiscarded, e => e.ItemId = held.Id); // R-025
        }

        private void Place(ItemDef item)
        {
            if (item.Effect == ItemEffect.Shield) State.ShieldChargesLeft = _config.ShieldCharges;
            if (item.TwoHanded || State.MainHand == null)
            {
                if (item.TwoHanded) State.OffHand = null;
                State.MainHand = item;
            }
            else State.OffHand = item;
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
            if (State.Mission != null) // D-039
                for (int i = 0; i < completed * _config.LineCompleteClues; i++) ReceiveClue(State.Mission.Target);
            if (_config.Mode != GameMode.Open && _config.FogLineCompleteScoutShape >= 0) // D-043
                for (int i = 0; i < completed; i++) ReceiveScout(_config.FogLineCompleteScoutShape);
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

        /// <summary>D-038: esiste ancora almeno una tile coperta del bersaglio? Altrimenti l'indizio sarebbe inutile.</summary>
        public bool HasCoveredTarget(ClueTarget target)
        {
            for (int i = 0; i < _revealed.Length; i++)
                if (!_revealed[i] && CensusMapGenerator.Matches(_config, Layout.Tiles[i], target)) return true;
            return false;
        }

        /// <summary>D-038: bersaglio estratto (seed della partita) tra quelli del livello dato con tile ancora coperte.</summary>
        private ClueTarget? PickRandomClueTarget(int maskLevel)
        {
            var candidates = new List<ClueTarget>();
            foreach (ClueTarget t in Enum.GetValues(typeof(ClueTarget)))
            {
                bool atLevel = false;
                foreach (var def in _config.Contents.Values)
                    if (def.ClueTarget == t && def.MaskLevel == maskLevel && _config.Census.TryGetValue(def.Kind, out int q) && q > 0) atLevel = true;
                if (atLevel && HasCoveredTarget(t)) candidates.Add(t);
            }
            if (candidates.Count == 0) return null;
            return candidates[_rng.Range(0, candidates.Count)];
        }

        /// <summary>D-046: il bersaglio della missione è raggiunto aprendo quell'NPC o uccidendo un mini-boss (caccia ai mini-boss).</summary>
        private void CheckMission(ContentDef def, int index)
        {
            if (State.MissionReached || State.Mission == null || def.ClueTarget != State.Mission.Target) return;
            State.MissionReached = true;
            State.MissionBonus += _config.MissionRewardAttack;
            int amount = _config.MissionRewardAttack;
            Emit(RuleEventType.MissionReached, e => { e.TileIndex = index; e.Kind = def.Kind; e.Amount = amount; e.ClueTarget = def.ClueTarget.Value; });
        }

        // ---------------------------------------------------------------- D-055: indizi di posizione

        private static readonly ClueTarget[] LocatableNpcs =
            { ClueTarget.Witch, ClueTarget.Hunter, ClueTarget.Smith, ClueTarget.Inn, ClueTarget.Trader };

        /// <summary>Righe e colonne libere (mai usate per un indizio) che contengono un NPC coperto di quel tipo.</summary>
        private List<LineRef> LocateLines(ClueTarget target)
        {
            var lines = new List<LineRef>();
            int w = _config.Width;
            for (int i = 0; i < _revealed.Length; i++)
            {
                if (_revealed[i] || !CensusMapGenerator.Matches(_config, Layout.Tiles[i], target)) continue;
                foreach (var l in new[] { LineRef.Row(i / w), LineRef.Col(i % w) })
                    if (!_usedClueLines.Contains(l) && !lines.Contains(l)) lines.Add(l);
            }
            return lines;
        }

        private ClueTarget? PickLocatableNpc()
        {
            var candidates = new List<ClueTarget>();
            foreach (var t in LocatableNpcs) if (LocateLines(t).Count > 0) candidates.Add(t);
            if (candidates.Count == 0) return null;
            return candidates[_rng.Range(0, candidates.Count)];
        }

        /// <summary>Posa da solo un indizio su una riga o colonna che contiene davvero quell'NPC (conteggio visibile).</summary>
        private void LocateNpc(ClueTarget target)
        {
            var lines = LocateLines(target);
            if (lines.Count == 0) return;
            var line = lines[_rng.Range(0, lines.Count)];
            _usedClueLines.Add(line);
            int count = Layout.ClueCount(target, line);
            _clueResults[line] = (target, count);
            Emit(RuleEventType.ClueUsed, e => { e.Line = line; e.ClueTarget = target; e.Amount = count; });
        }

        /// <summary>D-053: gli indizi esistono in questa modalità?</summary>
        public bool CluesActive => _config.Mode != GameMode.Fog || _config.FogClues;

        private void ReceiveClue(ClueTarget target)
        {
            if (!CluesActive) return; // D-053
            // D-038: chi conferisce un indizio su contenuti già tutti scoperti non lo conferisce.
            if (!HasCoveredTarget(target)) return;
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

        private bool AnyHidden()
        {
            for (int i = 0; i < _revealed.Length; i++) if (IsHidden(i)) return true;
            return false;
        }

        private void ReceiveScout(int radius)
        {
            if (!AnyHidden()) return; // niente da vedere: non viene conferita (R-067)
            Emit(RuleEventType.ScoutReceived, e => e.Amount = radius);
            Push(new Decision { Kind = DecisionKind.Scout, ScoutShape = radius });
        }

        private void SubmitScout(Decision d, DecisionOption opt)
        {
            foreach (int idx in ScoutArea(opt.TileIndex, d.ScoutShape)) _scouted[idx] = true;
            Emit(RuleEventType.ScoutUsed, e => { e.TileIndex = opt.TileIndex; e.Amount = d.ScoutShape; });
        }

        /// <summary>Ricalcola le opzioni delle decisioni di linea e di Ricognizione in testa; senza opzioni sono perse (R-096).</summary>
        private void NormalizePending()
        {
            while (_pending.Count > 0 && (_pending[0].Kind == DecisionKind.ClueLine || _pending[0].Kind == DecisionKind.Scout))
            {
                var d = _pending[0];
                d.Options.Clear();
                if (d.Kind == DecisionKind.Scout)
                {
                    for (int i = 0; i < _revealed.Length; i++)
                    {
                        bool useful = false;
                        foreach (int idx in ScoutArea(i, d.ScoutShape)) if (IsHidden(idx)) { useful = true; break; }
                        if (useful) d.Options.Add(new DecisionOption { Id = OptionIds.Tile(i), LabelKey = "ui.scout.tile", TileIndex = i });
                    }
                    // D-045: la Ricognizione di riga/colonna si posa anche toccando l'etichetta della linea.
                    if (ScoutShapes.IsLine(d.ScoutShape) && d.Options.Count > 0)
                    {
                        int w = _config.Width;
                        var lines = new HashSet<string>();
                        var tileOpts = new List<DecisionOption>(d.Options);
                        foreach (var o in tileOpts)
                        {
                            var line = d.ScoutShape == ScoutShapes.Row ? LineRef.Row(o.TileIndex / w) : LineRef.Col(o.TileIndex % w);
                            string id = OptionIds.Line(line);
                            if (lines.Add(id)) d.Options.Add(new DecisionOption { Id = id, LabelKey = "ui.scout.line", TileIndex = o.TileIndex, Line = line });
                        }
                    }
                    if (d.Options.Count > 0) return;
                    _pending.RemoveAt(0);
                    Emit(RuleEventType.ScoutLost, e => e.Amount = d.ScoutShape);
                    continue;
                }
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
            ShowType(candidates[_rng.Range(0, candidates.Count)]);
        }

        /// <summary>D-051: forziere della Mappa (oro o argento) coperto e non ancora mostrato; -1 se non c'è.</summary>
        private int MapTarget(ItemEffect fx)
        {
            var kind = fx == ItemEffect.MapGold ? ContentKind.ChestGold : ContentKind.ChestSilver;
            for (int i = 0; i < _revealed.Length; i++)
                if (Layout.Tiles[i] == kind && !_revealed[i] && !_typeShown[i]) return i;
            return -1;
        }

        private void ShowType(int pick)
        {
            if (pick < 0) return;
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
