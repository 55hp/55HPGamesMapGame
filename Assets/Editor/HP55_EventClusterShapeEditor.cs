// Assets/Editor/HP55_EventClusterShapeEditor.cs
// Editor window for painting and saving EventClusterShape ScriptableObjects.
// Displays a 5x5 grid with odd-q hex stagger: odd columns are shifted down by half
// a cell, matching the flat-top odd-q layout used by the game's hex map. Square
// buttons are used; the visual offset ensures painted clusters are spatially correct.
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

        private static readonly TileType[] ExcludedTypes = { TileType.Strada, TileType.Neutra, TileType.Boss };

        private TileType[] _paintableTypes;
        private Color[] _typeColors;

        private int[,] _cellState; // [row, col], -1 = empty
        private string _shapeName = "NewEventCluster";
        private Vector2 _scroll;

        [MenuItem("hp55games Tools/MapGame/Event Cluster Shape Editor")]
        public static void ShowWindow()
        {
            GetWindow<HP55_EventClusterShapeEditor>("Event Cluster Shape Editor");
        }

        private void OnEnable()
        {
            _paintableTypes = ((TileType[])Enum.GetValues(typeof(TileType)))
                .Where(t => !ExcludedTypes.Contains(t))
                .ToArray();

            _typeColors = new Color[_paintableTypes.Length];
            for (int i = 0; i < _paintableTypes.Length; i++)
            {
                float hue = (i * 137.508f % 360f) / 360f;
                _typeColors[i] = Color.HSVToRGB(hue, 0.65f, 0.95f);
            }

            if (_cellState == null)
                ResetGrid();
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
                        GUI.backgroundColor = _typeColors[state];

                    string label = state == -1
                        ? (isOrigin ? "○" : "")
                        : _paintableTypes[state].ToString().Substring(0, 1);

                    if (GUI.Button(cell, label))
                        _cellState[r, c] = (state + 1 >= _paintableTypes.Length) ? -1 : state + 1;
                }
            }

            GUI.backgroundColor = prevBg;
        }

        private void DrawLegend()
        {
            GUILayout.Label("Legenda:");
            var prevBg = GUI.backgroundColor;
            for (int i = 0; i < _paintableTypes.Length; i++)
            {
                GUI.backgroundColor = _typeColors[i];
                GUILayout.Box(_paintableTypes[i].ToString(), GUILayout.Width(120), GUILayout.Height(18));
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

                tiles.Add(new EventClusterTileSpec
                {
                    RelativeQ = relCoord.Q,
                    RelativeR = relCoord.R,
                    Type      = _paintableTypes[state],
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
