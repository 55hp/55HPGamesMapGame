using System.Collections.Generic;
using hp55games.MapGame.Features.Configs;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEditor;
using UnityEngine;

namespace hp55games.MapGame.Editor
{
    [CustomEditor(typeof(MapGenerationConfig))]
    public sealed class MapGenerationConfigEditor : UnityEditor.Editor
    {
        // ── Constants ────────────────────────────────────────────────────────

        private const int CellPx  = 14;
        private const int CellGap =  1;

        // ── Color palette: one distinct color per TileType ───────────────────
        // Revisione 2026-08-05 per l'allineamento GDD 2026-07-25: Path/Shop erano alias
        // [Obsolete] di Road/Trader sullo stesso int, quindi qui restano una voce sola a
        // testa (una Dictionary<TileType,Color> non puo' avere due chiavi con lo stesso
        // valore sottostante). Boss/Npc/Goods/Miniboss/Chance restano in tavolozza per
        // sicurezza (dati serializzati vecchi potrebbero ancora referenziarli, es.
        // LevelConfig_Test_1.asset ha entry Npc/Goods non ancora sistemate) anche se il
        // generatore non li piazza piu'. Le 8 nuove voci (Bush/BeeHive/TurnipSprout/
        // MoneyBag/Inn/Witch/Farmer/Hunter) hanno colori placeholder scelti solo per
        // restare leggibili accanto agli altri, non dalla palette ufficiale Isle of
        // Lore 2 — -- Franci TASK -- se vuoi allinearli alla palette asset pack, non
        // l'ho fatto qui.
        //   Start marker: white inner square. End marker: black inner square.

        private static readonly Dictionary<TileType, Color> TileColors = new()
        {
            { TileType.Void,         new Color(0.500f, 0.500f, 0.500f) },  // #808080 — placeholder, vero no-op
            { TileType.Road,         new Color(0.863f, 0.725f, 0.373f) },  // #dcb95f — ex Path
            { TileType.Enemy,        new Color(0.612f, 0.278f, 0.255f) },  // #9c4741 — ex Battaglia
            { TileType.Trap,         new Color(0.369f, 0.251f, 0.639f) },  // #5e40a3 — ex Chance
            { TileType.Fountain,     new Color(0.243f, 0.588f, 0.780f) },  // #3e96c7
            { TileType.Tree,         new Color(0.322f, 0.494f, 0.243f) },  // #527e3e
            { TileType.Key,          new Color(0.816f, 0.706f, 0.235f) },  // #d0b43c
            { TileType.Chest,        new Color(0.616f, 0.443f, 0.196f) },  // #9d7132
            { TileType.Trader,       new Color(0.847f, 0.694f, 0.314f) },  // #d8b150 — ex Shop
            { TileType.Bush,         new Color(0.537f, 0.600f, 0.329f) },  // #889954 — ex Goods
            { TileType.BeeHive,      new Color(0.827f, 0.616f, 0.129f) },  // #d39d21
            { TileType.TurnipSprout, new Color(0.706f, 0.816f, 0.353f) },  // #b4d05a
            { TileType.MoneyBag,     new Color(0.827f, 0.702f, 0.161f) },  // #d3b329
            { TileType.Inn,          new Color(0.314f, 0.694f, 0.847f) },  // #50b1d8 — ex Npc
            { TileType.Witch,        new Color(0.549f, 0.314f, 0.847f) },  // #8c50d8
            { TileType.Farmer,       new Color(0.463f, 0.663f, 0.286f) },  // #76a949
            { TileType.Hunter,       new Color(0.494f, 0.400f, 0.267f) },  // #7e6644
            { TileType.Npc,          new Color(0.314f, 0.694f, 0.847f) },  // #50b1d8 — legacy, vedi nota sopra
            { TileType.Goods,        new Color(0.537f, 0.600f, 0.329f) },  // #889954 — legacy
            { TileType.Chance,       new Color(0.369f, 0.251f, 0.639f) },  // #5e40a3 — legacy
            { TileType.Miniboss,     new Color(0.400f, 0.176f, 0.153f) },  // #662d27 — legacy, piu' scuro di Enemy
            { TileType.Boss,         new Color(0.086f, 0.086f, 0.086f) },  // #161616 — legacy
        };

