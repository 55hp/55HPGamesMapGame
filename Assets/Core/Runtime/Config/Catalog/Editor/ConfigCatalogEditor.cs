using System.Collections.Generic;
using hp55games.Mobile.Core.Config;
using UnityEditor;
using UnityEngine;

namespace hp55games.Mobile.Core.Config.EditorTools
{
    /// <summary>
    /// Inspector di ConfigCatalog con lo scan automatico. Il tasto raccoglie ogni
    /// ScriptableObject che implementa IConfigAsset trovato dentro una cartella "Content"
    /// (a qualunque profondita': il filtro e' sul path che contiene "/Content/") e rimpiazza
    /// il contenuto del catalogo. Il ConfigCatalog stesso viene escluso.
    /// </summary>
    [CustomEditor(typeof(ConfigCatalog))]
    public sealed class ConfigCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Lo scan cerca tutti gli IConfigAsset dentro cartelle 'Content' e sottocartelle, " +
                "e rimpiazza la lista qui sopra. Rilancialo dopo aver aggiunto o spostato un config.",
                MessageType.Info);

            if (GUILayout.Button("Scansiona cartelle Content"))
                Scan((ConfigCatalog)target);
        }

        private static void Scan(ConfigCatalog catalog)
        {
            var found = new List<ScriptableObject>();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Content/")) continue;

                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;
                if (so is ConfigCatalog) continue;      // non aggregarsi da solo
                if (so is IConfigAsset)
                    found.Add(so);
            }

            found.Sort((a, b) => string.CompareOrdinal(a.GetType().Name, b.GetType().Name));

            Undo.RecordObject(catalog, "Scan Config Catalog");
            catalog.EditorSetConfigs(found);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ConfigCatalog] Scan completato: {found.Count} config trovati dentro cartelle Content.", catalog);
        }
    }
}
