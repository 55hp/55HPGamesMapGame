using System;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    [Serializable]
    public struct DistanceWeight
    {
        public int Distance;
        public int Weight;
    }

    /// <summary>
    /// Vincoli struttura e pesi per la rete di Strada (PathCluster): budget di tile totali,
    /// numero e lunghezza dei rami, pesi di biforcazione e deviazione. Letti sia dal
    /// generatore live sia dal PathClusterBudgetAnalyzer del Hex Debug Framework.
    /// </summary>
    [Serializable]
    public struct StradaNetworkSettings
    {
        [Header("Vincoli struttura")]
        public int MinBranchLength;
        public int MaxBranchLength;
        public int MaxBranches;
        public int MaxTotalTiles;

        [Header("Pesi biforcazione a fine ramo (STOP / fork a 3 / fork a 5)")]
        public int StopWeight;
        public int Fork3Weight;
        public int Fork5Weight;

        [Header("Pesi deviazione durante la crescita (dritto / svolta)")]
        public int KeepHeadingWeight;
        public int TurnWeight;
    }

    /// <summary>
    /// Parametri per il piazzamento degli EventCluster (generati proceduralmente a runtime
    /// da LevelConfig.Entries, vedi AestheticClusterMapGenerator.TryBuildProceduralCluster)
    /// e delle tessere singole via rejection sampling.
    ///
    /// 2026-08-07: rimosso il campo Catalog (EventClusterCatalog/EventClusterShape) — le
    /// forme autorate a mano erano un fallback per quando la generazione procedurale non
    /// bastava, ma i cluster sono ora generati interamente a runtime, i tipi
    /// EventClusterCatalog/EventClusterShape restano nel progetto (non toccati, nessuna
    /// perdita di asset) ma non sono piu' referenziati dal generatore.
    /// </summary>
    [Serializable]
    public struct EventClusterPlacementSettings
    {
        [Tooltip("Tentativi consecutivi falliti prima di considerare la griglia piena e fermarsi.")]
        public int MaxConsecutiveFailures;

        [Header("Rapporto cluster : singola (es. 2-3 cluster per ogni singola)")]
        public int ClusterToSingleRatioMin;
        public int ClusterToSingleRatioMax;
    }

    /// <summary>
    /// 2026-08-07: rimosso PlaceholderBalanceSettings/PlaceholderBalance — bilanciamento
    /// placeholder (Goods FoodRestore min/max) mai piu' letto dal generatore dalla
    /// revisione 2026-08-05 (sostituito dalla risoluzione via ElementCatalog, vedi
    /// AestheticClusterMapGenerator.ApplyElementStats), restava solo per non rompere gli
    /// asset serializzati esistenti. Vedi anche EventClusterPlacementSettings per la
    /// rimozione di Catalog, stessa pulizia.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Map Generation Config", fileName = "MapGenerationConfig")]
    public sealed class MapGenerationConfig : ScriptableObject, IConfigAsset
    {
        [Min(1)] public int Width  = 10;
        [Min(1)] public int Height = 10;

        /// <summary>
        /// Seed di default per la generazione.
        /// A runtime viene sovrascritto da IGameContextService.CurrentRunSeed se != 0.
        /// </summary>
        public int Seed = 12345;

        [Header("Piazzamento Start/End")]
        public DistanceWeight[] EndDistanceWeights =
        {
            new DistanceWeight { Distance = 4, Weight = 1 },
            new DistanceWeight { Distance = 5, Weight = 2 },
            new DistanceWeight { Distance = 6, Weight = 3 },
            new DistanceWeight { Distance = 7, Weight = 2 },
            new DistanceWeight { Distance = 8, Weight = 1 },
        };

        [Min(0)] public int StartMinBorderDistance = 3;
        [Min(0)] public int ClusterMinDistanceFromStartEnd = 2;

        [Header("Piazzamento EventCluster e singole")]
        public EventClusterPlacementSettings EventClusters = new EventClusterPlacementSettings
        {
            MaxConsecutiveFailures = 200,
            ClusterToSingleRatioMin = 2,
            ClusterToSingleRatioMax = 3,
        };

        [Header("Rete Strada (PathCluster)")]
        public StradaNetworkSettings StradaNetwork = new StradaNetworkSettings
        {
            MinBranchLength = 3, MaxBranchLength = 7, MaxBranches = 5, MaxTotalTiles = 15,
            StopWeight = 60, Fork3Weight = 35, Fork5Weight = 5,
            KeepHeadingWeight = 70, TurnWeight = 30,
        };
    }
}
