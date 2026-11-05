using System.Reflection;
using hp55games.MapGame.Features.UI;
using hp55games.MapGame.Features.UI.Run;
using UnityEditor;
using UnityEngine;

namespace hp55games.MapGame.Features.Editor
{
    /// <summary>
    /// Solo per controlli visivi in Play mode: dà al giocatore oggetti dai nomi lunghi e aggiorna la UI,
    /// per verificare che i riquadri della HUD li contengano. Non è gameplay: modifica lo stato via reflection.
    /// Menu: MapGame Tools › Debug › Oggetti di prova nella HUD.
    /// </summary>
    public static class RunDebugItemsTool
    {
        private static readonly string[] TestItems =
        {
            "hunter_bow", "hunter_tools", "lantern", "purse", "smith_whetstone", "map_gold", "map_silver", "herbarium",
        };

        [MenuItem("MapGame Tools/Debug/Oggetti di prova nella HUD")]
        public static void GiveTestItems()
        {
            if (!Application.isPlaying) { Debug.LogError("[RunDebugItemsTool] Serve la Play mode con una partita aperta."); return; }
            var session = Object.FindObjectOfType<RunSession>();
            if (session == null || session.Engine == null) { Debug.LogError("[RunDebugItemsTool] Nessuna partita in corso."); return; }

            var engine = session.Engine;
            var give = engine.State.GetType().GetMethod("Give", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var id in TestItems)
            {
                var item = engine.Config.Item(id);
                if (item == null) { Debug.LogError($"[RunDebugItemsTool] Oggetto {id} non trovato in RulesConfig."); continue; }
                give.Invoke(engine.State, new object[] { item });
            }

            RefreshUi();
            Debug.Log("[RunDebugItemsTool] Oggetti di prova dati: arma a due mani e 7 Trinket.");
        }

        /// <summary>Apre il pannello del primo Mercante coperto con 60 monete: serve a controllare il pannello con molte offerte.</summary>
        [MenuItem("MapGame Tools/Debug/Apri un Mercante con 60 monete")]
        public static void OpenTrader()
        {
            if (!Application.isPlaying) { Debug.LogError("[RunDebugItemsTool] Serve la Play mode con una partita aperta."); return; }
            var session = Object.FindObjectOfType<RunSession>();
            if (session == null || session.Engine == null) { Debug.LogError("[RunDebugItemsTool] Nessuna partita in corso."); return; }
            var engine = session.Engine;
            if (engine.CurrentDecision != null) { Debug.LogError("[RunDebugItemsTool] C'è già una decisione aperta: chiudila prima."); return; }
            engine.State.GetType().GetProperty("Coins").SetValue(engine.State, 60);
            int trader = -1;
            for (int i = 0; i < engine.Layout.Tiles.Length; i++)
                if (engine.Layout.Tiles[i] == hp55games.MapGame.Rules.ContentKind.Trader && !engine.IsRevealed(i)) { trader = i; break; }
            if (trader < 0) { Debug.LogError("[RunDebugItemsTool] Nessun Mercante coperto."); return; }
            engine.Reveal(trader);
            RefreshUi();
            Debug.Log($"[RunDebugItemsTool] Mercante aperto (tile {trader}).");
        }

        private static void RefreshUi()
        {
            var root = Object.FindObjectOfType<RunUiRoot>();
            root?.GetType().GetMethod("RefreshAll", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(root, null);
        }
    }
}
