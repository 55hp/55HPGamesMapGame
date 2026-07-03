using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEditor;
using UnityEngine;

namespace hp55games.MapGame.Editor
{
    [CustomEditor(typeof(HexGridController))]
    public sealed class HexGridControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Rigenera griglia"))
                    ((HexGridController)target).BuildGrid();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox(
                    "\"Rigenera griglia\" è disponibile solo in Play Mode.",
                    MessageType.Info);
        }
    }
}
