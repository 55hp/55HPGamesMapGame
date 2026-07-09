using hp55games.FranzTools.HexDebugFramework.Editor;
using UnityEditor;

namespace hp55games.MapGame.Features.Debug.Editor
{
    /// <summary>
    /// Registra MapGameHexTopology nel framework al caricamento dell'editor.
    /// Questo è il punto di integrazione tra il framework generico e MapGame:
    /// il framework non conosce MapGameHexTopology, ma il progetto la inietta qui.
    /// </summary>
    [InitializeOnLoad]
    public static class MapGameHexDebugSetup
    {
        static MapGameHexDebugSetup()
        {
            HexDebugSession.RegisterTopology(new MapGameHexTopology());
        }
    }
}
