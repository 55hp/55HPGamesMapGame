using System.Linq;
using UnityEditor;
using UnityEngine;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Editor
{
    /// <summary>
    /// Aggiunge un bottone "Carica tutte le forme dalla cartella" all'Inspector di
    /// EventClusterCatalog, che scansiona Assets/MapGame/Content/EventClusters/ e
    /// ripopola l'array Shapes, alternativa al trascinamento manuale.
    /// </summary>
    [CustomEditor(typeof(EventClusterCatalog))]
    public class EventClusterCatalogEditor : UnityEditor.Editor
    {
        private const string ShapesFolder = "Assets/MapGame/Content/EventClusters";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Carica tutte le forme dalla cartella"))
            {
                LoadAllShapes();
            }
        }

        private void LoadAllShapes()
        {
            var catalog = (EventClusterCatalog)target;

            if (!AssetDatabase.IsValidFolder(ShapesFolder))
            {
                EditorUtility.DisplayDialog("Event Cluster Catalog", $"Cartella non trovata: {ShapesFolder}", "Ok");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:EventClusterShape", new[] { ShapesFolder });
            var shapes = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<EventClusterShape>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(shape => shape != null)
                .OrderBy(shape => shape.ShapeName)
                .ToArray();

            catalog.Shapes = shapes;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Event Cluster Catalog", $"Caricate {shapes.Length} forme da {ShapesFolder}.", "Ok");
        }
    }
}
