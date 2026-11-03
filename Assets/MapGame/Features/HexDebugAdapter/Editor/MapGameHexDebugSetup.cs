using hp55games.Tools.HexDebugFramework.Editor;
using UnityEditor;

namespace hp55games.MapGame.Features.HexDebugAdapter.Editor
{
    /// <summary>
    /// Registra nel framework al caricamento dell'editor:
    /// - MapGameHexTopology come IHexTopology
    /// - MapGameRegistryPopulator come strategia di populate
    /// - Tutti gli analyzer specifici di MapGame
    ///
    /// Questo è il punto di integrazione tra il framework generico e MapGame.
    /// Il framework non conosce nessuno di questi tipi: il progetto li inietta qui.
    /// </summary>
    [InitializeOnLoad]
    public static class MapGameHexDebugSetup
    {
        static MapGameHexDebugSetup()
        {
            HexDebugSession.RegisterTopology(new MapGameHexTopology());
            HexDebugSession.PopulateStrategy = MapGameRegistryPopulator.Populate;

            // v2 (spec 0012): gli analyzer basati su cluster/strade/reachability sono stati rimossi
            // insieme a MapClusterGenerator: con il generatore a censimento e il click libero non hanno più senso.

            // BiomaConsistencyAnalyzer: non implementato.
            // HexTileData non ha proprietà Bioma: il Three-Axis Visual Model
            // (Bioma/Ambiente/Variante) è design-only in questa versione del progetto.
        }
    }
}
