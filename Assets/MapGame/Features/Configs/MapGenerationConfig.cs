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
    /// da LevelConfig.ClusterTileEntries, vedi AestheticClusterMapGenerator.TryBuildProceduralCluster)
    /// e delle tessere singole via rejection sampling. I tipi EventClusterCatalog/
    /// EventClusterShape restano nel progetto per lo Shape Editor ma non sono referenziati
    /// dal generatore, che costruisce i cluster interamente a runtime. Nessun rapporto
    /// cluster:singola qui: ogni tipo di cluster (Type+DifficultyLevel del centro) compare
    /// al massimo una volta per mappa, e ogni tessera singola ha gia' il proprio Amount
    /// esplicito su LevelConfig.SingleTileEntries — PlaceEventClusters piazza tutti i
    /// cluster disponibili, poi tutte le singole del manifest, in due fasi separate
    /// anziche' alternate.
    /// </summary>
    [Serializable]
    public struct EventClusterPlacementSettings
    {
        [Tooltip("Tentativi consecutivi falliti prima di considerare la griglia piena e fermarsi.")]
        public int MaxConsecutiveFailures;
    }

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
