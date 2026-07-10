using hp55games.Tools.HexDebugFramework.Editor;
using hp55games.MapGame.Features.Configs;
using UnityEditor;
using UnityEngine;

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

            var config = LoadMapGenerationConfig();
            var settings = config != null ? config.StradaNetwork : DefaultSettings();

            HexDebugSession.RegisterAnalyzer(new PathClusterBudgetAnalyzer(settings));
            HexDebugSession.RegisterAnalyzer(new NeutraSeparationAnalyzer());
            HexDebugSession.RegisterAnalyzer(new EventClusterSpacingAnalyzer());
            HexDebugSession.RegisterAnalyzer(new ReachabilityAnalyzer());
            HexDebugSession.RegisterAnalyzer(new ClusterBudgetHeatmap(settings.MaxTotalTiles));

            // BiomaConsistencyAnalyzer: non implementato.
            // HexTileData non ha proprietà Bioma: il Three-Axis Visual Model
            // (Bioma/Ambiente/Variante) è design-only in questa versione del progetto.
        }

        private static MapGenerationConfig LoadMapGenerationConfig()
        {
            var guids = AssetDatabase.FindAssets("t:MapGenerationConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(path);
            }

            Debug.LogWarning("[HexDebug] MapGenerationConfig non trovata. Usando valori di default per i budget.");
            return null;
        }

        private static StradaNetworkSettings DefaultSettings() => new StradaNetworkSettings
        {
            MinBranchLength = 3,
            MaxBranchLength = 7,
            MaxBranches     = 5,
            MaxTotalTiles   = 15,
        };
    }
}
