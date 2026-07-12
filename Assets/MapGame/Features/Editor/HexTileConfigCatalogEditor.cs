using System.Linq;
using UnityEditor;
using UnityEngine;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.Editor
{
    /// <summary>
    /// Aggiunge un bottone "Carica tutte le config dalla cartella" all'Inspector di
    /// HexTileConfigCatalog, che scansiona Assets/MapGame/Content/TileConfigs/ e
    /// ripopola l'array TileConfigs, alternativa al trascinamento manuale. Stesso
    /// pattern di EventClusterCatalogEditor.
    /// </summary>
    [CustomEditor(typeof(HexTileConfigCatalog))]
    public sealed class HexTileConfigCatalogEditor : UnityEditor.Editor
    {
        private const string ConfigsFolder = "Assets/MapGame/Content/Configs/TileConfigs";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Carica tutte le config dalla cartella"))
            {
                LoadAllConfigs();
            }
        }

        private void LoadAllConfigs()
        {
            var catalog = (HexTileConfigCatalog)target;

            if (!AssetDatabase.IsValidFolder(ConfigsFolder))
            {
                EditorUtility.DisplayDialog("Hex Tile Config Catalog", $"Cartella non trovata: {ConfigsFolder}", "Ok");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:HexTileConfig", new[] { ConfigsFolder });
            var configs = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<HexTileConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(cfg => cfg != null)
                .OrderBy(cfg => cfg.Type.ToString())
                .ToArray();

            catalog.TileConfigs = configs;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Hex Tile Config Catalog", $"Caricate {configs.Length} config da {ConfigsFolder}.", "Ok");
        }
    }
}
