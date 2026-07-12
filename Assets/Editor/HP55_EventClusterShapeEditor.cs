// Assets/Editor/HP55_EventClusterShapeEditor.cs
// Editor window for painting and saving EventClusterShape ScriptableObjects.
// Displays a 5x5 grid with odd-q hex stagger: odd columns are shifted down by half
// a cell, matching the flat-top odd-q layout used by the game's hex map. Square
// buttons are used; the visual offset ensures painted clusters are spatially correct.
//
// Revised 2026-07-10: painting now requires a LevelConfig reference. Paintable options
// come from LevelConfig.EventClusterTilesList (TileType + DifficultyLevel pairs) instead
// of the full TileType enum — each click cycles through eligible entries for that level,
// authoring stays per-cell manual, the LevelConfig only filters what's available.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.Editor.Tools.MapGame
{
    public class HP55_EventClusterShapeEditor : EditorWindow
    {
        private const int GridSize = 5;
        private const int Center = GridSize / 2; // = 2, the cluster origin cell

        private const float CellSize = 40f;
        private const float CellGap = 2f;
        private const float OddColShift = CellSize * 0.5f; // odd columns shift down

        private static readonly Color OffColor = Color.gray;
        private static readonly Color OriginTint = new Color(1f, 1f, 0.55f, 1f); // yellow tint for origin

        // Tipi esclusi anche se comparissero per errore in EventClusterTilesList: Strada e
        // Void sono strutturali (nessun DifficultyLevel), Boss e' piazzato deterministicamente
        // su End, mai autorato qui.
        private static readonly TileType[] ExcludedTypes = { TileType.Path, TileType.Void, TileType.Boss };

        [SerializeField] private LevelConfig _levelConfig;

        private LevelTileEntry[] _paintableEntries;
        private Color[] _entryColors;

        private int[,] _cellState; // [row, col], -1 = empty, altrimenti indice in _paintableEntries
        private string _shapeName = "NewEventCluster";
        private Vector2 _scroll;

        [MenuItem("hp55games Tools/MapGame/Event Cluster Shape Editor")]
        public static void ShowWindow()
        {
            GetWindow<HP55_EventClusterShapeEditor>("Event Cluster Shape Editor");
        }

        private void OnEnable()
        {
            RebuildPaintableEntries();

            if (_cellState == null)
                ResetGrid();
        }

        private void RebuildPaintableEntries()
        {
            var source = _levelConfig != null ? _levelConfig.EventClusterTilesList : null;

            _paintableEntries = source == null
                ? Array.Empty<LevelTileEntry>()
                : source.Where(e => !ExcludedTypes.Contains(e.Type)).ToArray();

            _entryColors = new Color[_paintableEntries.Length];
            for (int i = 0; i < _paintableEntries.Length; i++)
            {
                float hue = (i * 137.508f % 360f) / 360f;
                _entryColors[i] = Color.HSVToRGB(hue, 0.65f, 0.95f);
            }
        }

        private void ResetGrid()
        {
            _cellState = new int[GridSize, GridSize];
            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
                _cellState[r, c] = -1;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Event Cluster Shape Editor", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _levelConfig = (LevelConfig)EditorGUILayout.ObjectField("Level Config", _levelConfig, typeof(LevelConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPaintableEntries();
                ResetGrid();
            }

            if (_levelConfig == null)
            {
                EditorGUILayout.HelpBox("Assegna un LevelConfig per popolare le tipologie disponibili (EventClusterTilesList).", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_paintableEntries.Length == 0)
            {
                EditorGUILayout.HelpBox("EventClusterTilesList e' vuota su questo LevelConfig. Aggiungi almeno una entry Type/DifficultyLevel per poter dipingere.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            _shapeName = EditorGUILayout.TextField("Nome forma", _shapeName);

            EditorGUILayout.Space();

            DrawHexGrid();

            EditorGUILayout.Space();
            DrawLegend();
            EditorGUILayout.Space();

            if (GUILayout.Button("Salva come EventCluster"))
                SaveShape();

            if (GUILayout.Button("Pulisci griglia"))
                ResetGrid();

            EditorGUILayout.EndScrollView();
        }

        // Draws the 5x5 grid with odd-q stagger using absolute-positioned GUI.Button calls.
        // Columns are iterated first so the per-column y-offset can be applied cleanly.
        // _cellState[r, c] indexing is unchanged; only the draw position changes.
        private void DrawHexGrid()
        {
            float totalWidth  = GridSize * (CellSize + CellGap) - CellGap;
            float totalHeight = GridSize * (CellSize + CellGap) - CellGap + OddColShift;

            Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            var prevBg = GUI.backgroundColor;

            for (int c = 0; c < GridSize; c++)
            {
                float x        = gridRect.x + c * (CellSize + CellGap);
                float colShift = (c % 2 == 1) ? OddColShift : 0f;

                for (int r = 0; r < GridSize; r++)
                {
                    float y    = gridRect.y + colShift + r * (CellSize + CellGap);
                    Rect  cell = new Rect(x, y, CellSize, CellSize);

                    int state = _cellState[r, c];

                    bool isOrigin = (c == Center && r == Center);

                    if (state == -1)
                        GUI.backgroundColor = isOrigin ? OriginTint : OffColor;
                    else
                        GUI.backgroundColor = _entryColors[state];

                    string label = state == -1
                        ? (isOrigin ? "○" : "")
                        : _paintableEntries[state].Type.ToString().Substring(0, 1) + _paintableEntries[state].DifficultyLevel;

                    if (GUI.Button(cell, label))
                        _cellState[r, c] = (state + 1 >= _paintableEntries.Length) ? -1 : state + 1;
                }
            }

            GUI.backgroundColor = prevBg;
        }

        private void DrawLegend()
        {
            GUILayout.Label("Legenda:");
            var prevBg = GUI.backgroundColor;
            for (int i = 0; i < _paintableEntries.Length; i++)
            {
                GUI.backgroundColor = _entryColors[i];
                GUILayout.Box($"{_paintableEntries[i].Type} Lv{_paintableEntries[i].DifficultyLevel}", GUILayout.Width(120), GUILayout.Height(18));
            }
            GUI.backgroundColor = prevBg;
            EditorGUILayout.HelpBox("○ = cella di origine del cluster (Q=0, R=0)", MessageType.None);
        }

        private void SaveShape()
        {
            var tiles = new List<EventClusterTileSpec>();

            for (int r = 0; r < GridSize; r++)
            for (int c = 0; c < GridSize; c++)
            {
                int state = _cellState[r, c];
                if (state == -1) continue;

                int relCol   = c - Center;
                int relRow   = r - Center;
                var relCoord = HexCoord.FromOffsetOddQ(relCol, relRow);

                var entry = _paintableEntries[state];
                tiles.Add(new EventClusterTileSpec
                {
                    RelativeQ = relCoord.Q,
                    RelativeR = relCoord.R,
                    Type      = entry.Type,
                    DifficultyLevel = entry.DifficultyLevel,
                });
            }

            if (tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Event Cluster Shape Editor",
                    "Nessuna tessera dipinta, niente da salvare.", "Ok");
                return;
            }

            var asset = ScriptableObject.CreateInstance<EventClusterShape>();
            asset.ShapeName = _shapeName;
            asset.Tiles     = tiles.ToArray();

            const string folder = "Assets/MapGame/Content/EventClusters";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/MapGame/Content", "EventClusters");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_shapeName}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[HP55_EventClusterShapeEditor] Saved EventClusterShape → {path}");
            EditorUtility.DisplayDialog("Event Cluster Shape Editor", $"Salvato in {path}", "Ok");
        }
    }
}
