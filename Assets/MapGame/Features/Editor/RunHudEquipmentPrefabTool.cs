using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.Editor
{
    /// <summary>
    /// D-050: adatta la barra dell'equipaggiamento di UIRunHUD a due mani identiche + Trinket cumulabili.
    /// Modifica il prefab da codice (ripetibile, idempotente): Mano 1 e Mano 2 uguali, Trinket più largo
    /// con testo su più righe. I riferimenti di RunHudView (_weapon, _ring, _trinket) restano quelli.
    /// Menu: MapGame Tools › HUD › Aggiorna equipaggiamento (D-050).
    /// </summary>
    public static class RunHudEquipmentPrefabTool
    {
        private const string PrefabPath = "Assets/MapGame/Content/UI/Run/UIRunHUD.prefab";

        // Valori di layout (unica fonte: questo strumento li scrive nel prefab).
        private const float HandFlexibleWidth = 1f;
        private const float TrinketFlexibleWidth = 1.6f;
        private const float HandValueMinSize = 18f;
        private const float TrinketValueMinSize = 16f;

        [MenuItem("MapGame Tools/HUD/Aggiorna equipaggiamento (D-050)")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var bar = root.transform.Find("BottomBar");
                if (bar == null) { Debug.LogError("[RunHudEquipmentPrefabTool] BottomBar non trovato in " + PrefabPath); return; }

                var hand1 = Slot(bar, "Hand1Slot", "WeaponSlot");
                var hand2 = Slot(bar, "Hand2Slot", "RingSlot");
                var trinkets = Slot(bar, "TrinketSlot", "TrinketSlot");
                if (hand1 == null || hand2 == null || trinkets == null) return;

                SetupSlot(hand1, HandFlexibleWidth, HandValueMinSize, wrap: true);
                SetupSlot(hand2, HandFlexibleWidth, HandValueMinSize, wrap: true);
                SetupSlot(trinkets, TrinketFlexibleWidth, TrinketValueMinSize, wrap: true);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[RunHudEquipmentPrefabTool] UIRunHUD aggiornato: Mano 1, Mano 2, Trinket.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform Slot(Transform bar, string name, string legacyName)
        {
            var t = bar.Find(name) ?? bar.Find(legacyName);
            if (t == null) { Debug.LogError($"[RunHudEquipmentPrefabTool] Slot {name} ({legacyName}) non trovato sotto BottomBar."); return null; }
            t.name = name;
            return t;
        }

        private static void SetupSlot(Transform slot, float flexibleWidth, float minFont, bool wrap)
        {
            var le = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth;
            le.minWidth = 0;

            var value = slot.Find("Value");
            if (value == null) { Debug.LogError($"[RunHudEquipmentPrefabTool] Value non trovato sotto {slot.name}."); return; }
            var text = value.GetComponent<TextMeshProUGUI>();
            if (text == null) { Debug.LogError($"[RunHudEquipmentPrefabTool] TextMeshProUGUI mancante su {slot.name}/Value."); return; }

            float max = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMax = Mathf.Max(max, minFont);
            text.fontSizeMin = minFont;
            text.enableWordWrapping = wrap;
            text.overflowMode = TextOverflowModes.Ellipsis;

            // Il valore occupa tutto lo spazio verticale rimasto nello slot.
            var vle = value.GetComponent<LayoutElement>() ?? value.gameObject.AddComponent<LayoutElement>();
            vle.flexibleHeight = 1;
        }
    }
}
