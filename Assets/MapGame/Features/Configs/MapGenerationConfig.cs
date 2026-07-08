using System;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    [Serializable]
    public struct DistanceWeight
    {
        public int Distance;
        public int Weight;
    }

    [Serializable]
    public struct PlaceholderBalanceSettings
    {
        [Header("Risorsa — ripristino HP")]
        public int RisorsaHpRestoreMin;
        public int RisorsaHpRestoreMax;

        [Header("Battaglia — perdita HP / Monete guadagnate")]
        public int BattagliaHpLossMin;
        public int BattagliaHpLossMax;
        public int BattagliaMoneteMin;
        public int BattagliaMoneteMax;

        [Header("Trappola — perdita HP")]
        public int TrappolaHpLossMin;
        public int TrappolaHpLossMax;
    }

    /// <summary>
    /// Vincoli struttura e pesi per la mesh di Strada (fase 5, non ancora ricablata sul
    /// nuovo sistema a EventCluster multipli — campo tenuto per non perdere i valori).
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
    /// Parametri per il piazzamento degli EventCluster (forme dal catalogo) e delle
    /// tessere singole via rejection sampling.
    /// </summary>
    [Serializable]
    public struct EventClusterPlacementSettings
    {
        [Tooltip("Trascina qui l'asset EventClusterCatalog con le forme disponibili.")]
        public hp55games.MapGame.Features.Gameplay.HexGrid.EventClusterCatalog Catalog;

        [Tooltip("Tentativi consecutivi falliti prima di considerare la griglia piena e fermarsi.")]
        public int MaxConsecutiveFailures;

        [Header("Rapporto cluster : singola (es. 2-3 cluster per ogni singola)")]
        public int ClusterToSingleRatioMin;
        public int ClusterToSingleRatioMax;
    }

    [CreateAssetMenu(menuName = "MapGame/Map Generation Config", fileName = "MapGenerationConfig")]
    public sealed class MapGenerationConfig : ScriptableObject
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

        [Header("Bilanciamento placeholder (NON valori finali)")]
        public PlaceholderBalanceSettings PlaceholderBalance = new PlaceholderBalanceSettings
        {
            RisorsaHpRestoreMin  = 1, RisorsaHpRestoreMax  = 6,
            BattagliaHpLossMin   = 1, BattagliaHpLossMax   = 4,
            BattagliaMoneteMin   = 1, BattagliaMoneteMax   = 5,
            TrappolaHpLossMin    = 2, TrappolaHpLossMax    = 6,
        };

        [Header("Piazzamento EventCluster e singole")]
        public EventClusterPlacementSettings EventClusters = new EventClusterPlacementSettings
        {
            Catalog = null,
            MaxConsecutiveFailures = 200,
            ClusterToSingleRatioMin = 2,
            ClusterToSingleRatioMax = 3,
        };

        [Header("Mesh Strada (fase 5, non ancora ricablata)")]
        public StradaNetworkSettings StradaNetwork = new StradaNetworkSettings
        {
            MinBranchLength = 3, MaxBranchLength = 7, MaxBranches = 5, MaxTotalTiles = 15,
            StopWeight = 60, Fork3Weight = 35, Fork5Weight = 5,
            KeepHeadingWeight = 70, TurnWeight = 30,
        };
    }
}
