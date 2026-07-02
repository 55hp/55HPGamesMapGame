using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace hp55games.MapGame.Editor
{
    /// <summary>
    /// Forza lo zoom della Game view a 1x ogni volta che si entra in Play.
    /// Usa reflection su un campo privato interno di UnityEditor (non API pubblica
    /// stabile), quindi è avvolto in try/catch: se una futura versione di Unity
    /// rinomina questi membri, lo script smette silenziosamente di agire invece
    /// di rompere l'Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewZoomLock
    {
        private const float LockedZoom = 1f;

        static GameViewZoomLock()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            try
            {
                var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);

                var zoomAreaField = gameViewType.GetField("m_ZoomArea", BindingFlags.NonPublic | BindingFlags.Instance);
                object zoomArea = zoomAreaField?.GetValue(gameView);
                if (zoomArea == null) return;

                var scaleField = zoomArea.GetType().GetField("m_Scale", BindingFlags.NonPublic | BindingFlags.Instance);
                scaleField?.SetValue(zoomArea, new Vector2(LockedZoom, LockedZoom));

                gameView.Repaint();
            }
            catch
            {
                // API interna non disponibile in questa versione di Unity: nessun crash, nessun blocco applicato.
            }
        }
    }
}