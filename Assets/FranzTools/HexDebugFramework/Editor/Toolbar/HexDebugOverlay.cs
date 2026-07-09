using UnityEditor;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    public static class HexDebugOverlay
    {
        private static readonly Rect OverlayRect = new Rect(10, 30, 195, 118);

        public static void DrawSceneGUI()
        {
            var settings = HexDebugSession.DrawSettings;

            Handles.BeginGUI();
            GUILayout.BeginArea(OverlayRect, GUI.skin.box);

            GUILayout.Label("Hex Debug Framework", EditorStyles.boldLabel);
            settings.Enabled     = GUILayout.Toggle(settings.Enabled,     "Enable");
            settings.ShowLabels  = GUILayout.Toggle(settings.ShowLabels,  "Labels");
            settings.ShowConnections = GUILayout.Toggle(settings.ShowConnections, "Connections");

            if (GUILayout.Button("Open Analyzer", GUILayout.Height(22)))
                HexDebugWindow.Open();

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
