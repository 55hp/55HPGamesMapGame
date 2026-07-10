using UnityEditor;
using UnityEngine;

namespace hp55games.Tools.HexDebugFramework.Editor
{
    public static class HexDebugOverlay
    {
        private const float OverlayWidth  = 210f;
        private const float OverlayHeight = 162f;
        private const float OverlayMargin = 10f;
        private const float TopOffset     = 30f;

        public static void DrawSceneGUI(SceneView sceneView)
        {
            var settings = HexDebugSession.DrawSettings;

            float x = sceneView.position.width - OverlayWidth - OverlayMargin;
            var rect = new Rect(x, TopOffset, OverlayWidth, OverlayHeight);

            Handles.BeginGUI();
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("Hex Debug Framework", EditorStyles.boldLabel);
            settings.Enabled          = GUILayout.Toggle(settings.Enabled,          "Enable");
            settings.ShowLabels       = GUILayout.Toggle(settings.ShowLabels,        "Labels");
            settings.ShowConnections  = GUILayout.Toggle(settings.ShowConnections,   "Connections");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Label Color", GUILayout.Width(80));
            settings.LabelColor = EditorGUILayout.ColorField(settings.LabelColor);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Text Size", GUILayout.Width(80));
            settings.LabelFontScale = GUILayout.HorizontalSlider(settings.LabelFontScale, 1f, 6f);
            GUILayout.Label(settings.LabelFontScale.ToString("F1"), GUILayout.Width(28));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Open Analyzer", GUILayout.Height(22)))
                HexDebugWindow.Open();

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
