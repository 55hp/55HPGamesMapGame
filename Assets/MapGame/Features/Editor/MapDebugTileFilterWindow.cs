// MapDebugTileFilterWindow.cs
// Finestra editor per il debug del filtro tile sulla mappa in Play Mode.
// Ogni booleano attivato fa sì che le tile che soddisfano almeno una delle
// caratteristiche selezionate abbiano il proprio root GameObject attivo; le
// altre vengono disattivate. Con tutti i booleani a false (default) tutta la
// visibilità viene ripristinata.
// Colonne stat per ogni riga:
//   #   — conteggio tessere di quella categoria
//   %   — percentuale sul totale (1 decimale)
//   M   — somma algebrica di MoneteGained
//   C   — somma algebrica di FoodRestore  (Cibo)
//   HP  — somma algebrica di HpRestore
//
// Apri con: hp55games Tools / MapGame / Map Debug Tile Filter
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Editor
{
    public sealed class MapDebugTileFilterWindow : EditorWindow
    {
        // ── TileType attivi (non-obsoleti) ────────────────────────────────────────
        private static readonly TileType[] ActiveTileTypes =
        {
            TileType.Void,
            TileType.Road,
            TileType.Trader,
            TileType.Enemy,
            TileType.Trap,
            TileType.Fountain,
            TileType.Tree,
            TileType.Key,
            TileType.Chest,
            TileType.Bush,
            TileType.BeeHive,
            TileType.TurnipSprout,
            TileType.MoneyBag,
            TileType.Inn,
            TileType.Witch,
            TileType.Farmer,
            TileType.Hunter,
        };

        // ── Larghezze colonne ─────────────────────────────────────────────────────
        private const float W_COUNT = 32f;
        private const float W_PCT   = 50f;

        // ── Stato filtri ──────────────────────────────────────────────────────────
        private readonly Dictionary<TileType, bool> _typeFilters =
            new Dictionary<TileType, bool>();

        // Indice 0 → DifficultyLevel 0 (Road/Void, Black) … indice 6 → DifficultyLevel 6
        private readonly bool[] _dlFilters = new bool[7];

        private bool _filterStart;
        private bool _filterEnd;

        /// <summary>
        /// Attivo → mostra solo le tile che appartengono a un piazzamento EventCluster
        /// multi-tile (centro + ring da 7, vedi AestheticClusterMapGenerator.
        /// TryBuildProceduralCluster), escluse le tessere singole isolate. EventPlacementId
        /// e' condiviso sia dai cluster multi-tile sia dalle singole (assegnato per ogni
        /// piazzamento in PlaceEventClusters/PatchResidualGaps) — non basta controllare
        /// EventPlacementId >= 0, serve raggruppare per id e contare quante tile lo
        /// condividono: >1 = cluster vero, ==1 = singola isolata. Vedi
        /// _clusterPlacementIds/ComputeStats.
        /// </summary>
        private bool _filterEventCluster;

        // ── Cache riferimenti runtime ─────────────────────────────────────────────
        private HexGridViewSpawner _spawner;
        private HexGridController  _controller;

        // ── Statistiche — conteggi ────────────────────────────────────────────────
        private readonly Dictionary<TileType, int> _typeCount =
            new Dictionary<TileType, int>();
        private readonly int[] _dlCount = new int[7]; // indice 0 = DL0 … indice 6 = DL6
        private int  _startCount;
        private int  _endCount;
        private int  _totalCount;

        // EventPlacementId → quante tile lo condividono, tally temporaneo di ComputeStats.
        private readonly Dictionary<int, int> _eventPlacementCounts = new Dictionary<int, int>();
        // Sottoinsieme di quegli id con conteggio > 1 — i cluster multi-tile veri, letto da ApplyFilter.
        private readonly HashSet<int> _clusterPlacementIds = new HashSet<int>();
        private int _clusterTileCount;

        // ── Statistiche — totali risorse (somma su tutta la mappa) ───────────────
        private int _totalMonete;
        private int _totalCibo;
        private int _totalHp;

        private bool _statsValid;

        // ── Layout ────────────────────────────────────────────────────────────────
        private Vector2    _scroll;
        private bool       _foldTileType   = true;
        private bool       _foldDifficulty = true;
        private bool       _foldSpecial    = true;
        private GUIStyle   _numStyle;
        private GUIStyle   _hdrStyle;
        private GUIStyle   _posStyle;
        private GUIStyle   _negStyle;

        // ─────────────────────────────────────────────────────────────────────────

        [MenuItem("hp55games Tools/MapGame/Map Debug Tile Filter")]
        public static void Open()
        {
            var w = GetWindow<MapDebugTileFilterWindow>("Map Debug Tile Filter");
            w.minSize = new Vector2(370f, 520f);
        }

        private void OnEnable()
        {
            InitFilters();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _spawner    = null;
                _controller = null;
                _statsValid = false;
                InitFilters();
                Repaint();
            }
        }

        private void InitFilters()
        {
            foreach (var t in ActiveTileTypes)
                _typeFilters[t] = false;

            for (int i = 0; i < _dlFilters.Length; i++)
                _dlFilters[i] = false;

            _filterStart = false;
            _filterEnd   = false;
            _filterEventCluster = false;
        }

        // ── Stili (lazy init — non disponibili fuori da OnGUI) ────────────────────

        private void EnsureStyles()
        {
            if (_numStyle != null) return;

            _numStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize  = 11,
            };
            _hdrStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize  = 10,
            };
            _posStyle = new GUIStyle(_numStyle)
            {
                normal = { textColor = new Color(0.30f, 0.80f, 0.35f) },
            };
            _negStyle = new GUIStyle(_numStyle)
            {
                normal = { textColor = new Color(0.90f, 0.35f, 0.30f) },
            };
        }

        // ── Statistiche ───────────────────────────────────────────────────────────

        private void EnsureStats()
        {
            if (_statsValid) return;
            RefreshRuntimeRefs();
            if (_controller == null || _controller.Tiles == null) return;
            ComputeStats();
        }

        private void ComputeStats()
        {
            _typeCount.Clear();
            for (int i = 0; i < 6; i++) _dlCount[i] = 0;

            _startCount  = 0;
            _endCount    = 0;
            _totalCount  = _controller.Tiles.Count;
            _totalMonete = 0;
            _totalCibo   = 0;
            _totalHp     = 0;

            _eventPlacementCounts.Clear();

            foreach (var kvp in _controller.Tiles)
            {
                var data = kvp.Value;

                // TileType
                if (!_typeCount.ContainsKey(data.Type))
                    _typeCount[data.Type] = 0;
                _typeCount[data.Type]++;

                // DifficultyLevel 0-6
                int dl = data.DifficultyLevel;
                if (dl >= 0 && dl <= 6)
                    _dlCount[dl]++;

                // Start
                if (data.Coord.Equals(_controller.PlayerCoord))
                    _startCount++;

                // End
                if (data.IsObjective)
                    _endCount++;

                // Totali risorse — somma su tutta la mappa
                _totalMonete += data.MoneteGained;
                _totalCibo   += data.FoodRestore;
                _totalHp     += data.HpRestore;

                // EventPlacementId: tally per id, condiviso da cluster multi-tile e singole
                // (vedi doc su _filterEventCluster) — chi e' un cluster vero si scopre solo
                // dopo aver contato tutte le tile.
                if (data.EventPlacementId >= 0)
                {
                    _eventPlacementCounts.TryGetValue(data.EventPlacementId, out int c);
                    _eventPlacementCounts[data.EventPlacementId] = c + 1;
                }
            }

            // Seconda passata: quali EventPlacementId sono cluster veri (>1 tile), e quante
            // tile in totale ci appartengono — serve per la colonna stat della riga filtro.
            _clusterPlacementIds.Clear();
            _clusterTileCount = 0;
            foreach (var pair in _eventPlacementCounts)
            {
                if (pair.Value <= 1) continue;
                _clusterPlacementIds.Add(pair.Key);
                _clusterTileCount += pair.Value;
            }

            _statsValid = true;
        }

        private string CountStr(int v) =>
            _statsValid ? v.ToString() : "–";

        private string PctStr(int v) =>
            (_statsValid && _totalCount > 0)
                ? (v * 100f / _totalCount).ToString("F1") + "%"
                : "–";

        private string SignedStr(int v, out GUIStyle style)
        {
            if (!_statsValid) { style = _numStyle; return "–"; }
            if (v > 0)        { style = _posStyle; return "+" + v; }
            if (v < 0)        { style = _negStyle; return v.ToString(); }
            style = _numStyle;
            return "0";
        }

        private int TypeCount(TileType t) => _typeCount.TryGetValue(t, out int c) ? c : 0;

        // ── OnGUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Disponibile solo in Play Mode — avvia il gioco per usare il filtro.",
                    MessageType.Info);
                return;
            }

            EnsureStats();

            bool changed = false;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── TileType ──────────────────────────────────────────────────────────
            _foldTileType = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTileType, "TileType");
            if (_foldTileType)
            {
                DrawColumnHeaders();
                EditorGUI.indentLevel++;
                foreach (var t in ActiveTileTypes)
                {
                    bool prev = _typeFilters[t];
                    bool next = StatRow(t.ToString(), prev, TypeCount(t));
                    if (next != prev) { _typeFilters[t] = next; changed = true; }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4);

            // ── DifficultyLevel ───────────────────────────────────────────────────
            _foldDifficulty = EditorGUILayout.BeginFoldoutHeaderGroup(
                _foldDifficulty, "DifficultyLevel");
            if (_foldDifficulty)
            {
                DrawColumnHeaders();
                EditorGUI.indentLevel++;
                for (int i = 0; i < _dlFilters.Length; i++)
                {
                    string label = i == 0 ? "DL 0 (Black)" : $"DL {i}";
                    bool prev = _dlFilters[i];
                    bool next = StatRow(label, prev, _dlCount[i]);
                    if (next != prev) { _dlFilters[i] = next; changed = true; }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4);

            // ── Posizione speciale ────────────────────────────────────────────────
            _foldSpecial = EditorGUILayout.BeginFoldoutHeaderGroup(
                _foldSpecial, "Posizione speciale");
            if (_foldSpecial)
            {
                DrawColumnHeaders();
                EditorGUI.indentLevel++;

                {
                    bool next = StatRow("Player (Start)", _filterStart, _startCount);
                    if (next != _filterStart) { _filterStart = next; changed = true; }
                }
                {
                    bool next = StatRow("End (Objective)", _filterEnd, _endCount);
                    if (next != _filterEnd) { _filterEnd = next; changed = true; }
                }
                {
                    bool next = StatRow("EventCluster (multi-tile)", _filterEventCluster, _clusterTileCount);
                    if (next != _filterEventCluster) { _filterEventCluster = next; changed = true; }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(12);

            // ── Pulsanti ──────────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset (tutti false)"))
            {
                InitFilters();
                changed = true;
            }

            if (GUILayout.Button("↺ Aggiorna stats", GUILayout.Width(120f)))
            {
                _statsValid = false;
                EnsureStats();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (_statsValid)
            {
                EditorGUILayout.LabelField(
                    $"Totale tessere: {_totalCount}",
                    EditorStyles.centeredGreyMiniLabel);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Totali risorse mappa", EditorStyles.boldLabel);

                string mStr = SignedStr(_totalMonete, out var mStyle);
                string cStr = SignedStr(_totalCibo,   out var cStyle);
                string hStr = SignedStr(_totalHp,     out var hStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Monete (M)", GUILayout.MinWidth(80f));
                GUILayout.Label(mStr, mStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cibo (C)", GUILayout.MinWidth(80f));
                GUILayout.Label(cStr, cStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("HP", GUILayout.MinWidth(80f));
                GUILayout.Label(hStr, hStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (changed)
                ApplyFilter();
        }

        // ── Helpers UI ────────────────────────────────────────────────────────────

        /// <summary>Intestazione colonne allineata a destra (# e %).</summary>
        private void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("#", _hdrStyle, GUILayout.Width(W_COUNT));
            GUILayout.Label("%", _hdrStyle, GUILayout.Width(W_PCT));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Riga con toggle + etichetta e due colonne stat allineate a destra.
        /// Restituisce il nuovo valore del toggle.
        /// </summary>
        private bool StatRow(string label, bool current, int count)
        {
            EditorGUILayout.BeginHorizontal();
            bool next = EditorGUILayout.ToggleLeft(label, current);
            GUILayout.Label(CountStr(count), _numStyle, GUILayout.Width(W_COUNT));
            GUILayout.Label(PctStr(count),   _numStyle, GUILayout.Width(W_PCT));
            EditorGUILayout.EndHorizontal();
            return next;
        }

        // ── Filtro runtime ────────────────────────────────────────────────────────

        private bool AnyFilterActive()
        {
            foreach (var t in ActiveTileTypes)
                if (_typeFilters.TryGetValue(t, out bool on) && on)
                    return true;

            foreach (var d in _dlFilters)
                if (d) return true;

            return _filterStart || _filterEnd || _filterEventCluster;
        }

        private void RefreshRuntimeRefs()
        {
            if (_spawner == null)
                _spawner = FindObjectOfType<HexGridViewSpawner>();
            if (_controller == null)
                _controller = FindObjectOfType<HexGridController>();
        }

        private void ApplyFilter()
        {
            RefreshRuntimeRefs();

            if (_spawner    == null || _spawner.Views    == null ||
                _controller == null || _controller.Tiles == null)
                return;

            bool anyActive = AnyFilterActive();

            foreach (var kvp in _spawner.Views)
            {
                var view = kvp.Value;
                if (view == null) continue;

                if (!anyActive)
                {
                    view.gameObject.SetActive(true);
                    continue;
                }

                if (!_controller.Tiles.TryGetValue(kvp.Key, out var data))
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                bool matches = false;

                if (_typeFilters.TryGetValue(data.Type, out bool typeOn) && typeOn)
                    matches = true;

                if (!matches)
                {
                    int dl = data.DifficultyLevel;
                    if (dl >= 0 && dl <= 6 && _dlFilters[dl])
                        matches = true;
                }

                if (!matches && _filterStart && data.Coord.Equals(_controller.PlayerCoord))
                    matches = true;

                if (!matches && _filterEnd && data.IsObjective)
                    matches = true;

                if (!matches && _filterEventCluster && _clusterPlacementIds.Contains(data.EventPlacementId))
                    matches = true;

                view.gameObject.SetActive(matches);
            }
        }
    }
}
