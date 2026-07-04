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
        // Colors sourced from the Isle of Lore 2 official asset pack palette.
        //   Start marker: white inner square. End marker: black inner square.

        private static readonly Dictionary<TileType, Color> TileColors = new()
        {
            { TileType.Strada,    new Color(0.863f, 0.725f, 0.373f) },  // #dcb95f
            { TileType.Battaglia, new Color(0.612f, 0.278f, 0.255f) },  // #9c4741
            { TileType.Trappola,  new Color(0.729f, 0.263f, 0.227f) },  // #ba433a
            { TileType.Risorsa,   new Color(0.537f, 0.600f, 0.329f) },  // #889954
            { TileType.NPC,       new Color(0.314f, 0.694f, 0.847f) },  // #50b1d8
            { TileType.Mistery,   new Color(0.369f, 0.251f, 0.639f) },  // #5e40a3
            { TileType.Boss,      new Color(0.086f, 0.086f, 0.086f) },  // #161616
        };

        // Alpha multiplier per TileState (flagged as design decision):
        //   Scoperta = full (content resolved), Conosciuta = dimmed (position known, content pending),
        //   Sconosciuta = very dark (no information). Editor shows raw generator output before reveal.

        private static float StateAlpha(TileState s) => s switch
        {
            TileState.Scoperta    => 1.00f,
            TileState.Conosciuta  => 0.55f,
            TileState.Sconosciuta => 0.18f,
            _                     => 1.00f,
        };

        // ── State ────────────────────────────────────────────────────────────

        private MapGenerationResult _preview;

        // ── Inspector GUI ────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Genera Preview"))
            {
                var cfg = (MapGenerationConfig)target;
                _preview = new AestheticClusterMapGenerator().Generate(cfg.Width, cfg.Height, cfg.Seed);
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
                float alpha     = StateAlpha(tile.State);
                EditorGUI.DrawRect(new Rect(x, y, CellPx, CellPx), baseColor * alpha);

                // Start tile: white inner dot. End/Boss tile: black inner dot.
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
    }
}