        // Alpha multiplier per TileState (flagged as design decision):
        //   Scoperta = full (content resolved), Conosciuta = dimmed (position known, content pending),
        //   Sconosciuta = very dark (no information). Editor shows raw generator output before reveal.

        private static float StateAlpha(ExplorationState exploration, SpottingState spotting) => (exploration, spotting) switch
        {
            (ExplorationState.Explored,   SpottingState.Spotted)   => 1.00f, // ex TileState.Scoperta
            (ExplorationState.Unexplored, SpottingState.Spotted)   => 0.55f, // ex TileState.Conosciuta
            (ExplorationState.Unexplored, SpottingState.Unspotted) => 0.18f, // ex TileState.Sconosciuta
            _                                                       => 1.00f, // Explored+Unspotted: non dovrebbe mai accadere (il reveal marca sempre entrambi gli assi insieme), fallback come nell'originale
        };

        // ── State ────────────────────────────────────────────────────────────

        private MapGenerationResult _preview;

        // LevelConfig e' stato tolto da MapGenerationConfig (ora vive nel ConfigCatalog
        // ed e' passato a GenerateMap come parametro). La preview quindi ne ha bisogno
        // di uno esplicito: assegnabile qui, con default al primo LevelConfig trovato.
        private LevelConfig _previewLevel;

        // ElementCatalog (2026-08-05): il generatore ora risolve le specie da qui per
        // FoodRestore/CoinReward. Stesso pattern di _previewLevel: assegnabile a mano,
        // default al primo ElementCatalog trovato in progetto.
        private ElementCatalog _previewElementCatalog;

        // ── Inspector GUI ────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            if (_previewLevel == null)
                _previewLevel = FindFirstLevelConfig();

            _previewLevel = (LevelConfig)EditorGUILayout.ObjectField(
                "Level (preview)", _previewLevel, typeof(LevelConfig), false);

            if (_previewLevel == null)
                EditorGUILayout.HelpBox(
                    "Nessun LevelConfig assegnato: la preview genera con contenuto di default (nessuna istanza da piazzare). Assegnane uno per una preview realistica.",
                    MessageType.Warning);

            if (_previewElementCatalog == null)
                _previewElementCatalog = FindFirstElementCatalog();

            _previewElementCatalog = (ElementCatalog)EditorGUILayout.ObjectField(
                "Element Catalog (preview)", _previewElementCatalog, typeof(ElementCatalog), false);

            if (_previewElementCatalog == null)
                EditorGUILayout.HelpBox(
                    "Nessun ElementCatalog assegnato: le tessere content avranno FoodRestore/CoinReward a 0 (nessuna specie risolta). Assegnane uno per una preview realistica.",
                    MessageType.Warning);

            if (GUILayout.Button("Genera Preview"))
            {
                var cfg = (MapGenerationConfig)target;
                _preview = new MapGenerationService().GenerateMap(cfg, _previewLevel, _previewElementCatalog, cfg.Seed);
                Repaint();
            }

            if (_preview == null) return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            DrawLegend();
            EditorGUILayout.Space(4);
            DrawGrid();
        }

        // ── Grid rendering ───────────────────────────────────────────────────

