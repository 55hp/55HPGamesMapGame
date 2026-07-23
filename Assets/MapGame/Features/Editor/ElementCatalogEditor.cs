using System.Linq;
using UnityEditor;
using UnityEngine;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Editor
{
    /// <summary>
    /// Aggiunge un bottone "Carica tutte le specie dalla cartella" all'Inspector di
    /// ElementCatalog, che scansiona Assets/MapGame/Content/Elements/ (sottocartelle
    /// incluse) e ripopola l'array Elements, alternativa al trascinamento manuale.
    /// Stesso pattern di HexTileConfigCatalogEditor / EventClusterCatalogEditor.
    /// </summary>
    [CustomEditor(typeof(ElementCatalog))]
    public sealed class ElementCatalogEditor : UnityEditor.Editor
    {
        private const string ElementsFolder = "Assets/MapGame/Content/Elements";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Carica tutte le specie dalla cartella"))
            {
                LoadAllElements();
            }
        }

        private void LoadAllElements()
        {
            var catalog = (ElementCatalog)target;

            if (!AssetDatabase.IsValidFolder(ElementsFolder))
            {
                EditorUtility.DisplayDialog("Element Catalog", $"Cartella non trovata: {ElementsFolder}", "Ok");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:ElementConfig", new[] { ElementsFolder });
            var elements = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<ElementConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(cfg => cfg != null)
                .OrderBy(cfg => cfg.Type.ToString())
                .ThenBy(cfg => cfg.DifficultyLevel)
                .ThenBy(cfg => cfg.SpeciesId)
                .ToArray();

            catalog.Elements = elements;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Element Catalog", $"Caricate {elements.Length} specie da {ElementsFolder}.", "Ok");
        }
    }
}
