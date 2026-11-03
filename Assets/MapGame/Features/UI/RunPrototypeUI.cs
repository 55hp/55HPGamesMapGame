using System.Collections.Generic;
using System.Text;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.MapGame.Rules;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// UI di PROTOTIPO v2 (IMGUI): HUD, decisioni (NPC, level up, indizi, oggetti), indizi usati e
    /// registro degli eventi. Serve a giocare e bilanciare prima che esistano i prefab della spec 0006.
    /// Eccezione consapevole alle regole 10 (testi hardcoded, niente TMP): va rimossa quando
    /// HUD e pop-up v2 sono su prefab. Si aggiunge allo stesso GameObject di HexGridController.
    /// </summary>
    [RequireComponent(typeof(HexGridController))]
    public sealed class RunPrototypeUI : MonoBehaviour
    {
        [Tooltip("Altezza di riferimento: l'interfaccia scala con lo schermo.")]
        [SerializeField] private float _referenceHeight = 1080f;
        [SerializeField] private int _logLines = 5;

        private HexGridController _grid;
        private readonly List<string> _log = new List<string>();
        private GUIStyle _box, _label, _button, _title, _small;

        private void Awake() => _grid = GetComponent<HexGridController>();
        private void OnEnable()
        {
            _grid.RuleEventsRaised += OnEvents;
            _grid.GridInitialized += OnGridInitialized;
        }

        private void OnDisable()
        {
            _grid.RuleEventsRaised -= OnEvents;
            _grid.GridInitialized -= OnGridInitialized;
        }

        private void OnGridInitialized() => _log.Clear();

        private void Update()
        {
            // Mentre c'è una scelta i tocchi servono ai pulsanti, non alla griglia; dopo un clic su un pulsante
            // si lascia un margine di qualche frame perché il rilascio non scopra la tile sotto.
            if (_grid.CurrentDecision != null) _grid.InputBlockedUntilFrame = Time.frameCount + 3;
        }

        private bool LayoutButton(string text, params GUILayoutOption[] opts)
        {
            bool pressed = GUILayout.Button(text, _button, opts);
            if (pressed) _grid.InputBlockedUntilFrame = Time.frameCount + 3;
            return pressed;
        }

        // ---------------------------------------------------------------- registro eventi

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            foreach (var e in events)
            {
                string line = Describe(e);
                if (line == null) continue;
                _log.Add(line);
                if (_log.Count > 40) _log.RemoveAt(0);
            }
        }

        private string Describe(RuleEvent e)
        {
            switch (e.Type)
            {
                case RuleEventType.MissionStarted: return $"Missione: caccia al Boss. Indizi di partenza su {Txt.Clue(e.ClueTarget)}.";
                case RuleEventType.EncounterWon: return $"{Txt.Kind(e.Kind)} sconfitto (Attacco {e.Attack} ≥ Difesa {e.Defense}).";
                case RuleEventType.EncounterLost: return $"{Txt.Kind(e.Kind)} fugge: Attacco {e.Attack} < Difesa {e.Defense}, -{e.Amount} HP.";
                case RuleEventType.TrapTriggered: return $"Trappola! -{e.Amount} HP.";
                case RuleEventType.TrapDisarmed: return "Trappola disinnescata dagli Attrezzi.";
                case RuleEventType.Healed: return $"+{e.Amount} HP.";
                case RuleEventType.CoinsGained: return $"+{e.Amount} monete.";
                case RuleEventType.XpGained: return $"+{e.Amount} XP.";
                case RuleEventType.LineCompleted: return $"Linea {e.Line.Label} completata.";
                case RuleEventType.LevelUp: return $"Livello {e.Amount}!";
                case RuleEventType.ItemEquipped: return $"Equipaggiato: {Txt.Item(e.ItemId)}.";
                case RuleEventType.ItemDiscarded: return $"Perso: {Txt.Item(e.ItemId)}.";
                case RuleEventType.KeyObtained: return e.Key == KeyKind.Silver ? "Chiave d'argento ottenuta." : "Chiave d'oro ottenuta.";
                case RuleEventType.ChestOpened: return "Forziere aperto.";
                case RuleEventType.ChestVanished: return "Il forziere svanisce: serviva la chiave.";
                case RuleEventType.NpcKilled: return $"{Txt.Kind(e.Kind)} ucciso.";
                case RuleEventType.NpcFled: return $"{Txt.Kind(e.Kind)} fugge: -{e.Amount} HP.";
                case RuleEventType.OfferBought: return $"Acquistato: {Txt.Offer(e.OfferId)} (-{e.Amount} monete).";
                case RuleEventType.ClueUsed: return $"Indizio {Txt.Clue(e.ClueTarget)} su {e.Line.Label}: {e.Amount}.";
                case RuleEventType.ClueLost: return "Indizio perso: nessuna linea libera.";
                case RuleEventType.TileTypeShown: return $"La Lanterna rivela: {Txt.Kind(e.Kind)}.";
                case RuleEventType.GameWon: return "VITTORIA! Il Boss è sconfitto.";
                case RuleEventType.GameLost: return e.LossReason == LossReason.BossFled ? "SCONFITTA: il Boss è fuggito." : "SCONFITTA: sei morto.";
                default: return null;
            }
        }

        // ---------------------------------------------------------------- disegno

        private void OnGUI()
        {
            var engine = _grid.Engine;
            if (engine == null || !engine.Layout.Success) return;
            float scale = Screen.height / _referenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = _referenceHeight;
            EnsureStyles();

            DrawHud(engine, w);
            var d = engine.CurrentDecision;
            if (d != null) DrawDecision(engine, d, w, h);
            else DrawLog(w, h);
            if (d == null && engine.State.Status != RunStatus.Playing) DrawEnd(engine, w, h);
        }

        private void DrawHud(RulesEngine e, float w)
        {
            var s = e.State;
            var sb = new StringBuilder();
            sb.Append($"HP {s.Hp}/{s.MaxHp}   Att {s.Attack}   Monete {s.Coins}   Liv {s.Level} (XP {s.Xp}/{s.Level * e.Config.XpPerLevel})");
            if (s.HasSilverKey || s.HasGoldKey) sb.Append("   Chiavi: ").Append(s.HasSilverKey ? "argento " : "").Append(s.HasGoldKey ? "oro" : "");
            if (s.CoinBonusPerGain > 0) sb.Append($"   +{s.CoinBonusPerGain} monete/guadagno");
            sb.Append($"\nArma {Txt.Item(s.Weapon?.Id)} · Anello {Txt.Item(s.Ring?.Id)} · Trinket {Txt.Item(s.Trinket?.Id)}");
            sb.Append($"\nObiettivo: Boss (bordo viola, Difesa {e.Config.Content(ContentKind.Boss).Defense}).");
            if (e.ClueResults.Count > 0)
            {
                sb.Append("  Indizi: ");
                bool first = true;
                foreach (var kv in e.ClueResults)
                {
                    if (!first) sb.Append(" · ");
                    sb.Append($"{kv.Key.Label} {Txt.Clue(kv.Value.target)} {kv.Value.count}");
                    first = false;
                }
            }
            float height = _box.CalcHeight(new GUIContent(sb.ToString()), w - 20) + 4;
            GUI.Box(new Rect(10, 10, w - 20, height), sb.ToString(), _box);
        }

        private void DrawLog(float w, float h)
        {
            var sb = new StringBuilder();
            for (int i = Mathf.Max(0, _log.Count - _logLines); i < _log.Count; i++) sb.AppendLine(_log[i]);
            GUI.Box(new Rect(10, h - 200, w - 20, 190), sb.ToString(), _box);
        }

        private void DrawDecision(RulesEngine e, Decision d, float w, float h)
        {
            float pw = Mathf.Min(w - 20, 760);
            float ph = d.Kind == DecisionKind.ClueLine ? 260 : 70 + d.Options.Count * 62;
            var r = new Rect((w - pw) / 2, h - ph - 10, pw, ph);
            GUI.Box(r, Title(e, d), _title);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 56, r.width - 24, r.height - 62));

            if (d.Kind == DecisionKind.ClueLine)
            {
                GUILayout.Label("Scegli una riga (1–10) o una colonna (A–J) libera: vedrai quanti ce ne sono.", _small);
                DrawLineButtons(d, true);
                DrawLineButtons(d, false);
            }
            else
            {
                foreach (var o in d.Options)
                {
                    GUI.enabled = o.Enabled;
                    if (LayoutButton(OptionLabel(e, d, o), GUILayout.Height(56))) _grid.Submit(o.Id);
                    GUI.enabled = true;
                }
            }
            GUILayout.EndArea();
        }

        private void DrawLineButtons(Decision d, bool rows)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(rows ? "Righe" : "Colonne", _label, GUILayout.Width(100));
            for (int i = 0; i < 10; i++)
            {
                var line = rows ? LineRef.Row(i) : LineRef.Col(i);
                var opt = d.Find(OptionIds.Line(line));
                GUI.enabled = opt != null;
                if (LayoutButton(line.Label, GUILayout.Width(58), GUILayout.Height(58)) && opt != null) _grid.Submit(opt.Id);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        private string Title(RulesEngine e, Decision d)
        {
            switch (d.Kind)
            {
                case DecisionKind.Npc: return $"{Txt.Kind(d.Npc)} — una sola scelta";
                case DecisionKind.LevelUp: return $"Livello {e.State.Level}: scegli un potenziamento";
                case DecisionKind.ClueLine: return $"Indizio: {Txt.Clue(d.ClueTarget)}";
                case DecisionKind.Item: return $"Trovato: {Txt.Item(d.NewItemId)}";
            }
            return "";
        }

        private string OptionLabel(RulesEngine e, Decision d, DecisionOption o)
        {
            switch (o.Id)
            {
                case OptionIds.Attack: return $"Attacca (Difesa {e.Config.NpcDefense}, il tuo Attacco {e.State.Attack})";
                case OptionIds.Close: return "Vai via";
                case OptionIds.LevelAttack: return $"+{e.Config.LevelUpAttack} Attacco";
                case OptionIds.LevelCoins: return $"+{e.Config.LevelUpCoinBonus} moneta a ogni guadagno";
                case OptionIds.LevelMaxHp: return $"+{e.Config.LevelUpMaxHp} HP massimo";
                case OptionIds.Equip: return $"Equipaggia (perdi {Txt.Item(d.CurrentItemId)})";
                case OptionIds.Discard: return $"Lascialo (tieni {Txt.Item(d.CurrentItemId)})";
            }
            string reason = o.Enabled ? "" : o.DisabledReasonKey == "ui.npc.reason.already_bought" ? "  [già comprato]" : "  [monete insufficienti]";
            return $"{Txt.Offer(o.Id)} — {o.Price} monete{reason}";
        }

        private void DrawEnd(RulesEngine e, float w, float h)
        {
            var s = e.State;
            string t = s.Status == RunStatus.Won ? "VITTORIA" : s.LossReason == LossReason.BossFled ? "SCONFITTA — il Boss è fuggito" : "SCONFITTA";
            GUI.Box(new Rect((w - 620) / 2, h - 380, 620, 150), $"{t}\nLivello {s.Level} · Monete {s.Coins} · Attacco {s.Attack}", _title);
        }

        private void EnsureStyles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(12, 12, 10, 10) };
            _title = new GUIStyle(GUI.skin.box) { fontSize = 30, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, padding = new RectOffset(12, 12, 14, 10) };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        }

        // ---------------------------------------------------------------- testi provvisori

        private static class Txt
        {
            public static string Kind(ContentKind k)
            {
                switch (k)
                {
                    case ContentKind.Boss: return "Boss";
                    case ContentKind.Monster1: return "Piccolo mostro";
                    case ContentKind.Monster2: return "Mostro";
                    case ContentKind.MiniBossWeapon:
                    case ContentKind.MiniBossSilverKey:
                    case ContentKind.MiniBossGoldKey:
                    case ContentKind.MiniBossCoins: return "Mini-boss";
                    case ContentKind.Trap1:
                    case ContentKind.Trap2: return "Trappola";
                    case ContentKind.Bush: return "Arbusto";
                    case ContentKind.ChestSimpleBerries:
                    case ContentKind.ChestSimpleWeapon:
                    case ContentKind.ChestSimpleCoins: return "Forziere";
                    case ContentKind.ChestSilver: return "Forziere d'argento";
                    case ContentKind.ChestGold: return "Forziere d'oro";
                    case ContentKind.Fountain: return "Fontana";
                    case ContentKind.Witch: return "Strega";
                    case ContentKind.Hunter: return "Cacciatore";
                    case ContentKind.Smith: return "Fabbro";
                    case ContentKind.Inn: return "Locanda";
                    case ContentKind.Trader: return "Mercante";
                    case ContentKind.Farmer: return "Contadino";
                    case ContentKind.Villagers: return "Paesani";
                    default: return k.ToString();
                }
            }

            public static string Clue(ClueTarget t)
            {
                switch (t)
                {
                    case ClueTarget.MiniBoss: return "Mini-boss";
                    case ClueTarget.MonstersNormal: return "Mostri";
                    case ClueTarget.Witch: return "Strega";
                    case ClueTarget.Hunter: return "Cacciatore";
                    case ClueTarget.Smith: return "Fabbro";
                    case ClueTarget.Inn: return "Locanda";
                    case ClueTarget.Trader: return "Mercante";
                    case ClueTarget.Bush: return "Arbusti";
                    case ClueTarget.Fountain: return "Fontana";
                    default: return t.ToString();
                }
            }

            public static string Item(string id)
            {
                switch (id)
                {
                    case null: return "—";
                    case RulesConfigDefaults.Knife: return "Coltello (+1)";
                    case RulesConfigDefaults.WeaponL2: return "Spada corta (+2)";
                    case RulesConfigDefaults.WeaponL3: return "Spada (+3)";
                    case RulesConfigDefaults.WeaponL4: return "Spada d'argento (+4)";
                    case RulesConfigDefaults.HunterBow: return "Arco del Cacciatore (+5)";
                    case RulesConfigDefaults.SmithHammer: return "Martello del Fabbro (+5)";
                    case RulesConfigDefaults.WitchStaff: return "Bastone della Strega (+5)";
                    case RulesConfigDefaults.MiniBossWeapon: return "Ascia del Campione (+5)";
                    case RulesConfigDefaults.Dragonslayer: return "Ammazzadraghi (+6)";
                    case RulesConfigDefaults.Ring2: return "Anello (+2)";
                    case RulesConfigDefaults.HunterTools: return "Attrezzi del Cacciatore";
                    case RulesConfigDefaults.Lantern: return "Lanterna";
                    default: return id;
                }
            }

            public static string Offer(string id)
            {
                switch (id)
                {
                    case "hunter_game": return "Cacciagione (+3 HP)";
                    case "hunter_tools": return "Attrezzi del Cacciatore (le trappole non scattano)";
                    case "smith_upgrade": return "Potenziamento (+1 Attacco, una volta)";
                    case "witch_ring": return "Anello (+2 Attacco)";
                    case "witch_lantern": return "Lanterna (svela tile vicine)";
                    case "inn_rest": return "Riposo (cura completa)";
                    case "trader_weapon": return "Spada (+3 Attacco)";
                    case "farmer_food": return "Cibo (+1 HP)";
                    case "villagers_clue_fountain": return "Indizio: Fontana";
                    case "villagers_clue_smith": return "Indizio: Fabbro";
                    case "villagers_clue_bush": return "Indizio: Arbusti";
                }
                if (id != null && id.EndsWith("_1")) return "1 indizio: " + ClueOf(id);
                if (id != null && id.EndsWith("_2")) return "2 indizi: " + ClueOf(id);
                return id;
            }

            private static string ClueOf(string id)
            {
                if (id.StartsWith("hunter")) return "Mini-boss";
                if (id.StartsWith("smith")) return "Locanda";
                if (id.StartsWith("witch")) return "Cacciatore";
                if (id.StartsWith("inn_clue_trader")) return "Mercante";
                if (id.StartsWith("inn_clue_monsters")) return "Mostri";
                if (id.StartsWith("trader")) return "Boss";
                if (id.StartsWith("farmer")) return "Arbusti";
                return "?";
            }
        }
    }
}
