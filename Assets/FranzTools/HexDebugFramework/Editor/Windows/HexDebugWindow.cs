using System.Linq;
using UnityEditor;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    [InitializeOnLoad]
    public class HexDebugWindow : EditorWindow
    {
        private int _maxRoadLength = 15;
        private Vector2 _scrollPos;

        // Registra la callback SceneView al caricamento dell'editor, senza aprire la finestra.
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
            // Stato topology
            string topologyLabel = HexDebugSession.Topology != null
                ? HexDebugSession.Topology.GetType().Name
                : "<nessuna topology registrata>";
            EditorGUILayout.LabelField("Topology", topologyLabel);

            EditorGUILayout.Space();

            _maxRoadLength = EditorGUILayout.IntField("Max Road Length", _maxRoadLength);

            GUI.enabled = HexDebugSession.Topology != null;
            if (GUILayout.Button("Populate & Analyze"))
                RunAnalysis();
            GUI.enabled = true;

            if (HexDebugSession.Clusters == null)
                return;

            EditorGUILayout.Space();

            // Statistiche
            int totalCells  = HexDebugSession.Clusters.Sum(c => c.Cells.Count);
            int errorCount  = HexDebugSession.Clusters.Count(c => c.Severity == DebugSeverity.Error);
            EditorGUILayout.LabelField(
                $"Clusters: {HexDebugSession.Clusters.Length}   Cells: {totalCells}   Errors: {errorCount}",
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
            var analyzer = new RoadAnalyzer(_maxRoadLength);
            HexDebugSession.Registry.PopulateFromScene();
            HexDebugSession.Clusters = analyzer.Analyze(HexDebugSession.Registry, HexDebugSession.Topology);
            HexDebugSession.SelectedClusterIndex = -1;
            SceneView.RepaintAll();
            Repaint();
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
