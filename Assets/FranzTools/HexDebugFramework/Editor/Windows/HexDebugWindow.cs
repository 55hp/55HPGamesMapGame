using System.Linq;
using UnityEditor;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    [InitializeOnLoad]
    public class HexDebugWindow : EditorWindow
    {
        // Opzione 0 = RoadAnalyzer built-in; opzioni 1+ = analyzer registrati da MapGame.
        private int _maxRoadLength = 15;
        private int _analyzerSelectionIndex = 0;
        private Vector2 _scrollPos;

        static HexDebugWindow()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem("Tools/Hex Debug Framework/Open")]
        public static void Open()
        {
            GetWindow<HexDebugWindow>("Hex Debug Framework");
        }

        private void OnGUI()
        {
            // Topology status
            string topologyLabel = HexDebugSession.Topology != null
                ? HexDebugSession.Topology.GetType().Name
                : "<nessuna topology registrata>";
            EditorGUILayout.LabelField("Topology", topologyLabel);

            EditorGUILayout.Space();

            // Selezione analyzer
            var analyzerNames = BuildAnalyzerNames();
            _analyzerSelectionIndex = Mathf.Clamp(_analyzerSelectionIndex, 0, analyzerNames.Length - 1);
            _analyzerSelectionIndex = EditorGUILayout.Popup("Analyzer", _analyzerSelectionIndex, analyzerNames);

            // MaxRoadLength solo quando è selezionato il built-in RoadAnalyzer
            if (_analyzerSelectionIndex == 0)
                _maxRoadLength = EditorGUILayout.IntField("Max Road Length", _maxRoadLength);

            EditorGUILayout.Space();

            GUI.enabled = HexDebugSession.Topology != null;
            if (GUILayout.Button("Populate & Analyze"))
                RunAnalysis();
            GUI.enabled = true;

            if (HexDebugSession.Clusters == null)
                return;

            EditorGUILayout.Space();

            // Statistiche
            int totalCells = HexDebugSession.Clusters.Sum(c => c.Cells.Count);
            int errorCount = HexDebugSession.Clusters.Count(c => c.Severity == DebugSeverity.Error);
            EditorGUILayout.LabelField(
                $"Clusters: {HexDebugSession.Clusters.Length}   Celle: {totalCells}   Errori: {errorCount}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space();

            // Lista cluster
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < HexDebugSession.Clusters.Length; i++)
            {
                var cluster = HexDebugSession.Clusters[i];
                bool isSelected = i == HexDebugSession.SelectedClusterIndex;

                string label = $"{cluster.Name}   ({cluster.Cells.Count} celle)";
                if (cluster.Severity != DebugSeverity.Info)
                    label += $"   [{cluster.Severity}]";

                bool clicked = GUILayout.Toggle(isSelected, label, "Button");
                if (clicked != isSelected)
                {
                    HexDebugSession.SelectedClusterIndex = clicked ? i : -1;
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunAnalysis()
        {
            // Populate
            if (HexDebugSession.PopulateStrategy != null)
                HexDebugSession.PopulateStrategy(HexDebugSession.Registry);
            else
                HexDebugSession.Registry.PopulateFromScene();

            // Scegli analyzer
            IHexAnalyzer analyzer;
            if (_analyzerSelectionIndex == 0)
                analyzer = new RoadAnalyzer(_maxRoadLength);
            else
                analyzer = HexDebugSession.RegisteredAnalyzers[_analyzerSelectionIndex - 1];

            HexDebugSession.Clusters = analyzer.Analyze(HexDebugSession.Registry, HexDebugSession.Topology);
            HexDebugSession.SelectedClusterIndex = -1;
            SceneView.RepaintAll();
            Repaint();
        }

        private static string[] BuildAnalyzerNames()
        {
            var registered = HexDebugSession.RegisteredAnalyzers;
            var names = new string[1 + registered.Count];
            names[0] = "Road Analyzer (built-in)";
            for (int i = 0; i < registered.Count; i++)
                names[i + 1] = registered[i].Name;
            return names;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            HexDebugOverlay.DrawSceneGUI();

            if (HexDebugSession.DrawSettings.Enabled && HexDebugSession.Clusters != null)
            {
                HexSceneDrawer.Draw(
                    HexDebugSession.Clusters,
                    HexDebugSession.DrawSettings,
                    HexDebugSession.SelectedClusterIndex);
            }
        }
    }
}