        private void DrawGrid()
        {
            var cfg = (MapGenerationConfig)target;
            int w = cfg.Width;
            int h = cfg.Height;

            int    stride   = CellPx + CellGap;
            float  halfCell = CellPx * 0.5f;
            float  totalW   = w * stride + 4f;
            float  totalH   = h * stride + halfCell + 4f;  // extra half-cell for odd-column vertical shift

            // Reserve space for the whole grid in the inspector layout.
            Rect area = GUILayoutUtility.GetRect(totalW, totalH, GUILayout.ExpandWidth(false));

            if (Event.current.type != EventType.Repaint) return;

            foreach (var kvp in _preview.Tiles)
            {
                var coord = kvp.Key;
                var tile  = kvp.Value;

                coord.ToOffsetOddQ(out int col, out int row);

                // Odd columns are shifted down by half a cell to mimic flat-top hex layout.
                float yOffset = (col % 2 == 1) ? halfCell : 0f;
                float x = area.x + col * stride + 2f;
                float y = area.y + row * stride + yOffset + 2f;

                Color baseColor = TileColors.TryGetValue(tile.Type, out var c) ? c : Color.white;
                float alpha     = StateAlpha(tile.Exploration,tile.Spotting);
                EditorGUI.DrawRect(new Rect(x, y, CellPx, CellPx), baseColor * alpha);

                // Start tile: white inner dot. End/Enemy obiettivo: black inner dot.
                bool isStart = coord.Equals(_preview.StartCoord);
                bool isEnd   = coord.Equals(_preview.ObjectiveCoord);
                if (isStart || isEnd)
                {
                    const float pad = 4f;
                    var markerRect  = new Rect(x + pad, y + pad, CellPx - pad * 2f, CellPx - pad * 2f);
                    EditorGUI.DrawRect(markerRect, isStart ? Color.white : Color.black);
                }
            }
        }

        // ── Legend ───────────────────────────────────────────────────────────

        private void DrawLegend()
        {
            // Nota (2026-08-05): la tavolozza e' passata da 9 a 22 voci (nuovi TileType +
            // legacy mantenuti per sicurezza, vedi commento sopra TileColors). La riga
            // singola qui sotto e' la stessa struttura di prima, solo piu' lunga —
            // volutamente non ho aggiunto wrapping su piu' righe: e' codice Editor IMGUI
            // che non posso testare in questo ambiente, e la struttura originale gia'
            // funzionante e' piu' sicura di un wrapping scritto alla cieca. Se in Unity
            // risulta troppo larga, e' un cambiamento cosmetico facile da fare li'.
            EditorGUILayout.LabelField("Tipi", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var kvp in TileColors)
            {
                Rect swatch = GUILayoutUtility.GetRect(12f, 12f,
                    GUILayout.Width(12f), GUILayout.Height(12f));
                EditorGUI.DrawRect(swatch, kvp.Value);
                GUILayout.Space(2f);
                GUILayout.Label(kvp.Key.ToString(), EditorStyles.miniLabel);
                GUILayout.Space(6f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Stati  (α su colore base)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            DrawStateSwatch("Scoperta",    1.00f);
            DrawStateSwatch("Conosciuta",  0.55f);
            DrawStateSwatch("Sconosciuta", 0.18f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            DrawMarkerSwatch(Color.white, "Start (quadratino bianco)");
            DrawMarkerSwatch(Color.black, "End   (quadratino nero)");
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStateSwatch(string label, float alpha)
        {
            Rect r = GUILayoutUtility.GetRect(12f, 12f,
                GUILayout.Width(12f), GUILayout.Height(12f));
            // Show alpha concept as a grey swatch scaled by alpha.
            EditorGUI.DrawRect(r, new Color(alpha, alpha, alpha, 1f));
            GUILayout.Space(2f);
            GUILayout.Label($"{label} ({alpha:F2}α)", EditorStyles.miniLabel);
            GUILayout.Space(8f);
        }

        private static void DrawMarkerSwatch(Color col, string label)
        {
            Rect r = GUILayoutUtility.GetRect(12f, 12f,
                GUILayout.Width(12f), GUILayout.Height(12f));
            EditorGUI.DrawRect(r, col);
            GUILayout.Space(2f);
            GUILayout.Label(label, EditorStyles.miniLabel);
            GUILayout.Space(12f);
        }

        private static LevelConfig FindFirstLevelConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelConfig");
            if (guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
        }

        private static ElementCatalog FindFirstElementCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:ElementCatalog");
            if (guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ElementCatalog>(path);
        }

}
}
