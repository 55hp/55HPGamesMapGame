using System;
using System.Collections.Generic;
using System.Text;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Servizio della partita per la UI (IRunSession), sullo stesso GameObject di HexGridController.
    /// Se nessuna UI definitiva (IRunHud, prefab UIRunHUD) è attiva, disegna la UI di prototipo IMGUI:
    /// eccezione consapevole alla regola 10, resta solo come ripiego.
    /// </summary>
    [RequireComponent(typeof(HexGridController))]
    public sealed class RunSession : MonoBehaviour, IRunSession
    {
        [Tooltip("Altezza di riferimento: l'interfaccia di prototipo scala con lo schermo.")]
        [SerializeField] private float _referenceHeight = 1080f;
        [SerializeField] private int _logLines = 5;
        [Tooltip("Camera che inquadra la griglia: serve per le etichette di righe e colonne.")]
        [SerializeField] private Camera _camera;

        private HexGridController _grid;
        private HexGridViewSpawner _spawner;
        private readonly List<string> _log = new List<string>();
        private GUIStyle _box, _label, _button, _title, _small, _lineLabel, _lineLabelUsed;

        private void Awake()
        {
            _grid = GetComponent<HexGridController>();
            _spawner = GetComponent<HexGridViewSpawner>();
            if (_camera == null) Debug.LogError("[RunSession] _camera non assegnata: niente etichette di righe e colonne.", this);
            ServiceRegistry.Register<IRunSession>(this);
        }

        // ---------------------------------------------------------------- IRunSession

        public RulesEngine Engine => _grid != null ? _grid.Engine : null;
        public event Action<IReadOnlyList<RuleEvent>> RuleEventsRaised;
        public event Action SessionStarted;
        public bool Submit(string optionId) => _grid.Submit(optionId);
        public bool IsAwaitingEndConfirm => _grid.IsAwaitingEndConfirm;
        public void ConfirmEnd() => _grid.ConfirmEnd();
        public void BlockBoardInput() => _grid.InputBlockedUntilFrame = Time.frameCount + 3;

        public bool TryGetScreenPoint(int col, int row, out Vector2 screenPoint)
        {
            screenPoint = default;
            if (_camera == null || _spawner == null) return false;
            var p = _camera.WorldToScreenPoint(_spawner.GetWorldPosition(HexCoord.FromOffsetOddQ(col, row)));
            screenPoint = new Vector2(p.x, p.y);
            return true;
        }

        private bool HasFinalHud =>
            ServiceRegistry.TryResolve(out IRunHud hud) && hud is MonoBehaviour mb && mb != null && mb.isActiveAndEnabled;
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

        private void OnGridInitialized()
        {
            _log.Clear();
            SessionStarted?.Invoke();
        }

        private void Update()
        {
            // Mentre c'è una scelta i tocchi servono ai pulsanti, non alla griglia; dopo un clic su un pulsante
            // si lascia un margine di qualche frame perché il rilascio non scopra la tile sotto.
            if (_grid.CurrentDecision != null && !HasFinalHud) _grid.InputBlockedUntilFrame = Time.frameCount + 3;
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
            RuleEventsRaised?.Invoke(events);
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
                case RuleEventType.LineCompleted: return $"Linea {e.Line.Label(_grid.Engine.Config.Height)} completata.";
                case RuleEventType.LevelUp: return $"Livello {e.Amount}!";
                case RuleEventType.ItemEquipped: return $"Equipaggiato: {Txt.Item(e.ItemId)}.";
                case RuleEventType.ItemDiscarded: return $"Perso: {Txt.Item(e.ItemId)}.";
                case RuleEventType.KeyObtained: return e.Key == KeyKind.Silver ? "Chiave d'argento ottenuta." : "Chiave d'oro ottenuta.";
                case RuleEventType.ChestOpened: return "Forziere aperto.";
                case RuleEventType.ChestVanished: return "Il forziere svanisce: serviva la chiave.";
                case RuleEventType.NpcKilled: return $"{Txt.Kind(e.Kind)} ucciso.";
                case RuleEventType.NpcFled: return $"{Txt.Kind(e.Kind)} fugge: -{e.Amount} HP.";
                case RuleEventType.OfferBought: return $"Acquistato: {Txt.Offer(e.OfferId)} (-{e.Amount} monete).";
                case RuleEventType.ClueUsed: return $"Indizio {Txt.Clue(e.ClueTarget)} su {e.Line.Label(_grid.Engine.Config.Height)}: {e.Amount}.";
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
            if (engine == null || !engine.Layout.Success || HasFinalHud) return;
            float scale = Screen.height / _referenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = _referenceHeight;
            EnsureStyles();

            DrawLineLabels(engine, scale);
            DrawHud(engine, w);
            var d = engine.CurrentDecision;
            if (d != null) DrawDecision(engine, d, w, h);
            else DrawLog(w, h);
            if (d == null && engine.State.Status != RunStatus.Playing) DrawEnd(engine, w, h);
        }

        /// <summary>Etichette 1–10 / A–J ai bordi della griglia, con il risultato dell'indizio se la linea è usata (05 §5).</summary>
        private void DrawLineLabels(RulesEngine e, float scale)
        {
            if (_camera == null || _spawner == null) return;
            int w = e.Config.Width, h = e.Config.Height;
            Vector2 P(int col, int row)
            {
                var sp = _camera.WorldToScreenPoint(_spawner.GetWorldPosition(HexCoord.FromOffsetOddQ(col, row)));
                return new Vector2(sp.x / scale, (Screen.height - sp.y) / scale);
            }

            Vector2 c00 = P(0, 0), c10 = P(1, 0), c01 = P(0, 1);
            Vector2 colStep = c10 - c00, rowStep = c01 - c00;
            for (int r = 0; r < h; r++)
            {
                var line = LineRef.Row(r);
                // Solo lo spostamento orizzontale: le colonne dispari sono sfalsate di mezza cella in verticale.
                Vector2 pos = P(0, r) - new Vector2(colStep.x * 1.1f, 0f);
                DrawLabel(e, line, pos);
            }
            Vector2 colBase = c00 - rowStep * 1.0f;
            for (int c = 0; c < w; c++)
            {
                var line = LineRef.Col(c);
                Vector2 pos = new Vector2(P(c, 0).x, colBase.y);
                DrawLabel(e, line, pos);
            }
        }

        private void DrawLabel(RulesEngine e, LineRef line, Vector2 center)
        {
            string text = line.Label(e.Config.Height);
            bool used = e.ClueResults.TryGetValue(line, out var res);
            if (used) text += "\n" + Txt.ClueShort(res.target) + res.count;
            var style = used ? _lineLabelUsed : _lineLabel;
            var size = style.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(center.x - size.x / 2, center.y - size.y / 2, size.x, size.y), text, style);
        }

        private void DrawHud(RulesEngine e, float w)
        {
            var s = e.State;
            var sb = new StringBuilder();
            sb.Append($"HP {s.Hp}/{s.MaxHp}   Att {s.Attack}   Monete {s.Coins}   Liv {s.Level} (XP {s.Xp}/{s.Level * e.Config.XpPerLevel})");
            if (s.HasSilverKey || s.HasGoldKey) sb.Append("   Chiavi: ").Append(s.HasSilverKey ? "argento " : "").Append(s.HasGoldKey ? "oro" : "");
            if (s.CoinBonusPerGain > 0) sb.Append($"   +{s.CoinBonusPerGain} monete/guadagno");
            sb.Append($"\nMani {Txt.Item(s.MainHand?.Id)} + {Txt.Item(s.OffHand?.Id)} · Trinket {s.Trinkets.Count}");
            sb.Append($"\nObiettivo: Boss (bordo viola, Difesa {e.Config.Content(ContentKind.Boss).Defense}).");
            if (e.ClueResults.Count > 0)
            {
                sb.Append("  Indizi: ");
                bool first = true;
                foreach (var kv in e.ClueResults)
                {
                    if (!first) sb.Append(" · ");
                    sb.Append($"{kv.Key.Label(e.Config.Height)} {Txt.Clue(kv.Value.target)} {kv.Value.count}");
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
                if (LayoutButton(line.Label(_grid.Engine.Config.Height), GUILayout.Width(58), GUILayout.Height(58)) && opt != null) _grid.Submit(opt.Id);
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
                case OptionIds.EquipAll: return "Equipaggia (lasci tutto ciò che hai in mano)";
                case OptionIds.Discard: return "Lascialo";
            }
            if (OptionIds.IsEquip(o.Id)) return $"Equipaggia (lasci {Txt.Item(o.ItemId)})";
            string reason = o.Enabled ? "" : o.DisabledReasonKey == "ui.npc.reason.already_bought" ? "  [già comprato]" : "  [monete insufficienti]";
            return $"{Txt.Offer(o.Id)} — {o.Price} monete{reason}";
        }

        private void DrawEnd(RulesEngine e, float w, float h)
        {
            var s = e.State;
            string t = s.Status == RunStatus.Won ? "VITTORIA" : s.LossReason == LossReason.BossFled ? "SCONFITTA — il Boss è fuggito" : "SCONFITTA";
            GUI.Box(new Rect((w - 620) / 2, h - 380, 620, 220), $"{t}\nLivello {s.Level} · Monete {s.Coins} · Attacco {s.Attack}", _title);
            if (_grid.IsAwaitingEndConfirm && GUI.Button(new Rect((w - 300) / 2, h - 240, 300, 64), "Continua", _button))
            {
                _grid.InputBlockedUntilFrame = Time.frameCount + 3;
                _grid.ConfirmEnd();
            }
        }

        private void EnsureStyles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(12, 12, 10, 10) };
            _title = new GUIStyle(GUI.skin.box) { fontSize = 30, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, padding = new RectOffset(12, 12, 14, 10) };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            _lineLabel = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _lineLabel.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            _lineLabelUsed = new GUIStyle(_lineLabel) { fontSize = 20 };
            _lineLabelUsed.normal.textColor = new Color(1f, 0.85f, 0.3f, 1f);
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

            /// <summary>Sigla breve per le etichette di bordo.</summary>
            public static string ClueShort(ClueTarget t)
            {
                switch (t)
                {
                    case ClueTarget.Boss: return "Bo";
                    case ClueTarget.MiniBoss: return "MB";
                    case ClueTarget.MonstersNormal: return "Mo";
                    case ClueTarget.Witch: return "St";
                    case ClueTarget.Hunter: return "Ca";
                    case ClueTarget.Smith: return "Fa";
                    case ClueTarget.Inn: return "Lo";
                    case ClueTarget.Trader: return "Me";
                    case ClueTarget.Bush: return "Ar";
                    case ClueTarget.Fountain: return "Fo";
                }
                return "?";
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
